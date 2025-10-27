"""Data models for Loopai SDK."""

from datetime import datetime
from typing import Any, Dict, List, Optional
from uuid import UUID

from pydantic import BaseModel, Field


class Task(BaseModel):
    """Represents a Loopai task."""

    id: UUID
    name: str
    description: str
    input_schema: Dict[str, Any] = Field(alias="inputSchema")
    output_schema: Dict[str, Any] = Field(alias="outputSchema")
    accuracy_target: float = Field(alias="accuracyTarget")
    latency_target_ms: int = Field(alias="latencyTargetMs")
    sampling_rate: float = Field(alias="samplingRate")
    created_at: datetime = Field(alias="createdAt")

    class Config:
        populate_by_name = True


class ExecutionResult(BaseModel):
    """Represents the result of a task execution."""

    id: UUID
    task_id: UUID = Field(alias="taskId")
    version: int
    output: Any
    latency_ms: float = Field(alias="latencyMs")
    sampled_for_validation: bool = Field(alias="sampledForValidation")
    executed_at: datetime = Field(alias="executedAt")

    class Config:
        populate_by_name = True


class BatchExecuteItem(BaseModel):
    """Represents a single item in a batch execution request."""

    id: str
    input: Dict[str, Any]
    force_validation: bool = Field(default=False, alias="forceValidation")

    class Config:
        populate_by_name = True


class BatchExecuteRequest(BaseModel):
    """Request model for batch execution."""

    task_id: UUID = Field(alias="taskId")
    items: List[BatchExecuteItem]
    version: Optional[int] = None
    max_concurrency: int = Field(default=10, alias="maxConcurrency")
    stop_on_first_error: bool = Field(default=False, alias="stopOnFirstError")
    timeout_ms: Optional[int] = Field(default=None, alias="timeoutMs")

    class Config:
        populate_by_name = True


class BatchExecuteResult(BaseModel):
    """Result for a single item in a batch execution."""

    id: str
    execution_id: UUID = Field(alias="executionId")
    success: bool
    output: Optional[Any] = None
    error_message: Optional[str] = Field(default=None, alias="errorMessage")
    latency_ms: float = Field(alias="latencyMs")
    sampled_for_validation: bool = Field(alias="sampledForValidation")

    class Config:
        populate_by_name = True


class BatchExecuteResponse(BaseModel):
    """Response model for batch execution."""

    batch_id: UUID = Field(alias="batchId")
    task_id: UUID = Field(alias="taskId")
    version: int
    total_items: int = Field(alias="totalItems")
    success_count: int = Field(alias="successCount")
    failure_count: int = Field(alias="failureCount")
    total_duration_ms: float = Field(alias="totalDurationMs")
    avg_latency_ms: float = Field(alias="avgLatencyMs")
    results: List[BatchExecuteResult]
    started_at: datetime = Field(alias="startedAt")
    completed_at: datetime = Field(alias="completedAt")

    class Config:
        populate_by_name = True


class HealthResponse(BaseModel):
    """Health check response."""

    status: str
    version: str
    timestamp: datetime
