# Loopai 하이브리드 아키텍처 (Python + C#)

**Last Updated**: 2025-10-26
**Status**: 📋 제안 및 평가 단계

---

## 🎯 아키텍처 결정 배경

### 현재 상태 (Phase 3 완료)
- ✅ Python 기반 Edge Runtime 완성
- ✅ 실행 성능: 0.02ms (목표 10ms 대비 500배 초과 달성)
- ✅ 53개 테스트 100% 통과
- ✅ Docker 배포 준비 완료

### 제안된 하이브리드 접근법
각 언어의 강점을 극대화하여 **개발 속도**와 **성능** 모두 확보

---

## 📊 구성 요소별 언어 선택

| 컴포넌트 | 언어 | 주요 이유 | 성능 목표 |
|---------|------|----------|----------|
| **Cloud API/SignalR Hub** | C# ASP.NET Core | 고성능, 동시성, SignalR 네이티브 | 100K+ req/sec |
| **Program Generator** | Python | LLM SDK 풍부, ML 생태계, 빠른 개발 | - |
| **Edge Runtime** | Python (기본) + C# (옵션) | Python 충분히 빠름, .NET 엔터프라이즈 옵션 | <10ms (달성: 0.02ms) |
| **Dataset Manager** | Python | JSONL 처리, pandas, 데이터 분석 | - |
| **Improvement Engine** | Python | ML/AI 파이프라인, scikit-learn | - |

---

## ✅ 장점 분석

### 1. 성능 최적화
**C# Cloud API (ASP.NET Core)**:
- ✅ 100K+ req/sec 처리 가능 (Kestrel 서버)
- ✅ SignalR 네이티브 지원 (실시간 업데이트)
- ✅ gRPC 고성능 통신
- ✅ 멀티스레딩 및 비동기 I/O 최적화
- ✅ 메모리 효율적 (GC 최적화)

**Python Edge Runtime**:
- ✅ 이미 0.02ms 달성 (목표 대비 500배 빠름)
- ✅ 추가 최적화 불필요
- ✅ 개발 생산성 유지

### 2. 개발 생산성
**Python for AI/ML**:
- ✅ OpenAI/Anthropic SDK 1급 지원
- ✅ ML 라이브러리 풍부 (scikit-learn, pandas, numpy)
- ✅ 빠른 프로토타이핑
- ✅ 데이터 분석 생태계

**C# for Enterprise**:
- ✅ 강력한 타입 시스템
- ✅ Visual Studio 통합 개발 환경
- ✅ .NET 엔터프라이즈 고객 친화적
- ✅ Azure 통합

### 3. 시장 대응
**Python Edge Runtime**:
- 오픈소스 친화적
- 개발자 커뮤니티 넓음
- 빠른 채택률

**C# Edge Runtime (옵션)**:
- .NET 기업 고객 타겟
- 금융/헬스케어 등 엔터프라이즈
- 기존 .NET 인프라 통합

### 4. 기술 스택 최적화
```
Python 강점:
├─ LLM/AI 통합 (OpenAI, Anthropic)
├─ 데이터 처리 (pandas, numpy)
├─ 빠른 개발 사이클
└─ ML 파이프라인

C# 강점:
├─ 고성능 API 서버 (100K+ req/sec)
├─ SignalR 실시간 통신
├─ 엔터프라이즈 지원
└─ Azure 네이티브 통합
```

---

## ⚠️ 단점 및 리스크

