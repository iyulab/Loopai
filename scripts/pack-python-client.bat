@echo off
REM Build Python client package for local installation

echo ========================================
echo Loopai Python Client - Package Build
echo ========================================
echo.

REM Check if Python is available
python --version >nul 2>&1
if errorlevel 1 (
    echo Error: Python is not installed or not in PATH
    exit /b 1
)

REM Navigate to client directory
cd src\loopai_client

echo Installing build dependencies...
python -m pip install --upgrade pip build wheel
echo.

echo Building package...
python -m build
echo.

REM Create dist directory if it doesn't exist
if not exist "..\..\dist\" mkdir "..\..\dist\"

REM Copy wheel to dist directory
copy dist\*.whl ..\..\dist\
copy dist\*.tar.gz ..\..\dist\
echo.

echo Package built successfully!
echo.
echo To install locally:
echo   pip install dist\loopai_client-0.1.0-py3-none-any.whl
echo.
echo To install in development mode:
echo   cd src\loopai_client
echo   pip install -e .
echo.

cd ..\..
