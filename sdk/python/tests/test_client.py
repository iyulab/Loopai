"""Unit tests for LoopaiClient."""

import pytest
from datetime import datetime
from uuid import UUID, uuid4

from httpx_mock import HTTPXMock

from loopai import LoopaiClient
from loopai.exceptions import (
    LoopaiException,
    ValidationException,
    ExecutionException,
    ConnectionException,
)


class TestLoopaiClient:
    """Test suite for LoopaiClient."""

    @pytest.mark.asyncio
    async def test_client_initialization(self, mock_base_url: str) -> None:
        """Test client initialization with default and custom options."""
        # Default options
        client = LoopaiClient()
        assert client.base_url == "http://localhost:8080"
        assert client.timeout == 30.0
        assert client.max_retries == 3
        await client.close()

        # Custom options
        client = LoopaiClient(
            base_url=mock_base_url,
            api_key="test-key",
            timeout=60.0,
            max_retries=5,
        )
        assert client.base_url == mock_base_url
        assert client.api_key == "test-key"
        assert client.timeout == 60.0
        assert client.max_retries == 5
        await client.close()

    @pytest.mark.asyncio
    async def test_context_manager(self, mock_base_url: str) -> None:
        """Test async context manager."""
        async with LoopaiClient(base_url=mock_base_url) as client:
            assert client._client is not None

        # Client should be closed after context exit
        with pytest.raises(RuntimeError):
            await client._client.get("/test")

    @pytest.mark.asyncio
    async def test_get_health_success(
        self, mock_base_url: str, httpx_mock: HTTPXMock
    ) -> None:
        """Test successful health check."""
        # Mock response
        httpx_mock.add_response(
            url=f"{mock_base_url}/health",
            method="GET",
            json={
                "status": "healthy",
                "version": "1.0.0",
                "timestamp": datetime.utcnow().isoformat(),
            },
        )

        async with LoopaiClient(base_url=mock_base_url, max_retries=0) as client:
            health = await client.get_health()
            assert health.status == "healthy"
            assert health.version == "1.0.0"
            assert isinstance(health.timestamp, datetime)

    @pytest.mark.asyncio
    async def test_create_task_success(
        self, mock_base_url: str, httpx_mock: HTTPXMock
    ) -> None:
        """Test successful task creation."""
        task_id = uuid4()
        httpx_mock.add_response(
            url=f"{mock_base_url}/api/v1/tasks",
            method="POST",
            json={
                "id": str(task_id),
                "name": "test-task",
                "description": "Test task",
                "inputSchema": {"type": "object"},
                "outputSchema": {"type": "string"},
                "accuracyTarget": 0.9,
                "latencyTargetMs": 100,
                "samplingRate": 0.1,
                "createdAt": datetime.utcnow().isoformat(),
            },
        )

        async with LoopaiClient(base_url=mock_base_url, max_retries=0) as client:
            task = await client.create_task(
                name="test-task",
                description="Test task",
                input_schema={"type": "object"},
                output_schema={"type": "string"},
            )
            assert task.id == task_id
            assert task.name == "test-task"
            assert task.accuracy_target == 0.9

    @pytest.mark.asyncio
    async def test_get_task_success(
        self, mock_base_url: str, httpx_mock: HTTPXMock
    ) -> None:
        """Test successful task retrieval."""
        task_id = uuid4()
        httpx_mock.add_response(
            url=f"{mock_base_url}/api/v1/tasks/{task_id}",
            method="GET",
            json={
                "id": str(task_id),
                "name": "test-task",
                "description": "Test task",
                "inputSchema": {"type": "object"},
                "outputSchema": {"type": "string"},
                "accuracyTarget": 0.9,
                "latencyTargetMs": 100,
                "samplingRate": 0.1,
                "createdAt": datetime.utcnow().isoformat(),
            },
        )

        async with LoopaiClient(base_url=mock_base_url, max_retries=0) as client:
            task = await client.get_task(task_id)
            assert task.id == task_id
            assert task.name == "test-task"

    @pytest.mark.asyncio
    async def test_get_task_not_found(
        self, mock_base_url: str, httpx_mock: HTTPXMock
    ) -> None:
        """Test task not found error."""
        task_id = uuid4()
        httpx_mock.add_response(
            url=f"{mock_base_url}/api/v1/tasks/{task_id}",
            method="GET",
            status_code=404,
            json={"message": "Task not found"},
        )

        async with LoopaiClient(base_url=mock_base_url, max_retries=0) as client:
            with pytest.raises(LoopaiException) as exc_info:
                await client.get_task(task_id)

            assert exc_info.value.status_code == 404
            assert "not found" in str(exc_info.value).lower()

    @pytest.mark.asyncio
    async def test_execute_success(
        self, mock_base_url: str, httpx_mock: HTTPXMock
    ) -> None:
        """Test successful task execution."""
        task_id = uuid4()
        execution_id = uuid4()

        httpx_mock.add_response(
            url=f"{mock_base_url}/api/v1/tasks/execute",
            method="POST",
            json={
                "id": str(execution_id),
                "taskId": str(task_id),
                "version": 1,
                "output": "spam",
                "latencyMs": 42.5,
                "sampledForValidation": False,
                "executedAt": datetime.utcnow().isoformat(),
            },
        )

        async with LoopaiClient(base_url=mock_base_url, max_retries=0) as client:
            result = await client.execute(task_id, {"text": "Buy now!"})
            assert result.id == execution_id
            assert result.task_id == task_id
            assert result.output == "spam"
            assert result.latency_ms == 42.5

    @pytest.mark.asyncio
    async def test_execute_validation_error(
        self, mock_base_url: str, httpx_mock: HTTPXMock
    ) -> None:
        """Test execution with validation error."""
        task_id = uuid4()

        httpx_mock.add_response(
            url=f"{mock_base_url}/api/v1/tasks/execute",
            method="POST",
            status_code=400,
            json={
                "message": "Validation failed",
                "errors": {"text": ["Field required"]},
            },
        )

        async with LoopaiClient(base_url=mock_base_url, max_retries=0) as client:
            with pytest.raises(ValidationException) as exc_info:
                await client.execute(task_id, {})

            assert exc_info.value.status_code == 400
            assert "text" in exc_info.value.errors

    @pytest.mark.asyncio
    async def test_execute_execution_error(
        self, mock_base_url: str, httpx_mock: HTTPXMock
    ) -> None:
        """Test execution failure."""
        task_id = uuid4()
        execution_id = uuid4()

        httpx_mock.add_response(
            url=f"{mock_base_url}/api/v1/tasks/execute",
            method="POST",
            status_code=500,
            json={
                "message": "Execution failed",
                "executionId": str(execution_id),
            },
        )

        async with LoopaiClient(base_url=mock_base_url, max_retries=0) as client:
            with pytest.raises(ExecutionException) as exc_info:
                await client.execute(task_id, {"text": "Test"})

            assert exc_info.value.status_code == 500
            assert exc_info.value.execution_id == str(execution_id)

    @pytest.mark.asyncio
    async def test_batch_execute_success(
        self, mock_base_url: str, httpx_mock: HTTPXMock
    ) -> None:
        """Test successful batch execution."""
        task_id = uuid4()
        batch_id = uuid4()

        httpx_mock.add_response(
            url=f"{mock_base_url}/api/v1/batch/execute",
            method="POST",
            json={
                "batchId": str(batch_id),
                "taskId": str(task_id),
                "version": 1,
                "totalItems": 3,
                "successCount": 3,
                "failureCount": 0,
                "totalDurationMs": 150.0,
                "avgLatencyMs": 50.0,
                "results": [
                    {
                        "id": "0",
                        "executionId": str(uuid4()),
                        "success": True,
                        "output": "spam",
                        "latencyMs": 45.0,
                        "sampledForValidation": False,
                    },
                    {
                        "id": "1",
                        "executionId": str(uuid4()),
                        "success": True,
                        "output": "not_spam",
                        "latencyMs": 50.0,
                        "sampledForValidation": False,
                    },
                    {
                        "id": "2",
                        "executionId": str(uuid4()),
                        "success": True,
                        "output": "spam",
                        "latencyMs": 55.0,
                        "sampledForValidation": True,
                    },
                ],
                "startedAt": datetime.utcnow().isoformat(),
                "completedAt": datetime.utcnow().isoformat(),
            },
        )

        async with LoopaiClient(base_url=mock_base_url, max_retries=0) as client:
            inputs = [
                {"text": "Buy now!"},
                {"text": "Meeting at 2pm"},
                {"text": "Free money!!!"},
            ]
            result = await client.batch_execute(task_id, inputs, max_concurrency=10)

            assert result.batch_id == batch_id
            assert result.total_items == 3
            assert result.success_count == 3
            assert result.failure_count == 0
            assert len(result.results) == 3
            assert result.results[0].output == "spam"
            assert result.results[2].sampled_for_validation is True

    @pytest.mark.asyncio
    async def test_batch_execute_partial_failure(
        self, mock_base_url: str, httpx_mock: HTTPXMock
    ) -> None:
        """Test batch execution with partial failures."""
        task_id = uuid4()
        batch_id = uuid4()

        httpx_mock.add_response(
            url=f"{mock_base_url}/api/v1/batch/execute",
            method="POST",
            json={
                "batchId": str(batch_id),
                "taskId": str(task_id),
                "version": 1,
                "totalItems": 2,
                "successCount": 1,
                "failureCount": 1,
                "totalDurationMs": 100.0,
                "avgLatencyMs": 50.0,
                "results": [
                    {
                        "id": "0",
                        "executionId": str(uuid4()),
                        "success": True,
                        "output": "spam",
                        "latencyMs": 45.0,
                        "sampledForValidation": False,
                    },
                    {
                        "id": "1",
                        "executionId": str(uuid4()),
                        "success": False,
                        "errorMessage": "Execution timeout",
                        "latencyMs": 55.0,
                        "sampledForValidation": False,
                    },
                ],
                "startedAt": datetime.utcnow().isoformat(),
                "completedAt": datetime.utcnow().isoformat(),
            },
        )

        async with LoopaiClient(base_url=mock_base_url, max_retries=0) as client:
            inputs = [{"text": "Email 1"}, {"text": "Email 2"}]
            result = await client.batch_execute(task_id, inputs)

            assert result.success_count == 1
            assert result.failure_count == 1
            assert result.results[0].success is True
            assert result.results[1].success is False
            assert result.results[1].error_message == "Execution timeout"
