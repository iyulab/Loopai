# Phase 6 Plan: SDK Development & Extensibility

**Status**: 📋 **PLANNING**
**Priority**: HIGH
**Timeline**: 4-6 weeks
**Dependencies**: Phase 5 (Complete ✅)

---

## Overview

Phase 6 focuses on **developer experience** and **extensibility** to make Loopai easy to integrate and customize. This phase transforms Loopai from a standalone framework into a platform with rich SDKs, plugin system, and batch operations.

### Goals

1. **Easy Integration**: Client SDKs for .NET, Python, JavaScript
2. **Extensibility**: Plugin system for validators, samplers, and integrations
3. **Batch Operations**: Efficient bulk processing APIs
4. **Developer Experience**: Examples, documentation, tooling

---

## Phase 6.1: .NET SDK (Week 1-2)

### Objectives

Create a production-ready .NET client library for Loopai integration.

### Features

#### 1. Client Library (`Loopai.Client`)

**Package**: `Loopai.Client` NuGet package

**Core Components**:
```csharp
// LoopaiClient.cs - Main client
public class LoopaiClient : IDisposable
{
    public LoopaiClient(string baseUrl, string apiKey);
    public LoopaiClient(LoopaiClientOptions options);

    // Tasks
    public Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, CancellationToken ct = default);
    public Task<TaskResponse> GetTaskAsync(Guid taskId, CancellationToken ct = default);
    public Task<PaginatedResponse<TaskResponse>> ListTasksAsync(TaskListOptions options, CancellationToken ct = default);

    // Execution
    public Task<ExecuteResponse> ExecuteAsync(Guid taskId, ExecuteRequest request, CancellationToken ct = default);
    public Task<ExecuteResponse> ExecuteAsync(string taskId, object input, CancellationToken ct = default);

    // Validation
    public Task<ValidationResponse> ValidateAsync(Guid taskId, ValidationRequest request, CancellationToken ct = default);
    public Task<ValidationRecommendations> GetRecommendationsAsync(Guid taskId, CancellationToken ct = default);

    // Webhooks
    public Task<WebhookSubscription> SubscribeAsync(WebhookSubscriptionRequest request, CancellationToken ct = default);
    public Task UnsubscribeAsync(Guid subscriptionId, CancellationToken ct = default);

    // Health
    public Task<HealthResponse> GetHealthAsync(CancellationToken ct = default);
}
```

**Features**:
- ✅ Automatic retry with exponential backoff
- ✅ Request/response logging
- ✅ Telemetry integration (OpenTelemetry)
- ✅ Typed exceptions (`LoopaiException`, `ValidationException`, `ExecutionException`)
- ✅ Strongly-typed request/response models
- ✅ Fluent configuration

#### 2. Configuration

```csharp
// LoopaiClientOptions.cs
public class LoopaiClientOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8080";
    public string ApiKey { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
    public bool EnableTelemetry { get; set; } = true;
    public ILogger? Logger { get; set; }
}

// Usage
var client = new LoopaiClient(new LoopaiClientOptions
{
    BaseUrl = "https://api.loopai.dev",
    ApiKey = "sk-...",
    Timeout = TimeSpan.FromSeconds(60),
    MaxRetries = 5
});
```

#### 3. Dependency Injection Support

```csharp
// ServiceCollectionExtensions.cs
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

// Startup.cs / Program.cs
builder.Services.AddLoopaiClient(options =>
{
    options.BaseUrl = builder.Configuration["Loopai:BaseUrl"];
    options.ApiKey = builder.Configuration["Loopai:ApiKey"];
});

// Controller
public class MyController : ControllerBase
{
    private readonly ILoopaiClient _loopai;

    public MyController(ILoopaiClient loopai)
    {
        _loopai = loopai;
    }

    [HttpPost("classify")]
    public async Task<IActionResult> Classify([FromBody] ClassifyRequest request)
    {
        var result = await _loopai.ExecuteAsync("spam-detection", new { text = request.Text });
        return Ok(result);
    }
}
```

#### 4. Examples Project

**Project**: `examples/Loopai.Examples.AspNetCore/`

**Scenarios**:
- Basic task creation and execution
- Webhook event handling
- Batch processing
- Error handling and retry logic
- Telemetry and logging integration

### Deliverables

- [x] NuGet package `Loopai.Client`
- [x] XML documentation for IntelliSense
- [x] Unit tests (90%+ coverage)
- [x] Integration tests with real API
- [x] Example ASP.NET Core project
- [x] README with quick start guide

