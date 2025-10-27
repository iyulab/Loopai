# Loopai ASP.NET Core Example

This example demonstrates how to use the Loopai.Client SDK in an ASP.NET Core Web API application.

## Features Demonstrated

- ✅ Dependency Injection integration
- ✅ Configuration binding from appsettings.json
- ✅ Error handling with custom exceptions
- ✅ Logging integration
- ✅ Swagger/OpenAPI documentation
- ✅ Task creation, execution, and retrieval
- ✅ Batch execution with concurrency control
- ✅ Custom correlation IDs for batch items

## Prerequisites

- .NET 8.0 SDK
- Loopai Cloud API running (default: http://localhost:8080)

## Running the Example

### 1. Start Loopai Cloud API

```bash
# From repository root
cd src/Loopai.CloudApi
dotnet run
```

### 2. Run Example Application

```bash
# From repository root
cd examples/Loopai.Examples.AspNetCore
dotnet run
```

The example API will be available at `http://localhost:5000` (or configured port).

### 3. Open Swagger UI

Navigate to `http://localhost:5000/swagger` to explore the API endpoints.

## API Endpoints

### Health Check

```bash
GET /api/classification/health
```

Checks connectivity to Loopai API.

**Response**:
```json
{
  "status": "healthy",
  "version": "0.1.0",
  "timestamp": "2025-10-27T10:00:00Z"
}
```

### Create Task

```bash
POST /api/classification/tasks
Content-Type: application/json

{
  "name": "spam-detection",
  "description": "Classify emails as spam or ham",
  "inputSchema": "{\"type\":\"object\",\"properties\":{\"text\":{\"type\":\"string\"}}}",
  "outputSchema": "{\"type\":\"string\",\"enum\":[\"spam\",\"ham\"]}",
  "accuracyTarget": 0.9,
  "latencyTargetMs": 10
}
```

**Response**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "spam-detection",
  "description": "Classify emails as spam or ham",
  ...
}
```

### Get Task

```bash
GET /api/classification/tasks/{taskId}
```

**Response**: Task details with same structure as creation response.

### Classify Text

```bash
POST /api/classification/classify
Content-Type: application/json

{
  "taskId": "550e8400-e29b-41d4-a716-446655440000",
  "text": "Buy now for free money!"
}
```

**Response**:
```json
{
  "classification": "spam",
  "latencyMs": 4.2
}
```

## Configuration

Edit `appsettings.json` to configure the Loopai client:

```json
{
  "Loopai": {
    "BaseUrl": "http://localhost:8080",
    "ApiKey": null,
    "Timeout": "00:01:00",
    "MaxRetries": 3
  }
}
```

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `BaseUrl` | Loopai API base URL | `http://localhost:8080` |
| `ApiKey` | API key for authentication | `null` (optional) |
| `Timeout` | HTTP request timeout | `00:01:00` (1 minute) |
| `MaxRetries` | Max retry attempts | `3` |

## Code Examples

### Dependency Injection Setup

```csharp
// Program.cs
builder.Services.AddLoopaiClient(options =>
{
    options.BaseUrl = builder.Configuration["Loopai:BaseUrl"] ?? "http://localhost:8080";
    options.ApiKey = builder.Configuration["Loopai:ApiKey"];
    options.Timeout = TimeSpan.FromSeconds(60);
    options.MaxRetries = 3;
    options.EnableDetailedLogging = builder.Environment.IsDevelopment();
});
```

### Controller Usage

```csharp
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
        try
        {
            var result = await _loopai.ExecuteAsync(
                request.TaskId,
                new { text = request.Text }
            );

            var output = result.RootElement.GetProperty("output").GetString();
            return Ok(new { classification = output });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message, errors = ex.Errors });
        }
        catch (ExecutionException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
```

### Error Handling

```csharp
try
{
    var result = await _loopai.ExecuteAsync(taskId, input);
}
catch (ValidationException ex)
{
    // HTTP 400 - Validation errors
    _logger.LogWarning(ex, "Validation failed");
    return BadRequest(new ErrorResponse
    {
        Message = ex.Message,
        Errors = ex.Errors
    });
}
catch (ExecutionException ex)
{
    // Execution-specific errors
    _logger.LogError(ex, "Execution failed");
    return StatusCode(500, new ErrorResponse
    {
        Message = ex.Message,
        ExecutionId = ex.ExecutionId
    });
}
catch (LoopaiException ex)
{
    // General API errors
    _logger.LogError(ex, "Loopai API error");
    return StatusCode(ex.StatusCode ?? 500, new ErrorResponse
    {
        Message = ex.Message
    });
}
```

