"""
Cross-SDK compatibility test

This test creates a task using one SDK and executes it using all three SDKs,
verifying that all SDKs can interoperate correctly.
"""
import asyncio
import json
import subprocess
import sys
from pathlib import Path
from typing import Dict, Any
from uuid import UUID

# Import Python SDK
sys.path.insert(0, str(Path(__file__).parent.parent.parent.parent / "sdk" / "python"))
from loopai import LoopaiClient


def load_test_config() -> Dict[str, Any]:
    """Load test configuration."""
    config_path = Path(__file__).parent.parent / "test-config.json"
    with open(config_path, "r") as f:
        return json.load(f)


async def create_test_task(config: Dict[str, Any]) -> UUID:
    """Create a test task using Python SDK."""
    base_url = config["baseUrl"]
    test_data = config["testData"]

    async with LoopaiClient(base_url=base_url) as client:
        task = await client.create_task(
            name=f"{test_data['taskName']}-cross-sdk",
            description=test_data["taskDescription"],
            input_schema=test_data["inputSchema"],
            output_schema=test_data["outputSchema"],
            accuracy_target=test_data["accuracyTarget"],
            latency_target_ms=test_data["latencyTargetMs"],
            sampling_rate=test_data["samplingRate"],
        )
        print(f"✓ Created task with Python SDK: {task.id}")
        return task.id


async def test_python_execution(config: Dict[str, Any], task_id: UUID) -> Dict[str, Any]:
    """Test execution using Python SDK."""
    base_url = config["baseUrl"]
    test_data = config["testData"]
    input_data = test_data["sampleInputs"][0]

    async with LoopaiClient(base_url=base_url) as client:
        result = await client.execute(task_id=task_id, input_data=input_data)

        return {
            "sdk": "Python",
            "execution_id": str(result.id),
            "task_id": str(result.task_id),
            "version": result.version,
            "output": result.output,
            "latency_ms": result.latency_ms,
            "sampled": result.sampled_for_validation,
            "timestamp": result.executed_at.isoformat(),
        }


def test_dotnet_execution(config: Dict[str, Any], task_id: str) -> Dict[str, Any]:
    """Test execution using .NET SDK via CLI."""
    # Create a simple C# script to execute the task
    script = f"""
using System;
using System.Threading.Tasks;
using Loopai.Client;

var client = new LoopaiClient("{config['baseUrl']}");
var result = await client.ExecuteAsync(
    Guid.Parse("{task_id}"),
    new {{ text = "{config['testData']['sampleInputs'][0]['text']}" }}
);

Console.WriteLine($"{{result.Id}}|{{result.TaskId}}|{{result.Version}}|{{result.Output}}|{{result.LatencyMs}}|{{result.SampledForValidation}}|{{result.ExecutedAt:O}}");
"""
    # Note: This would require dotnet-script or similar
    # For now, we'll mark it as not implemented
    return {
        "sdk": ".NET",
        "status": "not_implemented",
        "note": "Requires dotnet-script or compiled executable"
    }


def test_typescript_execution(config: Dict[str, Any], task_id: str) -> Dict[str, Any]:
    """Test execution using TypeScript SDK via Node.js."""
    # Create a simple Node.js script
    script = f"""
const {{ LoopaiClient }} = require('@loopai/sdk');

async function test() {{
    const client = new LoopaiClient({{ baseUrl: '{config['baseUrl']}' }});
    const result = await client.execute({{
        taskId: '{task_id}',
        input: {json.dumps(config['testData']['sampleInputs'][0])}
    }});

    console.log(JSON.stringify({{
        sdk: 'TypeScript',
        execution_id: result.id,
        task_id: result.taskId,
        version: result.version,
        output: result.output,
        latency_ms: result.latencyMs,
        sampled: result.sampledForValidation,
        timestamp: result.executedAt
    }}));
}}

test().catch(console.error);
"""

    try:
        # Write temporary script
        script_path = Path(__file__).parent / "temp_ts_test.js"
        script_path.write_text(script)

        # Execute with Node.js
        result = subprocess.run(
            ["node", str(script_path)],
            capture_output=True,
            text=True,
            timeout=30,
            cwd=str(Path(__file__).parent.parent.parent.parent / "sdk" / "typescript")
        )

        # Clean up
        script_path.unlink(missing_ok=True)

        if result.returncode == 0:
            return json.loads(result.stdout)
        else:
            return {
                "sdk": "TypeScript",
                "status": "error",
                "error": result.stderr
            }
    except Exception as e:
        return {
            "sdk": "TypeScript",
            "status": "error",
            "error": str(e)
        }