---

## Phase 6.2: Plugin System (Week 2-3)

### Objectives

Create an extensible plugin architecture for validators, samplers, and integrations.

### Architecture

```
┌─────────────────────────────────────────┐
│         Plugin Host (Loopai.Core)       │
│  ┌───────────────────────────────────┐  │
│  │   IPluginRegistry                 │  │
│  │   - Register<T>()                 │  │
│  │   - Resolve<T>()                  │  │
│  │   - List<T>()                     │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
                  │
        ┌─────────┼─────────┐
        ▼         ▼         ▼
   ┌────────┐ ┌────────┐ ┌────────┐
   │Validator│ │Sampler │ │Webhook │
   │ Plugin  │ │ Plugin │ │ Plugin │
   └────────┘ └────────┘ └────────┘
```

### Plugin Interfaces

#### 1. Validator Plugin

```csharp
// IValidatorPlugin.cs
public interface IValidatorPlugin
{
    string Name { get; }
    string Description { get; }
    string Version { get; }

    Task<ValidationResult> ValidateAsync(
        ExecutionRecord execution,
        ValidationContext context,
        CancellationToken cancellationToken = default);
}

// Example: Custom JSON Schema Validator
public class CustomSchemaValidatorPlugin : IValidatorPlugin
{
    public string Name => "custom-schema-validator";
    public string Description => "Validates against custom JSON schemas";
    public string Version => "1.0.0";

    public async Task<ValidationResult> ValidateAsync(
        ExecutionRecord execution,
        ValidationContext context,
        CancellationToken cancellationToken)
    {
        // Custom validation logic
        var schema = context.GetSchema();
        var isValid = ValidateAgainstSchema(execution.OutputData, schema);

        return new ValidationResult
        {
            IsValid = isValid,
            ValidatorType = Name,
            Message = isValid ? "Valid" : "Schema validation failed"
        };
    }
}
```

#### 2. Sampler Plugin

```csharp
// ISamplerPlugin.cs
public interface ISamplerPlugin
{
    string Name { get; }
    string Description { get; }
    string Version { get; }

    Task<SamplingDecision> ShouldSampleAsync(
        ExecutionRecord execution,
        SamplingContext context,
        CancellationToken cancellationToken = default);
}

// Example: Time-Based Sampler
public class TimeBasedSamplerPlugin : ISamplerPlugin
{
    public string Name => "time-based-sampler";
    public string Description => "Samples based on time intervals";
    public string Version => "1.0.0";

    public async Task<SamplingDecision> ShouldSampleAsync(
        ExecutionRecord execution,
        SamplingContext context,
        CancellationToken cancellationToken)
    {
        // Sample every 5 minutes
        var lastSampleTime = await context.GetLastSampleTimeAsync();
        var timeSinceLastSample = DateTime.UtcNow - lastSampleTime;

        return new SamplingDecision
        {
            ShouldSample = timeSinceLastSample >= TimeSpan.FromMinutes(5),
            Reason = $"Last sample: {timeSinceLastSample.TotalMinutes:F2} minutes ago"
        };
    }
}
```

#### 3. Webhook Handler Plugin

```csharp
// IWebhookHandlerPlugin.cs
public interface IWebhookHandlerPlugin
{
    string Name { get; }
    string Description { get; }
    string Version { get; }
    IEnumerable<WebhookEventType> SupportedEvents { get; }

    Task HandleAsync(
        WebhookEvent webhookEvent,
        WebhookHandlerContext context,
        CancellationToken cancellationToken = default);
}

// Example: Slack Notification Handler
public class SlackWebhookHandlerPlugin : IWebhookHandlerPlugin
{
    public string Name => "slack-webhook-handler";
    public string Description => "Sends notifications to Slack";
    public string Version => "1.0.0";
    public IEnumerable<WebhookEventType> SupportedEvents => new[]
    {
        WebhookEventType.ProgramExecutionFailed,
        WebhookEventType.CanaryRollback
    };

    public async Task HandleAsync(
        WebhookEvent webhookEvent,
        WebhookHandlerContext context,
        CancellationToken cancellationToken)
    {
        var slackWebhook = context.Configuration["SlackWebhookUrl"];
        var message = FormatSlackMessage(webhookEvent);
        await SendToSlackAsync(slackWebhook, message, cancellationToken);
    }
}
```

### Plugin Discovery and Registration

