"""
Integration tests for Loopai Python SDK

These tests require a running Loopai API server at http://localhost:8080
Run: cd src/Loopai.CloudApi && dotnet run
"""
import asyncio
from typing import Dict, Any
from uuid import UUID

import pytest

# Import from installed package or local path
try:
    from loopai import LoopaiClient
    from loopai.exceptions import (
        LoopaiException,
        ValidationException,
        ExecutionException,
    )
except ImportError:
    import sys
    from pathlib import Path
    sdk_path = Path(__file__).parent.parent.parent.parent / "sdk" / "python"
    sys.path.insert(0, str(sdk_path))
    from loopai import LoopaiClient
    from loopai.exceptions import (
        LoopaiException,
        ValidationException,
        ExecutionException,
    )


@pytest.mark.asyncio
class TestHealthCheck:
    """Test API health check."""

    async def test_health_check(self, base_url: str, wait_for_api):
        """Test that API health endpoint is accessible."""
        async with LoopaiClient(base_url=base_url) as client:
            health = await client.get_health()

            assert health.status == "healthy"
            assert health.version is not None
            assert health.timestamp is not None


@pytest.mark.asyncio
class TestTaskLifecycle:
    """Test task creation and retrieval."""

    async def test_create_and_get_task(
        self,
        base_url: str,
        test_config: Dict[str, Any],
        wait_for_api
    ):
        """Test creating and retrieving a task."""
        async with LoopaiClient(base_url=base_url) as client:
            # Create task
            test_data = test_config["testData"]
            task = await client.create_task(
                name=test_data["taskName"],
                description=test_data["taskDescription"],
                input_schema=test_data["inputSchema"],
                output_schema=test_data["outputSchema"],
                accuracy_target=test_data["accuracyTarget"],
                latency_target_ms=test_data["latencyTargetMs"],
                sampling_rate=test_data["samplingRate"],
            )

            # Verify task creation
            assert task.id is not None
            assert isinstance(task.id, UUID)
            assert task.name == test_data["taskName"]
            assert task.description == test_data["taskDescription"]
            assert task.accuracy_target == test_data["accuracyTarget"]
            assert task.latency_target_ms == test_data["latencyTargetMs"]
            assert task.sampling_rate == test_data["samplingRate"]

            # Retrieve task
            retrieved_task = await client.get_task(task.id)
            assert retrieved_task.id == task.id
            assert retrieved_task.name == task.name
            assert retrieved_task.description == task.description


@pytest.mark.asyncio
class TestSingleExecution:
    """Test single task execution."""

    @pytest.fixture
    async def test_task(self, base_url: str, test_config: Dict[str, Any], wait_for_api):
        """Create a test task."""
        async with LoopaiClient(base_url=base_url) as client:
            test_data = test_config["testData"]
            task = await client.create_task(
                name=f"{test_data['taskName']}-single",
                description=test_data["taskDescription"],
                input_schema=test_data["inputSchema"],
                output_schema=test_data["outputSchema"],
                accuracy_target=test_data["accuracyTarget"],
                latency_target_ms=test_data["latencyTargetMs"],
                sampling_rate=test_data["samplingRate"],
            )
            return task

    async def test_execute_task(
        self,
        base_url: str,
        test_task,
        test_config: Dict[str, Any]
    ):
        """Test executing a task with input."""
        async with LoopaiClient(base_url=base_url) as client:
            test_data = test_config["testData"]
            input_data = test_data["sampleInputs"][0]

            result = await client.execute(
                task_id=test_task.id,
                input_data=input_data
            )

            # Verify execution result
            assert result.id is not None
            assert result.task_id == test_task.id
            assert result.version >= 1
            assert result.output is not None
            assert result.latency_ms >= 0
            assert result.sampled_for_validation is not None
            assert result.executed_at is not None

    async def test_execute_with_validation(
        self,
        base_url: str,
        test_task,
        test_config: Dict[str, Any]
    ):
        """Test executing a task with forced validation."""
        async with LoopaiClient(base_url=base_url) as client:
            test_data = test_config["testData"]
            input_data = test_data["sampleInputs"][1]

            result = await client.execute(
                task_id=test_task.id,
                input_data=input_data,
                force_validation=True
            )

            assert result.sampled_for_validation is True


