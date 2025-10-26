"""
Phase 3 Edge Runtime API Tests

TDD approach for FastAPI-based edge runtime:
- POST /execute endpoint
- GET /health endpoint
- GET /metrics endpoint
- Integration with Dataset Manager and Artifact Cache
"""

import json
import tempfile
from pathlib import Path
from uuid import uuid4

import pytest
from fastapi.testclient import TestClient

from loopai.models import (
    ComplexityMetrics,
    ProgramArtifact,
    ProgramStatus,
    SynthesisStrategy,
)


@pytest.fixture
def temp_data_dir():
    """Create temporary data directory for testing."""
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)


@pytest.fixture
def sample_artifact():
    """Create sample program artifact."""
    return ProgramArtifact(
        id=uuid4(),
        task_id=uuid4(),
        version=1,
        language="python",
        code='def classify(text: str) -> str:\n    return "spam" if "buy" in text.lower() else "ham"',
        synthesis_strategy=SynthesisStrategy.RULE,
        confidence_score=0.95,
        complexity_metrics=ComplexityMetrics(
            lines_of_code=2,
            cyclomatic_complexity=2,
            estimated_latency_ms=0.5,
        ),
        llm_provider="openai",
        llm_model="gpt-4",
        generation_cost=0.15,
        generation_time_sec=8.5,
        status=ProgramStatus.VALIDATED,
        deployment_percentage=0.0,
    )


@pytest.fixture
def client(temp_data_dir, sample_artifact):
    """Create FastAPI test client with pre-loaded artifact."""
    from loopai.runtime.api import create_app
    from loopai.runtime.artifact_cache import ArtifactCache

    # Set up artifact
    task_id = "test-task"
    cache = ArtifactCache(data_dir=str(temp_data_dir))
    cache.store_artifact(task_id, sample_artifact)
    cache.set_active_version(task_id, version=1)

    # Create app
    app = create_app(data_dir=str(temp_data_dir), task_id=task_id)

    return TestClient(app)


class TestExecuteEndpoint:
    """Test /execute endpoint."""

    def test_execute_returns_result(self, client):
        """Test that /execute endpoint returns classification result."""
        response = client.post(
            "/execute",
            json={"input": {"text": "Buy now!"}},
        )

        assert response.status_code == 200
        data = response.json()

        assert "output" in data
        assert data["output"] == "spam"
        assert "latency_ms" in data
        assert isinstance(data["latency_ms"], (int, float))

    def test_execute_with_different_input(self, client):
        """Test execute with input that should classify as ham."""
        response = client.post(
            "/execute",
            json={"input": {"text": "Meeting at 2pm"}},
        )

        assert response.status_code == 200
        data = response.json()
        assert data["output"] == "ham"

    def test_execute_logs_to_dataset(self, client, temp_data_dir):
        """Test that execution is logged to dataset manager."""
        # Execute
        response = client.post(
            "/execute",
            json={"input": {"text": "Test message"}},
        )

        assert response.status_code == 200

        # Check JSONL log file was created
        from datetime import datetime

        today = datetime.now().strftime("%Y-%m-%d")
        log_file = (
            temp_data_dir
            / "datasets"
            / "test-task"
            / "executions"
            / f"{today}.jsonl"
        )

        assert log_file.exists()

        # Verify log content
        with open(log_file, "r") as f:
            line = f.readline()
            log_data = json.loads(line)
            assert log_data["input_data"] == {"text": "Test message"}
            assert "output_data" in log_data
            assert "latency_ms" in log_data

    def test_execute_invalid_input(self, client):
        """Test execute with invalid input format."""
        response = client.post(
            "/execute",
            json={"invalid": "data"},
        )

        assert response.status_code == 422  # Validation error

    def test_execute_measures_latency(self, client):
        """Test that execution latency is measured."""
        response = client.post(
            "/execute",
            json={"input": {"text": "Test"}},
        )

        assert response.status_code == 200
        data = response.json()

        # Latency should be very small (<10ms)
        assert data["latency_ms"] < 100
        assert data["latency_ms"] > 0


