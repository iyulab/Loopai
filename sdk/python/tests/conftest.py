"""Pytest configuration and fixtures."""

import pytest
from httpx import AsyncClient, Response
from httpx_mock import HTTPXMock


@pytest.fixture
def mock_base_url() -> str:
    """Provide mock base URL for testing."""
    return "http://test-api.loopai.dev"


@pytest.fixture
def httpx_mock() -> HTTPXMock:
    """Provide HTTPX mock for testing HTTP requests."""
    return HTTPXMock()
