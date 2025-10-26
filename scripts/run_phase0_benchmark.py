"""
Phase 0 Benchmark Script

Run complete Phase 0 validation:
1. Generate program from task specification
2. Execute on all test cases
3. Validate sample against LLM oracle
4. Calculate metrics and cost analysis
"""

import json
import os
import statistics
from pathlib import Path
from typing import List

from dotenv import load_dotenv

from loopai.executor import ProgramExecutor
from loopai.generator import ProgramGenerator
from loopai.models import ExecutionRecord, TaskSpecification, ValidationRecord
from loopai.validator import ComparisonEngine, LLMOracle

# Load environment variables
load_dotenv()


def load_phase0_dataset():
    """Load Phase 0 test dataset."""
    dataset_path = Path(__file__).parent.parent / "tests" / "datasets" / "phase0_binary_sentiment_trivial.json"
    with open(dataset_path, "r", encoding="utf-8") as f:
        data = json.load(f)
    return data


def create_task_specification(dataset):
    """Create task specification from dataset metadata."""
    metadata = dataset["metadata"]
    test_cases = dataset["test_cases"]

    # Create examples from first 5 test cases
    examples = [
        {"input": {"text": tc["input"]}, "output": tc["expected_output"]}
        for tc in test_cases[:5]
    ]

    task_spec = TaskSpecification(
        name="phase0-binary-sentiment",
        description="Classify text as 'positive' or 'negative' based on sentiment",
        input_schema={"type": "object", "properties": {"text": {"type": "string"}}},
        output_schema={"type": "string", "enum": ["positive", "negative"]},
        examples=examples,
        accuracy_target=1.0,
        latency_target_ms=10,
        sampling_rate=0.1,  # 10% sampling for validation
    )

    return task_spec


