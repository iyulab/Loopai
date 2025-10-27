"""Advanced batch execution example with custom options."""

import asyncio
from uuid import UUID
from loopai import LoopaiClient
from loopai.models import BatchExecuteRequest, BatchExecuteItem


async def main() -> None:
    """Demonstrate advanced batch execution with custom options."""
    async with LoopaiClient("http://localhost:8080") as client:
        # Assume task already created
        task_id = UUID("your-task-id-here")  # Replace with actual task ID

        # Create batch items with custom IDs and validation flags
        items = [
            BatchExecuteItem(
                id="email-001",
                input={"text": "Congratulations! You won $1,000,000!"},
                force_validation=False,
            ),
            BatchExecuteItem(
                id="email-002",
                input={"text": "Hi, can we schedule a meeting tomorrow?"},
                force_validation=True,  # Force validation for this specific item
            ),
            BatchExecuteItem(
                id="email-003",
                input={"text": "URGENT: Click here to claim your prize NOW!"},
                force_validation=False,
            ),
        ]

        # Create batch request with advanced options
        request = BatchExecuteRequest(
            task_id=task_id,
            items=items,
            max_concurrency=5,  # Limit concurrent executions
            stop_on_first_error=False,  # Continue even if some items fail
            timeout_ms=30000,  # 30 second timeout per batch
        )

        # Execute batch
        result = await client.batch_execute_advanced(request)

        print(f"Batch ID: {result.batch_id}")
        print(f"Total items: {result.total_items}")
        print(f"Success: {result.success_count}")
        print(f"Failed: {result.failure_count}")
        print(f"Duration: {result.total_duration_ms:.2f}ms")
        print(f"Started: {result.started_at}")
        print(f"Completed: {result.completed_at}")

        print("\nResults:")
        for item in result.results:
            print(f"\nItem: {item.id}")
            print(f"  Success: {item.success}")
            if item.success:
                print(f"  Output: {item.output}")
                print(f"  Latency: {item.latency_ms:.2f}ms")
                print(f"  Sampled for validation: {item.sampled_for_validation}")
            else:
                print(f"  Error: {item.error_message}")


if __name__ == "__main__":
    asyncio.run(main())
