"""
Phase 3 Configuration Manager Tests

TDD approach for YAML-based configuration management:
- Load configuration from YAML
- Environment variable substitution
- Configuration validation
- Default values
"""

import os
import tempfile
from pathlib import Path

import pytest
import yaml


class TestConfigurationManager:
    """Test suite for Configuration Manager (YAML-based config)."""

    @pytest.fixture
    def temp_config_dir(self):
        """Create temporary directory for config files."""
        with tempfile.TemporaryDirectory() as tmpdir:
            yield Path(tmpdir)

    @pytest.fixture
    def sample_config(self):
        """Sample configuration dictionary."""
        return {
            "runtime": {
                "mode": "edge",
                "data_dir": "/loopai-data",
            },
            "execution": {
                "worker_count": 4,
                "timeout_ms": 1000,
            },
            "sampling": {
                "strategy": "random",
                "rate": 0.05,
            },
            "storage": {
                "retention_days": 7,
                "max_size_gb": 100,
            },
        }

    def test_load_yaml_config(self, temp_config_dir, sample_config):
        """Test loading configuration from YAML file."""
        from loopai.runtime.config_manager import ConfigurationManager

        # Write config to YAML file
        config_file = temp_config_dir / "deployment.yaml"
        with open(config_file, "w") as f:
            yaml.dump(sample_config, f)

        # Load configuration
        manager = ConfigurationManager(config_path=str(config_file))
        config = manager.load_config()

        # Verify loaded config
        assert config.runtime.mode == "edge"
        assert config.runtime.data_dir == "/loopai-data"
        assert config.execution.worker_count == 4
        assert config.execution.timeout_ms == 1000
        assert config.sampling.strategy == "random"
        assert config.sampling.rate == 0.05

    def test_env_var_substitution(self, temp_config_dir):
        """Test environment variable substitution in config."""
        from loopai.runtime.config_manager import ConfigurationManager

        # Set environment variables
        os.environ["TEST_DATA_DIR"] = "/custom/data"
        os.environ["TEST_WORKER_COUNT"] = "8"

        # Config with environment variables
        config_dict = {
            "runtime": {
                "mode": "edge",
                "data_dir": "${TEST_DATA_DIR}",
            },
            "execution": {
                "worker_count": "${TEST_WORKER_COUNT}",
                "timeout_ms": 1000,
            },
            "sampling": {
                "strategy": "random",
                "rate": 0.05,
            },
            "storage": {
                "retention_days": 7,
                "max_size_gb": 100,
            },
        }

        # Write config
        config_file = temp_config_dir / "deployment.yaml"
        with open(config_file, "w") as f:
            yaml.dump(config_dict, f)

        # Load with substitution
        manager = ConfigurationManager(config_path=str(config_file))
        config = manager.load_config()

        # Verify substitution
        assert config.runtime.data_dir == "/custom/data"
        assert config.execution.worker_count == 8

        # Cleanup
        del os.environ["TEST_DATA_DIR"]
        del os.environ["TEST_WORKER_COUNT"]

    def test_config_validation(self, temp_config_dir):
        """Test configuration validation with Pydantic."""
        from loopai.runtime.config_manager import ConfigurationManager

        # Valid config
        valid_config = {
            "runtime": {
                "mode": "edge",
                "data_dir": "/loopai-data",
            },
            "execution": {
                "worker_count": 4,
                "timeout_ms": 1000,
            },
            "sampling": {
                "strategy": "random",
                "rate": 0.05,
            },
            "storage": {
                "retention_days": 7,
                "max_size_gb": 100,
            },
        }

        config_file = temp_config_dir / "deployment.yaml"
        with open(config_file, "w") as f:
            yaml.dump(valid_config, f)

        # Should load successfully
        manager = ConfigurationManager(config_path=str(config_file))
        config = manager.load_config()
        assert config is not None

    def test_invalid_config_error(self, temp_config_dir):
        """Test that invalid configuration raises validation error."""
        from loopai.runtime.config_manager import ConfigurationManager
        from pydantic import ValidationError

        # Invalid config (negative worker_count)
        invalid_config = {
            "runtime": {
                "mode": "edge",
                "data_dir": "/loopai-data",
            },
            "execution": {
                "worker_count": -1,  # Invalid: must be positive
                "timeout_ms": 1000,
            },
            "sampling": {
                "strategy": "random",
                "rate": 0.05,
            },
            "storage": {
                "retention_days": 7,
                "max_size_gb": 100,
            },
        }

        config_file = temp_config_dir / "deployment.yaml"
        with open(config_file, "w") as f:
            yaml.dump(invalid_config, f)

        # Should raise validation error
        manager = ConfigurationManager(config_path=str(config_file))
        with pytest.raises(ValidationError):
            manager.load_config()

    def test_default_values(self, temp_config_dir):
        """Test that default values are applied for missing fields."""
        from loopai.runtime.config_manager import ConfigurationManager

        # Minimal config (only required fields)
        minimal_config = {
            "runtime": {
                "mode": "edge",
                "data_dir": "/loopai-data",
            },
        }

        config_file = temp_config_dir / "deployment.yaml"
        with open(config_file, "w") as f:
            yaml.dump(minimal_config, f)

        # Load config
        manager = ConfigurationManager(config_path=str(config_file))
        config = manager.load_config()

        # Verify defaults applied
        assert config.execution.worker_count == 4  # Default
        assert config.execution.timeout_ms == 1000  # Default
        assert config.sampling.strategy == "random"  # Default
        assert config.sampling.rate == 0.05  # Default

    def test_missing_required_field_error(self, temp_config_dir):
        """Test that missing required field raises validation error."""
        from loopai.runtime.config_manager import ConfigurationManager
        from pydantic import ValidationError

        # Config missing required 'mode' field
        incomplete_config = {
            "runtime": {
                "data_dir": "/loopai-data",
                # mode is missing
            },
        }

        config_file = temp_config_dir / "deployment.yaml"
        with open(config_file, "w") as f:
            yaml.dump(incomplete_config, f)

        # Should raise validation error
        manager = ConfigurationManager(config_path=str(config_file))
        with pytest.raises(ValidationError):
            manager.load_config()

    def test_config_to_dict(self, temp_config_dir, sample_config):
        """Test converting config back to dictionary."""
        from loopai.runtime.config_manager import ConfigurationManager

        # Write config
        config_file = temp_config_dir / "deployment.yaml"
        with open(config_file, "w") as f:
            yaml.dump(sample_config, f)

        # Load and convert to dict
        manager = ConfigurationManager(config_path=str(config_file))
        config = manager.load_config()
        config_dict = config.model_dump()

        # Verify structure preserved
        assert config_dict["runtime"]["mode"] == "edge"
        assert config_dict["execution"]["worker_count"] == 4
        assert config_dict["sampling"]["rate"] == 0.05


