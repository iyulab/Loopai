@echo off
REM Windows용 개발 빠른 실행 스크립트

echo ================================
echo Loopai 개발 환경
echo ================================
echo.

if "%1"=="" goto help
if "%1"=="help" goto help
if "%1"=="setup" goto setup
if "%1"=="test" goto test
if "%1"=="quality" goto quality
if "%1"=="artifact" goto artifact
if "%1"=="run" goto run
if "%1"=="docker" goto docker
goto help

:help
echo 사용법: dev.bat [command]
echo.
echo 명령어:
echo   setup     - 가상환경 생성 및 의존성 설치
echo   test      - Phase 3 테스트 실행
echo   quality   - 코드 품질 체크 (black, ruff, mypy, pytest)
echo   artifact  - 테스트 아티팩트 생성
echo   run       - Edge Runtime 실행
echo   docker    - Docker Compose 실행
echo   help      - 이 도움말 표시
goto end

:setup
echo 가상환경 생성 중...
python -m venv venv
echo.
echo 가상환경 활성화 중...
call venv\Scripts\activate
echo.
echo 의존성 설치 중...
pip install -r requirements.txt
echo.
echo .env 파일 생성...
if not exist .env (
    copy .env.example .env
    echo .env 파일을 편집하여 API 키를 설정하세요.
)
echo.
echo ✅ 설정 완료!
goto end

:test
echo Phase 3 테스트 실행 중...
pytest tests\test_phase3_*.py -v
goto end

:quality
echo 코드 품질 체크 실행 중...
python scripts\check_quality.py
goto end

:artifact
echo 테스트 아티팩트 생성 중...
python scripts\prepare_test_artifact.py
goto end

:run
echo Edge Runtime 실행 중...
echo 환경변수 설정:
set LOOPAI_DATA_DIR=./loopai-data
set LOOPAI_TASK_ID=test-task
echo   LOOPAI_DATA_DIR=%LOOPAI_DATA_DIR%
echo   LOOPAI_TASK_ID=%LOOPAI_TASK_ID%
echo.
echo 먼저 'dev.bat artifact'로 테스트 아티팩트를 생성하세요.
echo.
python -m uvicorn loopai.runtime.main:app --reload --port 8080
goto end

:docker
echo Docker Compose 실행 중...
docker-compose up
goto end

:end
