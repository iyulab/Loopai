#!/bin/bash
# Script to build and pack Loopai.Client NuGet package locally

echo "========================================"
echo "Loopai.Client Local NuGet Package Build"
echo "========================================"
echo ""

# Build in Release mode
echo "[1/3] Building Loopai.Client in Release mode..."
dotnet build src/Loopai.Client/Loopai.Client.csproj -c Release
if [ $? -ne 0 ]; then
    echo "ERROR: Build failed"
    exit 1
fi
echo ""

# Pack NuGet package
echo "[2/3] Creating NuGet package..."
dotnet pack src/Loopai.Client/Loopai.Client.csproj -c Release -o ./nupkg
if [ $? -ne 0 ]; then
    echo "ERROR: Pack failed"
    exit 1
fi
echo ""

# List created packages
echo "[3/3] Package created successfully:"
ls -1 nupkg/Loopai.Client.*.nupkg
echo ""

echo "========================================"
echo "Package Location: $(pwd)/nupkg"
echo "========================================"
echo ""
echo "To use this package locally:"
echo "1. Add local source: dotnet nuget add source $(pwd)/nupkg --name LoopaiLocal"
echo "2. Install package: dotnet add package Loopai.Client --version 0.1.0"
echo ""
