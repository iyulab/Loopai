"""
Configuration Manager: YAML-based configuration with environment variable support.

Manages deployment configuration for edge runtime.
"""

import os
import re
from pathlib import Path
from typing import Optional

import yaml
from pydantic import BaseModel, Field, field_validator


class RuntimeConfig(BaseModel):
    """Runtime configuration."""

    mode: str  # edge, central, hybrid
    data_dir: str


class ExecutionConfig(BaseModel):
    """Program execution configuration."""

    worker_count: int = Field(default=4, gt=0)
    timeout_ms: int = Field(default=1000, gt=0)


class SamplingConfig(BaseModel):
    """Sampling strategy configuration."""

    strategy: str = "random"  # random, uncertainty, stratified
    rate: float = Field(default=0.05, ge=0.0, le=1.0)


class StorageConfig(BaseModel):
    """Storage and retention configuration."""

    retention_days: int = Field(default=7, gt=0)
    max_size_gb: int = Field(default=100, gt=0)


class DeploymentConfig(BaseModel):
    """Complete deployment configuration."""

    runtime: RuntimeConfig
    execution: ExecutionConfig = Field(default_factory=ExecutionConfig)
    sampling: SamplingConfig = Field(default_factory=SamplingConfig)
    storage: StorageConfig = Field(default_factory=StorageConfig)


class ConfigurationManager:
    """
    Manage deployment configuration from YAML files.

    Features:
    - Load configuration from YAML
    - Environment variable substitution (${VAR} syntax)
    - Pydantic validation
    - Default values
    """

    def __init__(self, config_path: str):
        """
        Initialize Configuration Manager.

        Args:
            config_path: Path to deployment.yaml file
        """
        self.config_path = Path(config_path)

    def load_config(self) -> DeploymentConfig:
        """
        Load and validate configuration from YAML file.

        Returns:
            DeploymentConfig: Validated configuration object

        Raises:
            FileNotFoundError: If config file doesn't exist
            ValidationError: If config is invalid
        """
        if not self.config_path.exists():
            raise FileNotFoundError(f"Config file not found: {self.config_path}")

        # Load YAML
        with open(self.config_path, "r") as f:
            config_dict = yaml.safe_load(f)

        # Substitute environment variables
        config_dict = self._substitute_env_vars(config_dict)

        # Validate with Pydantic
        config = DeploymentConfig(**config_dict)

        return config

    def _substitute_env_vars(self, config_dict: dict) -> dict:
        """
        Recursively substitute environment variables in config.

        Syntax: ${VARIABLE_NAME}

        Args:
            config_dict: Configuration dictionary

        Returns:
            Dictionary with environment variables substituted
        """
        if isinstance(config_dict, dict):
            return {k: self._substitute_env_vars(v) for k, v in config_dict.items()}
        elif isinstance(config_dict, list):
            return [self._substitute_env_vars(item) for item in config_dict]
        elif isinstance(config_dict, str):
            # Match ${VAR_NAME} pattern
            pattern = r"\$\{([A-Za-z_][A-Za-z0-9_]*)\}"
            matches = re.findall(pattern, config_dict)

            for var_name in matches:
                var_value = os.environ.get(var_name)
                if var_value is not None:
                    config_dict = config_dict.replace(f"${{{var_name}}}", var_value)

            # Try to convert to int if string is all digits
            if config_dict.isdigit():
                return int(config_dict)

            return config_dict
        else:
            return config_dict
