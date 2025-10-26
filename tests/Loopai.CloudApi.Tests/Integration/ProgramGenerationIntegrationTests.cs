using System.Text.Json;
using Loopai.CloudApi.Tests.Mocks;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Loopai.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Loopai.CloudApi.Tests.Integration;

/// <summary>
/// Integration tests for program generation flow.
/// </summary>
public class ProgramGenerationIntegrationTests : IDisposable
{
    private readonly Mock<ITaskRepository> _taskRepoMock;
    private readonly Mock<IProgramArtifactRepository> _artifactRepoMock;
    private readonly Mock<IExecutionRecordRepository> _executionRepoMock;
    private readonly Mock<ILogger<ProgramExecutionService>> _loggerMock;
    private readonly MockProgramGeneratorService _mockGenerator;
    private readonly MockEdgeRuntimeService _mockRuntime;
    private readonly ProgramExecutionService _service;

    public ProgramGenerationIntegrationTests()
    {
        _taskRepoMock = new Mock<ITaskRepository>();
        _artifactRepoMock = new Mock<IProgramArtifactRepository>();
        _executionRepoMock = new Mock<IExecutionRecordRepository>();
        _loggerMock = new Mock<ILogger<ProgramExecutionService>>();
        _mockGenerator = new MockProgramGeneratorService();
        _mockRuntime = new MockEdgeRuntimeService();

        _service = new ProgramExecutionService(
            _taskRepoMock.Object,
            _artifactRepoMock.Object,
            _executionRepoMock.Object,
            _loggerMock.Object,
            _mockGenerator,
            _mockRuntime
        );
    }

    [Fact]
    public async Task ExecuteAsync_NoActiveArtifact_GeneratesAndExecutesProgram()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskSpecification
        {
            Id = taskId,
            Name = "test-task",
            Description = "Test task",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            SamplingRate = 1.0
        };

        var generatedCode = @"
async function main(input: any): Promise<any> {
    return { result: input.value * 2 };
}";

        var input = JsonDocument.Parse("{\"value\": 42}");
        var expectedOutput = JsonDocument.Parse("{\"result\": 84}");

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        _artifactRepoMock.Setup(r => r.GetActiveByTaskIdAsync(taskId, default))
            .ReturnsAsync((ProgramArtifact?)null);

        _artifactRepoMock.Setup(r => r.GetLatestVersionAsync(taskId, default))
            .ReturnsAsync(0);

        _artifactRepoMock.Setup(r => r.CreateAsync(It.IsAny<ProgramArtifact>(), default))
            .ReturnsAsync((ProgramArtifact a, CancellationToken _) => a);

        _executionRepoMock.Setup(r => r.CreateAsync(It.IsAny<ExecutionRecord>(), default))
            .ReturnsAsync((ExecutionRecord e, CancellationToken _) => e);

        _mockGenerator.ConfigureProgram(taskId, generatedCode);
        _mockRuntime.ConfigureExecutor(generatedCode, _ => expectedOutput);

        // Act
        var result = await _service.ExecuteAsync(taskId, input);

        // Assert
        Assert.NotEqual(Guid.Empty, result.ExecutionId);
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.NotNull(result.Output);
        Assert.Equal(1, result.Version);
        Assert.True(result.SampledForValidation);

        // Verify artifact was created
        _artifactRepoMock.Verify(r => r.CreateAsync(
            It.Is<ProgramArtifact>(a =>
                a.TaskId == taskId &&
                a.Version == 1 &&
                a.Status == ProgramStatus.Active &&
                a.DeploymentPercentage == 100.0
            ),
            default
        ), Times.Once);

        // Verify execution record was created
        _executionRepoMock.Verify(r => r.CreateAsync(
            It.Is<ExecutionRecord>(e =>
                e.TaskId == taskId &&
                e.Status == ExecutionStatus.Success &&
                e.SampledForValidation == true
            ),
            default
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_GenerationFails_ThrowsException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskSpecification
        {
            Id = taskId,
            Name = "test-task",
            Description = "Test task",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}")
        };

        var input = JsonDocument.Parse("{\"value\": 42}");

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        _artifactRepoMock.Setup(r => r.GetActiveByTaskIdAsync(taskId, default))
            .ReturnsAsync((ProgramArtifact?)null);

        _artifactRepoMock.Setup(r => r.GetLatestVersionAsync(taskId, default))
            .ReturnsAsync(0);

        _mockGenerator.ConfigureFailure("Generation service unavailable");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExecuteAsync(taskId, input)
        );

        Assert.Contains("Program generation failed", exception.Message);
        Assert.Contains("Generation service unavailable", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_RuntimeExecutionFails_RecordsError()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var task = new TaskSpecification
        {
            Id = taskId,
            Name = "test-task",
            Description = "Test task",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}")
        };

        var artifact = new ProgramArtifact
        {
            Id = programId,
            TaskId = taskId,
            Version = 1,
            Code = "throw new Error('Runtime error');",
            Language = "typescript",
            Status = ProgramStatus.Active
        };

        var input = JsonDocument.Parse("{\"value\": 42}");

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        _artifactRepoMock.Setup(r => r.GetActiveByTaskIdAsync(taskId, default))
            .ReturnsAsync(artifact);

        _executionRepoMock.Setup(r => r.CreateAsync(It.IsAny<ExecutionRecord>(), default))
            .ReturnsAsync((ExecutionRecord e, CancellationToken _) => e);

        _mockRuntime.ConfigureFailure("Runtime error: division by zero");

        // Act
        var result = await _service.ExecuteAsync(taskId, input);

        // Assert
        Assert.Equal(ExecutionStatus.Error, result.Status);
        Assert.Null(result.Output);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Runtime error", result.ErrorMessage);

        // Verify error was recorded
        _executionRepoMock.Verify(r => r.CreateAsync(
            It.Is<ExecutionRecord>(e =>
                e.Status == ExecutionStatus.Error &&
                e.OutputData == null
            ),
            default
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingArtifact_SkipsGeneration()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var task = new TaskSpecification
        {
            Id = taskId,
            Name = "test-task",
            Description = "Test task",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}")
        };

        var existingCode = "async function main(input) { return input; }";
        var artifact = new ProgramArtifact
        {
            Id = programId,
            TaskId = taskId,
            Version = 5,
            Code = existingCode,
            Language = "typescript",
            Status = ProgramStatus.Active
        };

        var input = JsonDocument.Parse("{\"value\": 42}");

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        _artifactRepoMock.Setup(r => r.GetActiveByTaskIdAsync(taskId, default))
            .ReturnsAsync(artifact);

        _executionRepoMock.Setup(r => r.CreateAsync(It.IsAny<ExecutionRecord>(), default))
            .ReturnsAsync((ExecutionRecord e, CancellationToken _) => e);

        // Act
        var result = await _service.ExecuteAsync(taskId, input);

        // Assert
        Assert.Equal(programId, result.ProgramId);
        Assert.Equal(5, result.Version);

        // Verify no new artifact was created
        _artifactRepoMock.Verify(r => r.CreateAsync(
            It.IsAny<ProgramArtifact>(),
            default
        ), Times.Never);

        // Verify generation was not called (no new program configured)
        _artifactRepoMock.Verify(r => r.GetLatestVersionAsync(
            It.IsAny<Guid>(),
            default
        ), Times.Never);
    }

    public void Dispose()
    {
        _mockGenerator.Reset();
        _mockRuntime.Reset();
    }
}
