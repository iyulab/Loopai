"""
Phase 3 Dataset Manager Tests

TDD approach for file system-based dataset management:
- JSONL execution logging
- Daily log rotation
- Local analytics generation
- Retention policy enforcement
"""

import json
import os
import tempfile
from datetime import datetime, timedelta
from pathlib import Path
from typing import List
from uuid import uuid4

import pytest

from loopai.models import ExecutionRecord, ExecutionStatus


class TestDatasetManager:
    """Test suite for Dataset Manager (file system storage)."""

    @pytest.fixture
    def temp_data_dir(self):
        """Create temporary data directory for testing."""
        with tempfile.TemporaryDirectory() as tmpdir:
            yield Path(tmpdir)

    @pytest.fixture
    def task_id(self):
        """Generate test task ID."""
        return f"task-{uuid4()}"

    def test_create_directory_structure(self, temp_data_dir, task_id):
        """Test that Dataset Manager creates correct directory structure."""
        from loopai.runtime.dataset_manager import DatasetManager

        manager = DatasetManager(data_dir=str(temp_data_dir))
        manager.initialize_task(task_id)

        # Verify directory structure
        task_dir = temp_data_dir / "datasets" / task_id
        assert task_dir.exists()
        assert (task_dir / "executions").exists()
        assert (task_dir / "validations").exists()
        assert (task_dir / "analytics").exists()

    def test_log_execution(self, temp_data_dir, task_id):
        """Test logging single execution record to JSONL."""
        from loopai.runtime.dataset_manager import DatasetManager

        manager = DatasetManager(data_dir=str(temp_data_dir))
        manager.initialize_task(task_id)

        # Create execution record
        execution = ExecutionRecord(
            id=uuid4(),
            program_id=uuid4(),
            task_id=uuid4(),
            input_data={"text": "Buy now!"},
            output_data={"result": "spam"},
            latency_ms=5.2,
            status=ExecutionStatus.SUCCESS,
            sampled_for_validation=False,
            executed_at=datetime.now(),
        )

        # Log execution
        manager.log_execution(task_id, execution)

        # Verify JSONL file created
        today = datetime.now().strftime("%Y-%m-%d")
        log_file = (
            temp_data_dir / "datasets" / task_id / "executions" / f"{today}.jsonl"
        )
        assert log_file.exists()

        # Verify content
        with open(log_file, "r") as f:
            line = f.readline()
            data = json.loads(line)
            assert data["id"] == str(execution.id)
            assert data["input_data"] == {"text": "Buy now!"}
            assert data["output_data"] == {"result": "spam"}
            assert data["latency_ms"] == 5.2

    def test_append_multiple_executions(self, temp_data_dir, task_id):
        """Test appending multiple execution records to same JSONL file."""
        from loopai.runtime.dataset_manager import DatasetManager

        manager = DatasetManager(data_dir=str(temp_data_dir))
        manager.initialize_task(task_id)

        # Log 10 executions
        execution_ids = []
        for i in range(10):
            execution = ExecutionRecord(
                id=uuid4(),
                program_id=uuid4(),
                task_id=uuid4(),
                input_data={"text": f"test {i}"},
                output_data={"result": "spam" if i % 2 == 0 else "ham"},
                latency_ms=5.0 + i * 0.1,
                status=ExecutionStatus.SUCCESS,
                sampled_for_validation=False,
                executed_at=datetime.now(),
            )
            execution_ids.append(execution.id)
            manager.log_execution(task_id, execution)

        # Verify all 10 records in file
        today = datetime.now().strftime("%Y-%m-%d")
        log_file = (
            temp_data_dir / "datasets" / task_id / "executions" / f"{today}.jsonl"
        )

        with open(log_file, "r") as f:
            lines = f.readlines()
            assert len(lines) == 10

            # Verify IDs match
            for i, line in enumerate(lines):
                data = json.loads(line)
                assert data["id"] == str(execution_ids[i])

    def test_read_execution_logs(self, temp_data_dir, task_id):
        """Test reading execution logs from JSONL file."""
        from loopai.runtime.dataset_manager import DatasetManager

        manager = DatasetManager(data_dir=str(temp_data_dir))
        manager.initialize_task(task_id)

        # Log 5 executions
        for i in range(5):
            execution = ExecutionRecord(
                id=uuid4(),
                program_id=uuid4(),
                task_id=uuid4(),
                input_data={"text": f"test {i}"},
                output_data={"result": "spam"},
                latency_ms=5.0,
                status=ExecutionStatus.SUCCESS,
                sampled_for_validation=False,
                executed_at=datetime.now(),
            )
            manager.log_execution(task_id, execution)

        # Read logs
        today = datetime.now().strftime("%Y-%m-%d")
        executions = manager.read_execution_logs(task_id, date=today)

        assert len(executions) == 5
        assert all(isinstance(e, dict) for e in executions)
        assert all(e["output_data"]["result"] == "spam" for e in executions)

    def test_generate_daily_analytics(self, temp_data_dir, task_id):
        """Test generating daily analytics from execution logs."""
        from loopai.runtime.dataset_manager import DatasetManager

        manager = DatasetManager(data_dir=str(temp_data_dir))
        manager.initialize_task(task_id)

        # Log 100 executions with varying latencies
        for i in range(100):
            execution = ExecutionRecord(
                id=uuid4(),
                program_id=uuid4(),
                task_id=uuid4(),
                input_data={"text": f"test {i}"},
                output_data={"result": "spam" if i % 3 == 0 else "ham"},
                latency_ms=3.0 + (i % 10) * 0.5,  # Range: 3.0-8.0ms
                status=ExecutionStatus.SUCCESS if i < 95 else ExecutionStatus.ERROR,
                sampled_for_validation=i % 10 == 0,  # 10% sampled
                executed_at=datetime.now(),
            )
            manager.log_execution(task_id, execution)

        # Generate analytics
        today = datetime.now().strftime("%Y-%m-%d")
        analytics = manager.generate_daily_analytics(task_id, date=today)

        # Verify analytics
        assert analytics["total_executions"] == 100
        assert analytics["successful_executions"] == 95
        assert analytics["failed_executions"] == 5
        assert analytics["success_rate"] == 0.95
        assert analytics["sampled_count"] == 10
        assert analytics["sampling_rate"] == 0.1

        # Latency stats
        assert "avg_latency_ms" in analytics
        assert "p50_latency_ms" in analytics
        assert "p99_latency_ms" in analytics
        assert 3.0 <= analytics["avg_latency_ms"] <= 8.0

        # Verify analytics file created
        analytics_file = (
            temp_data_dir
            / "datasets"
            / task_id
            / "analytics"
            / f"daily-stats-{today}.json"
        )
        assert analytics_file.exists()

    def test_retention_policy(self, temp_data_dir, task_id):
        """Test retention policy deletes old execution logs."""
        from loopai.runtime.dataset_manager import DatasetManager

        manager = DatasetManager(data_dir=str(temp_data_dir), retention_days=7)
        manager.initialize_task(task_id)

        # Create log files for last 10 days
        executions_dir = temp_data_dir / "datasets" / task_id / "executions"
        for i in range(10):
            date = (datetime.now() - timedelta(days=i)).strftime("%Y-%m-%d")
            log_file = executions_dir / f"{date}.jsonl"

            # Create dummy log file
            with open(log_file, "w") as f:
                f.write('{"test": true}\n')

        # Verify 10 files exist
        log_files = list(executions_dir.glob("*.jsonl"))
        assert len(log_files) == 10

        # Apply retention policy (keep 7 days)
        manager.apply_retention_policy(task_id)

        # Verify only 7 files remain
        log_files = list(executions_dir.glob("*.jsonl"))
        assert len(log_files) == 7

        # Verify oldest files deleted
        remaining_dates = [f.stem for f in log_files]
        for i in range(7):
            expected_date = (datetime.now() - timedelta(days=i)).strftime("%Y-%m-%d")
            assert expected_date in remaining_dates

    def test_log_validation_result(self, temp_data_dir, task_id):
        """Test logging validation results to separate JSONL file."""
        from loopai.runtime.dataset_manager import DatasetManager

        manager = DatasetManager(data_dir=str(temp_data_dir))
        manager.initialize_task(task_id)

        # Create validation record (dictionary for now)
        validation = {
            "execution_id": str(uuid4()),
            "oracle_output": {"result": "spam"},
            "program_output": {"result": "spam"},
            "match": True,
            "oracle_cost": 0.002,
            "oracle_latency_ms": 2500.0,
            "validated_at": datetime.now().isoformat(),
        }

        # Log validation
        manager.log_validation(task_id, validation)

        # Verify validation JSONL file created
        today = datetime.now().strftime("%Y-%m-%d")
        validation_file = (
            temp_data_dir
            / "datasets"
            / task_id
            / "validations"
            / f"sampled-{today}.jsonl"
        )
        assert validation_file.exists()

        # Verify content
        with open(validation_file, "r") as f:
            line = f.readline()
            data = json.loads(line)
            assert data["match"] is True
            assert data["oracle_cost"] == 0.002


