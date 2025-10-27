# Phase 8: .NET SDK 개발 (진행 중)

**시작일**: 2025-10-27
**목표**: Loopai 플랫폼을 위한 프로덕션급 .NET 클라이언트 SDK 개발

---

## 📊 개요

Phase 6 계획에 따라 .NET SDK를 구현하여 개발자가 Loopai API를 쉽게 통합할 수 있도록 합니다.

**주요 목표**:
- ✅ 타입 세이프한 .NET 클라이언트 라이브러리
- ✅ 배치 API 지원
- ✅ 자동 재시도 및 복원력 (Polly)
- ✅ Dependency Injection 지원
- ⏳ 포괄적인 테스트 및 문서화

---

## 🎯 완료된 작업

### 1. 프로젝트 구조

**프로젝트 생성**:
```
src/Loopai.Client/              # SDK 클라이언트 라이브러리
tests/Loopai.Client.Tests/      # 단위 및 통합 테스트
examples/Loopai.Examples.AspNetCore/  # 사용 예제
```

### 2. 코어 클라이언트 구현

**파일**: `src/Loopai.Client/LoopaiClient.cs` (~240 lines)

**주요 기능**:
```csharp
public class LoopaiClient : ILoopaiClient
{
    // 생성자
    public LoopaiClient(LoopaiClientOptions options)
    public LoopaiClient(IOptions<LoopaiClientOptions> options)
    public LoopaiClient(string baseUrl, string? apiKey = null)

    // Task 관리
    Task<JsonDocument> CreateTaskAsync(...)
    Task<JsonDocument> GetTaskAsync(Guid taskId, ...)

    // 실행
    Task<JsonDocument> ExecuteAsync(Guid taskId, JsonDocument input, ...)
    Task<JsonDocument> ExecuteAsync(Guid taskId, object input, ...)  // 간편 버전

    // 배치 실행 (NEW)
    Task<BatchExecuteResponse> BatchExecuteAsync(BatchExecuteRequest request, ...)
    Task<BatchExecuteResponse> BatchExecuteAsync(Guid taskId, IEnumerable<object> inputs, ...)

    // Health
    Task<HealthResponse> GetHealthAsync(...)
}
```

**Polly를 이용한 자동 재시도**:
```csharp
private ResiliencePipeline<HttpResponseMessage> BuildRetryPipeline()
{
    return new ResiliencePipelineBuilder<HttpResponseMessage>()
        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = _options.MaxRetries,
            Delay = _options.RetryDelay,
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(r => r.StatusCode == HttpStatusCode.RequestTimeout ||
                                  r.StatusCode == HttpStatusCode.TooManyRequests ||
                                  (int)r.StatusCode >= 500)
        })
        .Build();
}
```

**에러 핸들링**:
```csharp
private async Task<T> HandleResponseAsync<T>(HttpResponseMessage response)
{
    if (!response.IsSuccessStatusCode)
    {
        var errorContent = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new ValidationException($"Validation failed: {errorContent}", ...);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new LoopaiException($"Resource not found: {errorContent}", ...);

        if (response.StatusCode == HttpStatusCode.InternalServerError)
            throw new ExecutionException($"Server error: {errorContent}", ...);
    }

    return await response.Content.ReadFromJsonAsync<T>();
}
```

### 3. 배치 API 모델

**파일**: `src/Loopai.Client/Models/BatchExecuteRequest.cs` (~60 lines)

```csharp
public record BatchExecuteRequest
{
    public required Guid TaskId { get; init; }
    public required IEnumerable<BatchExecuteItem> Items { get; init; }
    public int? Version { get; init; }
    public int? MaxConcurrency { get; init; } = 10;
    public bool StopOnFirstError { get; init; } = false;
    public int? TimeoutMs { get; init; }
}

public record BatchExecuteItem
{
    public required string Id { get; init; }  // 상관 ID
    public required JsonDocument Input { get; init; }
    public bool ForceValidation { get; init; } = false;
}
```

**파일**: `src/Loopai.Client/Models/BatchExecuteResponse.cs` (~90 lines)

