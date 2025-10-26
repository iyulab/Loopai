"""
Phase 3 Integration Tests

End-to-end workflow testing for complete Edge Runtime system.
Tests integration of all Phase 3 components:
- Dataset Manager
- Configuration Manager
- Artifact Cache
- Edge Runtime API
"""

import json
import tempfile
import time
from datetime import datetime
from pathlib import Path
from uuid import uuid4

import pytest
import yaml
from fastapi.testclient import TestClient

from loopai.models import (
    ComplexityMetrics,
    ProgramArtifact,
    ProgramStatus,
    SynthesisStrategy,
)
from loopai.runtime.api import create_app
from loopai.runtime.artifact_cache import ArtifactCache
from loopai.runtime.config_manager import ConfigurationManager
from loopai.runtime.dataset_manager import DatasetManager


@pytest.fixture
def temp_data_dir():
    """Create temporary data directory for integration testing."""
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)


@pytest.fixture
def sample_artifact_v1():
    """Create sample program artifact version 1."""
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
def sample_artifact_v2(sample_artifact_v1):
    """Create sample program artifact version 2 with improved logic."""
    return sample_artifact_v1.model_copy(
        update={
            "id": uuid4(),
            "version": 2,
            "code": 'def classify(text: str) -> str:\n    spam_words = ["buy", "free", "winner", "click"]\n    return "spam" if any(word in text.lower() for word in spam_words) else "ham"',
            "confidence_score": 0.97,
        }
    )


class TestCompleteWorkflow:
    """Test complete end-to-end workflow."""

    def test_full_deployment_workflow(
        self, temp_data_dir, sample_artifact_v1, sample_artifact_v2
    ):
        """
        Test complete workflow from artifact storage to execution and logging.

        Steps:
        1. Initialize all components
        2. Store artifacts (v1, v2)
        3. Set active version
        4. Start Edge Runtime
        5. Execute programs
        6. Verify logging
        7. Check analytics
        8. Test version switching
        """
        task_id = "integration-test"

        # Step 1: Initialize components
        artifact_cache = ArtifactCache(data_dir=str(temp_data_dir))
        dataset_manager = DatasetManager(data_dir=str(temp_data_dir))
        dataset_manager.initialize_task(task_id)

        # Step 2: Store artifacts
        artifact_cache.store_artifact(task_id, sample_artifact_v1)
        artifact_cache.store_artifact(task_id, sample_artifact_v2)

        # Verify artifacts stored
        versions = artifact_cache.list_versions(task_id)
        assert len(versions) == 2
        assert 1 in versions
        assert 2 in versions

        # Step 3: Set active version to v1
        artifact_cache.set_active_version(task_id, version=1)
        active = artifact_cache.get_active_artifact(task_id)
        assert active.version == 1

        # Step 4: Start Edge Runtime with v1
        app = create_app(data_dir=str(temp_data_dir), task_id=task_id)
        client = TestClient(app)

        # Step 5: Execute programs with v1
        test_inputs = [
            {"text": "Buy now!"},
            {"text": "Meeting at 2pm"},
            {"text": "Free winner click here!"},
            {"text": "Regular message"},
        ]

        responses = []
        for input_data in test_inputs:
            response = client.post("/execute", json={"input": input_data})
            assert response.status_code == 200
            responses.append(response.json())

        # Verify v1 logic (only detects "buy")
        assert responses[0]["output"] == "spam"  # "Buy now!" → spam
        assert responses[1]["output"] == "ham"  # "Meeting" → ham
        assert responses[2]["output"] == "ham"  # v1 doesn't detect "free"
        assert responses[3]["output"] == "ham"

        # Step 6: Verify logging
        today = datetime.now().strftime("%Y-%m-%d")
        log_file = (
            temp_data_dir
            / "datasets"
            / task_id
            / "executions"
            / f"{today}.jsonl"
        )
        assert log_file.exists()

        # Read and verify logs
        logs = []
        with open(log_file, "r") as f:
            for line in f:
                logs.append(json.loads(line))

        assert len(logs) == 4
        assert logs[0]["input_data"] == {"text": "Buy now!"}
        assert logs[0]["output_data"]["result"] == "spam"

        # Step 7: Check analytics
        analytics = dataset_manager.generate_daily_analytics(task_id, date=today)
        assert analytics["total_executions"] == 4
        assert analytics["successful_executions"] == 4
        assert analytics["success_rate"] == 1.0

        # Step 8: Switch to v2 and test improved logic
        artifact_cache.set_active_version(task_id, version=2)

        # Create new runtime with v2
        app_v2 = create_app(data_dir=str(temp_data_dir), task_id=task_id)
        client_v2 = TestClient(app_v2)

        # Execute same inputs with v2
        response = client_v2.post(
            "/execute", json={"input": {"text": "Free winner click here!"}}
        )
        assert response.status_code == 200
        assert response.json()["output"] == "spam"  # v2 detects "free"

    def test_concurrent_executions(self, temp_data_dir, sample_artifact_v1):
        """Test multiple concurrent executions."""
        task_id = "concurrent-test"

        # Setup
        cache = ArtifactCache(data_dir=str(temp_data_dir))
        cache.store_artifact(task_id, sample_artifact_v1)
        cache.set_active_version(task_id, version=1)

        app = create_app(data_dir=str(temp_data_dir), task_id=task_id)
        client = TestClient(app)

        # Execute 10 requests
        num_requests = 10
        start_time = time.time()

        for i in range(num_requests):
            response = client.post(
                "/execute",
                json={"input": {"text": f"Test message {i}"}},
            )
            assert response.status_code == 200

        total_time = time.time() - start_time

        # Verify all logged
        today = datetime.now().strftime("%Y-%m-%d")
        log_file = (
            temp_data_dir
            / "datasets"
            / task_id
            / "executions"
            / f"{today}.jsonl"
        )

        logs = []
        with open(log_file, "r") as f:
            for line in f:
                logs.append(json.loads(line))

        assert len(logs) == num_requests

        # Performance check (should be fast for simple programs)
        avg_time_per_request = total_time / num_requests
        assert avg_time_per_request < 0.1  # Less than 100ms per request

    def test_error_recovery(self, temp_data_dir, sample_artifact_v1):
        """Test error handling and recovery."""
        task_id = "error-test"

        # Setup
        cache = ArtifactCache(data_dir=str(temp_data_dir))
        cache.store_artifact(task_id, sample_artifact_v1)
        cache.set_active_version(task_id, version=1)

        app = create_app(data_dir=str(temp_data_dir), task_id=task_id)
        client = TestClient(app)

        # Valid request
        response = client.post("/execute", json={"input": {"text": "Test"}})
        assert response.status_code == 200

        # Invalid request (missing input field)
        response = client.post("/execute", json={"invalid": "data"})
        assert response.status_code == 422

        # After error, system should still work
        response = client.post("/execute", json={"input": {"text": "Test 2"}})
        assert response.status_code == 200