class TestDatasetManagerEdgeCases:
    """Test edge cases and error handling."""

    @pytest.fixture
    def temp_data_dir(self):
        """Create temporary data directory for testing."""
        with tempfile.TemporaryDirectory() as tmpdir:
            yield Path(tmpdir)

    def test_initialize_task_idempotent(self, temp_data_dir):
        """Test that initialize_task can be called multiple times safely."""
        from loopai.runtime.dataset_manager import DatasetManager

        manager = DatasetManager(data_dir=str(temp_data_dir))
        task_id = "test-task"

        # Initialize twice
        manager.initialize_task(task_id)
        manager.initialize_task(task_id)  # Should not error

        # Verify structure exists only once
        task_dir = temp_data_dir / "datasets" / task_id
        assert task_dir.exists()

    def test_read_nonexistent_logs(self, temp_data_dir):
        """Test reading logs for date with no executions."""
        from loopai.runtime.dataset_manager import DatasetManager

        manager = DatasetManager(data_dir=str(temp_data_dir))
        task_id = "test-task"
        manager.initialize_task(task_id)

        # Read logs for yesterday (no executions)
        yesterday = (datetime.now() - timedelta(days=1)).strftime("%Y-%m-%d")
        executions = manager.read_execution_logs(task_id, date=yesterday)

        assert executions == []

    def test_analytics_with_no_executions(self, temp_data_dir):
        """Test analytics generation with no execution data."""
        from loopai.runtime.dataset_manager import DatasetManager

        manager = DatasetManager(data_dir=str(temp_data_dir))
        task_id = "test-task"
        manager.initialize_task(task_id)

        # Generate analytics for today (no executions)
        today = datetime.now().strftime("%Y-%m-%d")
        analytics = manager.generate_daily_analytics(task_id, date=today)

        assert analytics["total_executions"] == 0
        assert analytics["successful_executions"] == 0
        assert analytics["avg_latency_ms"] == 0.0
