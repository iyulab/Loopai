"""
Comparison Engine: Compare program outputs with oracle ground truth.

Supports multiple comparison strategies:
- Exact match (Phase 0)
- Semantic similarity (Phase 3+)
- Fuzzy matching (Phase 2+)
- Structured comparison (Phase 4+)
"""

from typing import Any, Dict
from uuid import UUID

from loopai.models import (
    ComparisonMethod,
    ExecutionRecord,
    FailureType,
    ValidationRecord,
)


class ComparisonEngine:
    """Compare program outputs with LLM oracle outputs."""

    def compare(
        self,
        execution: ExecutionRecord,
        oracle_result: Dict[str, Any],
        method: ComparisonMethod = ComparisonMethod.EXACT,
    ) -> ValidationRecord:
        """
        Compare program output with oracle output.

        Args:
            execution: Execution record from program
            oracle_result: Oracle result dict with output, cost, latency_ms
            method: Comparison method to use

        Returns:
            ValidationRecord with comparison results
        """
        # Extract outputs
        program_output = self._extract_output(execution.output_data)
        oracle_output = oracle_result["output"]

        # Normalize both outputs
        program_output_norm = self._normalize_output(program_output)
        oracle_output_norm = self._normalize_output(oracle_output)

        # Compare based on method
        if method == ComparisonMethod.EXACT:
            match, similarity = self._exact_match(program_output_norm, oracle_output_norm)
        else:
            # Future: Implement other comparison methods
            match, similarity = self._exact_match(program_output_norm, oracle_output_norm)

        # Determine failure type if mismatch
        failure_type = None
        failure_details = None
        if not match:
            failure_type = FailureType.LOGIC_ERROR
            failure_details = f"Program output '{program_output}' != Oracle output '{oracle_output}'"

        # Create validation record
        validation = ValidationRecord(
            execution_id=execution.id,
            program_id=execution.program_id,
            oracle_output={"result": oracle_output},
            oracle_provider="openai",
            oracle_model="gpt-4",
            oracle_cost=oracle_result["cost"],
            oracle_latency_ms=oracle_result["latency_ms"],
            match=match,
            similarity_score=similarity,
            comparison_method=method,
            failure_type=failure_type,
            failure_category="classification_error" if not match else None,
            failure_details=failure_details,
            tier1_passed=True,  # Syntax validation passed (program executed)
            tier2_passed=True,  # No unit tests in Phase 0
            tier3_passed=match,  # Oracle validation result
        )

        return validation

    def _extract_output(self, output_data: Dict[str, Any] | None) -> str:
        """Extract output string from execution output data."""
        if output_data is None:
            return ""

        if isinstance(output_data, dict):
            return str(output_data.get("result", ""))

        return str(output_data)

    def _normalize_output(self, output: str) -> str:
        """Normalize output for comparison (lowercase, strip whitespace)."""
        return output.strip().lower()

    def _exact_match(self, output1: str, output2: str) -> tuple[bool, float]:
        """
        Exact string match comparison.

        Returns:
            Tuple of (match: bool, similarity: float)
        """
        match = output1 == output2
        similarity = 1.0 if match else 0.0
        return match, similarity

    def _semantic_similarity(self, output1: str, output2: str) -> tuple[bool, float]:
        """
        Semantic similarity comparison using embeddings.

        Phase 3+ implementation.
        """
        # TODO: Implement using sentence embeddings
        raise NotImplementedError("Semantic similarity not implemented yet")

    def _fuzzy_match(self, output1: str, output2: str, threshold: float = 0.8) -> tuple[bool, float]:
        """
        Fuzzy string matching for minor variations.

        Phase 2+ implementation.
        """
        # TODO: Implement using edit distance or similar
        raise NotImplementedError("Fuzzy matching not implemented yet")
