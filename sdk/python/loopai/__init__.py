"""Loopai Python SDK - Official Python client for Loopai API.

Loopai is a Human-in-the-Loop AI Self-Improvement Framework that enables
automatic program synthesis, validation, and continuous improvement.
"""

from loopai.client import LoopaiClient
from loopai.exceptions import (
    LoopaiException,
    ValidationException,
    ExecutionException,
    ConnectionException,
)
from loopai.models import (
    Task,
    ExecutionResult,
    BatchExecuteRequest,
    BatchExecuteResponse,
    HealthResponse,
)

__version__ = "0.1.0"
__all__ = [
    "LoopaiClient",
    "LoopaiException",
    "ValidationException",
    "ExecutionException",
    "ConnectionException",
    "Task",
    "ExecutionResult",
    "BatchExecuteRequest",
    "BatchExecuteResponse",
    "HealthResponse",
]
