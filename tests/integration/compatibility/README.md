# Cross-SDK Compatibility Tests

SDK 간 상호 호환성을 검증하는 테스트입니다. 한 SDK로 생성한 Task를 다른 SDK로 실행하여 결과가 일관되는지 확인합니다.

## 목적

- 모든 SDK가 동일한 API 계약을 준수하는지 검증
- Task, Execution, Batch 결과의 일관성 확인
- 타임스탬프, ID 형식 등 데이터 형식 호환성 검증
- SDK 간 상호 운용성 보장

## 테스트 시나리오

### 1. Task Creation Cross-Compatibility
- Python SDK로 Task 생성
- .NET SDK로 동일 Task 조회
- TypeScript SDK로 동일 Task 실행
- 결과 일관성 검증

### 2. Execution Result Format
- 동일 Task를 3개 SDK로 실행
- ExecutionResult 구조 비교
- 필드 이름 및 타입 검증
- 타임스탬프 형식 확인

### 3. Batch Operation Compatibility
- Python SDK로 배치 작업 생성
- TypeScript SDK로 배치 결과 조회
- ID 형식 및 결과 구조 일관성

### 4. Error Handling Consistency
- 동일한 오류 조건에서 3개 SDK 응답 비교
- HTTP 상태 코드 일관성
- 오류 메시지 형식 검증

## 실행 방법

### 전제 조건
```bash
# API 서버 실행
cd ../../../src/Loopai.CloudApi
dotnet run

# 모든 SDK 설치
cd ../../../sdk/dotnet && dotnet build
cd ../../../sdk/python && pip install -e .
cd ../../../sdk/typescript && npm install && npm run build
```

### Python 기반 호환성 테스트
```bash
cd tests/integration/compatibility
python test-cross-sdk.py
```

## 검증 항목

### 데이터 형식 호환성

| 필드 | .NET | Python | TypeScript | 검증 |
|------|------|--------|------------|------|
| Task ID | `Guid` | `UUID` | `string` | UUID 형식 |
| Timestamp | `DateTime` | `datetime` | `string` (ISO 8601) | ISO 8601 |
| Output | `object` | `Any` | `unknown` | JSON 직렬화 가능 |
| Latency | `double` | `float` | `number` | 밀리초 단위 |
| Sampled | `bool` | `bool` | `boolean` | true/false |

### 필드 이름 규칙

| API (camelCase) | .NET (PascalCase) | Python (snake_case) | TypeScript (camelCase) |
|-----------------|-------------------|---------------------|------------------------|
| `taskId` | `TaskId` | `task_id` | `taskId` |
| `inputSchema` | `InputSchema` | `input_schema` | `inputSchema` |
| `executedAt` | `ExecutedAt` | `executed_at` | `executedAt` |
| `latencyMs` | `LatencyMs` | `latency_ms` | `latencyMs` |
| `sampledForValidation` | `SampledForValidation` | `sampled_for_validation` | `sampledForValidation` |

### API 동작 일관성

1. **Task 생성**
   - 동일한 입력 → 동일한 Task 속성
   - 생성 시간은 다를 수 있음 (허용)
   - ID는 서버에서 생성 (SDK 영향 없음)

2. **Task 실행**
   - 동일한 Task + 입력 → 동일한 Version 사용
   - 출력은 버전에 따라 결정 (SDK 무관)
   - 샘플링은 서버 측 로직 (SDK 무관)

3. **배치 실행**
   - 동일한 항목 → 동일한 순서로 결과 반환
   - 커스텀 ID 보존
   - 부분 실패 처리 일관성

4. **오류 처리**
   - 동일한 HTTP 상태 코드
   - 유사한 오류 메시지 구조
   - 재시도 로직 (SDK별 구현)

## 예상 결과

```json
{
  "compatible": true,
  "issues": null,
  "summary": {
    "sdks_tested": 3,
    "successful": 3,
    "failed": 0
  },
  "results": [
    {
      "sdk": "Python",
      "execution_id": "...",
      "task_id": "...",
      "version": 1,
      "output": "spam",
      "latency_ms": 42.5,
      "sampled": false,
      "timestamp": "2025-01-15T10:30:00Z"
    },
    {
      "sdk": ".NET",
      "execution_id": "...",
      "task_id": "...",
      "version": 1,
      "output": "spam",
      "latency_ms": 45.2,
      "sampled": false,
      "timestamp": "2025-01-15T10:30:01Z"
    },
    {
      "sdk": "TypeScript",
      "execution_id": "...",
      "task_id": "...",
      "version": 1,
      "output": "spam",
      "latency_ms": 43.8,
      "sampled": false,
      "timestamp": "2025-01-15T10:30:02Z"
    }
  ]
}
```

## 알려진 차이점

### 허용되는 차이점
- **타임스탬프 형식**: 모두 ISO 8601이지만 표현이 다를 수 있음
  - .NET: `2025-01-15T10:30:00.1234567Z`
  - Python: `2025-01-15T10:30:00.123456+00:00`
  - TypeScript: `2025-01-15T10:30:00.123Z`

- **실행 시간**: 네트워크 지연, 서버 부하로 인한 차이

- **재시도 로직**: SDK별로 다르게 구현될 수 있음

### 허용되지 않는 차이점
- Task ID 형식 불일치
- 필수 필드 누락
- 잘못된 데이터 타입
- API 계약 위반

## CI/CD 통합

```yaml
name: Cross-SDK Compatibility

on: [push, pull_request]

jobs:
  compatibility:
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

      - name: Build all SDKs
        run: |
          cd sdk/dotnet && dotnet build
          cd ../python && pip install -e .
          cd ../typescript && npm install && npm run build

      - name: Start API Server
        run: |
          cd src/Loopai.CloudApi
          dotnet run &
          sleep 10

      - name: Run compatibility tests
        run: |
          cd tests/integration/compatibility
          python test-cross-sdk.py
```

## 트러블슈팅

### TypeScript SDK 실행 실패
```bash
# SDK가 빌드되었는지 확인
cd sdk/typescript
npm run build

# Node.js에서 로드 가능한지 확인
node -e "require('./dist/index.js')"
```

### .NET SDK 실행 실패
```bash
# SDK가 빌드되었는지 확인
cd sdk/dotnet
dotnet build

# 참조 확인
dotnet list package
```

### Python SDK 실행 실패
```bash
# SDK가 설치되었는지 확인
pip show loopai

# 재설치
cd sdk/python
pip install -e . --force-reinstall
```

## 향후 개선 사항

1. **자동화된 호환성 매트릭스**
   - 모든 SDK 조합 테스트 (3x3 = 9 조합)
   - Task 생성, 실행, 조회 각각 다른 SDK 사용

2. **버전 호환성 테스트**
   - 구버전 SDK와 신버전 API 호환성
   - 신버전 SDK와 구버전 API 호환성

3. **성능 비교**
   - SDK별 처리 속도 비교
   - 메모리 사용량 비교
   - 동시성 처리 능력 비교

4. **스트레스 테스트**
   - 대량 요청 처리
   - 장시간 연결 유지
   - 오류 복구 능력