```csharp
public record BatchExecuteResponse
{
    public required Guid BatchId { get; init; }
    public required Guid TaskId { get; init; }
    public required int Version { get; init; }
    public required int TotalItems { get; init; }
    public required int SuccessCount { get; init; }
    public required int FailureCount { get; init; }
    public required double TotalDurationMs { get; init; }
    public required double AvgLatencyMs { get; init; }
    public required IReadOnlyList<BatchExecuteResult> Results { get; init; }
    public required DateTime StartedAt { get; init; }
    public required DateTime CompletedAt { get; init; }
}

public record BatchExecuteResult
{
    public required string Id { get; init; }  // 요청 Item ID와 매칭
    public required Guid ExecutionId { get; init; }
    public required bool Success { get; init; }
    public JsonDocument? Output { get; init; }
    public string? ErrorMessage { get; init; }
    public required double LatencyMs { get; init; }
    public required bool SampledForValidation { get; init; }
}
```

### 4. Dependency Injection 지원

**파일**: `src/Loopai.Client/ServiceCollectionExtensions.cs`

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLoopaiClient(
        this IServiceCollection services,
        Action<LoopaiClientOptions> configure)
    {
        services.Configure(configure);
        services.AddHttpClient<ILoopaiClient, LoopaiClient>();
        services.AddSingleton<ILoopaiClient, LoopaiClient>();
        return services;
    }
}
```

**사용 예제**:
```csharp
// Program.cs
builder.Services.AddLoopaiClient(options =>
{
    options.BaseUrl = "https://api.loopai.dev";
    options.ApiKey = builder.Configuration["Loopai:ApiKey"];
    options.Timeout = TimeSpan.FromSeconds(60);
    options.MaxRetries = 5;
});

// Controller
public class ClassificationController : ControllerBase
{
    private readonly ILoopaiClient _loopai;

    public ClassificationController(ILoopaiClient loopai)
    {
        _loopai = loopai;
    }

    [HttpPost("classify")]
    public async Task<IActionResult> Classify([FromBody] ClassifyRequest request)
    {
        var result = await _loopai.ExecuteAsync(
            request.TaskId,
            new { text = request.Text });

        return Ok(result);
    }
}
```

### 5. 구성 옵션

**파일**: `src/Loopai.Client/LoopaiClientOptions.cs`

```csharp
public class LoopaiClientOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8080";
    public string? ApiKey { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public bool EnableDetailedLogging { get; set; } = false;
    public ILogger? Logger { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new ArgumentException("BaseUrl is required", nameof(BaseUrl));

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            throw new ArgumentException("BaseUrl must be a valid URI", nameof(BaseUrl));

        if (MaxRetries < 0 || MaxRetries > 10)
            throw new ArgumentOutOfRangeException(nameof(MaxRetries), "MaxRetries must be between 0 and 10");
    }
}
```

### 6. 예외 클래스

**기존 파일**:
- `src/Loopai.Client/Exceptions/LoopaiException.cs` - 기본 예외
- `src/Loopai.Client/Exceptions/ValidationException.cs` - 검증 실패
- `src/Loopai.Client/Exceptions/ExecutionException.cs` - 실행 오류

### 7. 빌드 및 패키지 참조 수정

**Loopai.Core 패키지 업데이트**:
- `Microsoft.Extensions.Configuration.Abstractions` 9.0.10
- `Microsoft.Extensions.DependencyInjection.Abstractions` 9.0.10
- `Microsoft.Extensions.Options` 9.0.10
- `Microsoft.Extensions.Options.ConfigurationExtensions` 9.0.10

**빌드 성공**:
```bash
cd src/Loopai.Client && dotnet build
# Build succeeded.
```

---

## 📁 파일 구조

```
src/Loopai.Client/
├── LoopaiClient.cs                      # 코어 클라이언트 구현 (~240 lines)
├── ILoopaiClient.cs                     # 클라이언트 인터페이스 (~105 lines)
├── LoopaiClientOptions.cs               # 구성 옵션 (~40 lines)
├── ServiceCollectionExtensions.cs       # DI 확장 (~30 lines)
├── Models/
│   ├── HealthResponse.cs               # Health 응답 모델
│   ├── BatchExecuteRequest.cs          # 배치 요청 모델 (NEW ~60 lines)
│   └── BatchExecuteResponse.cs         # 배치 응답 모델 (NEW ~90 lines)
└── Exceptions/
    ├── LoopaiException.cs              # 기본 예외
    ├── ValidationException.cs          # 검증 예외
    └── ExecutionException.cs           # 실행 예외

