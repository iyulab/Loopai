"""Basic usage example for Loopai Python SDK."""

import asyncio
from loopai import LoopaiClient


async def main() -> None:
    """Demonstrate basic SDK usage."""
    # Initialize client
    async with LoopaiClient("http://localhost:8080") as client:
        # Check health
        health = await client.get_health()
        print(f"API Status: {health.status} (v{health.version})")

        # Create a task
        task = await client.create_task(
            name="spam-classifier",
            description="Classify emails as spam or not spam",
            input_schema={
                "type": "object",
                "properties": {"text": {"type": "string"}},
                "required": ["text"],
            },
            output_schema={"type": "string", "enum": ["spam", "not_spam"]},
            accuracy_target=0.95,
            latency_target_ms=50,
        )
        print(f"Created task: {task.name} ({task.id})")

        # Execute single input
        result = await client.execute(
            task_id=task.id, input_data={"text": "Buy now for free money!"}
        )
        print(f"Classification: {result.output} (latency: {result.latency_ms}ms)")

        # Batch execute
        emails = [
            {"text": "Buy now for free money!"},
            {"text": "Meeting tomorrow at 2pm"},
            {"text": "URGENT: Click here NOW!!!"},
            {"text": "Can we schedule a call next week?"},
        ]

        batch_result = await client.batch_execute(task.id, emails, max_concurrency=10)

        print(f"\nBatch Results:")
        print(f"  Total: {batch_result.total_items}")
        print(f"  Success: {batch_result.success_count}")
        print(f"  Failed: {batch_result.failure_count}")
        print(f"  Avg latency: {batch_result.avg_latency_ms:.2f}ms")
        print(f"  Total duration: {batch_result.total_duration_ms:.2f}ms")

        print(f"\nIndividual Results:")
        for item in batch_result.results:
            if item.success:
                print(
                    f"  {item.id}: {item.output} (latency: {item.latency_ms:.2f}ms, "
                    f"sampled: {item.sampled_for_validation})"
                )
            else:
                print(f"  {item.id}: FAILED - {item.error_message}")


if __name__ == "__main__":
    asyncio.run(main())
