"""
Edge Runtime Entry Point: Launch FastAPI application with environment configuration.
"""

import os
import sys

from loopai.runtime.api import create_app


def main():
    """Main entry point for Edge Runtime."""
    # Get configuration from environment variables
    data_dir = os.getenv("LOOPAI_DATA_DIR", "/loopai-data")
    task_id = os.getenv("LOOPAI_TASK_ID")

    if not task_id:
        print("Error: LOOPAI_TASK_ID environment variable is required", file=sys.stderr)
        sys.exit(1)

    # Create FastAPI app
    app = create_app(data_dir=data_dir, task_id=task_id)

    return app


# For uvicorn to import
app = main()