```csharp
// PluginRegistry.cs
public class PluginRegistry : IPluginRegistry
{
    private readonly Dictionary<Type, List<object>> _plugins = new();

    public void Register<T>(T plugin) where T : class
    {
        var type = typeof(T);
        if (!_plugins.ContainsKey(type))
            _plugins[type] = new List<object>();

        _plugins[type].Add(plugin);
    }

    public T? Resolve<T>(string name) where T : class
    {
        var type = typeof(T);
        if (!_plugins.ContainsKey(type))
            return null;

        return _plugins[type]
            .OfType<T>()
            .FirstOrDefault(p => GetPluginName(p) == name);
    }

    public IEnumerable<T> List<T>() where T : class
    {
        var type = typeof(T);
        return _plugins.ContainsKey(type)
            ? _plugins[type].OfType<T>()
            : Enumerable.Empty<T>();
    }
}

// Program.cs
builder.Services.AddSingleton<IPluginRegistry, PluginRegistry>();

// Register plugins
var registry = app.Services.GetRequiredService<IPluginRegistry>();
registry.Register<IValidatorPlugin>(new CustomSchemaValidatorPlugin());
registry.Register<ISamplerPlugin>(new TimeBasedSamplerPlugin());
registry.Register<IWebhookHandlerPlugin>(new SlackWebhookHandlerPlugin());
```

### Plugin Configuration

```json
// appsettings.json
{
  "Loopai": {
    "Plugins": {
      "Validators": [
        {
          "Name": "custom-schema-validator",
          "Enabled": true,
          "Priority": 10,
          "Configuration": {
            "SchemaPath": "/schemas/custom-schema.json"
          }
        }
      ],
      "Samplers": [
        {
          "Name": "time-based-sampler",
          "Enabled": true,
          "Configuration": {
            "IntervalMinutes": 5
          }
        }
      ],
      "WebhookHandlers": [
        {
          "Name": "slack-webhook-handler",
          "Enabled": true,
          "Configuration": {
            "SlackWebhookUrl": "https://hooks.slack.com/services/..."
          }
        }
      ]
    }
  }
}
```

### Deliverables

- [x] Plugin interfaces (`IValidatorPlugin`, `ISamplerPlugin`, `IWebhookHandlerPlugin`)
- [x] Plugin registry and discovery
- [x] Configuration system
- [x] Example plugins (3-5)
- [x] Plugin development guide
- [x] Unit tests for plugin system

---

## Phase 6.3: Batch Operations API (Week 3-4)

### Objectives

Efficient bulk processing capabilities for high-throughput scenarios.

### Features

#### 1. Batch Execution API

```csharp
// BatchExecuteRequest.cs
public record BatchExecuteRequest
{
    public required Guid TaskId { get; init; }
    public required IEnumerable<BatchExecuteItem> Items { get; init; }
    public int? MaxConcurrency { get; init; }
    public bool StopOnFirstError { get; init; } = false;
}

public record BatchExecuteItem
{
    public required string Id { get; init; }  // Client-provided correlation ID
    public required JsonDocument Input { get; init; }
    public int? TimeoutMs { get; init; }
}

// BatchExecuteResponse.cs
public record BatchExecuteResponse
{
    public required int TotalItems { get; init; }
    public required int SuccessCount { get; init; }
    public required int FailureCount { get; init; }
    public required IEnumerable<BatchExecuteResult> Results { get; init; }
    public required double TotalDurationMs { get; init; }
}

public record BatchExecuteResult
{
    public required string Id { get; init; }  // Matches request item ID
    public required bool Success { get; init; }
    public JsonDocument? OutputData { get; init; }
    public string? ErrorMessage { get; init; }
    public required double DurationMs { get; init; }
}
```

**Controller**:
```csharp
// TasksController.cs
[HttpPost("batch/execute")]
[ProducesResponseType(typeof(BatchExecuteResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> BatchExecute([FromBody] BatchExecuteRequest request)
{
    var stopwatch = Stopwatch.StartNew();
    var results = new List<BatchExecuteResult>();

    var semaphore = new SemaphoreSlim(request.MaxConcurrency ?? 10);
    var tasks = request.Items.Select(async item =>
    {
        await semaphore.WaitAsync();
        try
        {
            var itemStopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _executionService.ExecuteAsync(
                    request.TaskId,
                    item.Input,
                    item.TimeoutMs,
                    HttpContext.RequestAborted);

                itemStopwatch.Stop();
                return new BatchExecuteResult
                {
                    Id = item.Id,
                    Success = true,
                    OutputData = result.OutputData,
                    DurationMs = itemStopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                itemStopwatch.Stop();
                return new BatchExecuteResult
                {
                    Id = item.Id,
                    Success = false,
                    ErrorMessage = ex.Message,
                    DurationMs = itemStopwatch.ElapsedMilliseconds
                };
            }
        }
        finally
        {
            semaphore.Release();
        }
    });

    results.AddRange(await Task.WhenAll(tasks));
    stopwatch.Stop();

    return Ok(new BatchExecuteResponse
    {
        TotalItems = results.Count,
        SuccessCount = results.Count(r => r.Success),
        FailureCount = results.Count(r => !r.Success),
        Results = results,
        TotalDurationMs = stopwatch.ElapsedMilliseconds
    });
}
```

