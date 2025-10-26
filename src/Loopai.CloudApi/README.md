# Loopai Cloud API

ASP.NET Core REST API for the Loopai program synthesis and execution platform.

## Overview

The Cloud API provides HTTP endpoints for:
- **Task Management**: Create and manage task specifications
- **Program Execution**: Execute synthesized programs with validation
- **Artifact Retrieval**: Access program versions and execution history
- **Health Monitoring**: Health checks and metrics for observability

## Architecture

### Project Structure

```
src/Loopai.CloudApi/
├── DTOs/                    # Data Transfer Objects
│   ├── ExecuteRequest.cs    # Execution request model
│   ├── ExecuteResponse.cs   # Execution response model
│   ├── CreateTaskRequest.cs # Task creation request
│   ├── TaskResponse.cs      # Task information response
│   └── ErrorResponse.cs     # Standardized error format
├── Validators/              # FluentValidation validators
│   ├── ExecuteRequestValidator.cs
│   └── CreateTaskRequestValidator.cs
├── Program.cs               # Application entry point
└── appsettings.json        # Configuration

src/Loopai.Core/
└── Models/                  # Core domain models
    ├── Enums.cs
    ├── ComplexityMetrics.cs
    ├── TaskSpecification.cs
    ├── ProgramArtifact.cs
    └── ExecutionRecord.cs
```

## Technology Stack

- **.NET 8.0**: Target framework
- **ASP.NET Core**: Web API framework
- **FluentValidation**: Request/response validation
- **Serilog**: Structured logging
- **Swashbuckle**: OpenAPI/Swagger documentation
- **xUnit**: Testing framework
- **FluentAssertions**: Test assertions
- **Moq**: Mocking framework

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider (optional)

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --project src/Loopai.CloudApi
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `http://localhost:5000` (development only)

### Test

```bash
dotnet test
```

## API Endpoints

### Tasks

- `POST /api/v1/tasks` - Create a new task specification
- `GET /api/v1/tasks/{taskId}` - Get task details
- `POST /api/v1/tasks/{taskId}/execute` - Execute a program
- `GET /api/v1/tasks/{taskId}/artifacts` - List program versions
- `GET /api/v1/tasks/{taskId}/artifacts/{version}` - Get specific version

### Health

- `GET /health` - Health check endpoint

### Metrics

- `GET /api/v1/metrics` - Prometheus metrics

## Configuration

### appsettings.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*",
  "AllowedOrigins": []
}
```

### Environment Variables

- `ASPNETCORE_ENVIRONMENT`: Environment (Development, Staging, Production)
- `ASPNETCORE_URLS`: Listening URLs

## API Conventions

### JSON Naming

All API requests/responses use `snake_case` for JSON property names to maintain compatibility with the Python components.

**Example Request:**
```json
{
  "task_id": "123e4567-e89b-12d3-a456-426614174000",
  "input": {"value": 42},
  "force_validation": true,
  "timeout_ms": 5000
}
```

### Error Responses

All errors follow a standardized format:

```json
{
  "code": "TASK_NOT_FOUND",
  "message": "Task with ID 12345 not found",
  "details": {},
  "trace_id": "abc123",
  "timestamp": "2025-01-15T10:30:00Z"
}
```

### Validation

All requests are validated using FluentValidation with descriptive error messages:

- Task names: alphanumeric + `_`, `-`, `.` only (max 200 chars)
- Descriptions: max 5000 chars
- Accuracy targets: 0.0 to 1.0
- Latency targets: 1 to 60000ms
- Sampling rates: 0.0 to 1.0

## Development

### Adding New Endpoints

1. Create DTOs in `DTOs/` directory
2. Add validators in `Validators/` directory
3. Create controller in `Controllers/` directory
4. Add unit tests in test project
5. Update Swagger documentation

### Testing

Tests are organized by component:

```
tests/Loopai.CloudApi.Tests/
├── DTOs/
│   └── JsonSerializationTests.cs
├── Validators/
│   ├── ExecuteRequestValidatorTests.cs
│   └── CreateTaskRequestValidatorTests.cs
└── Controllers/
    └── (future controller tests)
```

Run specific test categories:
```bash
dotnet test --filter "FullyQualifiedName~Validators"
dotnet test --filter "FullyQualifiedName~DTOs"
```

## Logging

Structured logging with Serilog:

```csharp
Log.Information("Executing program {ProgramId} for task {TaskId}", programId, taskId);
Log.Warning("Execution timeout for {TaskId} after {TimeoutMs}ms", taskId, timeoutMs);
Log.Error(ex, "Execution failed for {TaskId}", taskId);
```

## Deployment

### Docker (Planned)

```bash
docker build -t loopai-cloudapi .
docker run -p 5000:8080 loopai-cloudapi
```

### Azure App Service (Planned)

```bash
az webapp up --name loopai-cloudapi --runtime "DOTNETCORE:8.0"
```

## License

[Your License Here]

## Contributing

[Contribution Guidelines]
