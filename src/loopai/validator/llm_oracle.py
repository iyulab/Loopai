"""
LLM Oracle: Query LLM for ground truth outputs.

The LLM serves as the authoritative source for correct answers,
used to validate generated program outputs.
"""

import os
import time
from typing import Any, Dict, Optional

from openai import OpenAI

from loopai.models import TaskSpecification


class LLMOracle:
    """Query LLM for ground truth outputs to validate program results."""

    def __init__(
        self,
        api_key: Optional[str] = None,
        model: Optional[str] = None,
        provider: str = "openai",
    ):
        """
        Initialize LLM oracle.

        Args:
            api_key: OpenAI API key (or None to use OPENAI_API_KEY env variable)
            model: Model to use as oracle (or None to use OPENAI_MODEL env variable)
            provider: LLM provider name
        """
        # Use environment variables if not provided
        if api_key is None:
            api_key = os.getenv("OPENAI_API_KEY")

        if model is None:
            model = os.getenv("OPENAI_MODEL", "gpt-4")  # Default to gpt-4 if not set

        self.client = OpenAI(api_key=api_key)
        self.model = model
        self.provider = provider

    def query(
        self,
        task_spec: TaskSpecification,
        input_data: Dict[str, Any],
    ) -> Dict[str, Any]:
        """
        Query LLM oracle for ground truth output.

        Args:
            task_spec: Task specification
            input_data: Input data to classify

        Returns:
            Dictionary with:
                - output: Oracle's output
                - cost: Cost of query in USD
                - latency_ms: Query latency in milliseconds
        """
        start_time = time.time()

        # Build prompt
        prompt = self._build_prompt(task_spec, input_data)

        # Query LLM
        # Build API call parameters
        api_params = {
            "model": self.model,
            "messages": [
                {
                    "role": "system",
                    "content": "You are a helpful assistant that provides accurate classifications.",
                },
                {"role": "user", "content": prompt},
            ],
        }

        # Add max tokens parameter (different parameter names for different models)
        if "gpt-5" in self.model.lower():
            # gpt-5 reasoning models can use hundreds of tokens for internal reasoning
            # before generating the actual output. Need enough tokens for reasoning + output.
            # For simple classification, reasoning can be 200-500 tokens, output is 1-5 tokens.
            api_params["max_completion_tokens"] = 1000  # gpt-5 uses max_completion_tokens
        else:
            api_params["max_tokens"] = 10  # gpt-4 and gpt-3.5 use max_tokens (short output)

        # Add temperature only if using standard OpenAI models
        # Some models may not support temperature parameter
        if "gpt-4" in self.model.lower() or "gpt-3.5" in self.model.lower():
            api_params["temperature"] = 0.0  # Deterministic for ground truth

        response = self.client.chat.completions.create(**api_params)

        # Extract output
        raw_output = response.choices[0].message.content
        output = raw_output or ""
        output = output.strip().lower()  # Normalize output

        # Calculate metrics
        end_time = time.time()
        latency_ms = (end_time - start_time) * 1000
        cost = self._estimate_cost(response)

        return {
            "output": output,
            "cost": cost,
            "latency_ms": latency_ms,
        }

    def _build_prompt(self, task_spec: TaskSpecification, input_data: Dict[str, Any]) -> str:
        """Build prompt for oracle query."""
        # Extract input text
        input_text = input_data.get("text", input_data.get("input", str(input_data)))

        # Build prompt based on task description
        prompt_parts = [
            task_spec.description,
            "",
            f"Text: {input_text}",
            "",
            "Respond with ONLY the classification label (e.g., 'positive' or 'negative'). No explanations.",
        ]

        return "\n".join(prompt_parts)

    def _estimate_cost(self, response) -> float:
        """Estimate cost of LLM API call."""
        # GPT-4 pricing
        input_tokens = response.usage.prompt_tokens
        output_tokens = response.usage.completion_tokens

        input_cost = input_tokens * 0.00003  # $0.03 per 1K tokens
        output_cost = output_tokens * 0.00006  # $0.06 per 1K tokens

        return input_cost + output_cost
