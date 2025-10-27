---
title: Development
sidebar_position: 4
---


완전한 로컬 개발 환경 설정 가이드입니다.

---

## 📋 필수 요구사항

### 소프트웨어 요구사항

- **Python**: 3.9 이상 (3.13 권장)
- **Git**: 최신 버전
- **Docker**: 20.10+ (선택사항, Docker 테스트용)
- **Visual Studio Code** 또는 PyCharm (권장)

### 시스템 요구사항

- **OS**: Windows 10+, macOS 10.15+, Linux (Ubuntu 20.04+)
- **RAM**: 최소 4GB (8GB 권장)
- **디스크**: 최소 500MB 여유 공간

---

## 🚀 빠른 시작

### 1. 저장소 클론

```bash
# HTTPS
git clone https://github.com/iyulab/loopai.git
cd loopai

# SSH (권장)
git clone git@github.com:iyulab/loopai.git
cd loopai
```

### 2. 가상환경 생성 및 활성화

**Windows**:
```bash
# 가상환경 생성
python -m venv venv

# 활성화
venv\Scripts\activate
```

**macOS/Linux**:
```bash
# 가상환경 생성
python -m venv venv

# 활성화
source venv/bin/activate
```

### 3. 의존성 설치

```bash
# 프로덕션 의존성
pip install -r requirements.txt

# 개발 도구 포함 (권장)
pip install -e .
```

### 4. 환경변수 설정

```bash
# .env 파일 생성
cp .env.example .env

# .env 파일 편집
# OPENAI_API_KEY=your-openai-api-key-here
# ANTHROPIC_API_KEY=your-anthropic-api-key-here
```

### 5. 테스트 실행으로 설정 확인

```bash
# 모든 테스트 실행
pytest

# Phase 3 테스트만 실행
pytest tests/test_phase3_*.py -v

# 커버리지와 함께 실행
pytest --cov=src/loopai --cov-report=html
```

---

## 🛠️ 개발 도구

### 코드 포맷팅 (Black)

```bash
# 전체 프로젝트 포맷팅
black src/ tests/

# 특정 파일만
black src/loopai/runtime/api.py

# 체크만 (변경 안 함)
black --check src/
```

### 린팅 (Ruff)

```bash
# 전체 프로젝트 린팅
ruff check src/ tests/

# 자동 수정
ruff check --fix src/ tests/

# 특정 파일만
ruff check src/loopai/runtime/api.py
```

### 타입 체크 (MyPy)

```bash
# 전체 프로젝트 타입 체크
mypy src/

# 특정 모듈만
mypy src/loopai/runtime/
```

### 통합 품질 체크

```bash
# 모든 품질 체크 실행
black --check src/ tests/ && \
ruff check src/ tests/ && \
mypy src/ && \
pytest
```

---

## 🔬 테스트

### 테스트 실행

```bash
# 모든 테스트
pytest

# 특정 Phase 테스트
pytest tests/test_phase0.py
pytest tests/test_phase1.py
pytest tests/test_phase2.py
pytest tests/test_phase3_*.py

# 특정 테스트 클래스
pytest tests/test_phase3_integration.py::TestCompleteWorkflow

# 특정 테스트 함수
pytest tests/test_phase3_integration.py::TestCompleteWorkflow::test_full_deployment_workflow

# 상세 출력
pytest -v

# 실패한 테스트만 재실행
pytest --lf

# 병렬 실행 (pytest-xdist 설치 필요)
pytest -n auto
```

### 커버리지

```bash
# 커버리지 리포트 생성
pytest --cov=src/loopai --cov-report=html

# HTML 리포트 열기
# Windows
start htmlcov/index.html

# macOS
open htmlcov/index.html

# Linux
xdg-open htmlcov/index.html
```

### 벤치마크

```bash
# Phase 2 벤치마크
python scripts/run_phase2_benchmark.py

# 커스텀 벤치마크 작성
# tests/benchmarks/ 디렉토리에 추가
```

---

## 🏃 로컬에서 Edge Runtime 실행

### 방법 1: Python 직접 실행

```bash
# 환경변수 설정
export LOOPAI_DATA_DIR=./loopai-data
export LOOPAI_TASK_ID=test-task

# Windows
set LOOPAI_DATA_DIR=./loopai-data
set LOOPAI_TASK_ID=test-task

# 아티팩트 준비 (예제)
python scripts/prepare_test_artifact.py

# Runtime 실행
python -m uvicorn loopai.runtime.main:app --reload --port 8080
```

