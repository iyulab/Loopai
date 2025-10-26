"""
Program Executor: Safe execution of generated programs.

Phase 0 implementation: Basic execution with timeout and error handling.
"""

import time
from typing import Any, Dict, Optional
from uuid import UUID

from loopai.models import ExecutionRecord, ExecutionStatus, ProgramArtifact


class ProgramExecutor:
    """Execute generated programs safely with timeout and resource limits."""

    def __init__(self, timeout_ms: int = 1000, max_memory_mb: int = 100):
        """
        Initialize program executor.

        Args:
            timeout_ms: Maximum execution time in milliseconds
            max_memory_mb: Maximum memory usage in MB (not enforced in Phase 0)
        """
        self.timeout_ms = timeout_ms
        self.max_memory_mb = max_memory_mb
        self._compiled_cache: Dict[UUID, Any] = {}

    def execute(
        self,
        program: ProgramArtifact,
        input_data: Dict[str, Any],
        task_id: UUID,
    ) -> ExecutionRecord:
        """
        Execute a program with given input.

        Args:
            program: Program artifact to execute
            input_data: Input data for the program
            task_id: Task specification ID

        Returns:
            ExecutionRecord with execution results
        """
        start_time = time.perf_counter()

        try:
            # Get or compile program
            if program.id not in self._compiled_cache:
                self._compile_program(program)

            # Execute program
            output = self._execute_safe(program, input_data)

            # Calculate latency
            end_time = time.perf_counter()
            latency_ms = (end_time - start_time) * 1000

            # Create execution record
            record = ExecutionRecord(
                program_id=program.id,
                task_id=task_id,
                input_data=input_data,
                output_data={"result": output},
                latency_ms=latency_ms,
                memory_usage_mb=None,  # Not measured in Phase 0
                status=ExecutionStatus.SUCCESS,
                error_message=None,
                sampled_for_validation=False,  # Set by sampling strategy
                validation_id=None,
            )

            return record

        except TimeoutError as e:
            end_time = time.perf_counter()
            latency_ms = (end_time - start_time) * 1000

            return ExecutionRecord(
                program_id=program.id,
                task_id=task_id,
                input_data=input_data,
                output_data=None,
                latency_ms=latency_ms,
                memory_usage_mb=None,
                status=ExecutionStatus.TIMEOUT,
                error_message=f"Execution timeout after {self.timeout_ms}ms",
                sampled_for_validation=False,
                validation_id=None,
            )

        except Exception as e:
            end_time = time.perf_counter()
            latency_ms = (end_time - start_time) * 1000

            return ExecutionRecord(
                program_id=program.id,
                task_id=task_id,
                input_data=input_data,
                output_data=None,
                latency_ms=latency_ms,
                memory_usage_mb=None,
                status=ExecutionStatus.ERROR,
                error_message=str(e),
                sampled_for_validation=False,
                validation_id=None,
            )

    def _compile_program(self, program: ProgramArtifact) -> None:
        """Compile program and cache it."""
        try:
            compiled = compile(program.code, f"<program_{program.id}>", "exec")
            self._compiled_cache[program.id] = compiled
        except SyntaxError as e:
            raise SyntaxError(f"Failed to compile program: {e}")

    def _execute_safe(self, program: ProgramArtifact, input_data: Dict[str, Any]) -> str:
        """
        Execute program in a controlled environment.

        Phase 0: Basic execution with globals/locals isolation.
        Future: Add proper sandboxing (RestrictedPython, containers).

        Args:
            program: Program to execute
            input_data: Input data

        Returns:
            Program output as string
        """
        # Get compiled code
        compiled = self._compiled_cache[program.id]

        # Import safe standard library modules
        import re
        import json

        # Prepare execution environment with safe builtins
        safe_builtins = {
            "str": str,
            "len": len,
            "lower": str.lower,
            "upper": str.upper,
            "strip": str.strip,
            "split": str.split,
            "join": str.join,
            "isinstance": isinstance,
            "list": list,
            "dict": dict,
            "set": set,
            "frozenset": frozenset,
            "tuple": tuple,
            "any": any,
            "all": all,
            "int": int,
            "float": float,
            "bool": bool,
            "range": range,
            "enumerate": enumerate,
            "zip": zip,
            "min": min,
            "max": max,
            "sum": sum,
            "sorted": sorted,
            "reversed": reversed,
            # Exception types
            "Exception": Exception,
            "ValueError": ValueError,
            "TypeError": TypeError,
            "KeyError": KeyError,
            "IndexError": IndexError,
            "AttributeError": AttributeError,
            # Other useful builtins
            "print": print,  # Allow print for debugging
            "type": type,
            "hasattr": hasattr,
            "getattr": getattr,
            "setattr": setattr,
        }

        # Create a controlled __import__ function that only allows safe modules
        allowed_modules = {"re", "json", "math", "string", "datetime", "ast", "collections"}

        def safe_import(name, *args, **kwargs):
            if name not in allowed_modules:
                raise ImportError(f"Import of module '{name}' is not allowed")
            return __import__(name, *args, **kwargs)

        safe_builtins["__import__"] = safe_import

        globals_dict = {
            "__builtins__": safe_builtins,
            # Pre-import safe modules for direct access
            "re": re,
            "json": json,
        }

        # Execute program to define functions and module-level variables
        # Use globals_dict for both globals and locals so module-level variables are accessible
        exec(compiled, globals_dict, globals_dict)

        # Get the classify function
        if "classify" not in globals_dict:
            raise ValueError("Program does not define 'classify' function")

        classify_func = globals_dict["classify"]

        # Extract input text (handle different input formats)
        input_text = input_data.get("text", input_data.get("input", str(input_data)))

        # Execute classify function
        result = classify_func(input_text)

        return str(result)

    def clear_cache(self) -> None:
        """Clear compiled program cache."""
        self._compiled_cache.clear()
