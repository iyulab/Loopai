"""
Phase 1 Benchmark Script

Run complete Phase 1 validation across multiple datasets:
1. Spam Detection (binary classification)
2. Language Detection (5-class classification)
3. Sentiment Analysis (3-class classification)

Features:
- Random sampling strategy (20% sampling rate)
- Multi-class classification support
- Comprehensive metrics per dataset
"""

import json
import os
import statistics
from pathlib import Path
from typing import Dict, List

from dotenv import load_dotenv

from loopai.executor import ProgramExecutor
from loopai.generator import ProgramGenerator
from loopai.models import ExecutionRecord, TaskSpecification, ValidationRecord
from loopai.sampler import RandomSampler
from loopai.validator import ComparisonEngine, LLMOracle

# Load environment variables
load_dotenv()


def load_dataset(dataset_name: str) -> dict:
    """Load Phase 1 dataset."""
    dataset_path = Path(__file__).parent.parent / "tests" / "datasets" / f"{dataset_name}.json"
    with open(dataset_path, "r", encoding="utf-8") as f:
        data = json.load(f)
    return data


def create_task_specification(dataset: dict, dataset_name: str) -> TaskSpecification:
    """Create task specification from dataset metadata."""
    metadata = dataset["metadata"]
    test_cases = dataset["test_cases"]

    # Create examples from first 5 test cases
    examples = [
        {"input": {"text": tc["input"]}, "output": tc["expected_output"]}
        for tc in test_cases[:5]
    ]

    # Build description based on task type
    if "spam" in dataset_name:
        description = "Classify messages as 'spam' or 'ham' (not spam)"
    elif "language" in dataset_name:
        classes_str = ", ".join(metadata["classes"])
        description = f"Detect the language of text. Possible languages: {classes_str}"
    elif "sentiment" in dataset_name:
        classes_str = ", ".join(metadata["classes"])
        description = f"Classify sentiment as: {classes_str}"
    else:
        description = metadata["description"]

    task_spec = TaskSpecification(
        name=metadata["name"],
        description=description,
        input_schema={"type": "object", "properties": {"text": {"type": "string"}}},
        output_schema={"type": "string", "enum": metadata["classes"]},
        examples=examples,
        accuracy_target=metadata["accuracy_target"],
        latency_target_ms=10,
        sampling_rate=metadata["sampling_rate"],
    )

    return task_spec


