using System.Text.Json;
using Loopai.CloudApi.Services;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Loopai.CloudApi.Tests.Services;

/// <summary>
/// Unit tests for SchemaOutputValidator.
/// </summary>
public class SchemaOutputValidatorTests
{
    private readonly Mock<ITaskRepository> _taskRepoMock;
    private readonly Mock<IExecutionRecordRepository> _executionRepoMock;
    private readonly Mock<ILogger<SchemaOutputValidator>> _loggerMock;
    private readonly SchemaOutputValidator _validator;

    public SchemaOutputValidatorTests()
    {
        _taskRepoMock = new Mock<ITaskRepository>();
        _executionRepoMock = new Mock<IExecutionRecordRepository>();
        _loggerMock = new Mock<ILogger<SchemaOutputValidator>>();

        _validator = new SchemaOutputValidator(
            _taskRepoMock.Object,
            _executionRepoMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task ValidateOutputAsync_ValidOutput_ReturnsValid()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskSpecification
        {
            Id = taskId,
            Name = "test-task",
            Description = "Test",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "result": { "type": "number" }
                },
                "required": ["result"]
            }
            """)
        };

        var output = JsonDocument.Parse("{\"result\": 42}");

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var result = await _validator.ValidateOutputAsync(taskId, output);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1.0, result.Score);
        Assert.Empty(result.Errors);
        Assert.Equal("schema", result.Method);
    }

    [Fact]
    public async Task ValidateOutputAsync_InvalidSchema_ReturnsInvalid()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskSpecification
        {
            Id = taskId,
            Name = "test-task",
            Description = "Test",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "result": { "type": "number" }
                },
                "required": ["result"]
            }
            """)
        };

        var output = JsonDocument.Parse("{\"result\": \"not a number\"}");

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var result = await _validator.ValidateOutputAsync(taskId, output);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(0.0, result.Score);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Type == "schema");
    }

    [Fact]
    public async Task ValidateOutputAsync_MissingRequiredProperty_ReturnsInvalid()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskSpecification
        {
            Id = taskId,
            Name = "test-task",
            Description = "Test",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "result": { "type": "number" },
                    "status": { "type": "string" }
                },
                "required": ["result", "status"]
            }
            """)
        };

        var output = JsonDocument.Parse("{\"result\": 42}");

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var result = await _validator.ValidateOutputAsync(taskId, output);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ValidateOutputAsync_WithExpectedOutput_ComparesValues()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskSpecification
        {
            Id = taskId,
            Name = "test-task",
            Description = "Test",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "result": { "type": "number" }
                }
            }
            """)
        };

        var output = JsonDocument.Parse("{\"result\": 42}");
        var expected = JsonDocument.Parse("{\"result\": 42}");

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var result = await _validator.ValidateOutputAsync(taskId, output, expected);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1.0, result.Score);
        Assert.Equal("schema+comparison", result.Method);
    }

    [Fact]
    public async Task ValidateOutputAsync_WithExpectedOutput_ValueMismatch_ReturnsErrors()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskSpecification
        {
            Id = taskId,
            Name = "test-task",
            Description = "Test",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "result": { "type": "number" }
                }
            }
            """)
        };

        var output = JsonDocument.Parse("{\"result\": 42}");
        var expected = JsonDocument.Parse("{\"result\": 100}");

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var result = await _validator.ValidateOutputAsync(taskId, output, expected);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Type == "value_mismatch");
    }

    [Fact]
    public async Task ValidateExecutionAsync_SuccessfulExecution_ValidatesOutput()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var execution = new ExecutionRecord
        {
            Id = executionId,
            TaskId = taskId,
            ProgramId = Guid.NewGuid(),
            Status = ExecutionStatus.Success,
            InputData = JsonDocument.Parse("{}"),
            OutputData = JsonDocument.Parse("{\"result\": 42}"),
            LatencyMs = 100,
            ExecutedAt = DateTime.UtcNow
        };

        var task = new TaskSpecification
        {
            Id = taskId,
            Name = "test-task",
            Description = "Test",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "result": { "type": "number" }
                },
                "required": ["result"]
            }
            """)
        };

        _executionRepoMock.Setup(r => r.GetByIdAsync(executionId, default))
            .ReturnsAsync(execution);

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var result = await _validator.ValidateExecutionAsync(executionId);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1.0, result.Score);
    }

    [Fact]
    public async Task ValidateExecutionAsync_FailedExecution_ReturnsInvalid()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var execution = new ExecutionRecord
        {
            Id = executionId,
            TaskId = Guid.NewGuid(),
            ProgramId = Guid.NewGuid(),
            Status = ExecutionStatus.Error,
            InputData = JsonDocument.Parse("{}"),
            OutputData = null,
            ErrorMessage = "Runtime error",
            LatencyMs = 100,
            ExecutedAt = DateTime.UtcNow
        };

        _executionRepoMock.Setup(r => r.GetByIdAsync(executionId, default))
            .ReturnsAsync(execution);

        // Act
        var result = await _validator.ValidateExecutionAsync(executionId);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(0.0, result.Score);
        Assert.Contains(result.Errors, e => e.Type == "execution_failed");
    }

    [Fact]
    public async Task ValidateOutputAsync_TaskNotFound_ReturnsError()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var output = JsonDocument.Parse("{\"result\": 42}");

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync((TaskSpecification?)null);

        // Act
        var result = await _validator.ValidateOutputAsync(taskId, output);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(0.0, result.Score);
        Assert.Contains(result.Errors, e => e.Type == "task_not_found");
    }

    [Fact]
    public async Task ValidateOutputAsync_ComplexNestedSchema_ValidatesCorrectly()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskSpecification
        {
            Id = taskId,
            Name = "complex-task",
            Description = "Test",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "user": {
                        "type": "object",
                        "properties": {
                            "id": { "type": "number" },
                            "name": { "type": "string" }
                        },
                        "required": ["id", "name"]
                    },
                    "items": {
                        "type": "array",
                        "items": { "type": "number" }
                    }
                },
                "required": ["user", "items"]
            }
            """)
        };

        var output = JsonDocument.Parse("""
        {
            "user": {
                "id": 123,
                "name": "Test User"
            },
            "items": [1, 2, 3, 4, 5]
        }
        """);

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var result = await _validator.ValidateOutputAsync(taskId, output);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1.0, result.Score);
    }
}
