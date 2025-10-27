"""Loopai Python client implementation."""

import asyncio
from typing import Any, Dict, List, Optional, Union
from uuid import UUID

import httpx
from pydantic import ValidationError

from loopai.exceptions import (
    ConnectionException,
    ExecutionException,
    LoopaiException,
    ValidationException,
)
from loopai.models import (
    BatchExecuteRequest,
    BatchExecuteResponse,
    ExecutionResult,
    HealthResponse,
    Task,
)


class LoopaiClient:
    """Official Python client for Loopai API.

    Examples:
        Basic usage:
            >>> client = LoopaiClient("http://localhost:8080")
            >>> task = await client.create_task(
            ...     name="spam-classifier",
            ...     description="Classify emails as spam or not spam",
            ...     input_schema={"type": "object", "properties": {"text": {"type": "string"}}},
            ...     output_schema={"type": "string", "enum": ["spam", "not_spam"]}
            ... )

        With context manager:
            >>> async with LoopaiClient("http://localhost:8080") as client:
            ...     result = await client.execute(task_id, {"text": "Buy now!"})
            ...     print(result.output)

        Batch execution:
            >>> inputs = [{"text": "Email 1"}, {"text": "Email 2"}]
            >>> batch_result = await client.batch_execute(task_id, inputs, max_concurrency=10)
            >>> print(f"{batch_result.success_count}/{batch_result.total_items} succeeded")
    """

    def __init__(
        self,
        base_url: str = "http://localhost:8080",
        api_key: Optional[str] = None,
        timeout: float = 30.0,
        max_retries: int = 3,
    ) -> None:
        """Initialize Loopai client.

        Args:
            base_url: Base URL of the Loopai API (default: http://localhost:8080)
            api_key: Optional API key for authentication
            timeout: Request timeout in seconds (default: 30.0)
            max_retries: Maximum number of retry attempts (default: 3)
        """
        self.base_url = base_url.rstrip("/")
        self.api_key = api_key
        self.timeout = timeout
        self.max_retries = max_retries

        # Configure HTTP client
        headers = {"User-Agent": "loopai-python/0.1.0"}
        if api_key:
            headers["Authorization"] = f"Bearer {api_key}"

        self._client = httpx.AsyncClient(
            base_url=self.base_url,
            headers=headers,
            timeout=httpx.Timeout(timeout),
        )

    async def create_task(
        self,
        name: str,
        description: str,
        input_schema: Dict[str, Any],
        output_schema: Dict[str, Any],
        examples: Optional[List[Dict[str, Any]]] = None,
        accuracy_target: float = 0.9,
        latency_target_ms: int = 10,
        sampling_rate: float = 0.1,
    ) -> Task:
        """Create a new task.

        Args:
            name: Task name
            description: Task description
            input_schema: JSON schema for input validation
            output_schema: JSON schema for output validation
            examples: Optional list of example input/output pairs
            accuracy_target: Target accuracy (0.0 to 1.0, default: 0.9)
            latency_target_ms: Target latency in milliseconds (default: 10)
            sampling_rate: Sampling rate for validation (0.0 to 1.0, default: 0.1)

        Returns:
            Created task

        Raises:
            ValidationException: If request validation fails
            LoopaiException: If API request fails
        """
        request = {
            "name": name,
            "description": description,
            "input_schema": input_schema,
            "output_schema": output_schema,
            "examples": examples or [],
            "accuracy_target": accuracy_target,
            "latency_target_ms": latency_target_ms,
            "sampling_rate": sampling_rate,
        }

        response_data = await self._post("/api/v1/tasks", json=request)
        return Task(**response_data)

    async def get_task(self, task_id: Union[str, UUID]) -> Task:
        """Get task by ID.

        Args:
            task_id: Task identifier

        Returns:
            Task details

        Raises:
            LoopaiException: If task not found or API request fails
        """
        response_data = await self._get(f"/api/v1/tasks/{task_id}")
        return Task(**response_data)

    async def execute(
        self,
        task_id: Union[str, UUID],
        input_data: Dict[str, Any],
        version: Optional[int] = None,
        timeout_ms: Optional[int] = None,
        force_validation: bool = False,
    ) -> ExecutionResult:
        """Execute a task with given input.

        Args:
            task_id: Task identifier
            input_data: Input data matching task's input schema
            version: Optional specific program version to execute
            timeout_ms: Optional execution timeout in milliseconds
            force_validation: Force validation for this execution

        Returns:
            Execution result with output

        Raises:
            ValidationException: If input validation fails
            ExecutionException: If execution fails
            LoopaiException: If API request fails
        """
        request = {
            "task_id": str(task_id),
            "input": input_data,
            "version": version,
            "timeout_ms": timeout_ms,
            "force_validation": force_validation,
        }

        response_data = await self._post("/api/v1/tasks/execute", json=request)
        return ExecutionResult(**response_data)

    async def batch_execute(
        self,
        task_id: Union[str, UUID],
        inputs: List[Dict[str, Any]],
        max_concurrency: int = 10,
    ) -> BatchExecuteResponse:
        """Execute multiple inputs in batch (simplified interface).

        Args:
            task_id: Task identifier
            inputs: List of input data dictionaries
            max_concurrency: Maximum concurrent executions (default: 10)

        Returns:
            Batch execution response with results

        Raises:
            ValidationException: If request validation fails
            LoopaiException: If API request fails

        Example:
            >>> inputs = [
            ...     {"text": "Email 1"},
            ...     {"text": "Email 2"},
            ...     {"text": "Email 3"}
            ... ]
            >>> result = await client.batch_execute(task_id, inputs, max_concurrency=5)
            >>> print(f"Success: {result.success_count}/{result.total_items}")
        """
        from loopai.models import BatchExecuteItem

        items = [
            BatchExecuteItem(id=str(i), input=input_data, force_validation=False)
            for i, input_data in enumerate(inputs)
        ]

        request = BatchExecuteRequest(
            task_id=UUID(str(task_id)),
            items=items,
            max_concurrency=max_concurrency,
        )

        return await self.batch_execute_advanced(request)

    async def batch_execute_advanced(
        self, request: BatchExecuteRequest
    ) -> BatchExecuteResponse:
        """Execute batch with advanced options.

        Args:
            request: Complete batch execute request with all options

        Returns:
            Batch execution response with detailed results

        Raises:
            ValidationException: If request validation fails
            LoopaiException: If API request fails

        Example:
            >>> from loopai.models import BatchExecuteRequest, BatchExecuteItem
            >>> items = [
            ...     BatchExecuteItem(id="item-1", input={"text": "Email"}, force_validation=True)
            ... ]
            >>> request = BatchExecuteRequest(
            ...     task_id=task_id,
            ...     items=items,
            ...     max_concurrency=5,
            ...     stop_on_first_error=False,
            ...     timeout_ms=30000
            ... )
            >>> result = await client.batch_execute_advanced(request)
        """
        response_data = await self._post(
            "/api/v1/batch/execute", json=request.model_dump(by_alias=True)
        )
        return BatchExecuteResponse(**response_data)

    async def get_health(self) -> HealthResponse:
        """Get API health status.

        Returns:
            Health check response

        Raises:
            ConnectionException: If connection fails
        """
        response_data = await self._get("/health")
        return HealthResponse(**response_data)

    async def close(self) -> None:
        """Close the HTTP client and cleanup resources."""
        await self._client.aclose()

    async def __aenter__(self) -> "LoopaiClient":
        """Enter async context manager."""
        return self

    async def __aexit__(self, exc_type: Any, exc_val: Any, exc_tb: Any) -> None:
        """Exit async context manager and cleanup resources."""
        await self.close()

    async def _get(self, path: str) -> Dict[str, Any]:
        """Execute GET request with retry logic.

        Args:
            path: API endpoint path

        Returns:
            Response JSON data

        Raises:
            LoopaiException: If request fails after retries
        """
        return await self._request("GET", path)

    async def _post(self, path: str, json: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        """Execute POST request with retry logic.

        Args:
            path: API endpoint path
            json: Request JSON payload

        Returns:
            Response JSON data

        Raises:
            LoopaiException: If request fails after retries
        """
        return await self._request("POST", path, json=json)

    async def _request(
        self, method: str, path: str, json: Optional[Dict[str, Any]] = None
    ) -> Dict[str, Any]:
        """Execute HTTP request with retry logic and error handling.

        Args:
            method: HTTP method (GET, POST, etc.)
            path: API endpoint path
            json: Optional request JSON payload

        Returns:
            Response JSON data

        Raises:
            ValidationException: For 400 Bad Request errors
            ExecutionException: For 500 Internal Server Error
            LoopaiException: For other HTTP errors
            ConnectionException: For connection failures
        """
        last_exception: Optional[Exception] = None

        for attempt in range(self.max_retries + 1):
            try:
                response = await self._client.request(method, path, json=json)

                # Handle successful responses
                if response.status_code < 400:
                    return response.json()

                # Handle error responses
                error_data = response.json() if response.text else {}
                error_message = error_data.get("message", response.text or "Unknown error")

                if response.status_code == 400:
                    errors = error_data.get("errors", {})
                    raise ValidationException(
                        message=error_message,
                        status_code=response.status_code,
                        errors=errors,
                    )
                elif response.status_code == 404:
                    raise LoopaiException(
                        message=f"Resource not found: {error_message}",
                        status_code=response.status_code,
                        response=error_data,
                    )
                elif response.status_code == 500:
                    execution_id = error_data.get("executionId")
                    raise ExecutionException(
                        message=error_message,
                        status_code=response.status_code,
                        execution_id=execution_id,
                    )
                else:
                    raise LoopaiException(
                        message=f"HTTP {response.status_code}: {error_message}",
                        status_code=response.status_code,
                        response=error_data,
                    )

            except (httpx.RequestError, httpx.TimeoutException) as exc:
                last_exception = exc

                # Retry on transient errors (unless last attempt)
                if attempt < self.max_retries:
                    # Exponential backoff
                    delay = 0.5 * (2**attempt)
                    await asyncio.sleep(delay)
                    continue

                # Give up after max retries
                raise ConnectionException(
                    message=f"Failed to connect to Loopai API after {self.max_retries + 1} attempts",
                    original_exception=exc,
                ) from exc

            except ValidationError as exc:
                # Pydantic validation error (malformed response)
                raise LoopaiException(
                    message=f"Invalid response from API: {exc}",
                    response={"validation_error": str(exc)},
                ) from exc

        # Should not reach here, but handle gracefully
        raise ConnectionException(
            message="Request failed after all retries",
            original_exception=last_exception,
        )
