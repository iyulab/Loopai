using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Loopai.CloudApi.Controllers;
using Loopai.CloudApi.DTOs;
using Loopai.CloudApi.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Loopai.CloudApi.Tests.Controllers;

public class TasksControllerTests
{
    private readonly Mock<ILogger<TasksController>> _loggerMock;
    private readonly IValidator<ExecuteRequest> _executeValidator;
    private readonly IValidator<CreateTaskRequest> _createTaskValidator;
    private readonly Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment> _environmentMock;
    private readonly Mock<Core.Interfaces.ITaskService> _taskServiceMock;
    private readonly Mock<Core.Interfaces.IProgramExecutionService> _executionServiceMock;
    private readonly TasksController _controller;

    public TasksControllerTests()
    {
        _loggerMock = new Mock<ILogger<TasksController>>();
        _executeValidator = new ExecuteRequestValidator();
        _createTaskValidator = new CreateTaskRequestValidator();
        _environmentMock = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        _taskServiceMock = new Mock<Core.Interfaces.ITaskService>();
        _executionServiceMock = new Mock<Core.Interfaces.IProgramExecutionService>();

        _controller = new TasksController(
            _loggerMock.Object,
            _executeValidator,
            _createTaskValidator,
            _environmentMock.Object,
            _taskServiceMock.Object,
            _executionServiceMock.Object
        );

        // Setup HttpContext for TraceIdentifier
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = "test-trace-id"
            }
        };
    }

    [Fact]
    public async Task ExecuteProgram_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var request = new ExecuteRequest
        {
            TaskId = taskId,
            Input = JsonDocument.Parse("{\"value\": 42}")
        };

        var executionResult = new Core.Interfaces.ExecutionResult
        {
            ExecutionId = Guid.NewGuid(),
            TaskId = taskId,
            ProgramId = Guid.NewGuid(),
            Version = 1,
            Status = Core.Models.ExecutionStatus.Success,
            Output = JsonDocument.Parse("{\"result\": \"test\"}"),
            LatencyMs = 5.0,
            SampledForValidation = false,
            ExecutedAt = DateTime.UtcNow
        };

        _executionServiceMock
            .Setup(s => s.ExecuteAsync(taskId, It.IsAny<JsonDocument>(), null, false, null, default))
            .ReturnsAsync(executionResult);

        // Act
        var result = await _controller.ExecuteProgram(taskId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ExecuteResponse>();

        var response = okResult.Value as ExecuteResponse;
        response!.TaskId.Should().Be(taskId);
        response.Status.Should().Be(Core.Models.ExecutionStatus.Success);
    }

    [Fact]
    public async Task ExecuteProgram_WithMismatchedTaskId_ReturnsBadRequest()
    {
        // Arrange
        var routeTaskId = Guid.NewGuid();
        var requestTaskId = Guid.NewGuid();
        var request = new ExecuteRequest
        {
            TaskId = requestTaskId,
            Input = JsonDocument.Parse("{\"value\": 42}")
        };

        // Act
        var result = await _controller.ExecuteProgram(routeTaskId, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest!.Value.Should().BeOfType<ErrorResponse>();

        var error = badRequest.Value as ErrorResponse;
        error!.Code.Should().Be("TASK_ID_MISMATCH");
    }

    [Fact]
    public async Task ExecuteProgram_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var request = new ExecuteRequest
        {
            TaskId = Guid.Empty, // Invalid
            Input = JsonDocument.Parse("{\"value\": 42}")
        };

        // Act
        var result = await _controller.ExecuteProgram(taskId, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest!.Value.Should().BeOfType<ErrorResponse>();

        var error = badRequest.Value as ErrorResponse;
        error!.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task ExecuteProgram_WithVersion_IncludesVersionInResponse()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var request = new ExecuteRequest
        {
            TaskId = taskId,
            Version = 5,
            Input = JsonDocument.Parse("{\"value\": 42}")
        };

        var executionResult = new Core.Interfaces.ExecutionResult
        {
            ExecutionId = Guid.NewGuid(),
            TaskId = taskId,
            ProgramId = Guid.NewGuid(),
            Version = 5,
            Status = Core.Models.ExecutionStatus.Success,
            Output = JsonDocument.Parse("{\"result\": \"test\"}"),
            LatencyMs = 5.0,
            SampledForValidation = false,
            ExecutedAt = DateTime.UtcNow
        };

        _executionServiceMock
            .Setup(s => s.ExecuteAsync(taskId, It.IsAny<JsonDocument>(), 5, false, null, default))
            .ReturnsAsync(executionResult);

        // Act
        var result = await _controller.ExecuteProgram(taskId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ExecuteResponse;
        response!.Version.Should().Be(5);
    }

    [Fact]
    public async Task CreateTask_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Name = "test_task",
            Description = "Test task description",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}")
        };

        var createdTask = new Core.Models.TaskSpecification
        {
            Id = Guid.NewGuid(),
            Name = "test_task",
            Description = "Test task description",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            AccuracyTarget = 0.9,
            LatencyTargetMs = 10,
            SamplingRate = 0.1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _taskServiceMock
            .Setup(s => s.CreateTaskAsync(It.IsAny<Core.Models.TaskSpecification>(), default))
            .ReturnsAsync(createdTask);

        // Act
        var result = await _controller.CreateTask(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult!.Value.Should().BeOfType<TaskResponse>();

        var response = createdResult.Value as TaskResponse;
        response!.Name.Should().Be("test_task");
        response.Description.Should().Be("Test task description");
        response.TotalVersions.Should().Be(0);
    }

    [Fact]
    public async Task CreateTask_WithInvalidName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Name = "invalid name with spaces",
            Description = "Test description",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}")
        };

        // Act
        var result = await _controller.CreateTask(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest!.Value.Should().BeOfType<ErrorResponse>();

        var error = badRequest.Value as ErrorResponse;
        error!.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task CreateTask_WithEmptyDescription_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Name = "valid_task",
            Description = "",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}")
        };

        // Act
        var result = await _controller.CreateTask(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetTask_ReturnsNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _taskServiceMock
            .Setup(s => s.GetTaskWithActiveArtifactAsync(taskId, default))
            .ReturnsAsync((Core.Interfaces.TaskWithArtifactInfo?)null);

        // Act
        var result = await _controller.GetTask(taskId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFound = result as NotFoundObjectResult;
        notFound!.Value.Should().BeOfType<ErrorResponse>();

        var error = notFound.Value as ErrorResponse;
        error!.Code.Should().Be("TASK_NOT_FOUND");
    }

    [Fact]
    public async Task GetArtifacts_ReturnsEmptyList()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _executionServiceMock
            .Setup(s => s.GetArtifactsAsync(taskId, default))
            .ReturnsAsync(Array.Empty<Core.Models.ProgramArtifact>());

        // Act
        var result = await _controller.GetArtifacts(taskId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeAssignableTo<IEnumerable<Core.Models.ProgramArtifact>>();

        var artifacts = okResult.Value as IEnumerable<Core.Models.ProgramArtifact>;
        artifacts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetArtifactVersion_ReturnsNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var version = 1;

        _executionServiceMock
            .Setup(s => s.GetArtifactVersionAsync(taskId, version, default))
            .ReturnsAsync((Core.Models.ProgramArtifact?)null);

        // Act
        var result = await _controller.GetArtifactVersion(taskId, version);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFound = result as NotFoundObjectResult;
        notFound!.Value.Should().BeOfType<ErrorResponse>();

        var error = notFound.Value as ErrorResponse;
        error!.Code.Should().Be("ARTIFACT_NOT_FOUND");
    }

    [Fact]
    public async Task CreateTask_WithCustomTargets_PreservesValues()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Name = "custom_task",
            Description = "Task with custom targets",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            AccuracyTarget = 0.95,
            LatencyTargetMs = 5000,
            SamplingRate = 0.2
        };

        var createdTask = new Core.Models.TaskSpecification
        {
            Id = Guid.NewGuid(),
            Name = "custom_task",
            Description = "Task with custom targets",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            AccuracyTarget = 0.95,
            LatencyTargetMs = 5000,
            SamplingRate = 0.2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _taskServiceMock
            .Setup(s => s.CreateTaskAsync(It.IsAny<Core.Models.TaskSpecification>(), default))
            .ReturnsAsync(createdTask);

        // Act
        var result = await _controller.CreateTask(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        var response = createdResult!.Value as TaskResponse;

        response!.AccuracyTarget.Should().Be(0.95);
        response.LatencyTargetMs.Should().Be(5000);
        response.SamplingRate.Should().Be(0.2);
    }
}
