"""
Program Generator: LLM-based program synthesis from task specifications.

Phase 0 implementation: Generate simple rule-based sentiment classifier.
"""

import ast
import os
import time
from typing import Optional

from openai import OpenAI

from loopai.models import (
    ComplexityMetrics,
    ProgramArtifact,
    ProgramStatus,
    SynthesisStrategy,
    TaskSpecification,
)


class ProgramGenerator:
    """Generate executable programs from task specifications using LLM."""

    def __init__(
        self,
        api_key: Optional[str] = None,
        model: Optional[str] = None,
        provider: str = "openai",
    ):
        """
        Initialize program generator.

        Args:
            api_key: OpenAI API key (or None to use OPENAI_API_KEY env variable)
            model: Model to use for generation (or None to use OPENAI_MODEL env variable)
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

    def generate(
        self,
        task_spec: TaskSpecification,
        strategy: SynthesisStrategy = SynthesisStrategy.AUTO,
    ) -> ProgramArtifact:
        """
        Generate a program from task specification.

        Args:
            task_spec: Task specification
            strategy: Synthesis strategy to use

        Returns:
            ProgramArtifact with generated code
        """
        start_time = time.time()

        # Build prompt for LLM
        prompt = self._build_prompt(task_spec, strategy)

        # Call LLM
        # Build API call parameters
        api_params = {
            "model": self.model,
            "messages": [
                {
                    "role": "system",
                    "content": "You are an expert Python programmer. Generate clean, efficient, and correct Python code based on specifications.",
                },
                {"role": "user", "content": prompt},
            ],
        }

        # Add temperature only if using standard OpenAI models
        # Some models (like gpt-5-nano) may not support temperature parameter
        if "gpt-4" in self.model.lower() or "gpt-3.5" in self.model.lower():
            api_params["temperature"] = 0.3  # Lower temperature for more deterministic output

        response = self.client.chat.completions.create(**api_params)

        # Extract code from response
        code = self._extract_code(response.choices[0].message.content or "")

        # Validate generated code
        self._validate_code(code)

        # Calculate metrics
        generation_time = time.time() - start_time
        generation_cost = self._estimate_cost(response)
        complexity = self._calculate_complexity(code)

        # Create program artifact
        artifact = ProgramArtifact(
            task_id=task_spec.id,
            version=1,
            language="python",
            code=code,
            synthesis_strategy=strategy if strategy != SynthesisStrategy.AUTO else SynthesisStrategy.RULE,
            confidence_score=0.95,  # High confidence for Phase 0 simple rules
            complexity_metrics=complexity,
            llm_provider=self.provider,
            llm_model=self.model,
            generation_cost=generation_cost,
            generation_time_sec=generation_time,
            status=ProgramStatus.VALIDATED,
            deployment_percentage=0.0,
        )

        return artifact

    def _build_prompt(self, task_spec: TaskSpecification, strategy: SynthesisStrategy) -> str:
        """Build LLM prompt from task specification."""
        prompt_parts = [
            f"Task: {task_spec.description}",
            "",
            "Requirements:",
            f"- Create a Python function named 'classify' that takes a text string as input",
            f"- Return the classification result as a string",
            f"- Use simple keyword-based rules for classification",
            f"- The function should be deterministic and fast (< 10ms)",
        ]

        # Add examples if provided
        if task_spec.examples:
            prompt_parts.append("")
            prompt_parts.append("Examples:")
            # Limit to 6 examples max for optimal LLM generation quality
            # (stratified sampling in task spec ensures diversity across classes)
            max_examples = min(6, len(task_spec.examples))
            for i, example in enumerate(task_spec.examples[:max_examples], 1):
                input_text = example.get("input", example.get("text", ""))
                output = example.get("output", example.get("expected_output", ""))
                prompt_parts.append(f"{i}. Input: '{input_text}' → Output: '{output}'")

        prompt_parts.extend(
            [
                "",
                "Generate a complete Python function with the following structure:",
                "```python",
                "def classify(text: str) -> str:",
                '    """Classify the input text."""',
                "    # Your implementation here",
                "    pass",
                "```",
                "",
                "Return ONLY the Python code, no explanations.",
            ]
        )

        return "\n".join(prompt_parts)

    def _extract_code(self, response: str) -> str:
        """Extract Python code from LLM response."""
        # Remove markdown code blocks if present
        if "```python" in response:
            code_start = response.find("```python") + len("```python")
            code_end = response.find("```", code_start)
            code = response[code_start:code_end].strip()
        elif "```" in response:
            code_start = response.find("```") + 3
            code_end = response.find("```", code_start)
            code = response[code_start:code_end].strip()
        else:
            code = response.strip()

        return code

    def _validate_code(self, code: str) -> None:
        """
        Validate generated code.

        Raises:
            SyntaxError: If code has syntax errors
            ValueError: If code doesn't meet requirements
        """
        # Check syntax by parsing
        try:
            tree = ast.parse(code)
        except SyntaxError as e:
            raise SyntaxError(f"Generated code has syntax error: {e}")

        # Check that code defines a 'classify' function
        function_names = [
            node.name for node in ast.walk(tree) if isinstance(node, ast.FunctionDef)
        ]

        if "classify" not in function_names:
            raise ValueError("Generated code must define a 'classify' function")

    def _estimate_cost(self, response) -> float:
        """Estimate cost of LLM API call."""
        # GPT-4 pricing (approximate)
        input_tokens = response.usage.prompt_tokens
        output_tokens = response.usage.completion_tokens

        input_cost = input_tokens * 0.00003  # $0.03 per 1K tokens
        output_cost = output_tokens * 0.00006  # $0.06 per 1K tokens

        return input_cost + output_cost

    def _calculate_complexity(self, code: str) -> ComplexityMetrics:
        """Calculate code complexity metrics."""
        lines = [line for line in code.split("\n") if line.strip() and not line.strip().startswith("#")]
        loc = len(lines)

        # Estimate cyclomatic complexity (simple heuristic)
        complexity = 1  # Base complexity
        complexity += code.count("if ")
        complexity += code.count("elif ")
        complexity += code.count("for ")
        complexity += code.count("while ")
        complexity += code.count("and ")
        complexity += code.count("or ")

        # Estimate latency (heuristic: ~0.1ms per LOC for simple operations)
        estimated_latency = loc * 0.1

        return ComplexityMetrics(
            cyclomatic_complexity=complexity,
            lines_of_code=loc,
            estimated_latency_ms=estimated_latency,
        )
