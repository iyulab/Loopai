"""Exception classes for Loopai SDK."""

from typing import Any, Dict, Optional


class LoopaiException(Exception):
    """Base exception for all Loopai SDK errors."""

    def __init__(
        self,
        message: str,
        status_code: Optional[int] = None,
        response: Optional[Dict[str, Any]] = None,
    ) -> None:
        super().__init__(message)
        self.message = message
        self.status_code = status_code
        self.response = response

    def __str__(self) -> str:
        if self.status_code:
            return f"HTTP {self.status_code}: {self.message}"
        return self.message


class ValidationException(LoopaiException):
    """Exception raised when input or output validation fails."""

    def __init__(
        self,
        message: str,
        status_code: int = 400,
        errors: Optional[Dict[str, list[str]]] = None,
    ) -> None:
        super().__init__(message, status_code)
        self.errors = errors or {}

    def __str__(self) -> str:
        if self.errors:
            error_details = ", ".join(
                f"{field}: {', '.join(msgs)}" for field, msgs in self.errors.items()
            )
            return f"{self.message} - {error_details}"
        return self.message


class ExecutionException(LoopaiException):
    """Exception raised when program execution fails."""

    def __init__(
        self,
        message: str,
        status_code: int = 500,
        execution_id: Optional[str] = None,
    ) -> None:
        super().__init__(message, status_code)
        self.execution_id = execution_id

    def __str__(self) -> str:
        if self.execution_id:
            return f"Execution {self.execution_id} failed: {self.message}"
        return f"Execution failed: {self.message}"


class ConnectionException(LoopaiException):
    """Exception raised when connection to Loopai API fails."""

    def __init__(self, message: str, original_exception: Optional[Exception] = None) -> None:
        super().__init__(message)
        self.original_exception = original_exception

    def __str__(self) -> str:
        if self.original_exception:
            return f"{self.message} ({type(self.original_exception).__name__}: {self.original_exception})"
        return self.message
