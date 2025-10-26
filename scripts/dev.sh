#!/bin/bash
# Linux/macOS용 개발 빠른 실행 스크립트

set -e

# 색상 코드
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

print_header() {
    echo -e "${BLUE}================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}================================${NC}"
    echo ""
}

show_help() {
    print_header "Loopai 개발 환경"
    echo "사용법: ./dev.sh [command]"
    echo ""
    echo "명령어:"
    echo "  setup     - 가상환경 생성 및 의존성 설치"
    echo "  test      - Phase 3 테스트 실행"
    echo "  quality   - 코드 품질 체크 (black, ruff, mypy, pytest)"
    echo "  artifact  - 테스트 아티팩트 생성"
    echo "  run       - Edge Runtime 실행"
    echo "  docker    - Docker Compose 실행"
    echo "  help      - 이 도움말 표시"
}

setup() {
    print_header "개발 환경 설정"

    echo "가상환경 생성 중..."
    python -m venv venv
    echo ""

    echo "가상환경 활성화 중..."
    source venv/bin/activate
    echo ""

    echo "의존성 설치 중..."
    pip install -r requirements.txt
    echo ""

    if [ ! -f .env ]; then
        echo ".env 파일 생성..."
        cp .env.example .env
        echo -e "${YELLOW}.env 파일을 편집하여 API 키를 설정하세요.${NC}"
    fi
    echo ""

    echo -e "${GREEN}✅ 설정 완료!${NC}"
}

run_tests() {
    print_header "Phase 3 테스트 실행"
    pytest tests/test_phase3_*.py -v
}

check_quality() {
    print_header "코드 품질 체크"
    python scripts/check_quality.py
}

create_artifact() {
    print_header "테스트 아티팩트 생성"
    python scripts/prepare_test_artifact.py
}

run_runtime() {
    print_header "Edge Runtime 실행"

    echo "환경변수 설정:"
    export LOOPAI_DATA_DIR=./loopai-data
    export LOOPAI_TASK_ID=test-task
    echo "  LOOPAI_DATA_DIR=$LOOPAI_DATA_DIR"
    echo "  LOOPAI_TASK_ID=$LOOPAI_TASK_ID"
    echo ""

    echo -e "${YELLOW}먼저 './dev.sh artifact'로 테스트 아티팩트를 생성하세요.${NC}"
    echo ""

    python -m uvicorn loopai.runtime.main:app --reload --port 8080
}

run_docker() {
    print_header "Docker Compose 실행"
    docker-compose up
}

# 메인 로직
case "$1" in
    setup)
        setup
        ;;
    test)
        run_tests
        ;;
    quality)
        check_quality
        ;;
    artifact)
        create_artifact
        ;;
    run)
        run_runtime
        ;;
    docker)
        run_docker
        ;;
    help|"")
        show_help
        ;;
    *)
        echo -e "${YELLOW}알 수 없는 명령: $1${NC}"
        echo ""
        show_help
        exit 1
        ;;
esac
