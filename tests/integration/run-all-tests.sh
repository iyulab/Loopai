#!/bin/bash
# Run all SDK integration tests
# Usage: ./run-all-tests.sh

set -e

echo "============================================"
echo "Loopai SDK Integration Tests"
echo "============================================"
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if API server is running
echo "Checking API server..."
if ! curl -s http://localhost:8080/health > /dev/null; then
    echo -e "${RED}✗ API server is not running at http://localhost:8080${NC}"
    echo ""
    echo "Please start the API server first:"
    echo "  cd src/Loopai.CloudApi"
    echo "  dotnet run"
    exit 1
fi
echo -e "${GREEN}✓ API server is running${NC}"
echo ""

# Test results
DOTNET_RESULT=0
PYTHON_RESULT=0
TYPESCRIPT_RESULT=0
COMPATIBILITY_RESULT=0

# 1. .NET SDK Integration Tests
echo "============================================"
echo "1. .NET SDK Integration Tests"
echo "============================================"
cd ../../Loopai.Client.IntegrationTests
if dotnet test --no-build --verbosity normal; then
    echo -e "${GREEN}✓ .NET SDK tests passed${NC}"
else
    echo -e "${RED}✗ .NET SDK tests failed${NC}"
    DOTNET_RESULT=1
fi
echo ""

# 2. Python SDK Integration Tests
echo "============================================"
echo "2. Python SDK Integration Tests"
echo "============================================"
cd ../integration/python
if pytest -v; then
    echo -e "${GREEN}✓ Python SDK tests passed${NC}"
else
    echo -e "${RED}✗ Python SDK tests failed${NC}"
    PYTHON_RESULT=1
fi
echo ""

# 3. TypeScript SDK Integration Tests
echo "============================================"
echo "3. TypeScript SDK Integration Tests"
echo "============================================"
cd ../typescript
if npm test; then
    echo -e "${GREEN}✓ TypeScript SDK tests passed${NC}"
else
    echo -e "${RED}✗ TypeScript SDK tests failed${NC}"
    TYPESCRIPT_RESULT=1
fi
echo ""

# 4. Cross-SDK Compatibility Tests
echo "============================================"
echo "4. Cross-SDK Compatibility Tests"
echo "============================================"
cd ../compatibility
if python test-cross-sdk.py; then
    echo -e "${GREEN}✓ Compatibility tests passed${NC}"
else
    echo -e "${YELLOW}⚠ Compatibility tests had issues${NC}"
    COMPATIBILITY_RESULT=1
fi
echo ""

# Summary
echo "============================================"
echo "Test Summary"
echo "============================================"
echo ""

TOTAL_TESTS=4
PASSED_TESTS=$((4 - DOTNET_RESULT - PYTHON_RESULT - TYPESCRIPT_RESULT - COMPATIBILITY_RESULT))

if [ $DOTNET_RESULT -eq 0 ]; then
    echo -e "${GREEN}✓ .NET SDK Integration Tests${NC}"
else
    echo -e "${RED}✗ .NET SDK Integration Tests${NC}"
fi

if [ $PYTHON_RESULT -eq 0 ]; then
    echo -e "${GREEN}✓ Python SDK Integration Tests${NC}"
else
    echo -e "${RED}✗ Python SDK Integration Tests${NC}"
fi

if [ $TYPESCRIPT_RESULT -eq 0 ]; then
    echo -e "${GREEN}✓ TypeScript SDK Integration Tests${NC}"
else
    echo -e "${RED}✗ TypeScript SDK Integration Tests${NC}"
fi

if [ $COMPATIBILITY_RESULT -eq 0 ]; then
    echo -e "${GREEN}✓ Cross-SDK Compatibility Tests${NC}"
else
    echo -e "${YELLOW}⚠ Cross-SDK Compatibility Tests${NC}"
fi

echo ""
echo "Results: ${PASSED_TESTS}/${TOTAL_TESTS} test suites passed"

# Exit with error if any tests failed
if [ $((DOTNET_RESULT + PYTHON_RESULT + TYPESCRIPT_RESULT)) -gt 0 ]; then
    echo ""
    echo -e "${RED}Some tests failed. Please check the output above.${NC}"
    exit 1
fi

if [ $COMPATIBILITY_RESULT -gt 0 ]; then
    echo ""
    echo -e "${YELLOW}Compatibility tests had issues, but core tests passed.${NC}"
    exit 0
fi

echo ""
echo -e "${GREEN}All tests passed!${NC}"
exit 0