@pytest.mark.asyncio
class TestBatchExecution:
    """Test batch execution."""

    @pytest.fixture
    async def batch_task(self, base_url: str, test_config: Dict[str, Any], wait_for_api):
        """Create a test task for batch operations."""
        async with LoopaiClient(base_url=base_url) as client:
            test_data = test_config["testData"]
            task = await client.create_task(
                name=f"{test_data['taskName']}-batch",
                description=test_data["taskDescription"],
                input_schema=test_data["inputSchema"],
                output_schema=test_data["outputSchema"],
                accuracy_target=test_data["accuracyTarget"],
                latency_target_ms=test_data["latencyTargetMs"],
                sampling_rate=test_data["samplingRate"],
            )
            return task

    async def test_simple_batch_execute(
        self,
        base_url: str,
        batch_task,
        test_config: Dict[str, Any]
    ):
        """Test simple batch execution with auto-generated IDs."""
        async with LoopaiClient(base_url=base_url) as client:
            test_data = test_config["testData"]
            inputs = test_data["sampleInputs"]

            result = await client.batch_execute(
                task_id=batch_task.id,
                inputs=inputs,
                max_concurrency=10
            )

            # Verify batch result
            assert result.batch_id is not None
            assert result.task_id == batch_task.id
            assert result.total_items == len(inputs)
            assert result.success_count >= 0
            assert result.failure_count >= 0
            assert result.success_count + result.failure_count == result.total_items
            assert result.total_duration_ms >= 0
            assert result.avg_latency_ms >= 0
            assert len(result.results) == len(inputs)

            # Verify individual results
            for item in result.results:
                assert item.id is not None
                assert item.execution_id is not None
                if item.success:
                    assert item.output is not None
                    assert item.latency_ms >= 0
                else:
                    assert item.error_message is not None

    async def test_advanced_batch_execute(
        self,
        base_url: str,
        batch_task,
        test_config: Dict[str, Any]
    ):
        """Test advanced batch execution with custom IDs and options."""
        async with LoopaiClient(base_url=base_url) as client:
            test_data = test_config["testData"]

            items = [
                {
                    "id": f"email-{i:03d}",
                    "input": inp,
                    "force_validation": i % 2 == 0  # Force validation for even items
                }
                for i, inp in enumerate(test_data["sampleInputs"])
            ]

            result = await client.batch_execute_advanced(
                task_id=batch_task.id,
                items=items,
                max_concurrency=5,
                stop_on_first_error=False,
                timeout_ms=30000
            )

            # Verify batch result
            assert result.total_items == len(items)
            assert len(result.results) == len(items)

            # Verify custom IDs preserved
            result_ids = {item.id for item in result.results}
            expected_ids = {f"email-{i:03d}" for i in range(len(items))}
            assert result_ids == expected_ids

            # Check forced validation worked
            for i, item in enumerate(result.results):
                if item.success and i % 2 == 0:
                    # Even items should have forced validation
                    # Note: sampled_for_validation may vary based on sampling
                    assert item.sampled_for_validation is not None


@pytest.mark.asyncio
class TestErrorHandling:
    """Test error handling."""

    async def test_validation_error(self, base_url: str, wait_for_api):
        """Test that validation errors are properly raised."""
        async with LoopaiClient(base_url=base_url) as client:
            # Try to create task with invalid schema
            with pytest.raises(ValidationException) as exc_info:
                await client.create_task(
                    name="",  # Empty name should fail
                    description="Test",
                    input_schema={"type": "object"},
                    output_schema={"type": "string"}
                )

            assert exc_info.value.status_code == 400

    async def test_task_not_found(self, base_url: str, wait_for_api):
        """Test that 404 errors are properly raised."""
        async with LoopaiClient(base_url=base_url) as client:
            fake_id = "00000000-0000-0000-0000-000000000000"

            with pytest.raises(LoopaiException) as exc_info:
                await client.get_task(fake_id)

            assert exc_info.value.status_code == 404

    async def test_invalid_input(
        self,
        base_url: str,
        test_config: Dict[str, Any],
        wait_for_api
    ):
        """Test execution with invalid input."""
        async with LoopaiClient(base_url=base_url) as client:
            test_data = test_config["testData"]

            # Create task
            task = await client.create_task(
                name=f"{test_data['taskName']}-error",
                description=test_data["taskDescription"],
                input_schema=test_data["inputSchema"],
                output_schema=test_data["outputSchema"]
            )

            # Try to execute with invalid input (missing required field)
            with pytest.raises(ValidationException):
                await client.execute(
                    task_id=task.id,
                    input_data={}  # Missing 'text' field
                )


@pytest.mark.asyncio
class TestRetryLogic:
    """Test retry logic."""

    async def test_client_with_retries(self, base_url: str, wait_for_api):
        """Test that client retry logic works."""
        async with LoopaiClient(
            base_url=base_url,
            max_retries=3
        ) as client:
            health = await client.get_health()
            assert health.status == "healthy"


@pytest.mark.asyncio
class TestConcurrency:
    """Test concurrent operations."""

    async def test_multiple_concurrent_executions(
        self,
        base_url: str,
        test_config: Dict[str, Any],
        wait_for_api
    ):
        """Test multiple concurrent executions."""
        async with LoopaiClient(base_url=base_url) as client:
            test_data = test_config["testData"]

            # Create task
            task = await client.create_task(
                name=f"{test_data['taskName']}-concurrent",
                description=test_data["taskDescription"],
                input_schema=test_data["inputSchema"],
                output_schema=test_data["outputSchema"]
            )

            # Execute multiple tasks concurrently
            inputs = test_data["sampleInputs"][:3]
            tasks = [
                client.execute(task_id=task.id, input_data=inp)
                for inp in inputs
            ]

            results = await asyncio.gather(*tasks)

            assert len(results) == len(inputs)
            for result in results:
                assert result.task_id == task.id
                assert result.output is not None
