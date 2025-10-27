# Run all SDK integration tests (Windows PowerShell)
# Usage: .\run-all-tests.ps1

$ErrorActionPreference = "Continue"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Loopai SDK Integration Tests" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check if API server is running
Write-Host "Checking API server..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8080/health" -UseBasicParsing -TimeoutSec 5
    Write-Host "✓ API server is running" -ForegroundColor Green
} catch {
    Write-Host "✗ API server is not running at http://localhost:8080" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please start the API server first:"
    Write-Host "  cd src\Loopai.CloudApi"
    Write-Host "  dotnet run"
    exit 1
}
Write-Host ""

# Test results
$dotnetResult = 0
$pythonResult = 0
$typescriptResult = 0
$compatibilityResult = 0

# 1. .NET SDK Integration Tests
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "1. .NET SDK Integration Tests" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Set-Location "..\..\Loopai.Client.IntegrationTests"
dotnet test --no-build --verbosity normal
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ .NET SDK tests passed" -ForegroundColor Green
} else {
    Write-Host "✗ .NET SDK tests failed" -ForegroundColor Red
    $dotnetResult = 1
}
Write-Host ""

# 2. Python SDK Integration Tests
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "2. Python SDK Integration Tests" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Set-Location "..\integration\python"
pytest -v
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Python SDK tests passed" -ForegroundColor Green
} else {
    Write-Host "✗ Python SDK tests failed" -ForegroundColor Red
    $pythonResult = 1
}
Write-Host ""

# 3. TypeScript SDK Integration Tests
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "3. TypeScript SDK Integration Tests" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Set-Location "..\typescript"
npm test
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ TypeScript SDK tests passed" -ForegroundColor Green
} else {
    Write-Host "✗ TypeScript SDK tests failed" -ForegroundColor Red
    $typescriptResult = 1
}
Write-Host ""

# 4. Cross-SDK Compatibility Tests
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "4. Cross-SDK Compatibility Tests" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Set-Location "..\compatibility"
python test-cross-sdk.py
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Compatibility tests passed" -ForegroundColor Green
} else {
    Write-Host "⚠ Compatibility tests had issues" -ForegroundColor Yellow
    $compatibilityResult = 1
}
Write-Host ""

# Summary
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$totalTests = 4
$passedTests = $totalTests - $dotnetResult - $pythonResult - $typescriptResult - $compatibilityResult

if ($dotnetResult -eq 0) {
    Write-Host "✓ .NET SDK Integration Tests" -ForegroundColor Green
} else {
    Write-Host "✗ .NET SDK Integration Tests" -ForegroundColor Red
}

if ($pythonResult -eq 0) {
    Write-Host "✓ Python SDK Integration Tests" -ForegroundColor Green
} else {
    Write-Host "✗ Python SDK Integration Tests" -ForegroundColor Red
}

if ($typescriptResult -eq 0) {
    Write-Host "✓ TypeScript SDK Integration Tests" -ForegroundColor Green
} else {
    Write-Host "✗ TypeScript SDK Integration Tests" -ForegroundColor Red
}

if ($compatibilityResult -eq 0) {
    Write-Host "✓ Cross-SDK Compatibility Tests" -ForegroundColor Green
} else {
    Write-Host "⚠ Cross-SDK Compatibility Tests" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Results: $passedTests/$totalTests test suites passed"

# Exit with error if any critical tests failed
if (($dotnetResult + $pythonResult + $typescriptResult) -gt 0) {
    Write-Host ""
    Write-Host "Some tests failed. Please check the output above." -ForegroundColor Red
    exit 1
}

if ($compatibilityResult -gt 0) {
    Write-Host ""
    Write-Host "Compatibility tests had issues, but core tests passed." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "All tests passed!" -ForegroundColor Green
exit 0
