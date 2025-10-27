#!/bin/bash
# Build Python client package for local installation

echo "========================================"
echo "Loopai Python Client - Package Build"
echo "========================================"
echo

# Check if Python is available
if ! command -v python3 &> /dev/null; then
    echo "Error: Python 3 is not installed or not in PATH"
    exit 1
fi

# Navigate to client directory
cd src/loopai_client

echo "Installing build dependencies..."
python3 -m pip install --upgrade pip build wheel
echo

echo "Building package..."
python3 -m build
echo

# Create dist directory if it doesn't exist
mkdir -p ../../dist

# Copy wheel to dist directory
cp dist/*.whl ../../dist/
cp dist/*.tar.gz ../../dist/
echo

echo "Package built successfully!"
echo
echo "To install locally:"
echo "  pip install dist/loopai_client-0.1.0-py3-none-any.whl"
echo
echo "To install in development mode:"
echo "  cd src/loopai_client"
echo "  pip install -e ."
echo

cd ../..
