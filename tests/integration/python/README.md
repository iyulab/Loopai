# Python SDK Integration Tests

Python SDK의 실제 API 동작을 검증하는 통합 테스트입니다.

## 설치

```bash
# Python SDK 설치
cd ../../../sdk/python
pip install -e .

# 테스트 의존성 설치
cd ../../tests/integration/python
pip install -r requirements.txt
```

## API 서버 실행

테스트를 실행하기 전에 Loopai API 서버가 실행 중이어야 합니다:

```bash
cd ../../../src/Loopai.CloudApi
dotnet run
```

API 서버는 `http://localhost:8080`에서 실행됩니다.

## 테스트 실행

### 모든 테스트 실행
```bash
pytest -v
```

### 특정 테스트 클래스 실행
```bash
pytest -v test_integration.py::TestHealthCheck
pytest -v test_integration.py::TestTaskLifecycle
pytest -v test_integration.py::TestSingleExecution
pytest -v test_integration.py::TestBatchExecution
pytest -v test_integration.py::TestErrorHandling
```

### 커버리지와 함께 실행
```bash
pytest --cov=loopai --cov-report=html
```

### 병렬 실행
```bash
pytest -n auto
```

## 테스트 시나리오

### 1. Health Check (`TestHealthCheck`)
- API 서버 상태 확인
- 버전 정보 검증

### 2. Task Lifecycle (`TestTaskLifecycle`)
- Task 생성
- Task 조회
- Task 메타데이터 검증

### 3. Single Execution (`TestSingleExecution`)
- 단일 입력 실행
- 출력 검증
- 강제 검증(forced validation) 테스트

### 4. Batch Execution (`TestBatchExecution`)
- 간단한 배치 실행 (자동 ID)
- 고급 배치 실행 (커스텀 ID, 옵션)
- 동시성 제어 검증

### 5. Error Handling (`TestErrorHandling`)
- 입력 유효성 검증 오류 (400)
- Task 없음 오류 (404)
- 잘못된 입력 처리

### 6. Retry Logic (`TestRetryLogic`)
- 재시도 로직 검증

### 7. Concurrency (`TestConcurrency`)
- 동시 실행 테스트

## 설정

테스트 설정은 `../test-config.json`에 정의되어 있습니다:
- `baseUrl`: API 서버 URL
- `timeout`: 요청 타임아웃
- `retries`: 재시도 횟수
- `testData`: 테스트 데이터 및 스키마

## 트러블슈팅

### API 서버에 연결할 수 없음
```bash
# API 서버가 실행 중인지 확인
curl http://localhost:8080/health

# 또는
python -c "import httpx; print(httpx.get('http://localhost:8080/health').json())"
```

### 테스트 타임아웃
- `test-config.json`에서 timeout 값 증가
- API 서버 로그 확인

### 모듈을 찾을 수 없음
```bash
# Python SDK를 개발 모드로 설치
cd ../../../sdk/python
pip install -e .
```

## CI/CD 통합

```yaml
# .github/workflows/python-integration-tests.yml
name: Python SDK Integration Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Setup Python
        uses: actions/setup-python@v4
        with:
          python-version: '3.9'

      - name: Start API Server
        run: |
          cd src/Loopai.CloudApi
          dotnet run &
          sleep 10

      - name: Install dependencies
        run: |
          cd sdk/python
          pip install -e .
          cd ../../tests/integration/python
          pip install -r requirements.txt

      - name: Run tests
        run: |
          cd tests/integration/python
          pytest -v
```