class TestDataPersistence:
    """Test data persistence and recovery."""

    def test_data_survives_restart(self, temp_data_dir, sample_artifact_v1):
        """Test that data persists across runtime restarts."""
        task_id = "persistence-test"

        # First runtime session
        cache = ArtifactCache(data_dir=str(temp_data_dir))
        cache.store_artifact(task_id, sample_artifact_v1)
        cache.set_active_version(task_id, version=1)

        app = create_app(data_dir=str(temp_data_dir), task_id=task_id)
        client = TestClient(app)

        # Execute
        response = client.post("/execute", json={"input": {"text": "Test"}})
        assert response.status_code == 200

        # Simulate restart - create new instances
        cache2 = ArtifactCache(data_dir=str(temp_data_dir))
        active = cache2.get_active_artifact(task_id)
        assert active.version == 1

        # New runtime should work with persisted data
        app2 = create_app(data_dir=str(temp_data_dir), task_id=task_id)
        client2 = TestClient(app2)

        response = client2.post("/execute", json={"input": {"text": "Test 2"}})
        assert response.status_code == 200

        # Verify logs accumulated
        today = datetime.now().strftime("%Y-%m-%d")
        log_file = (
            temp_data_dir
            / "datasets"
            / task_id
            / "executions"
            / f"{today}.jsonl"
        )

        logs = []
        with open(log_file, "r") as f:
            for line in f:
                logs.append(json.loads(line))

        assert len(logs) == 2