#### 2. Streaming Execution API

```csharp
// StreamExecuteRequest.cs
public record StreamExecuteRequest
{
    public required Guid TaskId { get; init; }
    public int? MaxConcurrency { get; init; }
}

// Controller with Server-Sent Events (SSE)
[HttpPost("stream/execute")]
[Produces("text/event-stream")]
public async Task StreamExecute([FromBody] StreamExecuteRequest request)
{
    Response.ContentType = "text/event-stream";
    Response.Headers.Add("Cache-Control", "no-cache");
    Response.Headers.Add("Connection", "keep-alive");

    // Read input items from request body stream
    await foreach (var item in ReadStreamItemsAsync(Request.Body, HttpContext.RequestAborted))
    {
        try
        {
            var result = await _executionService.ExecuteAsync(
                request.TaskId,
                item.Input,
                item.TimeoutMs,
                HttpContext.RequestAborted);

            var sseData = JsonSerializer.Serialize(new
            {
                id = item.Id,
                success = true,
                output = result.OutputData,
                duration_ms = result.LatencyMs
            });

            await Response.WriteAsync($"data: {sseData}\n\n");
            await Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            var sseData = JsonSerializer.Serialize(new
            {
                id = item.Id,
                success = false,
                error = ex.Message
            });

            await Response.WriteAsync($"data: {sseData}\n\n");
            await Response.Body.FlushAsync();
        }
    }
}
```

#### 3. Batch Validation API

```csharp
// BatchValidateRequest.cs
public record BatchValidateRequest
{
    public required Guid TaskId { get; init; }
    public required IEnumerable<BatchValidateItem> Items { get; init; }
}

public record BatchValidateItem
{
    public required string Id { get; init; }
    public required JsonDocument Input { get; init; }
    public required JsonDocument Output { get; init; }
    public required JsonDocument ExpectedOutput { get; init; }
}

// BatchValidateResponse.cs
public record BatchValidateResponse
{
    public required int TotalItems { get; init; }
    public required int ValidCount { get; init; }
    public required int InvalidCount { get; init; }
    public required double AccuracyRate { get; init; }
    public required IEnumerable<BatchValidateResult> Results { get; init; }
}
```

### Deliverables

- [x] Batch execution endpoint
- [x] Streaming execution endpoint (SSE)
- [x] Batch validation endpoint
- [x] Concurrency control
- [x] Progress reporting
- [x] Performance tests (1K, 10K, 100K items)
- [x] SDK support for batch operations

---

## Phase 6.4: Python SDK (Week 4-5)

### Objectives

Create a Python client library for data science and ML workflows.

### Features