tests/Loopai.Client.Tests/
└── (테스트 파일 예정)

examples/Loopai.Examples.AspNetCore/
├── Program.cs
└── Controllers/
    └── ClassificationController.cs
```

---

## 🔧 기술 스택

- **.NET 8.0**: 최신 LTS 버전
- **Polly**: 복원력 및 재시도 패턴
- **System.Text.Json**: JSON 직렬화
- **Microsoft.Extensions.DependencyInjection**: DI 지원
- **Microsoft.Extensions.Http**: HTTP 클라이언트 팩토리
- **Microsoft.Extensions.Logging**: 구조화된 로깅

---

## 📊 코드 통계

**신규 파일**: 2개 (~150 lines)
- `BatchExecuteRequest.cs`: ~60 lines
- `BatchExecuteResponse.cs`: ~90 lines

**수정 파일**: 2개 (+50 lines)
- `ILoopaiClient.cs`: +20 lines (배치 API 메서드)
- `LoopaiClient.cs`: +50 lines (배치 API 구현)

**Loopai.Core 수정**: 1개
- `Loopai.Core.csproj`: 패키지 참조 추가

**총 SDK 라인**: ~550 lines (기존 + 신규)

---

## ⏳ 남은 작업

### 1. 단위 테스트 작성
**예상 공수**: 2-3시간

테스트 시나리오:
- [ ] LoopaiClient 생성자 및 구성 검증
- [ ] HTTP 요청/응답 처리 (Moq)
- [ ] 재시도 로직 검증
- [ ] 에러 핸들링 테스트
- [ ] 배치 API 로직 테스트

### 2. 통합 테스트 작성
**예상 공수**: 2-3시간

테스트 시나리오:
- [ ] 실제 API 호출 (TestServer)
- [ ] 엔드투엔드 Task 생성 및 실행
- [ ] 배치 실행 시나리오
- [ ] 에러 시나리오 (404, 500 등)

### 3. 예제 프로젝트 완성
**예상 공수**: 1-2시간

예제 시나리오:
- [ ] 기본 Task 실행 예제
- [ ] 배치 실행 예제
- [ ] 에러 핸들링 예제
- [ ] DI 통합 예제
- [ ] 텔레메트리 로깅 예제

### 4. README 및 문서화
**예상 공수**: 2-3시간

문서 구성:
- [ ] Quick Start 가이드
- [ ] API 참조 문서 (XML 주석)
- [ ] 배치 API 가이드
- [ ] 에러 핸들링 가이드
- [ ] 구성 옵션 설명
- [ ] 사용 예제 컬렉션

### 5. NuGet 패키지 준비
**예상 공수**: 1-2시간

준비 사항:
- [ ] .csproj 메타데이터 (패키지 ID, 설명, 저자, 라이센스)
- [ ] 아이콘 및 README.md 추가
- [ ] 버전 관리 (SemVer)
- [ ] NuGet.org 게시 준비

---

## 🎯 사용 예제

### 기본 사용

```csharp
using Loopai.Client;
using System.Text.Json;

// 클라이언트 생성
var client = new LoopaiClient("https://api.loopai.dev", "sk-...");

// Task 생성
var task = await client.CreateTaskAsync(
    name: "spam-detection",
    description: "Classify emails as spam or ham",
    inputSchema: JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"text\":{\"type\":\"string\"}}}"),
    outputSchema: JsonDocument.Parse("{\"type\":\"string\",\"enum\":[\"spam\",\"ham\"]}"));

// 실행
var result = await client.ExecuteAsync(
    taskId: Guid.Parse(task.RootElement.GetProperty("id").GetString()!),
    input: new { text = "Buy now!" });

