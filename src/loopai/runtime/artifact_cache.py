"""
Artifact Cache: Local storage for program artifacts with version management.

Manages local copies of program artifacts with symlink-based active version.
"""

import json
import os
from pathlib import Path
from typing import Dict, List, Optional

from loopai.models import ProgramArtifact


class ArtifactCache:
    """
    Manage local artifact storage with versioning.

    Features:
    - Store artifacts by version
    - Symlink to active version (or marker file on Windows)
    - Version listing
    - Metadata loading
    """

    def __init__(self, data_dir: str):
        """
        Initialize Artifact Cache.

        Args:
            data_dir: Root directory for all data (/loopai-data)
        """
        self.data_dir = Path(data_dir)
        self.artifacts_dir = self.data_dir / "artifacts"

    def store_artifact(self, task_id: str, artifact: ProgramArtifact) -> None:
        """
        Store program artifact with version.

        Directory structure:
        /artifacts/{task_id}/v{version}/
        ├── program.py      (executable code)
        └── metadata.json   (artifact metadata)

        Args:
            task_id: Task identifier
            artifact: Program artifact to store
        """
        # Create version directory
        version_dir = self.artifacts_dir / task_id / f"v{artifact.version}"
        version_dir.mkdir(parents=True, exist_ok=True)

        # Save program code
        program_file = version_dir / "program.py"
        with open(program_file, "w", encoding="utf-8") as f:
            f.write(artifact.code)

        # Save metadata (exclude code to save space)
        metadata = artifact.model_dump(mode="json", exclude={"code"})
        metadata_file = version_dir / "metadata.json"
        with open(metadata_file, "w", encoding="utf-8") as f:
            json.dump(metadata, f, indent=2)

    def list_versions(self, task_id: str) -> List[int]:
        """
        List all artifact versions for a task.

        Args:
            task_id: Task identifier

        Returns:
            List of version numbers (e.g., [1, 2, 3])
        """
        task_dir = self.artifacts_dir / task_id

        if not task_dir.exists():
            return []

        versions = []
        for version_dir in task_dir.iterdir():
            if version_dir.is_dir() and version_dir.name.startswith("v"):
                try:
                    version = int(version_dir.name[1:])  # Extract number from "v1"
                    versions.append(version)
                except ValueError:
                    # Skip non-numeric version directories
                    continue

        return sorted(versions)

    def get_active_artifact(self, task_id: str) -> Optional[ProgramArtifact]:
        """
        Get active artifact version via symlink or marker file.

        Args:
            task_id: Task identifier

        Returns:
            Active program artifact, or None if not set
        """
        task_dir = self.artifacts_dir / task_id

        if not task_dir.exists():
            return None

        # Try symlink first (Unix/Linux)
        active_link = task_dir / "active"
        if active_link.exists() and active_link.is_symlink():
            target = active_link.resolve()
            version = int(target.name[1:])  # Extract from "v1"
            return self.get_artifact(task_id, version)

        # Fallback: marker file (Windows-friendly)
        marker_file = task_dir / ".active_version"
        if marker_file.exists():
            with open(marker_file, "r") as f:
                version = int(f.read().strip())
                return self.get_artifact(task_id, version)

        return None

    def set_active_version(self, task_id: str, version: int) -> None:
        """
        Set active artifact version using symlink or marker file.

        Args:
            task_id: Task identifier
            version: Version number to set as active

        Raises:
            FileNotFoundError: If version doesn't exist
        """
        task_dir = self.artifacts_dir / task_id
        version_dir = task_dir / f"v{version}"

        if not version_dir.exists():
            raise FileNotFoundError(
                f"Artifact version {version} not found for task {task_id}"
            )

        # Try to create symlink (may fail on Windows without admin)
        active_link = task_dir / "active"

        try:
            # Remove existing symlink if present
            if active_link.exists() or active_link.is_symlink():
                active_link.unlink()

            # Create new symlink
            os.symlink(version_dir, active_link, target_is_directory=True)

        except (OSError, NotImplementedError):
            # Fallback: use marker file on Windows
            marker_file = task_dir / ".active_version"
            with open(marker_file, "w") as f:
                f.write(str(version))

    def get_artifact(self, task_id: str, version: int) -> Optional[ProgramArtifact]:
        """
        Get specific artifact version.

        Args:
            task_id: Task identifier
            version: Version number

        Returns:
            Program artifact, or None if not found
        """
        version_dir = self.artifacts_dir / task_id / f"v{version}"

        if not version_dir.exists():
            return None

        # Load metadata
        metadata_file = version_dir / "metadata.json"
        with open(metadata_file, "r", encoding="utf-8") as f:
            metadata = json.load(f)

        # Load code
        program_file = version_dir / "program.py"
        with open(program_file, "r", encoding="utf-8") as f:
            code = f.read()

        # Reconstruct artifact
        metadata["code"] = code
        artifact = ProgramArtifact(**metadata)

        return artifact

    def load_metadata(self, task_id: str, version: int) -> Optional[Dict]:
        """
        Load artifact metadata without loading code.

        Useful for listing artifacts or checking properties without
        loading full program code.

        Args:
            task_id: Task identifier
            version: Version number

        Returns:
            Metadata dictionary, or None if not found
        """
        metadata_file = (
            self.artifacts_dir / task_id / f"v{version}" / "metadata.json"
        )

        if not metadata_file.exists():
            return None

        with open(metadata_file, "r", encoding="utf-8") as f:
            metadata = json.load(f)

        return metadata

    def artifact_exists(self, task_id: str, version: int) -> bool:
        """
        Check if artifact version exists.

        Args:
            task_id: Task identifier
            version: Version number

        Returns:
            True if artifact exists, False otherwise
        """
        version_dir = self.artifacts_dir / task_id / f"v{version}"
        return version_dir.exists()
