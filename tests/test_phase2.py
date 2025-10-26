"""
Phase 2 Tests: Pattern Recognition (Moderate Complexity)

Test-Driven Development approach:
1. Write tests first (this file)
2. Run tests (they will fail)
3. Implement features to pass tests
4. Refactor without breaking tests

Phase 2 Focus:
- Email categorization (work, personal, promotional, spam)
- Intent classification (question, command, statement)
- Pattern matching with regex support
- 85-90% accuracy target
"""

import json
import pytest
from pathlib import Path
from uuid import uuid4

from loopai.executor import ProgramExecutor
from loopai.generator import ProgramGenerator
from loopai.models import (
    ExecutionStatus,
    TaskSpecification,
    ProgramArtifact,
    SynthesisStrategy,
    ComplexityMetrics,
)
from loopai.sampler import RandomSampler
from loopai.validator import ComparisonEngine, LLMOracle


# ============================================================================
# Dataset Validation Tests (Test First - datasets don't exist yet)
# ============================================================================


@pytest.mark.phase2
class TestPhase2Datasets:
    """Test Phase 2 dataset structure and quality."""

    def test_email_categorization_dataset_exists(self):
        """Email categorization dataset should exist and be valid."""
        dataset_path = Path(__file__).parent / "datasets" / "phase2_email_categorization.json"
        assert dataset_path.exists(), "Email categorization dataset not found"

        with open(dataset_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        # Validate structure
        assert "metadata" in data
        assert "test_cases" in data

        # Validate metadata
        metadata = data["metadata"]
        assert metadata["phase"] == 2
        assert metadata["name"] == "email_categorization"
        assert metadata["difficulty"] == "moderate"
        assert metadata["task_type"] == "classification"

        # Should have 4 classes: work, personal, promotional, spam
        expected_classes = {"work", "personal", "promotional", "spam"}
        assert set(metadata["classes"]) == expected_classes

        # Should have 200+ samples for Phase 2
        test_cases = data["test_cases"]
        assert len(test_cases) >= 200, f"Expected 200+ samples, got {len(test_cases)}"
        assert metadata["total_samples"] == len(test_cases)

        # Validate test case structure
        for tc in test_cases[:5]:  # Check first 5
            assert "id" in tc
            assert "input" in tc
            assert "expected_output" in tc
            assert "difficulty" in tc
            assert tc["expected_output"] in expected_classes

        # Check class balance (should be reasonably balanced)
        class_counts = {}
        for tc in test_cases:
            cls = tc["expected_output"]
            class_counts[cls] = class_counts.get(cls, 0) + 1

        # Each class should have at least 40 samples (200 / 4 = 50, allow some imbalance)
        for cls in expected_classes:
            assert class_counts.get(cls, 0) >= 40, f"Class {cls} has too few samples"

    def test_intent_classification_dataset_exists(self):
        """Intent classification dataset should exist and be valid."""
        dataset_path = Path(__file__).parent / "datasets" / "phase2_intent_classification.json"
        assert dataset_path.exists(), "Intent classification dataset not found"

        with open(dataset_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        # Validate structure
        assert "metadata" in data
        assert "test_cases" in data

        # Validate metadata
        metadata = data["metadata"]
        assert metadata["phase"] == 2
        assert metadata["name"] == "intent_classification"
        assert metadata["difficulty"] == "moderate"

        # Should have 3 classes: question, command, statement
        expected_classes = {"question", "command", "statement"}
        assert set(metadata["classes"]) == expected_classes

        # Should have 150+ samples
        test_cases = data["test_cases"]
        assert len(test_cases) >= 150, f"Expected 150+ samples, got {len(test_cases)}"


# ============================================================================
# Pattern Matching Support Tests
# ============================================================================


@pytest.mark.phase2
class TestPatternMatchingSupport:
    """Test that executor supports pattern matching and regex."""

    def test_executor_supports_regex_module(self):
        """Executor should allow regex operations in generated code."""
        # Create a simple program that uses regex
        program_code = """
import re

def classify(text: str) -> str:
    # Test regex functionality
    if re.search(r'\\d{3}-\\d{4}', text):
        return "phone_number"
    elif re.search(r'\\w+@\\w+\\.\\w+', text):
        return "email"
    else:
        return "other"
"""

        program = ProgramArtifact(
            task_id=uuid4(),
            version=1,
            language="python",
            code=program_code,
            synthesis_strategy=SynthesisStrategy.RULE,
            confidence_score=0.9,
            complexity_metrics=ComplexityMetrics(lines_of_code=10, cyclomatic_complexity=3),
            llm_provider="test",
            llm_model="test",
            generation_cost=0.0,
            generation_time_sec=0.0,
        )

        executor = ProgramExecutor()

        # Test phone number detection
        result = executor.execute(program, {"text": "Call me at 555-1234"}, uuid4())
        assert result.status == ExecutionStatus.SUCCESS
        assert result.output_data["result"] == "phone_number"

        # Test email detection
        result = executor.execute(program, {"text": "Email me at test@example.com"}, uuid4())
        assert result.status == ExecutionStatus.SUCCESS
        assert result.output_data["result"] == "email"

    def test_executor_supports_string_methods(self):
        """Executor should support advanced string methods for pattern matching."""
        program_code = """
def classify(text: str) -> str:
    # Test various string methods
    if text.startswith("Dear"):
        return "formal"
    elif text.endswith("?"):
        return "question"
    elif any(word in text.lower() for word in ["please", "kindly", "would you"]):
        return "polite_request"
    else:
        return "other"
"""

        program = ProgramArtifact(
            task_id=uuid4(),
            version=1,
            language="python",
            code=program_code,
            synthesis_strategy=SynthesisStrategy.RULE,
            confidence_score=0.9,
            complexity_metrics=ComplexityMetrics(lines_of_code=10, cyclomatic_complexity=3),
            llm_provider="test",
            llm_model="test",
            generation_cost=0.0,
            generation_time_sec=0.0,
        )

        executor = ProgramExecutor()

        # Test startswith
        result = executor.execute(program, {"text": "Dear Sir or Madam"}, uuid4())
        assert result.status == ExecutionStatus.SUCCESS
        assert result.output_data["result"] == "formal"

        # Test endswith
        result = executor.execute(program, {"text": "How are you?"}, uuid4())
        assert result.status == ExecutionStatus.SUCCESS
        assert result.output_data["result"] == "question"


# ============================================================================
# Program Generation Tests
# ============================================================================


@pytest.mark.phase2
class TestPhase2ProgramGeneration:
    """Test program generation for Phase 2 tasks."""

    @pytest.mark.slow
    def test_generate_email_categorization_program(self):
        """Should generate a valid program for email categorization."""
        task_spec = TaskSpecification(
            name="email_categorization",
            description="Categorize emails into: work, personal, promotional, or spam",
            input_schema={"type": "object", "properties": {"text": {"type": "string"}}},
            output_schema={"type": "string", "enum": ["work", "personal", "promotional", "spam"]},
            examples=[
                {"input": {"text": "Meeting tomorrow at 10am"}, "output": "work"},
                {"input": {"text": "Happy birthday!"}, "output": "personal"},
                {"input": {"text": "50% off sale this weekend!"}, "output": "promotional"},
                {"input": {"text": "You won $1000000!"}, "output": "spam"},
            ],
            accuracy_target=0.85,
            latency_target_ms=10,
            sampling_rate=0.2,
        )

        generator = ProgramGenerator()
        program = generator.generate(task_spec)

        # Validate program was generated
        assert program is not None
        assert program.code is not None
        assert len(program.code) > 0
        assert "def classify" in program.code

        # Validate execution works
        executor = ProgramExecutor()
        result = executor.execute(program, {"text": "Meeting at 2pm"}, task_spec.id)
        assert result.status == ExecutionStatus.SUCCESS
        assert result.output_data["result"] in ["work", "personal", "promotional", "spam"]


# ============================================================================
# Sampling Strategy Tests
# ============================================================================


@pytest.mark.phase2
class TestPhase2Sampling:
    """Test sampling strategies for Phase 2."""

    def test_random_sampler_handles_larger_datasets(self):
        """Random sampler should work efficiently with 200+ samples."""
        from loopai.models import ExecutionRecord

        # Create 200 mock execution records
        executions = []
        for i in range(200):
            exec_record = ExecutionRecord(
                program_id=uuid4(),
                task_id=uuid4(),
                input_data={"text": f"sample {i}"},
                output_data={"result": "test"},
                latency_ms=1.0,
                status=ExecutionStatus.SUCCESS,
            )
            executions.append(exec_record)

        # Test with 20% sampling rate
        sampler = RandomSampler(sampling_rate=0.2, seed=42)
        sampled_indices = sampler.select_for_validation(executions)

        # Should sample approximately 40 items (20% of 200)
        assert 35 <= len(sampled_indices) <= 45, f"Expected ~40 samples, got {len(sampled_indices)}"

        # All sampled indices should be valid
        assert all(0 <= idx < 200 for idx in sampled_indices)

        # Should be unique (no duplicates)
        assert len(sampled_indices) == len(set(sampled_indices))


# ============================================================================
# Integration Tests
# ============================================================================


@pytest.mark.phase2
@pytest.mark.slow
class TestPhase2Integration:
    """Integration tests for Phase 2 end-to-end flow."""

    def test_email_categorization_end_to_end(self):
        """Test complete flow: generate → execute → sample → validate."""
        # This test will fail until all components are implemented
        pytest.skip("Integration test - implement after components are ready")

    def test_pattern_matching_accuracy(self):
        """Test that pattern-based classification achieves 85%+ accuracy."""
        pytest.skip("Accuracy test - implement after datasets are ready")


# ============================================================================
# Performance Tests
# ============================================================================


@pytest.mark.phase2
class TestPhase2Performance:
    """Performance tests for Phase 2."""

    def test_execution_latency_under_10ms(self):
        """Pattern matching programs should execute in <10ms."""
        program_code = """
import re

def classify(text: str) -> str:
    patterns = {
        'work': r'(meeting|deadline|project|report|presentation)',
        'personal': r'(birthday|vacation|family|friend)',
        'promotional': r'(sale|discount|offer|deal|limited)',
        'spam': r'(winner|prize|claim|urgent|click here)',
    }

    text_lower = text.lower()
    for category, pattern in patterns.items():
        if re.search(pattern, text_lower):
            return category

    return 'personal'  # default
"""

        program = ProgramArtifact(
            task_id=uuid4(),
            version=1,
            language="python",
            code=program_code,
            synthesis_strategy=SynthesisStrategy.RULE,
            confidence_score=0.9,
            complexity_metrics=ComplexityMetrics(lines_of_code=10, cyclomatic_complexity=3),
            llm_provider="test",
            llm_model="test",
            generation_cost=0.0,
            generation_time_sec=0.0,
        )

        executor = ProgramExecutor()

        # Execute 10 times and measure latency
        latencies = []
        for _ in range(10):
            result = executor.execute(program, {"text": "Meeting tomorrow at 10am"}, uuid4())
            assert result.status == ExecutionStatus.SUCCESS
            latencies.append(result.latency_ms)

        # Average latency should be well under 10ms
        avg_latency = sum(latencies) / len(latencies)
        assert avg_latency < 10.0, f"Average latency {avg_latency}ms exceeds 10ms target"


# ============================================================================
# Code Quality Tests (No Technical Debt)
# ============================================================================


@pytest.mark.phase2
class TestCodeQuality:
    """Ensure code quality and no technical debt."""

    def test_all_modules_have_docstrings(self):
        """All modules should have proper docstrings."""
        from loopai import sampler

        # Check sampler module has docstring
        assert sampler.__doc__ is not None
        assert len(sampler.__doc__.strip()) > 0

    def test_no_todo_comments_in_code(self):
        """Production code should not have TODO comments."""
        import loopai.sampler.random_sampler as random_sampler

        source = Path(random_sampler.__file__).read_text()
        assert "TODO" not in source, "Found TODO comments in production code"
        assert "FIXME" not in source, "Found FIXME comments in production code"

    def test_type_hints_present(self):
        """Functions should have proper type hints."""
        from loopai.sampler import RandomSampler

        # Check that key methods have type annotations
        import inspect

        sig = inspect.signature(RandomSampler.select_for_validation)
        assert sig.return_annotation != inspect.Parameter.empty, "Missing return type hint"


if __name__ == "__main__":
    pytest.main([__file__, "-v", "-m", "phase2"])