## Testing with curl

### Create a task

```bash
curl -X POST http://localhost:5000/api/classification/tasks \
  -H "Content-Type: application/json" \
  -d '{
    "name": "spam-detection",
    "description": "Classify emails",
    "inputSchema": "{\"type\":\"object\",\"properties\":{\"text\":{\"type\":\"string\"}}}",
    "outputSchema": "{\"type\":\"string\",\"enum\":[\"spam\",\"ham\"]}",
    "accuracyTarget": 0.9,
    "latencyTargetMs": 10
  }'
```

### Classify text

```bash
# Replace {taskId} with actual task ID from creation response
curl -X POST http://localhost:5000/api/classification/classify \
  -H "Content-Type: application/json" \
  -d '{
    "taskId": "550e8400-e29b-41d4-a716-446655440000",
    "text": "Buy now for free money!"
  }'
```

### Check health

```bash
curl http://localhost:5000/api/classification/health
```

### Batch Classification

```bash
POST /api/batch/classify
Content-Type: application/json

{
  "taskId": "550e8400-e29b-41d4-a716-446655440000",
  "emails": [
    "Buy now for free money!",
    "Meeting tomorrow at 2pm",
    "URGENT: Click here NOW!!!"
  ]
}
```

**Response**:
```json
{
  "batchId": "789e4567-e89b-12d3-a456-426614174999",
  "totalItems": 3,
  "successCount": 3,
  "failureCount": 0,
  "totalDurationMs": 150.5,
  "avgLatencyMs": 50.2,
  "classifications": [
    {
      "id": "0",
      "success": true,
      "classification": "spam",
      "latencyMs": 48.3
    },
    {
      "id": "1",
      "success": true,
      "classification": "not_spam",
      "latencyMs": 45.1
    },
    {
      "id": "2",
      "success": true,
      "classification": "spam",
      "latencyMs": 57.1
    }
  ]
}
```

### Batch Sentiment Analysis

```bash
POST /api/batch/sentiment
Content-Type: application/json

{
  "taskId": "234e5678-e89b-12d3-a456-426614174001",
  "texts": [
    "I love this product!",
    "Terrible experience.",
    "It works okay."
  ]
}
```

**Response**:
```json
{
  "batchId": "890e4567-e89b-12d3-a456-426614174998",
  "results": [
    {
      "id": "sentiment-0",
      "success": true,
      "sentiment": "positive",
      "latencyMs": 42.1,
      "sampled": false
    },
    {
      "id": "sentiment-1",
      "success": true,
      "sentiment": "negative",
      "latencyMs": 39.8,
      "sampled": true
    },
    {
      "id": "sentiment-2",
      "success": true,
      "sentiment": "neutral",
      "latencyMs": 41.5,
      "sampled": false
    }
  ]
}
```

## Batch Processing Examples

### Simple Batch Execution

```csharp
// Simple batch with auto-generated IDs
var inputs = emails.Select(email => new { text = email });

var result = await _loopai.BatchExecuteAsync(
    taskId,
    inputs,
    maxConcurrency: 10);

_logger.LogInformation(
    "Processed {Total} items, {Success} succeeded in {Duration}ms",
    result.TotalItems,
    result.SuccessCount,
    result.TotalDurationMs);
```

### Advanced Batch Execution

```csharp
// Advanced batch with custom IDs and configuration
var batchItems = texts.Select((text, index) => new BatchExecuteItem
{
    Id = $"sentiment-{index}",
    Input = JsonSerializer.SerializeToDocument(new { text }),
    ForceValidation = false
});

var batchRequest = new BatchExecuteRequest
{
    TaskId = taskId,
    Items = batchItems.ToList(),
    MaxConcurrency = 5,
    StopOnFirstError = false,
    TimeoutMs = 30000
};

var result = await _loopai.BatchExecuteAsync(batchRequest);

// Process results with custom IDs
foreach (var item in result.Results)
{
    _logger.LogInformation(
        "Item {Id}: {Status} in {Latency}ms",
        item.Id,
        item.Success ? "succeeded" : "failed",
        item.LatencyMs);
}
```

## Next Steps

- Explore the [SDK documentation](../../src/Loopai.Client/README.md)
- Review [API reference](../../docs/API_REFERENCE.md)
- Check out [integration tests](../../tests/Loopai.Client.IntegrationTests/)
- Add authentication and authorization
- Deploy to production environment

## License

MIT License - see [LICENSE](../../LICENSE) for details.
