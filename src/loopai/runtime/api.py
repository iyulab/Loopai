"""
Edge Runtime API: FastAPI-based REST API for program execution.

Provides endpoints for executing programs, health checks, and metrics.
"""

import time
from datetime import datetime
from typing import Dict, Optional
from uuid import uuid4

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

from loopai.executor import ProgramExecutor
from loopai.models import ExecutionRecord, ExecutionStatus
from loopai.runtime.artifact_cache import ArtifactCache
from loopai.runtime.dataset_manager import DatasetManager


class ExecuteRequest(BaseModel):
    """Request model for /execute endpoint."""

    input: Dict[str, str]


class ExecuteResponse(BaseModel):
    """Response model for /execute endpoint."""

    output: str
    latency_ms: float


class HealthResponse(BaseModel):
    """Response model for /health endpoint."""

    status: str
    version: str
    task_id: str
    active_version: int


class MetricsResponse(BaseModel):
    """Response model for /metrics endpoint."""

    executions_today: int
    avg_latency_ms: float


class EdgeRuntime:
    """
    Edge Runtime orchestrator.

    Coordinates artifact cache, dataset manager, and program executor.
    """

    def __init__(self, data_dir: str, task_id: str):
        """
        Initialize Edge Runtime.

        Args:
            data_dir: Root directory for all data (/loopai-data)
            task_id: Task identifier for this runtime
        """
        self.data_dir = data_dir
        self.task_id = task_id

        # Initialize components
        self.artifact_cache = ArtifactCache(data_dir=data_dir)
        self.dataset_manager = DatasetManager(data_dir=data_dir)
        self.executor = ProgramExecutor()

        # Initialize task in dataset manager
        self.dataset_manager.initialize_task(task_id)

        # Load active artifact
        self.active_artifact = self.artifact_cache.get_active_artifact(task_id)
        if not self.active_artifact:
            raise ValueError(f"No active artifact found for task {task_id}")

    def execute(self, input_data: Dict) -> tuple[str, float]:
        """
        Execute program with input data.

        Args:
            input_data: Input dictionary (e.g., {"text": "..."})

        Returns:
            Tuple of (output, latency_ms)
        """
        # Execute program
        start_time = time.time()

        # ProgramExecutor.execute returns ExecutionRecord
        # Use task_id from artifact (UUID type)
        execution_record = self.executor.execute(
            self.active_artifact, input_data, task_id=self.active_artifact.task_id
        )
        latency_ms = (time.time() - start_time) * 1000

        # Update latency in record
        execution_record.latency_ms = latency_ms

        # Log execution to dataset
        self.dataset_manager.log_execution(self.task_id, execution_record)

        # Extract output
        output = execution_record.output_data.get("result", "")

        return output, latency_ms

    def get_metrics(self) -> Dict:
        """
        Get execution metrics for today.

        Returns:
            Dictionary with executions_today and avg_latency_ms
        """
        today = datetime.now().strftime("%Y-%m-%d")
        executions = self.dataset_manager.read_execution_logs(self.task_id, date=today)

        if not executions:
            return {"executions_today": 0, "avg_latency_ms": 0.0}

        # Calculate average latency
        latencies = [e["latency_ms"] for e in executions if e.get("latency_ms")]
        avg_latency = sum(latencies) / len(latencies) if latencies else 0.0

        return {
            "executions_today": len(executions),
            "avg_latency_ms": avg_latency,
        }


def create_app(data_dir: str, task_id: str) -> FastAPI:
    """
    Create FastAPI application for Edge Runtime.

    Args:
        data_dir: Root directory for all data
        task_id: Task identifier

    Returns:
        FastAPI application instance
    """
    app = FastAPI(
        title="Loopai Edge Runtime",
        description="Edge runtime for local program execution",
        version="0.1.0",
    )

    # Initialize runtime
    runtime = EdgeRuntime(data_dir=data_dir, task_id=task_id)

    @app.post("/execute", response_model=ExecuteResponse)
    async def execute(request: ExecuteRequest):
        """
        Execute program with input data.

        Args:
            request: Execute request with input data

        Returns:
            Execute response with output and latency
        """
        try:
            output, latency_ms = runtime.execute(request.input)
            return ExecuteResponse(output=output, latency_ms=latency_ms)
        except Exception as e:
            raise HTTPException(status_code=500, detail=str(e))

    @app.get("/health", response_model=HealthResponse)
    async def health():
        """
        Health check endpoint.

        Returns:
            Health status with runtime information
        """
        return HealthResponse(
            status="healthy",
            version="0.1.0",
            task_id=runtime.task_id,
            active_version=runtime.active_artifact.version,
        )

    @app.get("/metrics", response_model=MetricsResponse)
    async def metrics():
        """
        Get execution metrics.

        Returns:
            Metrics including execution count and average latency
        """
        metrics_data = runtime.get_metrics()
        return MetricsResponse(**metrics_data)

    return app
