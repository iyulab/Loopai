# Loopai.Client Integration Tests

Integration tests for the Loopai .NET SDK client library using `Microsoft.AspNetCore.Mvc.Testing`.

## Overview

These tests verify the SDK's functionality against a real (in-memory) Loopai Cloud API server using `WebApplicationFactory`.

## Test Coverage

### LoopaiClientIntegrationTests

Tests core SDK functionality with real API interactions:

- ✅ **Health Check** - Verify API connectivity
- ✅ **Task Creation** - Create tasks with JSON schemas
- ✅ **Task Retrieval** - Get task by ID
- ✅ **Single Execution** - Execute task with input
- ✅ **Batch Execution** - Process multiple inputs in parallel
- ✅ **Custom Batch IDs** - Use custom correlation IDs
- ✅ **Error Handling** - Verify 404 errors for invalid task IDs

### RetryIntegrationTests

Tests retry logic, error handling, and edge cases:

- ✅ **Retry Logic** - Verify exponential backoff on transient failures
- ✅ **Zero Retries** - Verify no retries when MaxRetries=0
- ✅ **Timeout Handling** - Verify timeout exceptions
- ✅ **Validation Errors** - Verify ValidationException on invalid input
- ✅ **Batch Stop-on-Error** - Verify StopOnFirstError behavior
- ✅ **Concurrency Limit** - Verify MaxConcurrency is respected
- ✅ **Invalid Base URL** - Verify exception on connection errors
- ✅ **Dispose Pattern** - Verify proper resource cleanup

## Running the Tests

### Prerequisites

- .NET 8.0 SDK
- All Loopai dependencies built

### Run All Integration Tests

```bash
dotnet test tests/Loopai.Client.IntegrationTests/
```

### Run Specific Test Class

```bash
# Run only core integration tests
dotnet test tests/Loopai.Client.IntegrationTests/ --filter FullyQualifiedName~LoopaiClientIntegrationTests

# Run only retry tests
dotnet test tests/Loopai.Client.IntegrationTests/ --filter FullyQualifiedName~RetryIntegrationTests
```

### Run Specific Test

```bash
dotnet test tests/Loopai.Client.IntegrationTests/ --filter "FullyQualifiedName~GetHealthAsync_ShouldReturnHealthyStatus"
```

### With Detailed Output

```bash
dotnet test tests/Loopai.Client.IntegrationTests/ --logger "console;verbosity=detailed"
```

## Architecture

### WebApplicationFactory

Uses `Microsoft.AspNetCore.Mvc.Testing` to create an in-memory test server:

```csharp
public class LoopaiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Configure test-specific services
    }
}
```

### Test Fixture

Each test class uses `IClassFixture<LoopaiWebApplicationFactory>` for efficient test server reuse:

```csharp
public class LoopaiClientIntegrationTests : IClassFixture<LoopaiWebApplicationFactory>
{
    private readonly HttpClient _httpClient;
    private readonly ILoopaiClient _client;

    public LoopaiClientIntegrationTests(LoopaiWebApplicationFactory factory)
    {
        _httpClient = factory.CreateClient();
        var baseUrl = _httpClient.BaseAddress.ToString();
        _client = new LoopaiClient(new LoopaiClientOptions { BaseUrl = baseUrl });
    }
}
```

## Test Patterns

### Creating Test Tasks

Helper method to create tasks for testing:

```csharp
private async Task<Guid> CreateTestTaskAsync()
{
    var inputSchema = JsonDocument.Parse(@"{
        ""type"": ""object"",
        ""properties"": { ""text"": { ""type"": ""string"" } },
        ""required"": [""text""]
    }");

    var outputSchema = JsonDocument.Parse(@"{
        ""type"": ""string"",
        ""enum"": [""spam"", ""not_spam""]
    }");

    var task = await _client.CreateTaskAsync(
        name: $"test-task-{Guid.NewGuid():N}",
        description: "Test task",
        inputSchema: inputSchema,
        outputSchema: outputSchema
    );

    return task.RootElement.GetProperty("id").GetGuid();
}
```

### Testing Batch Execution

```csharp
[Fact]
public async Task BatchExecuteAsync_WithMultipleItems_ShouldExecuteAll()
{
    var taskId = await CreateTestTaskAsync();

    var inputs = new[]
    {
        new { text = "Message 1" },
        new { text = "Message 2" },
        new { text = "Message 3" }
    };

    var result = await _client.BatchExecuteAsync(taskId, inputs, maxConcurrency: 10);

    result.TotalItems.Should().Be(3);
    result.Results.Should().HaveCount(3);
}
```

