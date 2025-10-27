using System.Net;
using System.Text.Json;
using FluentAssertions;
using Loopai.Client;
using Loopai.Client.Exceptions;
using Loopai.Client.Models;
using Moq;
using Moq.Protected;

namespace Loopai.Client.Tests;

public class LoopaiClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly LoopaiClientOptions _options;

    public LoopaiClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        _options = new LoopaiClientOptions
        {
            BaseUrl = "http://localhost:8080",
            ApiKey = "test-api-key",
            MaxRetries = 0 // Disable retries for unit tests
        };
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldSucceed()
    {
        // Arrange & Act
        var client = new LoopaiClient(_options);

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var act = () => new LoopaiClient((LoopaiClientOptions)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithInvalidBaseUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new LoopaiClientOptions { BaseUrl = "" };

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithInvalidMaxRetries_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new LoopaiClientOptions { MaxRetries = -1 };

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetHealthAsync_WithSuccessResponse_ShouldReturnHealthResponse()
    {
        // Arrange
        var expectedResponse = new HealthResponse
        {
            Status = "healthy",
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        var client = new LoopaiClient(_options);

        // Act
        var result = await client.GetHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("healthy");
    }

    [Fact]
    public async Task ExecuteAsync_WithValidInput_ShouldReturnResult()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var inputDoc = JsonDocument.Parse("{\"value\": 42}");
        var outputJson = "{\"result\": 84}";

        SetupHttpResponse(HttpStatusCode.OK, outputJson);

        var client = new LoopaiClient(_options);

        // Act
        var result = await client.ExecuteAsync(taskId, inputDoc);

        // Assert
        result.Should().NotBeNull();
        result.RootElement.GetProperty("result").GetInt32().Should().Be(84);
    }

    [Fact]
    public async Task ExecuteAsync_WithObject_ShouldSerializeAndExecute()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var input = new { value = 42 };
        var outputJson = "{\"result\": 84}";

        SetupHttpResponse(HttpStatusCode.OK, outputJson);

        var client = new LoopaiClient(_options);

        // Act
        var result = await client.ExecuteAsync(taskId, input);

        // Assert
        result.Should().NotBeNull();
        result.RootElement.GetProperty("result").GetInt32().Should().Be(84);
    }

    [Fact]
    public async Task ExecuteAsync_WithBadRequest_ShouldThrowValidationException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var input = new { value = 42 };

        SetupHttpResponse(HttpStatusCode.BadRequest, "{\"error\": \"Invalid input\"}");

        var client = new LoopaiClient(_options);

        // Act & Assert
        var act = async () => await client.ExecuteAsync(taskId, input);
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Validation failed*");
    }

    [Fact]
    public async Task ExecuteAsync_WithNotFound_ShouldThrowLoopaiException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var input = new { value = 42 };

        SetupHttpResponse(HttpStatusCode.NotFound, "{\"error\": \"Task not found\"}");

        var client = new LoopaiClient(_options);

        // Act & Assert
        var act = async () => await client.ExecuteAsync(taskId, input);
        await act.Should().ThrowAsync<LoopaiException>()
            .WithMessage("*Resource not found*");
    }

    [Fact]
    public async Task ExecuteAsync_WithInternalServerError_ShouldThrowExecutionException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var input = new { value = 42 };

        SetupHttpResponse(HttpStatusCode.InternalServerError, "{\"error\": \"Server error\"}");

        var client = new LoopaiClient(_options);

        // Act & Assert
        var act = async () => await client.ExecuteAsync(taskId, input);
        await act.Should().ThrowAsync<ExecutionException>()
            .WithMessage("*Server error*");
    }

    [Fact]
    public async Task BatchExecuteAsync_WithValidRequest_ShouldReturnBatchResponse()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        var items = new[]
        {
            new BatchExecuteItem
            {
                Id = "item1",
                Input = JsonDocument.Parse("{\"value\": 10}"),
                ForceValidation = false
            },
            new BatchExecuteItem
            {
                Id = "item2",
                Input = JsonDocument.Parse("{\"value\": 20}"),
                ForceValidation = false
            }
        };

        var request = new BatchExecuteRequest
        {
            TaskId = taskId,
            Items = items,
            MaxConcurrency = 10
        };

        var responseJson = JsonSerializer.Serialize(new
        {
            batch_id = batchId,
            task_id = taskId,
            version = 1,
            total_items = 2,
            success_count = 2,
            failure_count = 0,
            total_duration_ms = 150.0,
            avg_latency_ms = 75.0,
            results = new[]
            {
                new
                {
                    id = "item1",
                    execution_id = Guid.NewGuid(),
                    success = true,
                    output = JsonDocument.Parse("{\"output\": 20}"),
                    latency_ms = 70.0,
                    sampled_for_validation = false
                },
                new
                {
                    id = "item2",
                    execution_id = Guid.NewGuid(),
                    success = true,
                    output = JsonDocument.Parse("{\"output\": 40}"),
                    latency_ms = 80.0,
                    sampled_for_validation = false
                }
            },
            started_at = DateTime.UtcNow.AddSeconds(-1),
            completed_at = DateTime.UtcNow
        });

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        var client = new LoopaiClient(_options);

        // Act
        var result = await client.BatchExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.BatchId.Should().Be(batchId);
        result.TotalItems.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(0);
        result.Results.Should().HaveCount(2);
    }

    [Fact]
    public async Task BatchExecuteAsync_WithSimplifiedInputs_ShouldExecute()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var inputs = new[]
        {
            new { value = 10 },
            new { value = 20 }
        };

        var responseJson = JsonSerializer.Serialize(new
        {
            batch_id = Guid.NewGuid(),
            task_id = taskId,
            version = 1,
            total_items = 2,
            success_count = 2,
            failure_count = 0,
            total_duration_ms = 150.0,
            avg_latency_ms = 75.0,
            results = new[]
            {
                new
                {
                    id = "0",
                    execution_id = Guid.NewGuid(),
                    success = true,
                    output = JsonDocument.Parse("{\"output\": 20}"),
                    latency_ms = 70.0,
                    sampled_for_validation = false
                },
                new
                {
                    id = "1",
                    execution_id = Guid.NewGuid(),
                    success = true,
                    output = JsonDocument.Parse("{\"output\": 40}"),
                    latency_ms = 80.0,
                    sampled_for_validation = false
                }
            },
            started_at = DateTime.UtcNow.AddSeconds(-1),
            completed_at = DateTime.UtcNow
        });

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        var client = new LoopaiClient(_options);

        // Act
        var result = await client.BatchExecuteAsync(taskId, inputs, maxConcurrency: 10);

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.Results.Should().HaveCount(2);
        result.Results.First().Id.Should().Be("0");
    }

    [Fact]
    public async Task BatchExecuteAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var client = new LoopaiClient(_options);

        // Act & Assert
        var act = async () => await client.BatchExecuteAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateTaskAsync_WithValidParameters_ShouldReturnTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var inputSchema = JsonDocument.Parse("{\"type\": \"object\"}");
        var outputSchema = JsonDocument.Parse("{\"type\": \"string\"}");

        var responseJson = JsonSerializer.Serialize(new
        {
            id = taskId,
            name = "test-task",
            description = "Test task",
            input_schema = inputSchema,
            output_schema = outputSchema,
            created_at = DateTime.UtcNow
        });

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        var client = new LoopaiClient(_options);

        // Act
        var result = await client.CreateTaskAsync(
            name: "test-task",
            description: "Test task",
            inputSchema: inputSchema,
            outputSchema: outputSchema);

        // Assert
        result.Should().NotBeNull();
        result.RootElement.GetProperty("id").GetGuid().Should().Be(taskId);
        result.RootElement.GetProperty("name").GetString().Should().Be("test-task");
    }

    [Fact]
    public async Task GetTaskAsync_WithValidTaskId_ShouldReturnTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        var responseJson = JsonSerializer.Serialize(new
        {
            id = taskId,
            name = "test-task",
            description = "Test task"
        });

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        var client = new LoopaiClient(_options);

        // Act
        var result = await client.GetTaskAsync(taskId);

        // Assert
        result.Should().NotBeNull();
        result.RootElement.GetProperty("id").GetGuid().Should().Be(taskId);
    }

    [Fact]
    public void Dispose_ShouldDisposeHttpClient()
    {
        // Arrange
        var client = new LoopaiClient(_options);

        // Act
        client.Dispose();

        // Assert - Should not throw
        client.Dispose(); // Dispose twice should be safe
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });
    }
}
