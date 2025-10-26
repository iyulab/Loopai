"""
Dataset Manager: File system-based dataset storage with JSONL logging.

Manages local storage of execution logs, validation results, and analytics.
"""

import json
import statistics
from datetime import datetime, timedelta
from pathlib import Path
from typing import Dict, List, Optional

from loopai.models import ExecutionRecord


class DatasetManager:
    """
    Manage file system-based dataset storage.

    Features:
    - JSONL execution logging (append-only)
    - Daily log rotation
    - Local analytics generation
    - Retention policy enforcement
    """

    def __init__(self, data_dir: str, retention_days: int = 7):
        """
        Initialize Dataset Manager.

        Args:
            data_dir: Root directory for all data (/loopai-data)
            retention_days: Number of days to keep execution logs
        """
        self.data_dir = Path(data_dir)
        self.retention_days = retention_days

    def initialize_task(self, task_id: str) -> None:
        """
        Create directory structure for a task.

        Structure:
        /loopai-data/
        └── datasets/
            └── {task_id}/
                ├── executions/     (daily JSONL logs)
                ├── validations/    (sampled validation results)
                └── analytics/      (daily statistics)

        Args:
            task_id: Unique task identifier
        """
        task_dir = self.data_dir / "datasets" / task_id

        # Create subdirectories
        (task_dir / "executions").mkdir(parents=True, exist_ok=True)
        (task_dir / "validations").mkdir(parents=True, exist_ok=True)
        (task_dir / "analytics").mkdir(parents=True, exist_ok=True)

    def log_execution(self, task_id: str, execution: ExecutionRecord) -> None:
        """
        Log execution record to daily JSONL file.

        File format: /datasets/{task_id}/executions/YYYY-MM-DD.jsonl
        Each line is a JSON object with execution data.

        Args:
            task_id: Task identifier
            execution: Execution record to log
        """
        # Get today's log file
        today = datetime.now().strftime("%Y-%m-%d")
        log_file = (
            self.data_dir / "datasets" / task_id / "executions" / f"{today}.jsonl"
        )

        # Ensure directory exists
        log_file.parent.mkdir(parents=True, exist_ok=True)

        # Convert execution to dict and append to JSONL
        execution_dict = execution.model_dump(mode="json")

        with open(log_file, "a", encoding="utf-8") as f:
            f.write(json.dumps(execution_dict) + "\n")

    def read_execution_logs(
        self, task_id: str, date: str
    ) -> List[Dict]:
        """
        Read execution logs for a specific date.

        Args:
            task_id: Task identifier
            date: Date in YYYY-MM-DD format

        Returns:
            List of execution records as dictionaries
        """
        log_file = (
            self.data_dir / "datasets" / task_id / "executions" / f"{date}.jsonl"
        )

        if not log_file.exists():
            return []

        executions = []
        with open(log_file, "r", encoding="utf-8") as f:
            for line in f:
                if line.strip():
                    executions.append(json.loads(line))

        return executions

    def generate_daily_analytics(
        self, task_id: str, date: Optional[str] = None
    ) -> Dict:
        """
        Generate daily analytics from execution logs.

        Analytics include:
        - Total executions
        - Success/failure counts
        - Latency statistics (avg, p50, p99)
        - Sampling rate

        Args:
            task_id: Task identifier
            date: Date in YYYY-MM-DD format (defaults to today)

        Returns:
            Dictionary with analytics metrics
        """
        if date is None:
            date = datetime.now().strftime("%Y-%m-%d")

        # Read execution logs
        executions = self.read_execution_logs(task_id, date)

        if not executions:
            # No data - return zeros
            analytics = {
                "date": date,
                "total_executions": 0,
                "successful_executions": 0,
                "failed_executions": 0,
                "success_rate": 0.0,
                "sampled_count": 0,
                "sampling_rate": 0.0,
                "avg_latency_ms": 0.0,
                "p50_latency_ms": 0.0,
                "p99_latency_ms": 0.0,
            }
        else:
            # Calculate metrics
            total = len(executions)
            successful = sum(1 for e in executions if e["status"] == "success")
            failed = total - successful
            sampled = sum(1 for e in executions if e.get("sampled_for_validation", False))

            # Latency stats (only from successful executions)
            latencies = [
                e["latency_ms"]
                for e in executions
                if e["status"] == "success" and e.get("latency_ms") is not None
            ]

            if latencies:
                avg_latency = statistics.mean(latencies)
                sorted_latencies = sorted(latencies)
                p50_latency = sorted_latencies[int(len(sorted_latencies) * 0.50)]
                p99_latency = sorted_latencies[int(len(sorted_latencies) * 0.99)]
            else:
                avg_latency = p50_latency = p99_latency = 0.0

            analytics = {
                "date": date,
                "total_executions": total,
                "successful_executions": successful,
                "failed_executions": failed,
                "success_rate": successful / total if total > 0 else 0.0,
                "sampled_count": sampled,
                "sampling_rate": sampled / total if total > 0 else 0.0,
                "avg_latency_ms": avg_latency,
                "p50_latency_ms": p50_latency,
                "p99_latency_ms": p99_latency,
            }

        # Save analytics to file
        analytics_file = (
            self.data_dir
            / "datasets"
            / task_id
            / "analytics"
            / f"daily-stats-{date}.json"
        )
        analytics_file.parent.mkdir(parents=True, exist_ok=True)

        with open(analytics_file, "w", encoding="utf-8") as f:
            json.dump(analytics, f, indent=2)

        return analytics

    def apply_retention_policy(self, task_id: str) -> int:
        """
        Delete execution logs older than retention_days.

        Args:
            task_id: Task identifier

        Returns:
            Number of log files deleted
        """
        executions_dir = self.data_dir / "datasets" / task_id / "executions"

        if not executions_dir.exists():
            return 0

        # Calculate cutoff date
        cutoff_date = datetime.now() - timedelta(days=self.retention_days)

        # Find and delete old log files
        deleted_count = 0
        for log_file in executions_dir.glob("*.jsonl"):
            try:
                # Parse date from filename (YYYY-MM-DD.jsonl)
                file_date_str = log_file.stem
                file_date = datetime.strptime(file_date_str, "%Y-%m-%d")

                # Delete if older than retention period
                if file_date < cutoff_date:
                    log_file.unlink()
                    deleted_count += 1
            except (ValueError, OSError):
                # Skip files with invalid date format or deletion errors
                continue

        return deleted_count

    def log_validation(self, task_id: str, validation: Dict) -> None:
        """
        Log validation result to daily JSONL file.

        File format: /datasets/{task_id}/validations/sampled-YYYY-MM-DD.jsonl

        Args:
            task_id: Task identifier
            validation: Validation record as dictionary
        """
        # Get today's validation log file
        today = datetime.now().strftime("%Y-%m-%d")
        validation_file = (
            self.data_dir
            / "datasets"
            / task_id
            / "validations"
            / f"sampled-{today}.jsonl"
        )

        # Ensure directory exists
        validation_file.parent.mkdir(parents=True, exist_ok=True)

        # Append validation record
        with open(validation_file, "a", encoding="utf-8") as f:
            f.write(json.dumps(validation) + "\n")