### 1. 복잡성 증가
**문제**:
- 두 언어 생태계 관리
- 팀 역량 분산 (Python + C# 전문성 필요)
- 빌드/배포 파이프라인 이원화
- 디버깅 복잡도 증가

**완화 방안**:
- 명확한 경계 정의 (API 계약)
- REST/gRPC로 통신 표준화
- 각 컴포넌트 독립 배포
- Docker/K8s로 배포 통합

### 2. 개발 리소스
**문제**:
- Python + C# 개발자 필요
- 두 개 코드베이스 유지보수
- 학습 곡선

**완화 방안**:
- Phase별 순차 개발 (Python 먼저, C# 나중)
- API 우선 설계로 독립성 확보
- 각 언어별 전담 개발자 배정

### 3. 운영 복잡도
**문제**:
- 모니터링 통합
- 로그 집계
- 성능 프로파일링

**완화 방안**:
- 통합 모니터링 (Prometheus, Grafana)
- 중앙 로깅 (ELK Stack)
- OpenTelemetry로 분산 추적

---

## 🎯 성능 목표 및 검증

### Cloud API (C# ASP.NET Core)
**목표**: 100,000+ req/sec

**벤치마크 예상**:
```csharp
// ASP.NET Core Kestrel
- Plaintext: 7M+ req/sec (TechEmpower)
- JSON serialization: 1M+ req/sec
- 실제 워크로드: 100K-500K req/sec (충분히 가능)
```

**검증 방법**:
- Apache Bench / wrk
- K6 load testing
- TechEmpower benchmarks

### Edge Runtime (Python)
**목표**: <10ms 실행 지연

**현재 성능**: ✅ 0.02ms (평균)
- 목표 대비 500배 빠름
- 추가 최적화 불필요
- Python으로 충분

### Edge Runtime (C# 옵션)
**목표**: <5ms 실행 지연

**예상 성능**:
```csharp
// .NET 7+ JIT 컴파일
- 예상: 0.01-0.05ms
- Python 대비 유사하거나 약간 빠름
- 엔터프라이즈 요구사항 충족
```

---

## 🏗️ 아키텍처 다이어그램

```
┌─────────────────────────────────────────────────────────────┐
│                     Cloud Platform (C#)                      │
│  ┌─────────────────────────────────────────────────────┐   │
│  │        ASP.NET Core Web API (100K+ req/sec)         │   │
│  │  - /api/tasks (CRUD)                                 │   │
│  │  - /api/artifacts (저장/조회)                         │   │
│  │  - /api/analytics (집계)                             │   │
│  └─────────────────────────────────────────────────────┘   │
│                           ↕ gRPC/REST                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │           SignalR Hub (실시간 업데이트)               │   │
│  │  - 프로그램 배포 알림                                  │   │
│  │  - 메트릭 실시간 스트리밍                              │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                           ↕ REST API
┌─────────────────────────────────────────────────────────────┐
│              Program Generator (Python)                      │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  - OpenAI/Anthropic SDK                              │   │
│  │  - LLM 기반 프로그램 생성                              │   │
│  │  - Oracle 검증                                        │   │
│  └─────────────────────────────────────────────────────┘   │
│                           ↕ REST API                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │         Improvement Engine (Python)                  │   │
│  │  - 패턴 분석 (scikit-learn)                          │   │
│  │  - 프로그램 재생성                                     │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                           ↕ REST API
┌─────────────────────────────────────────────────────────────┐
│                  Edge Runtime (다중 옵션)                    │
│  ┌──────────────────────────┬──────────────────────────┐   │
│  │   Python (기본, 오픈소스)   │   C# (.NET 엔터프라이즈)  │   │
│  │  - FastAPI               │  - ASP.NET Core Minimal  │   │
│  │  - 0.02ms 실행           │  - 0.01-0.05ms 실행      │   │
│  │  - 이미 목표 달성          │  - 엔터프라이즈 옵션       │   │
│  └──────────────────────────┴──────────────────────────┘   │
│                           ↕                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │      Dataset Manager (Python)                        │   │
│  │  - JSONL 로깅                                        │   │
│  │  - pandas 분석                                        │   │
│  │  - 5% 샘플링                                         │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## 📅 구현 로드맵

### Phase 4.1: C# Cloud API 기반 구축 (4-6주)
**목표**: ASP.NET Core Web API + 데이터베이스

**작업**:
1. ASP.NET Core 프로젝트 생성
   - Minimal API 또는 Controller 패턴
   - Entity Framework Core (PostgreSQL/SQL Server)
   - Swagger/OpenAPI 문서

2. 핵심 API 엔드포인트
   ```csharp
   POST   /api/tasks                  // 태스크 생성
   GET    /api/tasks/{id}             // 태스크 조회
   POST   /api/artifacts              // 아티팩트 저장
   GET    /api/artifacts/{taskId}     // 아티팩트 조회
   GET    /api/analytics/{taskId}     // 분석 조회
   ```

3. 데이터베이스 스키마
   - Tasks 테이블
   - Artifacts 테이블
   - ExecutionLogs 테이블
   - Analytics 테이블

4. 인증/권한
   - JWT 토큰 기반
   - API Key 지원

**성공 기준**:
- 기본 CRUD 동작
- 10K req/sec 처리 (초기 목표)
- 통합 테스트 통과

### Phase 4.2: Python-C# 통합 (3-4주)
**목표**: Python Generator ↔ C# API 통신

**작업**:
1. Python HTTP 클라이언트
   ```python
   class CloudAPIClient:
       def create_task(self, spec: TaskSpecification) -> Task
       def store_artifact(self, artifact: ProgramArtifact) -> void
       def get_active_artifact(self, task_id: str) -> ProgramArtifact
   ```

2. API 계약 정의
   - OpenAPI/Swagger 스펙
   - Pydantic ↔ C# DTO 매핑
   - 에러 처리 표준화

3. 통합 테스트
   - End-to-end 워크플로우
   - 에러 시나리오
   - 성능 검증

**성공 기준**:
- Generator가 Cloud API 사용
- 기존 Python 테스트 통과
- API 응답 <100ms

### Phase 4.3: SignalR 실시간 업데이트 (2-3주)
**목표**: 실시간 프로그램 배포 알림

**작업**:
1. SignalR Hub 구현
   ```csharp
   public class RuntimeHub : Hub
   {
       public async Task SubscribeToTask(string taskId)
       public async Task NotifyProgramUpdate(string taskId, int version)
   }
   ```

2. Edge Runtime 통합
   - SignalR 클라이언트 (Python/C#)
   - 자동 프로그램 업데이트
   - 버전 전환 알림

3. 모니터링
   - 연결 상태 추적
   - 메시지 전송 통계

**성공 기준**:
- Edge Runtime 자동 업데이트
- <1초 알림 지연
- 1000+ 동시 연결 지원

### Phase 4.4: C# Edge Runtime (선택사항, 4-6주)
**목표**: .NET 엔터프라이즈 고객용

**작업**:
1. ASP.NET Core Minimal API
   ```csharp
   var app = WebApplication.CreateBuilder(args).Build();

   app.MapPost("/execute", async (ExecuteRequest req) => {
       var result = await executor.Execute(program, req.Input);
       return new ExecuteResponse(result.Output, result.LatencyMs);
   });
   ```

2. C# Program Executor
   - Roslyn 스크립팅 API
   - 샌드박스 실행
   - 성능 최적화

3. 호환성 검증
   - Python 아티팩트 → C# 실행
   - API 동등성
   - 성능 비교

**성공 기준**:
- Python과 동일한 API
- <5ms 실행 지연
- 모든 통합 테스트 통과

### Phase 4.5: 성능 최적화 (3-4주)
**목표**: 100K+ req/sec 달성

**작업**:
1. C# API 최적화
   - 응답 캐싱
   - 데이터베이스 연결 풀링
   - gRPC 도입 (필요시)

2. 부하 테스트
   - Apache Bench / wrk
   - K6 스크립트
   - 프로덕션 시뮬레이션

3. 모니터링 설정
   - Application Insights (Azure)
   - Prometheus + Grafana
   - 분산 추적 (OpenTelemetry)

**성공 기준**:
- 100K req/sec 달성
- p99 지연시간 <50ms
- 안정적인 메모리 사용

---

## 🔄 마이그레이션 전략

### 단계적 전환 (Strangler Fig Pattern)

**Phase 1: Python 기반 유지 (현재)**
```
사용자 → Python Edge Runtime → 로컬 JSONL
```

**Phase 2: Cloud API 추가 (병렬 운영)**
```
Generator → C# Cloud API (신규)
           ↓
사용자 → Python Edge Runtime → C# API로 데이터 전송
```

**Phase 3: 완전 통합**
```
Generator → C# Cloud API
           ↓ SignalR
사용자 → Python/C# Edge Runtime → C# Cloud API
```

### 호환성 보장

**API 버전 관리**:
```
/api/v1/tasks    # 초기 버전
/api/v2/tasks    # 향상된 버전
```

**하위 호환성**:
- Python 클라이언트가 C# API 사용 가능
- 기존 Python Edge Runtime 계속 지원
- JSONL 형식 유지

---

## 💰 비용-효과 분석

### 개발 비용
| 항목 | Python 전용 | 하이브리드 | 차이 |
|------|------------|----------|------|
| Cloud API | 6주 | 8주 (+33%) | C# 학습 곡선 |
| Edge Runtime | 완료 | +4주 (C# 옵션) | 엔터프라이즈용 |
| 유지보수 | 낮음 | 중간 (+20%) | 두 언어 관리 |
| **총 개발 시간** | - | **+12-16주** | 초기 투자 |

### 성능 이득
| 메트릭 | Python 전용 | 하이브리드 | 개선 |
|--------|------------|----------|------|
| Cloud API | 10K req/sec | 100K req/sec | **10배** |
| Edge Runtime | 0.02ms | 0.02ms (Python) / 0.01ms (C#) | 유사 |
| 실시간 업데이트 | 폴링 (5초) | SignalR (<1초) | **5배+** |

### ROI 분석
**투자**: 12-16주 추가 개발
**리턴**:
- ✅ 엔터프라이즈 고객 확보 (.NET 기업)
- ✅ 10배 높은 처리량 (비용 절감)
- ✅ 실시간 기능 (경쟁 우위)
- ✅ 확장성 (미래 성장 대비)

**결론**: 중장기적으로 **가치 있는 투자**

---

## 🎯 최종 권장 사항

### ✅ 하이브리드 아키텍처 채택 권장

**이유**:
1. **성능**: Cloud API 10배 향상 (10K → 100K req/sec)
2. **시장**: .NET 엔터프라이즈 고객 확보
3. **기술**: 각 언어의 강점 최대 활용
4. **경쟁력**: SignalR 실시간 기능으로 차별화

### 📅 권장 실행 계획

**즉시 시작** (Phase 4.1-4.3):
1. C# Cloud API 개발
2. Python Generator 통합
3. SignalR 실시간 업데이트

**나중에** (Phase 4.4):
- C# Edge Runtime은 시장 반응 보고 결정
- 엔터프라이즈 고객 요구 시 개발

**보류** (현재는 불필요):
- Python Edge Runtime 교체
- 이미 충분히 빠름 (0.02ms)

### ⚠️ 주의 사항

1. **명확한 경계 설정**
   - API 계약 먼저 정의
   - 각 컴포넌트 독립성 확보

2. **팀 역량 구축**
   - C# 개발자 확보 또는 교육
   - Python 팀과 협업 프로세스

3. **점진적 전환**
   - Strangler Fig 패턴 사용
   - 하위 호환성 유지

4. **모니터링 통합**
   - 처음부터 통합 모니터링 구축
   - 성능 메트릭 수집

---

## 📊 성공 지표 (KPI)

### 기술적 KPI
- [ ] Cloud API: 100K+ req/sec 달성
- [ ] API 응답: p99 <50ms
- [ ] SignalR: 1000+ 동시 연결
- [ ] Edge Runtime: <10ms 유지 (Python/C# 모두)

### 비즈니스 KPI
- [ ] .NET 엔터프라이즈 고객 확보 (3개월 내 5+ 계약)
- [ ] 처리량 증가로 인한 인프라 비용 절감 (30%+)
- [ ] 실시간 업데이트 만족도 (고객 설문 4.5/5 이상)

### 운영 KPI
- [ ] 배포 파이프라인 통합 (단일 CI/CD)
- [ ] 모니터링 대시보드 구축
- [ ] 장애 복구 시간 <15분

---

**다음 단계**: Phase 4.1 C# Cloud API 개발 착수 여부 결정

**예상 타임라인**: v0.2 완료까지 3-4개월