```python
# loopai/__init__.py
from .client import LoopaiClient
from .models import Task, ExecutionResult, ValidationResult
from .exceptions import LoopaiException, ValidationException

__version__ = "1.0.0"

# loopai/client.py
from typing import Optional, Dict, Any, List
import httpx
from .models import Task, ExecutionResult, ValidationResult

class LoopaiClient:
    def __init__(
        self,
        base_url: str = "http://localhost:8080",
        api_key: Optional[str] = None,
        timeout: float = 30.0,
        max_retries: int = 3
    ):
        self.base_url = base_url.rstrip("/")
        self.api_key = api_key
        self.timeout = timeout
        self.max_retries = max_retries
        self._client = httpx.AsyncClient(timeout=timeout)

    async def create_task(self, request: Dict[str, Any]) -> Task:
        """Create a new task."""
        response = await self._post("/api/v1/tasks", json=request)
        return Task(**response.json())

    async def execute(
        self,
        task_id: str,
        input_data: Dict[str, Any],
        timeout_ms: Optional[int] = None
    ) -> ExecutionResult:
        """Execute a program for a task."""
        request = {
            "taskId": task_id,
            "input": input_data,
            "timeoutMs": timeout_ms
        }
        response = await self._post("/api/v1/tasks/execute", json=request)
        return ExecutionResult(**response.json())

    async def batch_execute(
        self,
        task_id: str,
        items: List[Dict[str, Any]],
        max_concurrency: int = 10
    ) -> Dict[str, Any]:
        """Execute multiple items in batch."""
        request = {
            "taskId": task_id,
            "items": [
                {"id": str(i), "input": item}
                for i, item in enumerate(items)
            ],
            "maxConcurrency": max_concurrency
        }
        response = await self._post("/api/v1/tasks/batch/execute", json=request)
        return response.json()

    async def validate(
        self,
        task_id: str,
        execution_id: str,
        expected_output: Dict[str, Any]
    ) -> ValidationResult:
        """Validate an execution result."""
        request = {
            "taskId": task_id,
            "executionId": execution_id,
            "expectedOutput": expected_output
        }
        response = await self._post("/api/v1/validation/validate", json=request)
        return ValidationResult(**response.json())

    async def close(self):
        """Close the HTTP client."""
        await self._client.aclose()

    async def __aenter__(self):
        return self

    async def __aexit__(self, exc_type, exc_val, exc_tb):
        await self.close()

# Example usage
async def main():
    async with LoopaiClient(
        base_url="https://api.loopai.dev",
        api_key="sk-..."
    ) as client:
        # Create task
        task = await client.create_task({
            "name": "spam-detection",
            "description": "Classify emails as spam or ham",
            "inputSchema": {
                "type": "object",
                "properties": {"text": {"type": "string"}}
            },
            "outputSchema": {"type": "string", "enum": ["spam", "ham"]}
        })

        # Execute
        result = await client.execute(
            task_id=str(task.id),
            input_data={"text": "Buy now!"}
        )
        print(f"Result: {result.output_data}")

        # Batch execute
        batch_results = await client.batch_execute(
            task_id=str(task.id),
            items=[
                {"text": "Buy now!"},
                {"text": "Meeting at 2pm"},
                {"text": "Free money!!!"}
            ]
        )
        print(f"Batch: {batch_results['successCount']}/{batch_results['totalItems']}")
```

### Deliverables

- [x] PyPI package `loopai`
- [x] Async/await support
- [x] Type hints
- [x] Unit tests (pytest)
- [x] Integration tests
- [x] Example Jupyter notebooks
- [x] Documentation (Sphinx)

---

## Phase 6.5: JavaScript/TypeScript SDK (Week 5-6)

### Objectives

Create a JavaScript/TypeScript client library for web and Node.js applications.

### Features

```typescript
// src/client.ts
export interface LoopaiClientOptions {
  baseUrl?: string;
  apiKey?: string;
  timeout?: number;
  maxRetries?: number;
}

export interface Task {
  id: string;
  name: string;
  description: string;
  inputSchema: object;
  outputSchema: object;
  createdAt: string;
}

export interface ExecutionResult {
  id: string;
  taskId: string;
  outputData: any;
  latencyMs: number;
  executedAt: string;
}

export class LoopaiClient {
  private baseUrl: string;
  private apiKey?: string;
  private timeout: number;
  private maxRetries: number;

  constructor(options: LoopaiClientOptions = {}) {
    this.baseUrl = options.baseUrl || 'http://localhost:8080';
    this.apiKey = options.apiKey;
    this.timeout = options.timeout || 30000;
    this.maxRetries = options.maxRetries || 3;
  }

  async createTask(request: any): Promise<Task> {
    const response = await this.post('/api/v1/tasks', request);
    return response as Task;
  }

  async execute(
    taskId: string,
    input: any,
    timeoutMs?: number
  ): Promise<ExecutionResult> {
    const response = await this.post('/api/v1/tasks/execute', {
      taskId,
      input,
      timeoutMs
    });
    return response as ExecutionResult;
  }

  async batchExecute(
    taskId: string,
    items: any[],
    maxConcurrency: number = 10
  ): Promise<any> {
    const response = await this.post('/api/v1/tasks/batch/execute', {
      taskId,
      items: items.map((item, i) => ({ id: i.toString(), input: item })),
      maxConcurrency
    });
    return response;
  }

  private async post(path: string, data: any): Promise<any> {
    const url = `${this.baseUrl}${path}`;
    const headers: Record<string, string> = {
      'Content-Type': 'application/json'
    };

    if (this.apiKey) {
      headers['Authorization'] = `Bearer ${this.apiKey}`;
    }

    const response = await fetch(url, {
      method: 'POST',
      headers,
      body: JSON.stringify(data),
      signal: AbortSignal.timeout(this.timeout)
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${await response.text()}`);
    }

    return response.json();
  }
}

