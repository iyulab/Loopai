# Loopai.Client - Official .NET SDK

Official .NET client library for [Loopai](https://github.com/iyulab/loopai) - Human-in-the-Loop AI Self-Improvement Framework.

## Features

- ✅ **Async/Await Support**: Modern async programming patterns
- ✅ **Automatic Retry**: Exponential backoff for transient failures
- ✅ **Dependency Injection**: First-class ASP.NET Core integration
- ✅ **Strongly Typed**: Type-safe request/response models
- ✅ **Batch Execution**: Process multiple inputs with concurrency control
- ✅ **Telemetry**: Built-in logging and observability
- ✅ **Resilient**: Polly-based retry policies

## Installation

### Local NuGet Package

```bash
# Build the package
dotnet pack src/Loopai.Client/Loopai.Client.csproj -c Release -o ./nupkg

# Add local package source
dotnet nuget add source ./nupkg --name LoopaiLocal

# Install package
dotnet add package Loopai.Client --version 0.1.0
```

## Quick Start

### Basic Usage

```csharp
using Loopai.Client;
using System.Text.Json;

// Create client
var client = new LoopaiClient("http://localhost:8080");

// Create task
var inputSchema = JsonDocument.Parse(@"{
    ""type"": ""object"",
    ""properties"": { ""text"": { ""type"": ""string"" } }
}");

var outputSchema = JsonDocument.Parse(@"{
    ""type"": ""string"",
    ""enum"": [""spam"", ""ham""]
}");

var task = await client.CreateTaskAsync(
    name: "spam-detection",
    description: "Classify emails as spam or ham",
    inputSchema: inputSchema,
    outputSchema: outputSchema,
    accuracyTarget: 0.9
);

var taskId = task.RootElement.GetProperty("id").GetGuid();

// Execute program
var input = new { text = "Buy now for free!" };
var result = await client.ExecuteAsync(taskId, input);

var output = result.RootElement.GetProperty("output").GetString();
Console.WriteLine($"Classification: {output}"); // "spam"
```

### ASP.NET Core Integration

**Program.cs / Startup.cs**:

```csharp
using Loopai.Client;

var builder = WebApplication.CreateBuilder(args);

// Option 1: Configure with action
builder.Services.AddLoopaiClient(options =>
{
    options.BaseUrl = "http://localhost:8080";
    options.ApiKey = builder.Configuration["Loopai:ApiKey"];
    options.Timeout = TimeSpan.FromSeconds(60);
    options.MaxRetries = 5;
    options.EnableDetailedLogging = true;
});

// Option 2: Bind from configuration
builder.Services.AddLoopaiClient(
    builder.Configuration.GetSection("Loopai")
);

// Option 3: Simple configuration
builder.Services.AddLoopaiClient(
    baseUrl: "http://localhost:8080",
    apiKey: "your-api-key"
);

var app = builder.Build();
```

**appsettings.json**:

```json
{
  "Loopai": {
    "BaseUrl": "http://localhost:8080",
    "ApiKey": "your-api-key",
    "Timeout": "00:00:30",
    "MaxRetries": 3,
    "EnableTelemetry": true,
    "EnableDetailedLogging": false
  }
}
```

**Controller**:

```csharp
using Loopai.Client;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ClassificationController : ControllerBase
{
    private readonly ILoopaiClient _loopai;
    private readonly ILogger<ClassificationController> _logger;

    public ClassificationController(
        ILoopaiClient loopai,
        ILogger<ClassificationController> logger)
    {
        _loopai = loopai;
        _logger = logger;
    }

    [HttpPost("classify")]
    public async Task<IActionResult> Classify(
        [FromBody] ClassifyRequest request)
    {
        try
        {
            var taskId = Guid.Parse("your-task-id");
            var result = await _loopai.ExecuteAsync(
                taskId,
                new { text = request.Text }
            );

            var output = result.RootElement
                .GetProperty("output")
                .GetString();

            return Ok(new { classification = output });
        }
        catch (ExecutionException ex)
        {
            _logger.LogError(ex, "Execution failed");
            return StatusCode(500, new { error = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            return BadRequest(new { error = ex.Message, errors = ex.Errors });
        }
    }
}

public record ClassifyRequest(string Text);
```

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `BaseUrl` | `string` | `http://localhost:8080` | Base URL of Loopai API |
| `ApiKey` | `string?` | `null` | API key for authentication |
| `Timeout` | `TimeSpan` | 30 seconds | HTTP request timeout |
| `MaxRetries` | `int` | 3 | Max retry attempts for transient failures |
| `RetryDelay` | `TimeSpan` | 500ms | Base delay for exponential backoff |
| `EnableTelemetry` | `bool` | `true` | Enable logging and telemetry |
| `EnableDetailedLogging` | `bool` | `false` | Log request/response payloads |
| `Logger` | `ILogger?` | `null` | Custom logger instance |

## Error Handling

```csharp
using Loopai.Client.Exceptions;

try
{
    var result = await client.ExecuteAsync(taskId, input);
}
catch (ValidationException ex)
{
    // HTTP 400 - Validation errors
    Console.WriteLine($"Validation failed: {ex.Message}");
    if (ex.Errors != null)
    {
        foreach (var (field, errors) in ex.Errors)
        {
            Console.WriteLine($"{field}: {string.Join(", ", errors)}");
        }
    }
}
catch (ExecutionException ex)
{
    // Execution-specific errors
    Console.WriteLine($"Execution failed: {ex.Message}");
    if (ex.ExecutionId.HasValue)
    {
        Console.WriteLine($"Execution ID: {ex.ExecutionId}");
    }
}
catch (LoopaiException ex)
{
    // General API errors
    Console.WriteLine($"API error: {ex.Message}");
    if (ex.StatusCode.HasValue)
    {
        Console.WriteLine($"HTTP Status: {ex.StatusCode}");
    }
}
```

## Retry Policy

The client automatically retries transient failures with exponential backoff:

- **Retryable Status Codes**: 408, 429, 500+
- **Max Retries**: Configurable (default: 3)
- **Backoff Strategy**: Exponential with base delay (default: 500ms)
- **Retry Delays**: 500ms → 1000ms → 2000ms

```csharp
var client = new LoopaiClient(new LoopaiClientOptions
{
    MaxRetries = 5,
    RetryDelay = TimeSpan.FromSeconds(1)
});
```

## Batch Execution

Process multiple inputs in parallel with concurrency control:

### Simple Batch Execution

```csharp
var emails = new[]
{
    new { text = "Buy now for free money!" },
    new { text = "Meeting tomorrow at 2pm" },
    new { text = "URGENT: Click here NOW!!!" }
};

var result = await client.BatchExecuteAsync(
    taskId,
    emails,
    maxConcurrency: 10);

Console.WriteLine($"Processed {result.TotalItems} items in {result.TotalDurationMs}ms");
Console.WriteLine($"Success: {result.SuccessCount}, Failed: {result.FailureCount}");
Console.WriteLine($"Average latency: {result.AvgLatencyMs}ms");

foreach (var item in result.Results)
{
    if (item.Success)
    {
        var classification = item.Output?.RootElement.GetProperty("output").GetString();
        Console.WriteLine($"Item {item.Id}: {classification} ({item.LatencyMs}ms)");
    }
    else
    {
        Console.WriteLine($"Item {item.Id}: FAILED - {item.ErrorMessage}");
    }
}
```

### Advanced Batch Execution

For more control, use `BatchExecuteRequest`:

```csharp
using Loopai.Client.Models;
using System.Text.Json;

var batchItems = new[]
{
    new BatchExecuteItem
    {
        Id = "email-001",
        Input = JsonSerializer.SerializeToDocument(new { text = "Spam email" }),
        ForceValidation = false
    },
    new BatchExecuteItem
    {
        Id = "email-002",
        Input = JsonSerializer.SerializeToDocument(new { text = "Legitimate email" }),
        ForceValidation = true  // Force validation for this item
    }
};

var request = new BatchExecuteRequest
{
    TaskId = taskId,
    Items = batchItems,
    MaxConcurrency = 5,
    StopOnFirstError = false,
    TimeoutMs = 30000
};

var result = await client.BatchExecuteAsync(request);

Console.WriteLine($"Batch ID: {result.BatchId}");
Console.WriteLine($"Started: {result.StartedAt}, Completed: {result.CompletedAt}");

// Process results with custom IDs
foreach (var item in result.Results)
{
    Console.WriteLine($"{item.Id}: {(item.Success ? "✓" : "✗")} " +
                     $"(latency: {item.LatencyMs}ms, sampled: {item.SampledForValidation})");
}
```

### Batch Execution Features

- **Parallel Processing**: Execute multiple items concurrently
- **Concurrency Control**: Limit concurrent executions (default: 10)
- **Custom IDs**: Track items with your own correlation IDs
- **Error Handling**: Choose to stop on first error or continue
- **Timeouts**: Per-batch timeout configuration
- **Validation Sampling**: Some items may be sampled for validation
- **Detailed Metrics**: Total duration, average latency, success/failure counts

## Health Checks

```csharp
var health = await client.GetHealthAsync();

Console.WriteLine($"Status: {health.Status}");
Console.WriteLine($"Version: {health.Version}");
Console.WriteLine($"Timestamp: {health.Timestamp}");

if (health.Checks != null)
{
    foreach (var (component, status) in health.Checks)
    {
        Console.WriteLine($"{component}: {status}");
    }
}
```

## Best Practices

### 1. Use Dependency Injection

```csharp
// ✅ Good - Singleton registration
services.AddLoopaiClient(options => { /* config */ });

// ❌ Bad - Manual instantiation
var client = new LoopaiClient("http://localhost:8080");
```

### 2. Dispose Properly

```csharp
// ✅ Good - Using statement
using var client = new LoopaiClient("http://localhost:8080");
await client.ExecuteAsync(taskId, input);

// ✅ Good - DI handles disposal
public class MyService
{
    private readonly ILoopaiClient _client; // Disposed by DI container
}
```

### 3. Handle Exceptions

```csharp
// ✅ Good - Specific exception handling
try
{
    var result = await client.ExecuteAsync(taskId, input);
}
catch (ValidationException ex) { /* Handle validation */ }
catch (ExecutionException ex) { /* Handle execution */ }
catch (LoopaiException ex) { /* Handle API errors */ }

// ❌ Bad - Generic catch
try { /* ... */ }
catch (Exception ex) { /* Too broad */ }
```

### 4. Configure Timeout

```csharp
// ✅ Good - Appropriate timeout
var options = new LoopaiClientOptions
{
    Timeout = TimeSpan.FromSeconds(60), // Long-running operations
    MaxRetries = 5
};

// ❌ Bad - Too short for AI operations
var options = new LoopaiClientOptions
{
    Timeout = TimeSpan.FromSeconds(5) // May timeout prematurely
};
```

## Examples

See the [examples directory](../../examples/Loopai.Examples.AspNetCore/) for complete working examples:

- Basic task creation and execution
- ASP.NET Core integration
- Error handling patterns
- Logging and telemetry

## License

MIT License - see [LICENSE](../../LICENSE) for details.

## Support

- **GitHub Issues**: [github.com/iyulab/loopai/issues](https://github.com/iyulab/loopai/issues)
- **Documentation**: [github.com/iyulab/loopai](https://github.com/iyulab/loopai)