class TestConfigurationModels:
    """Test Pydantic configuration models."""

    def test_runtime_config_model(self):
        """Test RuntimeConfig Pydantic model."""
        from loopai.runtime.config_manager import RuntimeConfig

        config = RuntimeConfig(mode="edge", data_dir="/data")
        assert config.mode == "edge"
        assert config.data_dir == "/data"

    def test_execution_config_model(self):
        """Test ExecutionConfig Pydantic model with defaults."""
        from loopai.runtime.config_manager import ExecutionConfig

        # With defaults
        config = ExecutionConfig()
        assert config.worker_count == 4
        assert config.timeout_ms == 1000

        # Custom values
        config = ExecutionConfig(worker_count=8, timeout_ms=2000)
        assert config.worker_count == 8
        assert config.timeout_ms == 2000

    def test_sampling_config_model(self):
        """Test SamplingConfig Pydantic model."""
        from loopai.runtime.config_manager import SamplingConfig

        config = SamplingConfig(strategy="random", rate=0.1)
        assert config.strategy == "random"
        assert config.rate == 0.1

        # Test validation (rate must be 0-1)
        from pydantic import ValidationError

        with pytest.raises(ValidationError):
            SamplingConfig(strategy="random", rate=1.5)  # Invalid: >1

    def test_deployment_config_model(self):
        """Test complete DeploymentConfig model."""
        from loopai.runtime.config_manager import DeploymentConfig

        config = DeploymentConfig(
            runtime={"mode": "edge", "data_dir": "/data"},
            execution={"worker_count": 4, "timeout_ms": 1000},
            sampling={"strategy": "random", "rate": 0.05},
            storage={"retention_days": 7, "max_size_gb": 100},
        )

        assert config.runtime.mode == "edge"
        assert config.execution.worker_count == 4
        assert config.sampling.rate == 0.05
        assert config.storage.retention_days == 7
