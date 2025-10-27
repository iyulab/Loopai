# Loopai Python SDK

Official Python client library for [Loopai](https://github.com/iyulab/loopai) - Human-in-the-Loop AI Self-Improvement Framework.

## Features

- ✅ **Async/Await Support**: Modern async Python patterns with `asyncio`
- ✅ **Type Hints**: Full type annotations for better IDE support
- ✅ **Pydantic Models**: Strongly-typed request/response models
- ✅ **Automatic Retry**: Exponential backoff for transient failures
- ✅ **Batch Execution**: Process multiple inputs with concurrency control
- ✅ **Exception Handling**: Specific exceptions for different error types
- ✅ **Context Manager**: Automatic resource cleanup with `async with`

## Installation

### From PyPI (when published)

```bash
pip install loopai
```

### From Source

```bash
cd sdk/python
pip install -e .
```

### Development Installation

```bash
cd sdk/python
pip install -e ".[dev]"
```

## Quick Start

### Basic Usage

```python
import asyncio
from loopai import LoopaiClient


async def main():
    async with LoopaiClient("http://localhost:8080") as client:
        # Create a task
        task = await client.create_task(
            name="spam-classifier",
            description="Classify emails as spam or not spam",
            input_schema={
                "type": "object",
                "properties": {"text": {"type": "string"}},
                "required": ["text"]
            },
            output_schema={
                "type": "string",
                "enum": ["spam", "not_spam"]
            },
            accuracy_target=0.95,
            latency_target_ms=50
        )

        # Execute
        result = await client.execute(
            task_id=task.id,
            input_data={"text": "Buy now for free money!"}
        )
        print(f"Classification: {result.output}")


asyncio.run(main())
```

### Batch Execution

```python
async with LoopaiClient("http://localhost:8080") as client:
    # Simple batch execution
    emails = [
        {"text": "Buy now for free money!"},
        {"text": "Meeting tomorrow at 2pm"},
        {"text": "URGENT: Click here NOW!!!"}
    ]

    result = await client.batch_execute(
        task_id=task.id,
        inputs=emails,
        max_concurrency=10
    )

    print(f"Processed {result.total_items} items")
    print(f"Success: {result.success_count}/{result.total_items}")
    print(f"Average latency: {result.avg_latency_ms:.2f}ms")

    for item in result.results:
        if item.success:
            print(f"  {item.id}: {item.output}")
        else:
            print(f"  {item.id}: FAILED - {item.error_message}")
```

### Advanced Batch Execution

```python
from loopai.models import BatchExecuteRequest, BatchExecuteItem

# Create batch items with custom IDs and options
items = [
    BatchExecuteItem(
        id="email-001",
        input={"text": "Spam email"},
        force_validation=False
    ),
    BatchExecuteItem(
        id="email-002",
        input={"text": "Legitimate email"},
        force_validation=True  # Force validation for this item
    )
]

request = BatchExecuteRequest(
    task_id=task.id,
    items=items,
    max_concurrency=5,
    stop_on_first_error=False,
    timeout_ms=30000
)

result = await client.batch_execute_advanced(request)
```

### Error Handling

```python
from loopai.exceptions import (
    LoopaiException,
    ValidationException,
    ExecutionException,
    ConnectionException
)

try:
    result = await client.execute(task_id, input_data)
except ValidationException as exc:
    # Handle input validation errors
    print(f"Validation failed: {exc.message}")
    print(f"Field errors: {exc.errors}")
except ExecutionException as exc:
    # Handle execution failures
    print(f"Execution failed: {exc.message}")
    print(f"Execution ID: {exc.execution_id}")
except ConnectionException as exc:
    # Handle connection errors
    print(f"Connection failed: {exc.message}")
except LoopaiException as exc:
    # Handle general API errors
    print(f"API error: {exc.message}")
```

## Configuration

### Client Options

```python
client = LoopaiClient(
    base_url="http://localhost:8080",  # API base URL
    api_key="sk-...",                  # Optional API key
    timeout=30.0,                       # Request timeout (seconds)
    max_retries=3                       # Max retry attempts
)
```

### Environment Variables

```bash
export LOOPAI_BASE_URL="http://localhost:8080"
export LOOPAI_API_KEY="sk-..."
```

## API Reference

### LoopaiClient

#### `create_task()`

Create a new task.

```python
task = await client.create_task(
    name: str,
    description: str,
    input_schema: Dict[str, Any],
    output_schema: Dict[str, Any],
    examples: Optional[List[Dict[str, Any]]] = None,
    accuracy_target: float = 0.9,
    latency_target_ms: int = 10,
    sampling_rate: float = 0.1
) -> Task
```

#### `get_task()`

Retrieve task by ID.

```python
task = await client.get_task(task_id: Union[str, UUID]) -> Task
```

#### `execute()`

Execute a task with input.

```python
result = await client.execute(
    task_id: Union[str, UUID],
    input_data: Dict[str, Any],
    version: Optional[int] = None,
    timeout_ms: Optional[int] = None,
    force_validation: bool = False
) -> ExecutionResult
```

#### `batch_execute()`

Execute multiple inputs in batch (simplified).

```python
result = await client.batch_execute(
    task_id: Union[str, UUID],
    inputs: List[Dict[str, Any]],
    max_concurrency: int = 10
) -> BatchExecuteResponse
```

#### `batch_execute_advanced()`

Execute batch with advanced options.

```python
result = await client.batch_execute_advanced(
    request: BatchExecuteRequest
) -> BatchExecuteResponse
```

#### `get_health()`

Get API health status.

```python
health = await client.get_health() -> HealthResponse
```

## Models

### Task

```python
class Task(BaseModel):
    id: UUID
    name: str
    description: str
    input_schema: Dict[str, Any]
    output_schema: Dict[str, Any]
    accuracy_target: float
    latency_target_ms: int
    sampling_rate: float
    created_at: datetime
```

### ExecutionResult

```python
class ExecutionResult(BaseModel):
    id: UUID
    task_id: UUID
    version: int
    output: Any
    latency_ms: float
    sampled_for_validation: bool
    executed_at: datetime
```

### BatchExecuteResponse

```python
class BatchExecuteResponse(BaseModel):
    batch_id: UUID
    task_id: UUID
    version: int
    total_items: int
    success_count: int
    failure_count: int
    total_duration_ms: float
    avg_latency_ms: float
    results: List[BatchExecuteResult]
    started_at: datetime
    completed_at: datetime
```

## Exceptions

- `LoopaiException`: Base exception for all SDK errors
- `ValidationException`: Input or output validation failed (HTTP 400)
- `ExecutionException`: Program execution failed (HTTP 500)
- `ConnectionException`: Connection to API failed

## Development

### Running Tests

```bash
# Run all tests
pytest

# Run with coverage
pytest --cov=loopai --cov-report=html

# Run specific test file
pytest tests/test_client.py

# Run specific test
pytest tests/test_client.py::TestLoopaiClient::test_execute_success
```

### Code Quality

```bash
# Format code
black loopai tests examples

# Lint code
ruff check loopai tests examples

# Type check
mypy loopai
```

### Building Package

```bash
# Install build tools
pip install build

# Build package
python -m build

# Install locally
pip install dist/loopai-0.1.0-py3-none-any.whl
```

## Examples

See the [examples](examples/) directory for complete examples:

- [`basic_usage.py`](examples/basic_usage.py) - Basic task creation and execution
- [`advanced_batch.py`](examples/advanced_batch.py) - Advanced batch execution with custom options
- [`error_handling.py`](examples/error_handling.py) - Comprehensive error handling patterns

## Best Practices

### 1. Use Context Manager

```python
# ✅ Good - Automatic resource cleanup
async with LoopaiClient("http://localhost:8080") as client:
    result = await client.execute(task_id, input_data)

# ❌ Bad - Manual cleanup required
client = LoopaiClient("http://localhost:8080")
result = await client.execute(task_id, input_data)
await client.close()  # Easy to forget
```

### 2. Handle Specific Exceptions

```python
# ✅ Good - Handle specific error types
try:
    result = await client.execute(task_id, input_data)
except ValidationException as exc:
    # Handle validation errors
    handle_validation_error(exc.errors)
except ExecutionException as exc:
    # Handle execution failures
    log_execution_failure(exc.execution_id)

# ❌ Bad - Generic exception handling
try:
    result = await client.execute(task_id, input_data)
except Exception as exc:
    # Too broad
    pass
```

### 3. Use Type Hints

```python
# ✅ Good - Type hints for better IDE support
async def classify_email(client: LoopaiClient, task_id: UUID, email: str) -> str:
    result = await client.execute(task_id, {"text": email})
    return result.output

# ❌ Bad - No type hints
async def classify_email(client, task_id, email):
    result = await client.execute(task_id, {"text": email})
    return result.output
```

### 4. Configure Appropriate Timeout

```python
# ✅ Good - Appropriate timeout for long-running operations
client = LoopaiClient(timeout=60.0, max_retries=5)

# ❌ Bad - Too short timeout
client = LoopaiClient(timeout=1.0)  # May timeout prematurely
```

## Requirements

- Python >= 3.9
- httpx >= 0.27.0
- pydantic >= 2.0.0
- typing-extensions >= 4.0.0

## License

MIT License - see [LICENSE](../../LICENSE) for details.

## Support

- **GitHub Issues**: [github.com/iyulab/loopai/issues](https://github.com/iyulab/loopai/issues)
- **Documentation**: [github.com/iyulab/loopai](https://github.com/iyulab/loopai)

## Contributing

Contributions are welcome! Please read our contributing guidelines before submitting pull requests.