// Example usage
const client = new LoopaiClient({
  baseUrl: 'https://api.loopai.dev',
  apiKey: 'sk-...'
});

const task = await client.createTask({
  name: 'spam-detection',
  description: 'Classify emails',
  inputSchema: { type: 'object', properties: { text: { type: 'string' } } },
  outputSchema: { type: 'string', enum: ['spam', 'ham'] }
});

const result = await client.execute(task.id, { text: 'Buy now!' });
console.log('Result:', result.outputData);
```

### Deliverables

- [x] NPM package `@loopai/client`
- [x] TypeScript definitions
- [x] Browser and Node.js support
- [x] Unit tests (Jest)
- [x] Integration tests
- [x] Example React/Next.js project
- [x] Documentation (TypeDoc)

---

## Testing Strategy

### Unit Tests
- SDK client methods (90%+ coverage)
- Plugin interfaces and registry
- Batch operations logic
- Error handling and retries

### Integration Tests
- SDK against real API
- Plugin system with example plugins
- Batch operations (1K, 10K items)
- Streaming execution

### Performance Tests
- Batch execution throughput
- Concurrent execution limits
- Plugin overhead measurement
- SDK request latency

---

## Documentation

### SDK Documentation
- **Quick Start Guides**: Get started in 5 minutes
- **API Reference**: Complete API documentation
- **Examples**: Real-world usage scenarios
- **Best Practices**: Performance, error handling, security

### Plugin Development
- **Plugin Development Guide**: Step-by-step plugin creation
- **Plugin API Reference**: Interface documentation
- **Example Plugins**: 5+ working examples
- **Testing Guide**: How to test plugins

### Batch Operations
- **Batch API Guide**: When and how to use batch operations
- **Performance Tuning**: Concurrency, batching strategies
- **Error Handling**: Partial failures, retry logic

---

## Success Metrics

### Developer Experience
- [ ] SDK installation time < 2 minutes
- [ ] First successful API call < 5 minutes
- [ ] Documentation findability > 90%
- [ ] SDK API satisfaction > 4.5/5

### Extensibility
- [ ] Plugin development time < 2 hours
- [ ] 10+ community plugins within 3 months
- [ ] Plugin API stability (no breaking changes)

### Performance
- [ ] Batch execution: >1000 items/second
- [ ] Streaming: <100ms per item latency
- [ ] Plugin overhead: <5ms per execution

---

## Timeline

**Week 1-2**: .NET SDK
- Week 1: Core client, configuration, DI support
- Week 2: Examples, tests, NuGet publishing

**Week 2-3**: Plugin System
- Week 2: Plugin interfaces, registry
- Week 3: Example plugins, configuration, tests

**Week 3-4**: Batch Operations
- Week 3: Batch execution API, concurrency control
- Week 4: Streaming API, performance tests

**Week 4-5**: Python SDK
- Week 4: Core client, async support
- Week 5: Examples, tests, PyPI publishing

**Week 5-6**: JavaScript/TypeScript SDK
- Week 5: Core client, TypeScript definitions
- Week 6: Examples, tests, NPM publishing

---

## Risks and Mitigation

### Risk: SDK API Instability
**Mitigation**: Semantic versioning, beta releases, community feedback

### Risk: Plugin Security
**Mitigation**: Plugin sandboxing, security review process, signed plugins

### Risk: Batch Performance
**Mitigation**: Load testing, concurrency tuning, resource limits

### Risk: Documentation Quality
**Mitigation**: Technical writing review, user testing, iterative improvement

---

## Next Phase (Phase 7) Preview

After Phase 6, we'll focus on:
1. **Advanced Analytics**: Cost attribution, performance dashboards
2. **Multi-Tenancy**: Organization management, resource isolation
3. **Enterprise Features**: SSO, RBAC, audit logging
4. **Marketplace**: Plugin marketplace, community contributions

---

## Conclusion

Phase 6 transforms Loopai from a framework into a **platform** with:
- ✅ Rich SDKs for all major languages
- ✅ Extensible plugin architecture
- ✅ Efficient batch operations
- ✅ Excellent developer experience

This positions Loopai for **rapid adoption** and **community growth**.
