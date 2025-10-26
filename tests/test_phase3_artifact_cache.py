"""
Phase 3 Artifact Cache Tests

TDD approach for local artifact storage:
- Store artifacts with versioning
- List artifact versions
- Get active artifact via symlink
- Update active version
- Load artifact metadata
"""

import json
import tempfile
from pathlib import Path
from uuid import uuid4

import pytest

from loopai.models import ComplexityMetrics, ProgramArtifact, ProgramStatus, SynthesisStrategy


class TestArtifactCache:
    """Test suite for Artifact Cache (local artifact storage)."""

    @pytest.fixture
    def temp_data_dir(self):
        """Create temporary data directory for testing."""
        with tempfile.TemporaryDirectory() as tmpdir:
            yield Path(tmpdir)

    @pytest.fixture
    def task_id(self):
        """Generate test task ID."""
        return f"task-{uuid4()}"

    @pytest.fixture
    def sample_artifact(self, task_id):
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

    def test_store_artifact(self, temp_data_dir, task_id, sample_artifact):
        """Test storing artifact with version."""
        from loopai.runtime.artifact_cache import ArtifactCache

        cache = ArtifactCache(data_dir=str(temp_data_dir))

        # Store artifact version 1
        cache.store_artifact(task_id, sample_artifact)

        # Verify directory structure
        artifact_dir = temp_data_dir / "artifacts" / task_id / "v1"
        assert artifact_dir.exists()

        # Verify program file
        program_file = artifact_dir / "program.py"
        assert program_file.exists()

        with open(program_file, "r") as f:
            code = f.read()
            assert "def classify" in code
            assert "buy" in code

        # Verify metadata file
        metadata_file = artifact_dir / "metadata.json"
        assert metadata_file.exists()

        with open(metadata_file, "r") as f:
            metadata = json.load(f)
            assert metadata["version"] == 1
            assert metadata["language"] == "python"
            assert metadata["synthesis_strategy"] == "rule"

    def test_list_artifact_versions(self, temp_data_dir, task_id, sample_artifact):
        """Test listing all artifact versions."""
        from loopai.runtime.artifact_cache import ArtifactCache

        cache = ArtifactCache(data_dir=str(temp_data_dir))

        # Store 3 versions
        for version in [1, 2, 3]:
            artifact = sample_artifact.model_copy(update={"version": version})
            cache.store_artifact(task_id, artifact)

        # List versions
        versions = cache.list_versions(task_id)

        assert len(versions) == 3
        assert 1 in versions
        assert 2 in versions
        assert 3 in versions

    def test_get_active_artifact(self, temp_data_dir, task_id, sample_artifact):
        """Test getting active artifact via symlink."""
        from loopai.runtime.artifact_cache import ArtifactCache

        cache = ArtifactCache(data_dir=str(temp_data_dir))

        # Store version 1
        cache.store_artifact(task_id, sample_artifact)

        # Set as active
        cache.set_active_version(task_id, version=1)

        # Get active artifact
        artifact = cache.get_active_artifact(task_id)

        assert artifact is not None
        assert artifact.version == 1
        assert "def classify" in artifact.code

    def test_update_active_version(self, temp_data_dir, task_id, sample_artifact):
        """Test updating active version (change symlink)."""
        from loopai.runtime.artifact_cache import ArtifactCache

        cache = ArtifactCache(data_dir=str(temp_data_dir))

        # Store versions 1 and 2
        artifact_v1 = sample_artifact.model_copy(update={"version": 1})
        artifact_v2 = sample_artifact.model_copy(
            update={
                "version": 2,
                "code": 'def classify(text: str) -> str:\n    return "spam"  # Always spam',
            }
        )

        cache.store_artifact(task_id, artifact_v1)
        cache.store_artifact(task_id, artifact_v2)

        # Set v1 as active
        cache.set_active_version(task_id, version=1)
        artifact = cache.get_active_artifact(task_id)
        assert artifact.version == 1
        assert "buy" in artifact.code

        # Update to v2
        cache.set_active_version(task_id, version=2)
        artifact = cache.get_active_artifact(task_id)
        assert artifact.version == 2
        assert "Always spam" in artifact.code

    def test_load_artifact_metadata(self, temp_data_dir, task_id, sample_artifact):
        """Test loading artifact metadata without loading code."""
        from loopai.runtime.artifact_cache import ArtifactCache

        cache = ArtifactCache(data_dir=str(temp_data_dir))

        # Store artifact
        cache.store_artifact(task_id, sample_artifact)

        # Load metadata only
        metadata = cache.load_metadata(task_id, version=1)

        assert metadata["version"] == 1
        assert metadata["language"] == "python"
        assert metadata["generation_cost"] == 0.15
        assert metadata["complexity_metrics"]["lines_of_code"] == 2

    def test_get_artifact_by_version(self, temp_data_dir, task_id, sample_artifact):
        """Test getting specific artifact version."""
        from loopai.runtime.artifact_cache import ArtifactCache

        cache = ArtifactCache(data_dir=str(temp_data_dir))

        # Store versions 1, 2, 3
        for version in [1, 2, 3]:
            artifact = sample_artifact.model_copy(
                update={
                    "version": version,
                    "code": f'def classify(text: str) -> str:\n    return "v{version}"',
                }
            )
            cache.store_artifact(task_id, artifact)

        # Get version 2 specifically
        artifact = cache.get_artifact(task_id, version=2)

        assert artifact.version == 2
        assert "v2" in artifact.code

    def test_artifact_exists(self, temp_data_dir, task_id, sample_artifact):
        """Test checking if artifact version exists."""
        from loopai.runtime.artifact_cache import ArtifactCache

        cache = ArtifactCache(data_dir=str(temp_data_dir))

        # Store version 1
        cache.store_artifact(task_id, sample_artifact)

        # Check existence
        assert cache.artifact_exists(task_id, version=1) is True
        assert cache.artifact_exists(task_id, version=2) is False
        assert cache.artifact_exists("nonexistent-task", version=1) is False


