using System.Text.Json;
using FluentAssertions;
using Loopai.Client.Exceptions;
using Loopai.Client.Models;

namespace Loopai.Client.IntegrationTests;

/// <summary>
/// Integration tests for LoopaiClient against real API server.
/// </summary>
public class LoopaiClientIntegrationTests : IClassFixture<LoopaiWebApplicationFactory>
{
    private readonly LoopaiWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;
    private readonly ILoopaiClient _client;

    public LoopaiClientIntegrationTests(LoopaiWebApplicationFactory factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();

        // Create Loopai client pointing to test server
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost";
        _client = new LoopaiClient(new LoopaiClientOptions
        {
            BaseUrl = baseUrl,
            MaxRetries = 0,  // Disable retries for integration tests
            Timeout = TimeSpan.FromSeconds(30)
        });
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnHealthyStatus()
    {
        // Act
        var health = await _client.GetHealthAsync();

        // Assert
        health.Should().NotBeNull();
        health.Status.Should().NotBeNullOrWhiteSpace();
        health.Version.Should().NotBeNullOrWhiteSpace();
        health.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CreateTaskAsync_WithValidParameters_ShouldCreateTask()
    {
        // Arrange
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

        // Act
        var task = await _client.CreateTaskAsync(
            name: $"integration-test-task-{Guid.NewGuid():N}",
            description: "Integration test spam classifier",
            inputSchema: inputSchema,
            outputSchema: outputSchema,
            accuracyTarget: 0.9,
            latencyTargetMs: 100,
            samplingRate: 0.1
        );

        // Assert
        task.Should().NotBeNull();
        task.RootElement.TryGetProperty("id", out var idProp).Should().BeTrue();
        var taskId = idProp.GetGuid();
        taskId.Should().NotBeEmpty();

        // Cleanup: Verify task can be retrieved
        var retrievedTask = await _client.GetTaskAsync(taskId);
        retrievedTask.Should().NotBeNull();
        retrievedTask.RootElement.GetProperty("id").GetGuid().Should().Be(taskId);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidInput_ShouldExecuteSuccessfully()
    {
        // Arrange: Create a task first
        var taskId = await CreateTestTaskAsync();

        var input = new { text = "This is a test message for classification" };

        // Act
        var result = await _client.ExecuteAsync(taskId, input);

        // Assert
        result.Should().NotBeNull();
        result.RootElement.TryGetProperty("output", out _).Should().BeTrue();
        result.RootElement.TryGetProperty("latency_ms", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidTaskId_ShouldThrowLoopaiException()
    {
        // Arrange
        var invalidTaskId = Guid.NewGuid();
        var input = new { text = "Test" };

        // Act
        var act = async () => await _client.ExecuteAsync(invalidTaskId, input);

        // Assert
        await act.Should().ThrowAsync<LoopaiException>()
            .Where(ex => ex.StatusCode == 404);
    }

    [Fact]
    public async Task BatchExecuteAsync_WithMultipleItems_ShouldExecuteAll()
    {
        // Arrange: Create a task first
        var taskId = await CreateTestTaskAsync();

        var inputs = new[]
        {
            new { text = "First test message" },
            new { text = "Second test message" },
            new { text = "Third test message" }
        };

        // Act
        var result = await _client.BatchExecuteAsync(
            taskId,
            inputs,
            maxConcurrency: 10
        );

        // Assert
        result.Should().NotBeNull();
        result.BatchId.Should().NotBeEmpty();
        result.TaskId.Should().Be(taskId);
        result.TotalItems.Should().Be(3);
        result.Results.Should().HaveCount(3);

        // Verify all items processed
        foreach (var item in result.Results)
        {
            item.ExecutionId.Should().NotBeEmpty();
            item.LatencyMs.Should().BeGreaterThan(0);
        }

        // Verify metrics
        result.TotalDurationMs.Should().BeGreaterThan(0);
        result.AvgLatencyMs.Should().BeGreaterThan(0);
        (result.SuccessCount + result.FailureCount).Should().Be(result.TotalItems);
    }

    [Fact]
    public async Task BatchExecuteAsync_WithCustomIds_ShouldPreserveIds()
    {
        // Arrange: Create a task first
        var taskId = await CreateTestTaskAsync();

        var batchItems = new[]
        {
            new BatchExecuteItem
            {
                Id = "custom-id-1",
                Input = JsonSerializer.SerializeToDocument(new { text = "Message 1" }),
                ForceValidation = false
            },
            new BatchExecuteItem
            {
                Id = "custom-id-2",
                Input = JsonSerializer.SerializeToDocument(new { text = "Message 2" }),
                ForceValidation = true
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

        // Act
        var result = await _client.BatchExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(2);

        result.Results.Should().Contain(r => r.Id == "custom-id-1");
        result.Results.Should().Contain(r => r.Id == "custom-id-2");

        // Verify validation sampling flag
        var validationItem = result.Results.First(r => r.Id == "custom-id-2");
        validationItem.SampledForValidation.Should().BeTrue();
    }

    [Fact]
    public async Task GetTaskAsync_WithExistingTaskId_ShouldReturnTask()
    {
        // Arrange: Create a task first
        var taskId = await CreateTestTaskAsync();

        // Act
        var task = await _client.GetTaskAsync(taskId);

        // Assert
        task.Should().NotBeNull();
        task.RootElement.GetProperty("id").GetGuid().Should().Be(taskId);
        task.RootElement.TryGetProperty("name", out _).Should().BeTrue();
        task.RootElement.TryGetProperty("description", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetTaskAsync_WithNonExistentTaskId_ShouldThrowLoopaiException()
    {
        // Arrange
        var nonExistentTaskId = Guid.NewGuid();

        // Act
        var act = async () => await _client.GetTaskAsync(nonExistentTaskId);

        // Assert
        await act.Should().ThrowAsync<LoopaiException>()
            .Where(ex => ex.StatusCode == 404);
    }

    /// <summary>
    /// Helper method to create a test task for integration tests.
    /// </summary>
    private async Task<Guid> CreateTestTaskAsync()
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

        var task = await _client.CreateTaskAsync(
            name: $"test-task-{Guid.NewGuid():N}",
            description: "Test task for integration testing",
            inputSchema: inputSchema,
            outputSchema: outputSchema,
            accuracyTarget: 0.85,
            latencyTargetMs: 100,
            samplingRate: 0.1
        );

        return task.RootElement.GetProperty("id").GetGuid();
    }
}