def run_benchmark():
    """Run Phase 0 benchmark."""
    print("=" * 60)
    print("Phase 0 Benchmark: Binary Sentiment Classification")
    print("=" * 60)
    print()

    # Check API key
    api_key = os.getenv("OPENAI_API_KEY")
    if not api_key:
        print("❌ Error: OPENAI_API_KEY not found in environment")
        print("Please create a .env file with your OpenAI API key")
        return

    # Display configuration
    model = os.getenv("OPENAI_MODEL", "gpt-4")
    print("🔧 Configuration:")
    print(f"   OpenAI API Key: {'*' * 8}{api_key[-4:] if len(api_key) > 4 else '****'}")
    print(f"   OpenAI Model: {model}")
    print()

    # Load dataset
    print("📊 Loading Phase 0 dataset...")
    dataset = load_phase0_dataset()
    test_cases = dataset["test_cases"]
    print(f"   Loaded {len(test_cases)} test cases")
    print()

    # Create task specification
    print("📝 Creating task specification...")
    task_spec = create_task_specification(dataset)
    print(f"   Task: {task_spec.name}")
    print(f"   Description: {task_spec.description}")
    print()

    # Generate program
    print("🔧 Generating program...")
    generator = ProgramGenerator()
    try:
        program = generator.generate(task_spec)
        print(f"   ✅ Program generated successfully")
        print(f"   - Language: {program.language}")
        print(f"   - Strategy: {program.synthesis_strategy}")
        print(f"   - Confidence: {program.confidence_score:.2f}")
        print(f"   - Lines of code: {program.complexity_metrics.lines_of_code}")
        print(f"   - Generation cost: ${program.generation_cost:.4f}")
        print(f"   - Generation time: {program.generation_time_sec:.2f}s")
        print()
        print("   Generated code:")
        print("   " + "-" * 56)
        for line in program.code.split("\n")[:15]:  # Show first 15 lines
            print(f"   {line}")
        if len(program.code.split("\n")) > 15:
            print("   ...")
        print("   " + "-" * 56)
        print()
    except Exception as e:
        print(f"   ❌ Program generation failed: {e}")
        return

    # Execute on all test cases
    print(f"⚡ Executing program on {len(test_cases)} test cases...")
    executor = ProgramExecutor()
    executions: List[ExecutionRecord] = []

    for i, test_case in enumerate(test_cases):
        input_data = {"text": test_case["input"]}
        execution = executor.execute(program, input_data, task_spec.id)
        executions.append(execution)

        if (i + 1) % 20 == 0:
            print(f"   Progress: {i + 1}/{len(test_cases)}")

    print(f"   ✅ Completed {len(executions)} executions")
    print()

    # Calculate execution metrics
    successful = [e for e in executions if e.status.value == "success"]
    latencies = [e.latency_ms for e in successful if e.latency_ms]

    print("📈 Execution Metrics:")
    print(f"   - Success rate: {len(successful)}/{len(executions)} ({len(successful)/len(executions)*100:.1f}%)")
    if latencies:
        print(f"   - Average latency: {statistics.mean(latencies):.2f}ms")
        print(f"   - Median latency (p50): {statistics.median(latencies):.2f}ms")
        print(f"   - p95 latency: {sorted(latencies)[int(len(latencies)*0.95)]:.2f}ms")
        print(f"   - p99 latency: {sorted(latencies)[int(len(latencies)*0.99)]:.2f}ms")
    print()

    # Validate accuracy against expected outputs
    print("✅ Validating accuracy against test dataset...")
    correct = 0
    for execution, test_case in zip(executions, test_cases):
        if execution.status.value == "success" and execution.output_data:
            program_output = execution.output_data.get("result", "").lower().strip()
            expected_output = test_case["expected_output"].lower().strip()
            if program_output == expected_output:
                correct += 1

    accuracy = correct / len(test_cases)
    print(f"   Accuracy: {accuracy*100:.1f}% ({correct}/{len(test_cases)} correct)")
    print()

    # Sample validation against LLM oracle (10% sampling)
    sample_size = int(len(test_cases) * 0.1)  # 10% sampling
    print(f"🔍 Validating {sample_size} samples against LLM oracle...")

    oracle = LLMOracle()
    comparison = ComparisonEngine()
    validations: List[ValidationRecord] = []

    for i in range(sample_size):
        test_case = test_cases[i]
        execution = executions[i]

        if execution.status.value == "success":
            # Query oracle
            input_data = {"text": test_case["input"]}
            oracle_result = oracle.query(task_spec, input_data)

            # Compare
            validation = comparison.compare(execution, oracle_result)
            validations.append(validation)

            if (i + 1) % 5 == 0:
                print(f"   Progress: {i + 1}/{sample_size}")

    print(f"   ✅ Completed {len(validations)} validations")
    print()

    # Oracle validation metrics
    oracle_matches = sum(1 for v in validations if v.match)
    oracle_accuracy = oracle_matches / len(validations) if validations else 0
    total_oracle_cost = sum(v.oracle_cost for v in validations)
    avg_oracle_latency = statistics.mean([v.oracle_latency_ms for v in validations]) if validations else 0

    print("🎯 Oracle Validation Metrics:")
    print(f"   - Oracle agreement: {oracle_accuracy*100:.1f}% ({oracle_matches}/{len(validations)})")
    print(f"   - Total oracle cost: ${total_oracle_cost:.4f}")
    print(f"   - Average oracle latency: {avg_oracle_latency:.0f}ms")
    print()

    # Cost analysis
    print("💰 Cost Analysis:")
    program_gen_cost = program.generation_cost
    program_exec_cost = len(executions) * 0.00001  # ~$0.00001 per execution
    oracle_validation_cost = total_oracle_cost

    # Extrapolate to 100% LLM usage
    full_llm_cost = (len(test_cases) * total_oracle_cost / sample_size) if sample_size > 0 else 0

    print(f"   - Program generation (one-time): ${program_gen_cost:.4f}")
    print(f"   - Program execution ({len(executions)} runs): ${program_exec_cost:.4f}")
    print(f"   - Oracle validation ({sample_size} samples): ${oracle_validation_cost:.4f}")
    print(f"   - Total Loopai cost: ${program_gen_cost + program_exec_cost + oracle_validation_cost:.4f}")
    print()
    print(f"   - Direct LLM (100 calls): ${full_llm_cost:.4f}")
    if full_llm_cost > 0:
        cost_reduction = (1 - (program_gen_cost + program_exec_cost + oracle_validation_cost) / full_llm_cost) * 100
        print(f"   - Cost reduction: {cost_reduction:.1f}%")

        # Break-even calculation
        break_even = program_gen_cost / (full_llm_cost / len(test_cases) - 0.00001)
        print(f"   - Break-even point: ~{int(break_even)} executions")
    print()

    # Speedup analysis
    avg_program_latency = None
    if latencies and avg_oracle_latency > 0:
        avg_program_latency = statistics.mean(latencies)
        speedup = avg_oracle_latency / avg_program_latency
        print(f"⚡ Performance Improvement:")
        print(f"   - Program latency: {avg_program_latency:.2f}ms")
        print(f"   - LLM oracle latency: {avg_oracle_latency:.0f}ms")
        print(f"   - Speedup: {speedup:.0f}x faster")
        print()

    # Final verdict
    print("=" * 60)
    if accuracy >= 0.95 and oracle_accuracy >= 0.95 and (avg_program_latency is None or avg_program_latency < 10):
        print("✅ Phase 0 SUCCESS - All criteria met!")
        print()
        print("Next steps:")
        print("  1. Review generated program quality")
        print("  2. Begin Phase 1 implementation (basic classification)")
        print("  3. Implement sampling strategies")
    else:
        print("⚠️  Phase 0 needs improvement:")
        if accuracy < 0.95:
            print(f"   - Accuracy: {accuracy*100:.1f}% (target: 95%+)")
        if oracle_accuracy < 0.95:
            print(f"   - Oracle agreement: {oracle_accuracy*100:.1f}% (target: 95%+)")
        if avg_program_latency is not None and avg_program_latency >= 10:
            print(f"   - Latency: {avg_program_latency:.2f}ms (target: <10ms)")
        if len(successful) == 0:
            print(f"   - All executions failed! Check program errors.")
    print("=" * 60)


if __name__ == "__main__":
    run_benchmark()