class TestArtifactCacheEdgeCases:
    """Test edge cases and error handling."""

    @pytest.fixture
    def temp_data_dir(self):
        """Create temporary data directory for testing."""
        with tempfile.TemporaryDirectory() as tmpdir:
            yield Path(tmpdir)

    def test_get_active_artifact_without_active_symlink(self, temp_data_dir):
        """Test getting active artifact when no active version is set."""
        from loopai.runtime.artifact_cache import ArtifactCache

        cache = ArtifactCache(data_dir=str(temp_data_dir))
        task_id = "test-task"

        # Get active artifact without setting it
        artifact = cache.get_active_artifact(task_id)

        # Should return None or raise appropriate error
        assert artifact is None

    def test_list_versions_for_nonexistent_task(self, temp_data_dir):
        """Test listing versions for task with no artifacts."""
        from loopai.runtime.artifact_cache import ArtifactCache

        cache = ArtifactCache(data_dir=str(temp_data_dir))

        versions = cache.list_versions("nonexistent-task")

        assert versions == []

    def test_set_active_version_nonexistent(self, temp_data_dir):
        """Test setting active version that doesn't exist."""
        from loopai.runtime.artifact_cache import ArtifactCache

        cache = ArtifactCache(data_dir=str(temp_data_dir))
        task_id = "test-task"

        # Should raise error or handle gracefully
        with pytest.raises((FileNotFoundError, ValueError)):
            cache.set_active_version(task_id, version=999)

    def test_overwrite_existing_version(self, temp_data_dir):
        """Test that storing same version overwrites existing artifact."""
        from loopai.runtime.artifact_cache import ArtifactCache
        from loopai.models import ComplexityMetrics, ProgramArtifact, ProgramStatus, SynthesisStrategy
        from uuid import uuid4

        cache = ArtifactCache(data_dir=str(temp_data_dir))
        task_id = "test-task"

        # Store version 1
        artifact_v1_old = ProgramArtifact(
            id=uuid4(),
            task_id=uuid4(),
            version=1,
            language="python",
            code='def classify(text: str) -> str:\n    return "old"',
            synthesis_strategy=SynthesisStrategy.RULE,
            confidence_score=0.9,
            complexity_metrics=ComplexityMetrics(
                lines_of_code=2, cyclomatic_complexity=1, estimated_latency_ms=0.5
            ),
            llm_provider="openai",
            llm_model="gpt-4",
            generation_cost=0.1,
            generation_time_sec=5.0,
            status=ProgramStatus.VALIDATED,
            deployment_percentage=0.0,
        )

        cache.store_artifact(task_id, artifact_v1_old)

        # Overwrite with new version 1
        artifact_v1_new = artifact_v1_old.model_copy(
            update={"code": 'def classify(text: str) -> str:\n    return "new"'}
        )

        cache.store_artifact(task_id, artifact_v1_new)

        # Load and verify it was overwritten
        artifact = cache.get_artifact(task_id, version=1)
        assert "new" in artifact.code
        assert "old" not in artifact.code