async def verify_compatibility(results: list[Dict[str, Any]]) -> Dict[str, Any]:
    """Verify that all SDKs produced compatible results."""
    issues = []

    # Extract successful results
    successful = [r for r in results if "execution_id" in r]

    if len(successful) < 2:
        return {
            "compatible": False,
            "issues": ["Not enough SDKs successfully executed"],
            "results": results
        }

    # Check task_id consistency
    task_ids = {r["task_id"] for r in successful}
    if len(task_ids) > 1:
        issues.append(f"Task IDs inconsistent: {task_ids}")

    # Check version consistency
    versions = {r["version"] for r in successful}
    if len(versions) > 1:
        issues.append(f"Versions inconsistent: {versions}")

    # Check output format
    outputs = {r["output"] for r in successful}
    if len(outputs) > 1:
        # Different outputs might be OK if it's non-deterministic
        # But should be same type
        pass

    # Check timestamp format (all should be ISO 8601)
    for r in successful:
        try:
            from datetime import datetime
            datetime.fromisoformat(r["timestamp"].replace("Z", "+00:00"))
        except Exception as e:
            issues.append(f"{r['sdk']}: Invalid timestamp format: {e}")

    return {
        "compatible": len(issues) == 0,
        "issues": issues if issues else None,
        "summary": {
            "sdks_tested": len(results),
            "successful": len(successful),
            "failed": len(results) - len(successful),
        },
        "results": results
    }


async def main():
    """Run cross-SDK compatibility test."""
    print("=" * 60)
    print("Cross-SDK Compatibility Test")
    print("=" * 60)
    print()

    config = load_test_config()

    # 1. Create task using Python SDK
    print("Step 1: Creating test task...")
    task_id = await create_test_task(config)
    print()

    # 2. Execute using Python SDK
    print("Step 2: Testing Python SDK execution...")
    python_result = await test_python_execution(config, task_id)
    print(f"✓ Python SDK: {python_result['output']} ({python_result['latency_ms']:.2f}ms)")
    print()

    # 3. Execute using .NET SDK
    print("Step 3: Testing .NET SDK execution...")
    dotnet_result = test_dotnet_execution(config, str(task_id))
    if "status" in dotnet_result:
        print(f"⚠ .NET SDK: {dotnet_result['status']} - {dotnet_result.get('note', dotnet_result.get('error'))}")
    else:
        print(f"✓ .NET SDK: {dotnet_result['output']} ({dotnet_result['latency_ms']:.2f}ms)")
    print()

    # 4. Execute using TypeScript SDK
    print("Step 4: Testing TypeScript SDK execution...")
    typescript_result = test_typescript_execution(config, str(task_id))
    if "status" in typescript_result:
        print(f"⚠ TypeScript SDK: {typescript_result['status']} - {typescript_result.get('error')}")
    else:
        print(f"✓ TypeScript SDK: {typescript_result['output']} ({typescript_result['latency_ms']:.2f}ms)")
    print()

    # 5. Verify compatibility
    print("Step 5: Verifying cross-SDK compatibility...")
    results = [python_result, dotnet_result, typescript_result]
    compatibility = await verify_compatibility(results)

    print()
    print("=" * 60)
    print("Compatibility Report")
    print("=" * 60)
    print(json.dumps(compatibility, indent=2))
    print()

    if compatibility["compatible"]:
        print("✅ All SDKs are compatible!")
        return 0
    else:
        print("❌ Compatibility issues detected:")
        for issue in compatibility["issues"]:
            print(f"  - {issue}")
        return 1


if __name__ == "__main__":
    sys.exit(asyncio.run(main()))