### 방법 2: Docker 사용

```bash
# 이미지 빌드
docker-compose build

# 실행
docker-compose up

# 백그라운드 실행
docker-compose up -d

# 로그 확인
docker-compose logs -f edge-runtime
```

### API 테스트

```bash
# 헬스 체크
curl http://localhost:8080/health

# 프로그램 실행
curl -X POST http://localhost:8080/execute \
  -H "Content-Type: application/json" \
  -d '{"input": {"text": "Buy now!"}}'

# 메트릭 확인
curl http://localhost:8080/metrics
```

---

## 📝 개발 워크플로우

### Feature 개발

```bash
# 1. 최신 main 가져오기
git checkout main
git pull origin main

# 2. Feature 브랜치 생성
git checkout -b feature/your-feature-name

# 3. 개발 진행
# ... 코드 작성 ...

# 4. 테스트 작성 및 실행
# tests/ 디렉토리에 테스트 추가
pytest tests/test_your_feature.py -v

# 5. 품질 체크
black src/ tests/
ruff check --fix src/ tests/
mypy src/

# 6. 모든 테스트 실행
pytest

# 7. 커밋
git add .
git commit -m "feat: add your feature description"

# 8. 푸시
git push origin feature/your-feature-name

# 9. Pull Request 생성
# GitHub에서 PR 생성
```

### 커밋 메시지 규칙

```bash
# 타입: 제목

# 타입 종류:
# feat: 새로운 기능 추가
# fix: 버그 수정
# docs: 문서 변경
# style: 코드 포맷팅 (기능 변경 없음)
# refactor: 리팩토링
# test: 테스트 추가/수정
# chore: 빌드, 설정 변경

# 예시:
git commit -m "feat: add dataset retention policy"
git commit -m "fix: resolve task_id type mismatch in API"
git commit -m "docs: update deployment guide"
git commit -m "test: add integration tests for Edge Runtime"
```

---

## 🐛 디버깅

### VS Code 설정

`.vscode/launch.json`:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Python: Current File",
      "type": "python",
      "request": "launch",
      "program": "${file}",
      "console": "integratedTerminal",
      "justMyCode": true
    },
    {
      "name": "Python: pytest",
      "type": "python",
      "request": "launch",
      "module": "pytest",
      "args": ["-v", "${file}"],
      "console": "integratedTerminal",
      "justMyCode": false
    },
    {
      "name": "Edge Runtime",
      "type": "python",
      "request": "launch",
      "module": "uvicorn",
      "args": [
        "loopai.runtime.main:app",
        "--reload",
        "--port",
        "8080"
      ],
      "env": {
        "LOOPAI_DATA_DIR": "./loopai-data",
        "LOOPAI_TASK_ID": "test-task"
      },
      "console": "integratedTerminal",
      "justMyCode": true
    }
  ]
}
```

### PyCharm 설정

1. **Run Configuration 추가**:
   - `Run` → `Edit Configurations`
   - `+` → `Python`
   - **Script path**: `uvicorn`
   - **Parameters**: `loopai.runtime.main:app --reload --port 8080`
   - **Environment variables**: `LOOPAI_DATA_DIR=./loopai-data;LOOPAI_TASK_ID=test-task`

2. **pytest 설정**:
   - `Run` → `Edit Configurations`
   - `+` → `Python tests` → `pytest`
   - **Target**: `tests/`

### 로그 확인

```bash
# Runtime 로그 (uvicorn)
# 자동으로 콘솔에 출력됨

# Dataset 로그 확인
cat loopai-data/datasets/test-task/executions/$(date +%Y-%m-%d).jsonl

# Docker 로그
docker-compose logs -f edge-runtime

