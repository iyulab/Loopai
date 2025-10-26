"""
Random Sampling Strategy: Randomly select executions for oracle validation.

Phase 1 implementation: Simple random sampling with configurable rate.
"""

import random
from typing import List
from uuid import UUID

from loopai.models import ExecutionRecord


class RandomSampler:
    """Randomly sample executions for oracle validation."""

    def __init__(self, sampling_rate: float = 0.1, seed: int = None):
        """
        Initialize random sampler.

        Args:
            sampling_rate: Fraction of executions to sample (0.0 to 1.0)
            seed: Random seed for reproducibility (None for random)
        """
        if not 0.0 <= sampling_rate <= 1.0:
            raise ValueError(f"Sampling rate must be between 0.0 and 1.0, got {sampling_rate}")

        self.sampling_rate = sampling_rate
        self.seed = seed

        if seed is not None:
            random.seed(seed)

    def select_for_validation(self, executions: List[ExecutionRecord]) -> List[int]:
        """
        Select indices of executions to validate against oracle.

        Args:
            executions: List of execution records

        Returns:
            List of indices to validate
        """
        total_count = len(executions)
        sample_count = max(1, int(total_count * self.sampling_rate))  # At least 1 sample

        # Only sample from successful executions
        successful_indices = [
            i for i, exec in enumerate(executions) if exec.status.value == "success"
        ]

        if not successful_indices:
            return []

        # Sample without replacement
        sample_count = min(sample_count, len(successful_indices))
        sampled_indices = random.sample(successful_indices, sample_count)

        return sorted(sampled_indices)

    def mark_sampled(self, executions: List[ExecutionRecord], sampled_indices: List[int]) -> None:
        """
        Mark executions as sampled for validation.

        Args:
            executions: List of execution records
            sampled_indices: Indices that were sampled
        """
        for idx in sampled_indices:
            executions[idx].sampled_for_validation = True
