using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Loopai.CloudApi.DTOs;
using Loopai.Core.Models;

namespace Loopai.CloudApi.Tests.DTOs;

public class JsonSerializationTests
{
    private readonly JsonSerializerOptions _options;

    public JsonSerializationTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
    }

    [Fact]
    public void ExecuteRequest_Should_Serialize_With_SnakeCase()
    {
        // Arrange
        var request = new ExecuteRequest
        {
            TaskId = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            Version = 5,
            Input = JsonDocument.Parse("{\"value\": 42}"),
            ForceValidation = true,
            TimeoutMs = 10000
        };

        // Act
        var json = JsonSerializer.Serialize(request, _options);

        // Assert
        json.Should().Contain("\"task_id\"");
        json.Should().Contain("\"version\"");
        json.Should().Contain("\"input\"");
        json.Should().Contain("\"force_validation\"");
        json.Should().Contain("\"timeout_ms\"");
        json.Should().NotContain("TaskId");
        json.Should().NotContain("ForceValidation");
    }

    [Fact]
    public void ExecuteRequest_Should_Deserialize_From_SnakeCase()
    {
        // Arrange
        var json = """
        {
          "task_id": "12345678-1234-1234-1234-123456789012",
          "version": 5,
          "input": {"value": 42},
          "force_validation": true,
          "timeout_ms": 10000
        }
        """;

        // Act
        var request = JsonSerializer.Deserialize<ExecuteRequest>(json, _options);

        // Assert
        request.Should().NotBeNull();
        request!.TaskId.Should().Be(Guid.Parse("12345678-1234-1234-1234-123456789012"));
        request.Version.Should().Be(5);
        request.ForceValidation.Should().BeTrue();
        request.TimeoutMs.Should().Be(10000);
    }

    [Fact]
    public void ExecuteResponse_Should_Serialize_With_SnakeCase()
    {
        // Arrange
        var response = new ExecuteResponse
        {
            ExecutionId = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            TaskId = Guid.Parse("87654321-4321-4321-4321-210987654321"),
            ProgramId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Version = 3,
            Status = ExecutionStatus.Success,
            Output = JsonDocument.Parse("{\"result\": 100}"),
            LatencyMs = 125.5,
            MemoryUsageMb = 10.2,
            SampledForValidation = true,
            ExecutedAt = DateTime.Parse("2025-01-15T10:30:00Z").ToUniversalTime()
        };

        // Act
        var json = JsonSerializer.Serialize(response, _options);

        // Assert
        json.Should().Contain("\"execution_id\"");
        json.Should().Contain("\"task_id\"");
        json.Should().Contain("\"program_id\"");
        json.Should().Contain("\"version\"");
        json.Should().Contain("\"status\"");
        json.Should().Contain("\"output\"");
        json.Should().Contain("\"latency_ms\"");
        json.Should().Contain("\"memory_usage_mb\"");
        json.Should().Contain("\"sampled_for_validation\"");
        json.Should().Contain("\"executed_at\"");
    }

    [Fact]
    public void ExecuteResponse_Should_Serialize_Enum_As_String()
    {
        // Arrange
        var response = new ExecuteResponse
        {
            ExecutionId = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProgramId = Guid.NewGuid(),
            Version = 1,
            Status = ExecutionStatus.Error,
            LatencyMs = 50.0,
            SampledForValidation = false,
            ExecutedAt = DateTime.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(response, _options);

        // Assert
        json.Should().Contain("\"status\": \"error\"");
        json.Should().NotContain("\"status\": 1");
    }

    [Fact]
    public void CreateTaskRequest_Should_Serialize_With_SnakeCase()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Name = "test_task",
            Description = "Test description",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            AccuracyTarget = 0.95,
            LatencyTargetMs = 5000,
            SamplingRate = 0.1
        };

        // Act
        var json = JsonSerializer.Serialize(request, _options);

        // Assert
        json.Should().Contain("\"name\"");
        json.Should().Contain("\"description\"");
        json.Should().Contain("\"input_schema\"");
        json.Should().Contain("\"output_schema\"");
        json.Should().Contain("\"accuracy_target\"");
        json.Should().Contain("\"latency_target_ms\"");
        json.Should().Contain("\"sampling_rate\"");
    }

    [Fact]
    public void TaskResponse_Should_Serialize_With_SnakeCase()
    {
        // Arrange
        var response = new TaskResponse
        {
            Id = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            Name = "test_task",
            Description = "Test description",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            Examples = Array.Empty<JsonDocument>(),
            AccuracyTarget = 0.9,
            LatencyTargetMs = 10,
            SamplingRate = 0.1,
            ActiveVersion = 5,
            TotalVersions = 10,
            CreatedAt = DateTime.Parse("2025-01-15T10:30:00Z").ToUniversalTime(),
            UpdatedAt = DateTime.Parse("2025-01-15T11:30:00Z").ToUniversalTime()
        };

        // Act
        var json = JsonSerializer.Serialize(response, _options);

        // Assert
        json.Should().Contain("\"id\"");
        json.Should().Contain("\"name\"");
        json.Should().Contain("\"description\"");
        json.Should().Contain("\"input_schema\"");
        json.Should().Contain("\"output_schema\"");
        json.Should().Contain("\"active_version\"");
        json.Should().Contain("\"total_versions\"");
        json.Should().Contain("\"created_at\"");
        json.Should().Contain("\"updated_at\"");
    }

    [Fact]
    public void ErrorResponse_Should_Serialize_With_SnakeCase()
    {
        // Arrange
        var response = new ErrorResponse
        {
            Code = "TASK_NOT_FOUND",
            Message = "Task with ID 12345 not found",
            Details = new { TaskId = "12345", SearchedAt = DateTime.UtcNow },
            TraceId = "abc123",
            Timestamp = DateTime.Parse("2025-01-15T10:30:00Z").ToUniversalTime()
        };

        // Act
        var json = JsonSerializer.Serialize(response, _options);

        // Assert
        json.Should().Contain("\"code\"");
        json.Should().Contain("\"message\"");
        json.Should().Contain("\"details\"");
        json.Should().Contain("\"trace_id\"");
        json.Should().Contain("\"timestamp\"");
    }

    [Fact]
    public void Null_Values_Should_Be_Ignored()
    {
        // Arrange
        var response = new ExecuteResponse
        {
            ExecutionId = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProgramId = Guid.NewGuid(),
            Version = 1,
            Status = ExecutionStatus.Error,
            Output = null,
            ErrorMessage = null,
            MemoryUsageMb = null,
            LatencyMs = 50.0,
            SampledForValidation = false,
            ExecutedAt = DateTime.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(response, _options);

        // Assert
        json.Should().NotContain("\"output\"");
        json.Should().NotContain("\"error_message\"");
        json.Should().NotContain("\"memory_usage_mb\"");
    }
}