class TestComponentIntegration:
    """Test integration between components."""

    def test_config_and_runtime_integration(self, temp_data_dir, sample_artifact_v1):
        """Test Configuration Manager integration with runtime."""
        task_id = "config-test"

        # Create config file
        config_dir = temp_data_dir / "config"
        config_dir.mkdir(parents=True, exist_ok=True)

        config_data = {
            "runtime": {
                "mode": "edge",
                "data_dir": str(temp_data_dir),
            },
            "execution": {
                "worker_count": 1,
                "timeout_ms": 1000,
            },
            "sampling": {
                "strategy": "random",
                "rate": 0.05,
            },
            "storage": {
                "retention_days": 7,
                "max_size_gb": 10,
            },
        }

        config_file = config_dir / "deployment.yaml"
        with open(config_file, "w") as f:
            yaml.dump(config_data, f)

        # Load config
        config_manager = ConfigurationManager(config_path=str(config_file))
        config = config_manager.load_config()

        assert config.runtime.mode == "edge"
        assert config.runtime.data_dir == str(temp_data_dir)

        # Setup runtime with config
        cache = ArtifactCache(data_dir=config.runtime.data_dir)
        cache.store_artifact(task_id, sample_artifact_v1)
        cache.set_active_version(task_id, version=1)

        # Create runtime
        app = create_app(data_dir=config.runtime.data_dir, task_id=task_id)
        client = TestClient(app)

        # Execute
        response = client.post("/execute", json={"input": {"text": "Test"}})
        assert response.status_code == 200

    def test_metrics_integration(self, temp_data_dir, sample_artifact_v1):
        """Test metrics endpoint integration with dataset analytics."""
        task_id = "metrics-test"

        # Setup
        cache = ArtifactCache(data_dir=str(temp_data_dir))
        cache.store_artifact(task_id, sample_artifact_v1)
        cache.set_active_version(task_id, version=1)

        app = create_app(data_dir=str(temp_data_dir), task_id=task_id)
        client = TestClient(app)

        # Execute multiple times
        for i in range(5):
            response = client.post(
                "/execute",
                json={"input": {"text": f"Message {i}"}},
            )
            assert response.status_code == 200

        # Get metrics
        response = client.get("/metrics")
        assert response.status_code == 200

        data = response.json()
        assert data["executions_today"] == 5
        assert data["avg_latency_ms"] > 0


class TestHealthAndMonitoring:
    """Test health checks and monitoring."""

    def test_health_endpoint_detailed(self, temp_data_dir, sample_artifact_v1):
        """Test health endpoint provides correct information."""
        task_id = "health-test"

        # Setup
        cache = ArtifactCache(data_dir=str(temp_data_dir))
        cache.store_artifact(task_id, sample_artifact_v1)
        cache.set_active_version(task_id, version=1)

        app = create_app(data_dir=str(temp_data_dir), task_id=task_id)
        client = TestClient(app)

        # Check health
        response = client.get("/health")
        assert response.status_code == 200

        data = response.json()
        assert data["status"] == "healthy"
        assert data["version"] == "0.1.0"
        assert data["task_id"] == task_id
        assert data["active_version"] == 1

    def test_health_check_availability(self, temp_data_dir, sample_artifact_v1):
        """Test health check remains available during load."""
        task_id = "availability-test"

        # Setup
        cache = ArtifactCache(data_dir=str(temp_data_dir))
        cache.store_artifact(task_id, sample_artifact_v1)
        cache.set_active_version(task_id, version=1)

        app = create_app(data_dir=str(temp_data_dir), task_id=task_id)
        client = TestClient(app)

        # Execute requests while checking health
        for i in range(10):
            # Execute
            exec_response = client.post(
                "/execute",
                json={"input": {"text": f"Test {i}"}},
            )
            assert exec_response.status_code == 200

            # Health check should always work
            health_response = client.get("/health")
            assert health_response.status_code == 200
            assert health_response.json()["status"] == "healthy"


class TestPerformance:
    """Performance and scalability tests."""

    def test_execution_latency(self, temp_data_dir, sample_artifact_v1):
        """Test execution latency meets targets."""
        task_id = "latency-test"

        # Setup
        cache = ArtifactCache(data_dir=str(temp_data_dir))
        cache.store_artifact(task_id, sample_artifact_v1)
        cache.set_active_version(task_id, version=1)

        app = create_app(data_dir=str(temp_data_dir), task_id=task_id)
        client = TestClient(app)

        # Execute and measure
        latencies = []
        for i in range(10):
            response = client.post(
                "/execute",
                json={"input": {"text": f"Test {i}"}},
            )
            assert response.status_code == 200
            latencies.append(response.json()["latency_ms"])

        # Performance assertions
        avg_latency = sum(latencies) / len(latencies)
        max_latency = max(latencies)

        assert avg_latency < 10  # Average < 10ms (Phase 0 target)
        assert max_latency < 100  # Max < 100ms

    def test_logging_performance(self, temp_data_dir, sample_artifact_v1):
        """Test JSONL logging performance."""
        task_id = "logging-perf-test"

        # Setup
        cache = ArtifactCache(data_dir=str(temp_data_dir))
        cache.store_artifact(task_id, sample_artifact_v1)
        cache.set_active_version(task_id, version=1)

        app = create_app(data_dir=str(temp_data_dir), task_id=task_id)
        client = TestClient(app)

        # Execute many requests to test logging performance
        num_requests = 100
        start_time = time.time()

        for i in range(num_requests):
            response = client.post(
                "/execute",
                json={"input": {"text": f"Test {i}"}},
            )
            assert response.status_code == 200

        total_time = time.time() - start_time

        # Logging should not significantly impact performance
        avg_time = total_time / num_requests
        assert avg_time < 0.05  # Less than 50ms per request including logging