def run_single_benchmark(dataset_name: str, display_name: str):
    """Run benchmark for a single dataset."""
    print("=" * 60)
    print(f"Phase 1 Benchmark: {display_name}")
    print("=" * 60)
    print()

    # Load dataset
    print(f"📊 Loading {dataset_name} dataset...")
    dataset = load_dataset(dataset_name)
    test_cases = dataset["test_cases"]
    metadata = dataset["metadata"]
    print(f"   Loaded {len(test_cases)} test cases")
    print(f"   Classes: {', '.join(metadata['classes'])}")
    print(f"   Target accuracy: {metadata['accuracy_target']*100:.0f}%")
    print(f"   Sampling rate: {metadata['sampling_rate']*100:.0f}%")
    print()

    # Create task specification
    print("📝 Creating task specification...")
    task_spec = create_task_specification(dataset, dataset_name)
    print(f"   Task: {task_spec.name}")
    print(f"   Description: {task_spec.description}")
    print()

    # Generate program
    print("🔧 Generating program...")
    generator = ProgramGenerator()
    try:
        program = generator.generate(task_spec)
        print(f"   ✅ Program generated successfully")
        print(f"   - Lines of code: {program.complexity_metrics.lines_of_code}")
        print(f"   - Generation cost: ${program.generation_cost:.4f}")
        print(f"   - Generation time: {program.generation_time_sec:.2f}s")
        print()
    except Exception as e:
        print(f"   ❌ Program generation failed: {e}")
        return None

    # Execute on all test cases
    print(f"⚡ Executing program on {len(test_cases)} test cases...")
    executor = ProgramExecutor()
    executions: List[ExecutionRecord] = []

    for i, test_case in enumerate(test_cases):
        input_data = {"text": test_case["input"]}
        execution = executor.execute(program, input_data, task_spec.id)
        executions.append(execution)

        if (i + 1) % 10 == 0:
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

    accuracy = correct / len(test_cases) if test_cases else 0
    print(f"   Accuracy: {accuracy*100:.1f}% ({correct}/{len(test_cases)} correct)")
    print()

    # Sample validation against LLM oracle using random sampling
    print(f"🔍 Sampling executions for oracle validation...")
    sampler = RandomSampler(sampling_rate=task_spec.sampling_rate)
    sampled_indices = sampler.select_for_validation(executions)
    sampler.mark_sampled(executions, sampled_indices)

    print(f"   Selected {len(sampled_indices)} samples ({len(sampled_indices)/len(executions)*100:.1f}%)")
    print()

    print(f"🔍 Validating {len(sampled_indices)} samples against LLM oracle...")
    oracle = LLMOracle()
    comparison = ComparisonEngine()
    validations: List[ValidationRecord] = []

    for i, idx in enumerate(sampled_indices):
        test_case = test_cases[idx]
        execution = executions[idx]

        if execution.status.value == "success":
            # Query oracle
            input_data = {"text": test_case["input"]}
            oracle_result = oracle.query(task_spec, input_data)

            # Compare
            validation = comparison.compare(execution, oracle_result)
            validations.append(validation)

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
    program_exec_cost = len(executions) * 0.00001
    oracle_validation_cost = total_oracle_cost

    # Extrapolate to 100% LLM usage
    full_llm_cost = (len(test_cases) * total_oracle_cost / len(sampled_indices)) if len(sampled_indices) > 0 else 0

    print(f"   - Program generation (one-time): ${program_gen_cost:.4f}")
    print(f"   - Program execution ({len(executions)} runs): ${program_exec_cost:.4f}")
    print(f"   - Oracle validation ({len(sampled_indices)} samples): ${oracle_validation_cost:.4f}")
    print(f"   - Total Loopai cost: ${program_gen_cost + program_exec_cost + oracle_validation_cost:.4f}")
    print()
    print(f"   - Direct LLM ({len(test_cases)} calls): ${full_llm_cost:.4f}")
    if full_llm_cost > 0:
        cost_reduction = (1 - (program_gen_cost + program_exec_cost + oracle_validation_cost) / full_llm_cost) * 100
        print(f"   - Cost reduction: {cost_reduction:.1f}%")
    print()

    # Performance metrics
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
    meets_accuracy = accuracy >= metadata["accuracy_target"]
    meets_oracle = oracle_accuracy >= 0.80
    meets_latency = avg_program_latency is None or avg_program_latency < 10

    if meets_accuracy and meets_oracle and meets_latency:
        print(f"✅ {display_name} SUCCESS - All criteria met!")
    else:
        print(f"⚠️  {display_name} needs improvement:")
        if not meets_accuracy:
            print(f"   - Accuracy: {accuracy*100:.1f}% (target: {metadata['accuracy_target']*100:.0f}%+)")
        if not meets_oracle:
            print(f"   - Oracle agreement: {oracle_accuracy*100:.1f}% (target: 80%+)")
        if not meets_latency:
            print(f"   - Latency: {avg_program_latency:.2f}ms (target: <10ms)")
    print("=" * 60)
    print()

    return {
        "dataset": dataset_name,
        "accuracy": accuracy,
        "oracle_accuracy": oracle_accuracy,
        "success": meets_accuracy and meets_oracle and meets_latency,
    }


def run_phase1_benchmark():
    """Run Phase 1 benchmark across all datasets."""
    # Check API key
    api_key = os.getenv("OPENAI_API_KEY")
    if not api_key:
        print("❌ Error: OPENAI_API_KEY not found in environment")
        print("Please create a .env file with your OpenAI API key")
        return

    # Display configuration
    model = os.getenv("OPENAI_MODEL", "gpt-4")
    print("=" * 60)
    print("Phase 1 Benchmark Suite")
    print("=" * 60)
    print()
    print("🔧 Configuration:")
    print(f"   OpenAI API Key: {'*' * 8}{api_key[-4:] if len(api_key) > 4 else '****'}")
    print(f"   OpenAI Model: {model}")
    print()

    # Run benchmarks for each dataset
    results = []

    # 1. Spam Detection
    result = run_single_benchmark("phase1_spam_detection", "Spam Detection")
    if result:
        results.append(result)

    # 2. Language Detection
    result = run_single_benchmark("phase1_language_detection", "Language Detection")
    if result:
        results.append(result)

    # 3. Sentiment Analysis (3-class)
    result = run_single_benchmark("phase1_sentiment_3class", "Sentiment Analysis (3-class)")
    if result:
        results.append(result)

    # Summary
    print()
    print("=" * 60)
    print("Phase 1 Summary")
    print("=" * 60)
    for result in results:
        status = "✅" if result["success"] else "⚠️"
        print(f"{status} {result['dataset']}: {result['accuracy']*100:.1f}% accuracy, {result['oracle_accuracy']*100:.1f}% oracle agreement")

    success_count = sum(1 for r in results if r["success"])
    print()
    print(f"Overall: {success_count}/{len(results)} datasets passed")
    print("=" * 60)


if __name__ == "__main__":
    run_phase1_benchmark()