class TestHealthEndpoint:
    """Test /health endpoint."""

    def test_health_returns_healthy(self, client):
        """Test that /health endpoint returns healthy status."""
        response = client.get("/health")

        assert response.status_code == 200
        data = response.json()

        assert data["status"] == "healthy"
        assert "version" in data
        assert "task_id" in data

    def test_health_includes_task_info(self, client):
        """Test that health endpoint includes task information."""
        response = client.get("/health")

        assert response.status_code == 200
        data = response.json()

        assert data["task_id"] == "test-task"
        assert data["active_version"] == 1


class TestMetricsEndpoint:
    """Test /metrics endpoint."""

    def test_metrics_returns_stats(self, client):
        """Test that /metrics endpoint returns execution statistics."""
        # Execute a few times first
        for i in range(5):
            client.post(
                "/execute",
                json={"input": {"text": f"test {i}"}},
            )

        # Get metrics
        response = client.get("/metrics")

        assert response.status_code == 200
        data = response.json()

        assert "executions_today" in data
        assert data["executions_today"] == 5
        assert "avg_latency_ms" in data
        assert isinstance(data["avg_latency_ms"], (int, float))

    def test_metrics_with_no_executions(self, client):
        """Test metrics endpoint with no executions."""
        response = client.get("/metrics")

        assert response.status_code == 200
        data = response.json()

        assert data["executions_today"] == 0
        assert data["avg_latency_ms"] == 0.0


class TestEdgeRuntimeIntegration:
    """Integration tests for Edge Runtime."""

    def test_full_workflow(self, temp_data_dir, sample_artifact):
        """Test complete workflow: store artifact -> execute -> verify logs."""
        from loopai.runtime.api import create_app
        from loopai.runtime.artifact_cache import ArtifactCache

        task_id = "integration-test"

        # 1. Store artifact
        cache = ArtifactCache(data_dir=str(temp_data_dir))
        cache.store_artifact(task_id, sample_artifact)
        cache.set_active_version(task_id, version=1)

        # 2. Create app and execute
        app = create_app(data_dir=str(temp_data_dir), task_id=task_id)
        client = TestClient(app)

        response = client.post(
            "/execute",
            json={"input": {"text": "Buy now!"}},
        )

        assert response.status_code == 200
        assert response.json()["output"] == "spam"

        # 3. Verify logs exist
        from datetime import datetime

        today = datetime.now().strftime("%Y-%m-%d")
        log_file = (
            temp_data_dir
            / "datasets"
            / task_id
            / "executions"
            / f"{today}.jsonl"
        )

        assert log_file.exists()

    def test_multiple_versions(self, temp_data_dir, sample_artifact):
        """Test executing with different artifact versions."""
        from loopai.runtime.api import create_app
        from loopai.runtime.artifact_cache import ArtifactCache

        task_id = "version-test"
        cache = ArtifactCache(data_dir=str(temp_data_dir))

        # Store version 1
        cache.store_artifact(task_id, sample_artifact)

        # Store version 2 (always returns "spam")
        artifact_v2 = sample_artifact.model_copy(
            update={
                "version": 2,
                "code": 'def classify(text: str) -> str:\n    return "spam"',
            }
        )
        cache.store_artifact(task_id, artifact_v2)

        # Test with v1 active
        cache.set_active_version(task_id, version=1)
        app = create_app(data_dir=str(temp_data_dir), task_id=task_id)
        client = TestClient(app)

        response = client.post("/execute", json={"input": {"text": "Hello"}})
        assert response.json()["output"] == "ham"  # v1 logic

        # Switch to v2
        cache.set_active_version(task_id, version=2)
        # Note: In production, app would reload. For test, create new app
        app = create_app(data_dir=str(temp_data_dir), task_id=task_id)
        client = TestClient(app)

        response = client.post("/execute", json={"input": {"text": "Hello"}})
        assert response.json()["output"] == "spam"  # v2 always spam