Console.WriteLine($"Result: {result.RootElement.GetProperty("output")}");
```

### 배치 실행

```csharp
// 간편 버전
var inputs = new[]
{
    new { text = "Buy now!" },
    new { text = "Meeting at 2pm" },
    new { text = "Free money!!!" }
};

var batchResult = await client.BatchExecuteAsync(taskId, inputs, maxConcurrency: 10);

Console.WriteLine($"Batch: {batchResult.SuccessCount}/{batchResult.TotalItems} succeeded");
Console.WriteLine($"Avg latency: {batchResult.AvgLatencyMs:F2}ms");

foreach (var item in batchResult.Results)
{
    if (item.Success)
        Console.WriteLine($"Item {item.Id}: {item.Output}");
    else
        Console.WriteLine($"Item {item.Id}: Error - {item.ErrorMessage}");
}
```

### Dependency Injection

```csharp
// Program.cs
builder.Services.AddLoopaiClient(options =>
{
    options.BaseUrl = builder.Configuration["Loopai:BaseUrl"]!;
    options.ApiKey = builder.Configuration["Loopai:ApiKey"]!;
    options.MaxRetries = 5;
    options.EnableDetailedLogging = true;
});

// Controller
public class SpamDetectionController : ControllerBase
{
    private readonly ILoopaiClient _loopai;
    private readonly Guid _taskId = Guid.Parse("...");

    public SpamDetectionController(ILoopaiClient loopai)
    {
        _loopai = loopai;
    }

    [HttpPost("classify")]
    public async Task<IActionResult> Classify([FromBody] EmailRequest request)
    {
        try
        {
            var result = await _loopai.ExecuteAsync(_taskId, new { text = request.Text });
            return Ok(new { classification = result.RootElement.GetProperty("output").GetString() });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ExecutionException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
```

---

## 🚀 다음 단계

### Option A: SDK 테스트 및 문서화 완성 (권장)
- 단위 테스트 작성
- 통합 테스트 작성
- 예제 프로젝트 완성
- README 및 API 문서
- **예상 공수**: 1-2일

### Option B: 다른 언어 SDK 개발
- Python SDK (Phase 6.4)
- JavaScript/TypeScript SDK (Phase 6.5)
- **예상 공수**: 3-5일 per SDK

### Option C: Plugin System 구현
- Phase 6.2 계획에 따른 플러그인 시스템
- **예상 공수**: 3-5일

---

## 📋 체크리스트

**Core SDK**:
- [x] LoopaiClient 클래스
- [x] ILoopaiClient 인터페이스
- [x] LoopaiClientOptions 구성
- [x] ServiceCollectionExtensions (DI)
- [x] Task 생성/조회 API
- [x] 단일 실행 API
- [x] 배치 실행 API (NEW)
- [x] Health Check API
- [x] 예외 클래스
- [x] Polly 재시도 패턴
- [x] 빌드 성공

**테스트** (⏳ 진행 예정):
- [ ] 단위 테스트 (90%+ 커버리지)
- [ ] 통합 테스트
- [ ] 배치 API 테스트
- [ ] 에러 시나리오 테스트

**문서화** (⏳ 진행 예정):
- [ ] README.md
- [ ] Quick Start 가이드
- [ ] API 참조 문서
- [ ] 배치 API 가이드
- [ ] 사용 예제

**배포 준비** (⏳ 진행 예정):
- [ ] NuGet 패키지 메타데이터
- [ ] 버전 관리
- [ ] 라이센스 파일
- [ ] 아이콘 및 로고

---

## 🎓 학습 사항

1. **Polly 복원력 패턴**: 자동 재시도 및 에러 핸들링
2. **.NET 제너릭 호스트**: DI 및 구성 통합
3. **HttpClient 팩토리**: 효율적인 HTTP 클라이언트 관리
4. **System.Text.Json**: 고성능 JSON 직렬화
5. **배치 API 설계**: 대규모 처리를 위한 클라이언트 패턴

---

**Phase 8 진행 중** ⏳

핵심 SDK 구현 완료, 테스트 및 문서화 작업 남음
