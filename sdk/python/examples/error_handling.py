"""Error handling example for Loopai Python SDK."""

import asyncio
from uuid import uuid4
from loopai import LoopaiClient
from loopai.exceptions import (
    LoopaiException,
    ValidationException,
    ExecutionException,
    ConnectionException,
)


async def main() -> None:
    """Demonstrate error handling patterns."""
    client = LoopaiClient("http://localhost:8080", timeout=30.0, max_retries=3)

    try:
        # Example 1: Validation error (invalid input)
        print("Example 1: Validation Error")
        task_id = uuid4()  # Assume this exists
        try:
            result = await client.execute(task_id, {})  # Missing required field
        except ValidationException as exc:
            print(f"Validation failed: {exc.message}")
            print(f"Field errors: {exc.errors}")
            print(f"Status code: {exc.status_code}")

        # Example 2: Task not found (404)
        print("\nExample 2: Not Found Error")
        try:
            task = await client.get_task(uuid4())  # Non-existent task
        except LoopaiException as exc:
            if exc.status_code == 404:
                print(f"Task not found: {exc.message}")

        # Example 3: Execution error
        print("\nExample 3: Execution Error")
        try:
            result = await client.execute(task_id, {"text": "Test"})
        except ExecutionException as exc:
            print(f"Execution failed: {exc.message}")
            print(f"Execution ID: {exc.execution_id}")
            print(f"Status code: {exc.status_code}")

        # Example 4: Connection error
        print("\nExample 4: Connection Error")
        bad_client = LoopaiClient("http://invalid-host:9999", max_retries=1)
        try:
            await bad_client.get_health()
        except ConnectionException as exc:
            print(f"Connection failed: {exc.message}")
            print(f"Original error: {exc.original_exception}")
        finally:
            await bad_client.close()

        # Example 5: Generic error handling
        print("\nExample 5: Generic Error Handling")
        try:
            result = await client.execute(task_id, {"text": "Test"})
        except ValidationException as exc:
            # Handle validation errors
            print(f"Input validation failed: {exc.errors}")
        except ExecutionException as exc:
            # Handle execution errors
            print(f"Execution failed: {exc.execution_id}")
        except LoopaiException as exc:
            # Handle other API errors
            print(f"API error: {exc.message}")
        except Exception as exc:
            # Handle unexpected errors
            print(f"Unexpected error: {exc}")

    finally:
        await client.close()


if __name__ == "__main__":
    asyncio.run(main())
