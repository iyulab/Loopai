@echo off
REM Script to build and pack Loopai.Client NuGet package locally

echo ========================================
echo Loopai.Client Local NuGet Package Build
echo ========================================
echo.

REM Build in Release mode
echo [1/3] Building Loopai.Client in Release mode...
dotnet build src\Loopai.Client\Loopai.Client.csproj -c Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    exit /b %errorlevel%
)
echo.

REM Pack NuGet package
echo [2/3] Creating NuGet package...
dotnet pack src\Loopai.Client\Loopai.Client.csproj -c Release -o .\nupkg
if %errorlevel% neq 0 (
    echo ERROR: Pack failed
    exit /b %errorlevel%
)
echo.

REM List created packages
echo [3/3] Package created successfully:
dir /b nupkg\Loopai.Client.*.nupkg
echo.

echo ========================================
echo Package Location: %cd%\nupkg
echo ========================================
echo.
echo To use this package locally:
echo 1. Add local source: dotnet nuget add source %cd%\nupkg --name LoopaiLocal
echo 2. Install package: dotnet add package Loopai.Client --version 0.1.0
echo.
