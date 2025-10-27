# Loopai SDK Integration Tests

통합 테스트 환경으로 3개 SDK (.NET, Python, TypeScript)의 실제 API 동작을 검증합니다.

## 테스트 구조

```
tests/integration/
├── README.md                    # 이 파일
├── docker-compose.yml           # 테스트 환경 구성
├── test-config.json            # 공통 테스트 설정
├── dotnet/                     # .NET SDK 통합 테스트
│   └── (기존 Loopai.Client.IntegrationTests 참조)
├── python/                     # Python SDK 통합 테스트
│   ├── conftest.py            # pytest 설정
│   ├── test_integration.py    # 통합 테스트
│   └── requirements.txt       # 테스트 의존성
└── typescript/                # TypeScript SDK 통합 테스트
    ├── jest.config.js         # Jest 설정
    ├── integration.test.ts    # 통합 테스트
    └── package.json           # 테스트 의존성
```

## 테스트 환경 설정

### 전제 조건
- .NET 8.0 SDK
- Python 3.9+
- Node.js 16+
- Docker (선택사항)

### 로컬 API 서버 실행

#### Option 1: 직접 실행
```bash
cd src/Loopai.CloudApi
dotnet run
```

#### Option 2: Docker Compose
```bash
cd tests/integration
docker-compose up -d
```

API 서버가 `http://localhost:8080`에서 실행됩니다.

## 테스트 실행

### .NET SDK 통합 테스트
```bash
cd tests/Loopai.Client.IntegrationTests
dotnet test
```

### Python SDK 통합 테스트
```bash
cd tests/integration/python
pip install -r requirements.txt
pytest -v
```

### TypeScript SDK 통합 테스트
```bash
cd tests/integration/typescript
npm install
npm test
```

### 모든 테스트 실행
```bash
# 루트 디렉토리에서
./run-integration-tests.sh     # Linux/Mac
.\run-integration-tests.ps1    # Windows
```

## 테스트 시나리오

각 SDK는 다음 시나리오를 테스트합니다:

### 1. Health Check
- API 서버 상태 확인
- 버전 정보 검증

### 2. Task Lifecycle
- Task 생성
- Task 조회
- Task 메타데이터 검증

### 3. Single Execution
- 단일 입력 실행
- 출력 검증
- 지연시간 측정
- 샘플링 동작 확인

### 4. Batch Execution
- 간단한 배치 실행 (자동 ID)
- 고급 배치 실행 (커스텀 ID, 옵션)
- 부분 실패 처리
- 동시성 제어

### 5. Error Handling
- 입력 유효성 검증 오류 (400)
- Task 없음 오류 (404)
- 실행 실패 오류 (500)
- 연결 오류 처리
- 재시도 로직 검증

### 6. Cross-SDK Compatibility
- 동일 Task를 3개 SDK로 실행
- 결과 일관성 검증
- 타임스탬프 형식 호환성
- ID 형식 호환성

## 테스트 설정

`test-config.json`:
```json
{
  "baseUrl": "http://localhost:8080",
  "timeout": 30000,
  "retries": 3,
  "testData": {
    "taskName": "test-spam-classifier",
    "inputSchema": {
      "type": "object",
      "properties": { "text": { "type": "string" } },
      "required": ["text"]
    },
    "outputSchema": {
      "type": "string",
      "enum": ["spam", "not_spam"]
    },
    "sampleInputs": [
      { "text": "Buy now for free money!" },
      { "text": "Meeting tomorrow at 2pm" },
      { "text": "URGENT: Click here NOW!!!" }
    ]
  }
}
```

## CI/CD 통합

### GitHub Actions 예제
```yaml
name: SDK Integration Tests

on: [push, pull_request]

jobs:
  integration-tests:
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

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '16'

      - name: Start API Server
        run: |
          cd src/Loopai.CloudApi
          dotnet run &
          sleep 10

      - name: Run .NET Integration Tests
        run: |
          cd tests/Loopai.Client.IntegrationTests
          dotnet test

      - name: Run Python Integration Tests
        run: |
          cd tests/integration/python
          pip install -r requirements.txt
          pytest -v

      - name: Run TypeScript Integration Tests
        run: |
          cd tests/integration/typescript
          npm install
          npm test
```

## 결과 보고

테스트 실행 후 다음 정보가 보고됩니다:
- ✅/❌ 각 SDK별 테스트 통과율
- ⏱️ 평균 응답 시간
- 📊 배치 처리 성능
- 🔄 재시도 성공률
- 🔗 SDK 간 호환성 점수

## 트러블슈팅

### API 서버 연결 실패
```bash
# API 서버가 실행 중인지 확인
curl http://localhost:8080/health

# 포트 충돌 확인
netstat -ano | grep 8080      # Windows
lsof -i :8080                 # Linux/Mac
```

### 데이터베이스 오류
```bash
# 데이터베이스 마이그레이션 실행
cd src/Loopai.CloudApi
dotnet ef database update
```

### 테스트 타임아웃
- `test-config.json`에서 timeout 값 증가
- API 서버 로그 확인
- 네트워크 연결 확인

## 추가 리소스

- [.NET SDK Documentation](../../sdk/dotnet/README.md)
- [Python SDK Documentation](../../sdk/python/README.md)
- [TypeScript SDK Documentation](../../sdk/typescript/README.md)
- [API Documentation](../../docs/API.md)