### Testing Error Handling

```csharp
[Fact]
public async Task ExecuteAsync_WithInvalidTaskId_ShouldThrowLoopaiException()
{
    var invalidTaskId = Guid.NewGuid();
    var input = new { text = "Test" };

    var act = async () => await _client.ExecuteAsync(invalidTaskId, input);

    await act.Should().ThrowAsync<LoopaiException>()
        .Where(ex => ex.StatusCode == 404);
}
```

## Test Configuration

### Client Configuration for Tests

```csharp
var client = new LoopaiClient(new LoopaiClientOptions
{
    BaseUrl = testServer.BaseAddress.ToString(),
    MaxRetries = 0,  // Disable retries for deterministic tests
    Timeout = TimeSpan.FromSeconds(30)
});
```

### Test Environment

Tests run in "Testing" environment with:
- In-memory test server (no external dependencies)
- No authentication required
- Test-specific configurations

## Troubleshooting

### Tests Fail with Connection Errors

**Issue**: Cannot connect to test server

**Solution**: Ensure CloudApi builds successfully:
```bash
dotnet build src/Loopai.CloudApi/
```

### Tests Timeout

**Issue**: Tests take too long and timeout

**Solution**: Increase timeout in test configuration:
```csharp
var client = new LoopaiClient(new LoopaiClientOptions
{
    Timeout = TimeSpan.FromSeconds(60)  // Increase timeout
});
```

### Database Errors

**Issue**: Tests fail with database-related errors

**Solution**: Configure in-memory database for testing in `LoopaiWebApplicationFactory`:
```csharp
builder.ConfigureServices(services =>
{
    // Remove real database
    services.RemoveAll<DbContextOptions<LoopaiDbContext>>();

    // Add in-memory database
    services.AddDbContext<LoopaiDbContext>(options =>
        options.UseInMemoryDatabase("TestDb"));
});
```

## CI/CD Integration

### GitHub Actions

```yaml
- name: Run Integration Tests
  run: dotnet test tests/Loopai.Client.IntegrationTests/ --logger "trx;LogFileName=integration-tests.trx"

- name: Upload Test Results
  uses: actions/upload-artifact@v3
  if: always()
  with:
    name: integration-test-results
    path: '**/integration-tests.trx'
```

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Integration Tests'
  inputs:
    command: 'test'
    projects: 'tests/Loopai.Client.IntegrationTests/*.csproj'
    arguments: '--configuration Release --logger trx'
    publishTestResults: true
```

## Best Practices

### 1. Test Isolation

Each test should be independent and not rely on state from other tests:

```csharp
// ✅ Good - Creates own task
[Fact]
public async Task Test1()
{
    var taskId = await CreateTestTaskAsync();  // Own task
    // Test logic
}

// ❌ Bad - Shares task across tests
private static Guid _sharedTaskId;

[Fact]
public async Task Test2()
{
    if (_sharedTaskId == Guid.Empty)
        _sharedTaskId = await CreateTestTaskAsync();  // Shared state
}
```

### 2. Use Descriptive Test Names

```csharp
// ✅ Good - Clear what is being tested
[Fact]
public async Task BatchExecuteAsync_WithStopOnFirstError_ShouldStopOnError()

// ❌ Bad - Unclear intent
[Fact]
public async Task Test1()
```

### 3. Arrange-Act-Assert Pattern

```csharp
[Fact]
public async Task Test()
{
    // Arrange - Setup test data
    var taskId = await CreateTestTaskAsync();
    var input = new { text = "Test" };

    // Act - Perform the action
    var result = await _client.ExecuteAsync(taskId, input);

    // Assert - Verify results
    result.Should().NotBeNull();
}
```

### 4. Use FluentAssertions

```csharp
// ✅ Good - Readable assertions
result.TotalItems.Should().Be(10);
result.Results.Should().HaveCount(10);
result.SuccessCount.Should().BeGreaterThan(0);

// ❌ Bad - Hard to read
Assert.Equal(10, result.TotalItems);
Assert.True(result.Results.Count == 10);
```

## Next Steps

- Add performance benchmarks
- Add load testing scenarios
- Add webhook integration tests
- Add validation recommendation tests
- Add telemetry verification

## References

- [Microsoft.AspNetCore.Mvc.Testing Documentation](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Loopai SDK Documentation](../../src/Loopai.Client/README.md)