# Python 로깅 활성화
export PYTHONUNBUFFERED=1
```

---

## 📂 프로젝트 구조

```
Loopai/
├── src/loopai/              # 소스 코드
│   ├── __init__.py
│   ├── models.py            # Pydantic 모델
│   ├── generator/           # 프로그램 생성기
│   ├── executor/            # 프로그램 실행기
│   ├── validator/           # Oracle 검증기
│   ├── sampler/             # 샘플링 전략
│   └── runtime/             # Edge Runtime ⭐
│       ├── __init__.py
│       ├── dataset_manager.py
│       ├── config_manager.py
│       ├── artifact_cache.py
│       ├── api.py
│       └── main.py
│
├── tests/                   # 테스트
│   ├── datasets/            # 테스트 데이터셋
│   ├── test_phase0.py
│   ├── test_phase1.py
│   ├── test_phase2.py
│   ├── test_phase3_*.py    # Phase 3 테스트
│   └── conftest.py          # pytest 설정
│
├── scripts/                 # 유틸리티 스크립트
│   ├── run_phase2_benchmark.py
│   └── prepare_test_artifact.py  # 개발용
│
├── docs/                    # 문서
│   ├── ARCHITECTURE.md
│   ├── DEPLOYMENT.md
│   ├── DEVELOPMENT.md       # 이 문서
│   ├── PHASE*.md
│   └── GETTING_STARTED.md
│
├── loopai-data/             # 로컬 데이터 (git-ignored)
│   ├── datasets/
│   ├── artifacts/
│   └── config/
│
├── Dockerfile               # Docker 이미지
├── docker-compose.yml       # Docker Compose 설정
├── .dockerignore
├── requirements.txt         # Python 의존성
├── pyproject.toml          # 프로젝트 설정
├── .env.example            # 환경변수 템플릿
└── README.md
```

---

## 🔧 문제 해결

### 가상환경 문제

**증상**: `ModuleNotFoundError: No module named 'loopai'`

**해결**:
```bash
# 가상환경 활성화 확인
which python  # macOS/Linux
where python  # Windows

# 의존성 재설치
pip install -r requirements.txt
pip install -e .
```

### 테스트 실패

**증상**: `ImportError` 또는 테스트 실패

**해결**:
```bash
# 캐시 삭제
pytest --cache-clear

# 테스트 데이터 정리
rm -rf loopai-data/

# 의존성 확인
pip list | grep loopai
pip list | grep pytest
```

### Docker 문제

**증상**: Docker 컨테이너가 시작되지 않음

**해결**:
```bash
# 로그 확인
docker-compose logs edge-runtime

# 이미지 재빌드
docker-compose down
docker-compose build --no-cache
docker-compose up

# 볼륨 정리
docker volume prune
```

### API 키 문제

**증상**: `openai.error.AuthenticationError`

**해결**:
```bash
# .env 파일 확인
cat .env

# 환경변수 설정 확인 (Linux/macOS)
echo $OPENAI_API_KEY

# 환경변수 설정 확인 (Windows)
echo %OPENAI_API_KEY%

# .env 파일 다시 로드
# Python에서 python-dotenv 사용
from dotenv import load_dotenv
load_dotenv()
```

### 포트 충돌

**증상**: `OSError: [Errno 48] Address already in use`

**해결**:
```bash
# 포트 사용 확인 (macOS/Linux)
lsof -i :8080

# 포트 사용 확인 (Windows)
netstat -ano | findstr :8080

# 프로세스 종료 또는 다른 포트 사용
uvicorn loopai.runtime.main:app --port 8081
```

---

## 📚 추가 리소스

### 공식 문서

- **Architecture**: `docs/ARCHITECTURE.md`
- **Deployment**: `docs/DEPLOYMENT.md`
- **Phase Status**: `docs/PHASE3_STATUS.md`

### 학습 자료

- **FastAPI 문서**: https://fastapi.tiangolo.com/
- **Pydantic 문서**: https://docs.pydantic.dev/
- **pytest 문서**: https://docs.pytest.org/

### 커뮤니티

- **Issues**: https://github.com/iyulab/loopai/issues
- **Discussions**: https://github.com/iyulab/loopai/discussions

---

## 🎯 개발 체크리스트

새로운 기능 개발 시 확인 사항:

- [ ] 기능 브랜치 생성 (`feature/feature-name`)
- [ ] 테스트 작성 (TDD)
- [ ] 코드 구현
- [ ] 모든 테스트 통과 (`pytest`)
- [ ] 코드 포맷팅 (`black`)
- [ ] 린팅 통과 (`ruff`)
- [ ] 타입 체크 통과 (`mypy`)
- [ ] 문서 업데이트 (필요시)
- [ ] 커밋 메시지 규칙 준수
- [ ] Pull Request 생성

---

**Last Updated**: 2025-10-26
**Version**: Phase 3 Complete (v0.1)
