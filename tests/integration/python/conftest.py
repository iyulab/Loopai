"""
pytest configuration for Python SDK integration tests
"""
import asyncio
import json
from pathlib import Path
from typing import AsyncGenerator, Dict, Any

import pytest
import httpx


@pytest.fixture(scope="session")
def test_config() -> Dict[str, Any]:
    """Load test configuration."""
    config_path = Path(__file__).parent.parent / "test-config.json"
    with open(config_path, "r") as f:
        return json.load(f)


@pytest.fixture(scope="session")
def base_url(test_config: Dict[str, Any]) -> str:
    """Get base URL from config."""
    return test_config["baseUrl"]


@pytest.fixture(scope="session")
def event_loop():
    """Create event loop for async tests."""
    loop = asyncio.get_event_loop_policy().new_event_loop()
    yield loop
    loop.close()


@pytest.fixture
async def http_client(base_url: str) -> AsyncGenerator[httpx.AsyncClient, None]:
    """Create HTTP client for API requests."""
    async with httpx.AsyncClient(base_url=base_url, timeout=30.0) as client:
        yield client


@pytest.fixture
async def wait_for_api(http_client: httpx.AsyncClient) -> None:
    """Wait for API to be ready."""
    max_attempts = 30
    for attempt in range(max_attempts):
        try:
            response = await http_client.get("/health")
            if response.status_code == 200:
                return
        except (httpx.ConnectError, httpx.TimeoutException):
            if attempt == max_attempts - 1:
                raise
            await asyncio.sleep(1)
