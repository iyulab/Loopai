# TypeScript SDK Integration Tests

TypeScript/JavaScript SDK의 실제 API 동작을 검증하는 통합 테스트입니다.

## 설치

```bash
# TypeScript SDK 빌드
cd ../../../sdk/typescript
npm install
npm run build

# 테스트 의존성 설치
cd ../../tests/integration/typescript
npm install
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
npm test
```

### Watch 모드로 실행
```bash
npm run test:watch
```

### 커버리지와 함께 실행
```bash
npm run test:coverage
```

### 특정 테스트만 실행
```bash
npx jest --testNamePattern="Health Check"
npx jest --testNamePattern="Task Lifecycle"
npx jest --testNamePattern="Single Execution"
npx jest --testNamePattern="Batch Execution"
npx jest --testNamePattern="Error Handling"
```

## 테스트 시나리오

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
- 강제 검증(forced validation) 테스트

### 4. Batch Execution
- 간단한 배치 실행 (자동 ID)
- 고급 배치 실행 (커스텀 ID, 옵션)
- 동시성 제어 검증

### 5. Error Handling
- 입력 유효성 검증 오류 (400)
- Task 없음 오류 (404)
- 잘못된 입력 처리

### 6. Retry Logic
- 재시도 로직 검증

### 7. Concurrency
- 동시 실행 테스트

## 설정

테스트 설정은 `../test-config.json`에 정의되어 있습니다:
- `baseUrl`: API 서버 URL
- `timeout`: 요청 타임아웃
- `retries`: 재시도 횟수
- `testData`: 테스트 데이터 및 스키마

Jest 설정은 `jest.config.js`에 정의되어 있습니다:
- 테스트 타임아웃: 30초
- Global setup: API 서버 대기
- TypeScript 지원: ts-jest

## 트러블슈팅

### API 서버에 연결할 수 없음
```bash
# API 서버가 실행 중인지 확인
curl http://localhost:8080/health

# 또는
node -e "fetch('http://localhost:8080/health').then(r => r.json()).then(console.log)"
```

### 테스트 타임아웃
- `jest.config.js`에서 testTimeout 값 증가
- API 서버 로그 확인

### 모듈을 찾을 수 없음
```bash
# TypeScript SDK 빌드
cd ../../../sdk/typescript
npm install
npm run build

# 테스트 디렉토리에서 재설치
cd ../../tests/integration/typescript
rm -rf node_modules package-lock.json
npm install
```

### TypeScript 컴파일 오류
```bash
# TypeScript 버전 확인
npx tsc --version

# tsconfig.json 검증
npx tsc --noEmit
```

## CI/CD 통합

```yaml
# .github/workflows/typescript-integration-tests.yml
name: TypeScript SDK Integration Tests

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

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '16'

      - name: Start API Server
        run: |
          cd src/Loopai.CloudApi
          dotnet run &
          sleep 10

      - name: Build SDK
        run: |
          cd sdk/typescript
          npm install
          npm run build

      - name: Install test dependencies
        run: |
          cd tests/integration/typescript
          npm install

      - name: Run tests
        run: |
          cd tests/integration/typescript
          npm test
```

## 디버깅

Visual Studio Code에서 디버깅하려면 `.vscode/launch.json`에 다음 설정을 추가하세요:

```json
{
  "type": "node",
  "request": "launch",
  "name": "Jest Integration Tests",
  "program": "${workspaceFolder}/tests/integration/typescript/node_modules/.bin/jest",
  "args": ["--runInBand", "--no-cache"],
  "cwd": "${workspaceFolder}/tests/integration/typescript",
  "console": "integratedTerminal",
  "internalConsoleOptions": "neverOpen"
}
```
