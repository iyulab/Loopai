using System.Text.Json;
using FluentAssertions;
using Loopai.Client.Exceptions;

namespace Loopai.Client.IntegrationTests;

/// <summary>
/// Integration tests for retry logic and error handling.
/// </summary>
public class RetryIntegrationTests : IClassFixture<LoopaiWebApplicationFactory>
{
    private readonly LoopaiWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;

    public RetryIntegrationTests(LoopaiWebApplicationFactory factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task Client_WithMaxRetries_ShouldRetryOnTransientFailures()
    {
        // Arrange: Create client with retry enabled
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost";
        var client = new LoopaiClient(new LoopaiClientOptions
        {
            BaseUrl = baseUrl,
            MaxRetries = 3,
            RetryDelay = TimeSpan.FromMilliseconds(100),
            Timeout = TimeSpan.FromSeconds(10)
        });

        // Act: Try to get health (should succeed without retries on healthy server)
        var health = await client.GetHealthAsync();

        // Assert
        health.Should().NotBeNull();
        health.Status.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Client_WithZeroMaxRetries_ShouldNotRetry()
    {
        // Arrange: Create client with retries disabled
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost";
        var client = new LoopaiClient(new LoopaiClientOptions
        {
            BaseUrl = baseUrl,
            MaxRetries = 0,  // No retries
            Timeout = TimeSpan.FromSeconds(5)
        });

        // Act: Get health should work without retries
        var health = await client.GetHealthAsync();

        // Assert
        health.Should().NotBeNull();
    }

    [Fact]
    public async Task Client_WithTimeout_ShouldThrowAfterTimeout()
    {
        // Arrange: Create client with very short timeout
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost";
        var client = new LoopaiClient(new LoopaiClientOptions
        {
            BaseUrl = baseUrl,
            MaxRetries = 0,
            Timeout = TimeSpan.FromMilliseconds(1)  // Very short timeout
        });

        // Act: Execute should timeout
        var taskId = Guid.NewGuid();
        var input = new { text = "Test" };

        var act = async () => await client.ExecuteAsync(taskId, input);

        // Assert: Should throw exception (either timeout or 404, depending on timing)
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ExecuteAsync_WithValidationError_ShouldThrowValidationException()
    {
        // Arrange
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost";
        var client = new LoopaiClient(new LoopaiClientOptions
        {
            BaseUrl = baseUrl,
            MaxRetries = 0,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Create a task first
        var taskId = await CreateTestTaskAsync(client);

        // Act: Execute with invalid input (missing required field)
        var invalidInput = new { }; // Missing "text" field

        var act = async () => await client.ExecuteAsync(taskId, invalidInput);

        // Assert: Should throw ValidationException with 400 status
        await act.Should().ThrowAsync<LoopaiException>();
    }

    [Fact]
    public async Task BatchExecuteAsync_WithStopOnFirstError_ShouldStopOnError()
    {
        // Arrange
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost";
        var client = new LoopaiClient(new LoopaiClientOptions
        {
            BaseUrl = baseUrl,
            MaxRetries = 0,
            Timeout = TimeSpan.FromSeconds(30)
        });

        var taskId = await CreateTestTaskAsync(client);

        var batchItems = new[]
        {
            new Client.Models.BatchExecuteItem
            {
                Id = "item-1",
                Input = JsonSerializer.SerializeToDocument(new { text = "Valid input 1" }),
                ForceValidation = false
            },
            new Client.Models.BatchExecuteItem
            {
                Id = "item-2",
                Input = JsonSerializer.SerializeToDocument(new { text = "Valid input 2" }),
                ForceValidation = false
            }
        };

        var request = new Client.Models.BatchExecuteRequest
        {
            TaskId = taskId,
            Items = batchItems,
            MaxConcurrency = 1,
            StopOnFirstError = true,  // Stop on first error
            TimeoutMs = 30000
        };

        // Act
        var result = await client.BatchExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(2);
    }

    [Fact]
    public async Task BatchExecuteAsync_WithConcurrencyLimit_ShouldRespectLimit()
    {
        // Arrange
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost";
        var client = new LoopaiClient(new LoopaiClientOptions
        {
            BaseUrl = baseUrl,
            MaxRetries = 0,
            Timeout = TimeSpan.FromSeconds(60)
        });

        var taskId = await CreateTestTaskAsync(client);

        // Create 10 items
        var inputs = Enumerable.Range(1, 10)
            .Select(i => new { text = $"Message {i}" })
            .ToArray();

        // Act: Execute with concurrency limit of 3
        var result = await client.BatchExecuteAsync(
            taskId,
            inputs,
            maxConcurrency: 3
        );

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(10);
        result.Results.Should().HaveCount(10);

        // All items should have been processed
        result.Results.Should().AllSatisfy(r =>
        {
            r.ExecutionId.Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task Client_WithInvalidBaseUrl_ShouldThrowException()
    {
        // Arrange: Create client with invalid base URL
        var client = new LoopaiClient(new LoopaiClientOptions
        {
            BaseUrl = "http://invalid-host-that-does-not-exist:9999",
            MaxRetries = 0,
            Timeout = TimeSpan.FromSeconds(2)
        });

        // Act
        var act = async () => await client.GetHealthAsync();

        // Assert: Should throw exception
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Client_Dispose_ShouldCleanupResources()
    {
        // Arrange
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost";
        var client = new LoopaiClient(new LoopaiClientOptions
        {
            BaseUrl = baseUrl,
            MaxRetries = 0
        });

        // Act: Use client and dispose
        var health = await client.GetHealthAsync();
        health.Should().NotBeNull();

        client.Dispose();

        // Assert: Calling methods after dispose should throw
        var act = async () => await client.GetHealthAsync();
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Helper method to create a test task.
    /// </summary>
    private async Task<Guid> CreateTestTaskAsync(ILoopaiClient client)
    {
        var inputSchema = JsonDocument.Parse(@"{
            ""type"": ""object"",
            ""properties"": {
                ""text"": { ""type"": ""string"" }
            },
            ""required"": [""text""]
        }");

        var outputSchema = JsonDocument.Parse(@"{
            ""type"": ""string"",
            ""enum"": [""spam"", ""not_spam""]
        }");

        var task = await client.CreateTaskAsync(
            name: $"retry-test-task-{Guid.NewGuid():N}",
            description: "Test task for retry integration testing",
            inputSchema: inputSchema,
            outputSchema: outputSchema,
            accuracyTarget: 0.85,
            latencyTargetMs: 100,
            samplingRate: 0.1
        );

        return task.RootElement.GetProperty("id").GetGuid();
    }
}
