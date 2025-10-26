using Microsoft.AspNetCore.Mvc;
using Loopai.CloudApi.DTOs;
using Loopai.CloudApi.Validators;
using FluentValidation;
using System.Text.Json;

namespace Loopai.CloudApi.Controllers;

/// <summary>
/// API endpoints for task management and program execution.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Asp.Versioning.ApiVersion("1.0")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ILogger<TasksController> _logger;
    private readonly IValidator<ExecuteRequest> _executeValidator;
    private readonly IValidator<CreateTaskRequest> _createTaskValidator;
    private readonly IWebHostEnvironment _environment;
    private readonly Core.Interfaces.ITaskService _taskService;
    private readonly Core.Interfaces.IProgramExecutionService _executionService;

    public TasksController(
        ILogger<TasksController> logger,
        IValidator<ExecuteRequest> executeValidator,
        IValidator<CreateTaskRequest> createTaskValidator,
        IWebHostEnvironment environment,
        Core.Interfaces.ITaskService taskService,
        Core.Interfaces.IProgramExecutionService executionService)
    {
        _logger = logger;
        _executeValidator = executeValidator;
        _createTaskValidator = createTaskValidator;
        _environment = environment;
        _taskService = taskService;
        _executionService = executionService;
    }

    /// <summary>
    /// Execute a program for a specific task.
    /// </summary>
    /// <param name="taskId">Task identifier</param>
    /// <param name="request">Execution request</param>
    /// <response code="200">Execution completed successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Task or program not found</response>
    /// <response code="500">Execution error</response>
    [HttpPost("{taskId}/execute")]
    [ProducesResponseType(typeof(ExecuteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExecuteProgram(
        [FromRoute] Guid taskId,
        [FromBody] ExecuteRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await _executeValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "Request validation failed",
                    Details = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }),
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            // Ensure taskId from route matches request
            if (request.TaskId != taskId)
            {
                return BadRequest(new ErrorResponse
                {
                    Code = "TASK_ID_MISMATCH",
                    Message = $"Task ID in route ({taskId}) does not match request body ({request.TaskId})",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            // Execute program via service
            var result = await _executionService.ExecuteAsync(
                taskId,
                request.Input,
                request.Version,
                request.ForceValidation,
                request.TimeoutMs);

            var response = new ExecuteResponse
            {
                ExecutionId = result.ExecutionId,
                TaskId = result.TaskId,
                ProgramId = result.ProgramId,
                Version = result.Version,
                Status = result.Status,
                Output = result.Output,
                ErrorMessage = result.ErrorMessage,
                LatencyMs = result.LatencyMs,
                MemoryUsageMb = result.MemoryUsageMb,
                SampledForValidation = result.SampledForValidation,
                ExecutedAt = result.ExecutedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing program for task {TaskId}", taskId);
            return StatusCode(500, new ErrorResponse
            {
                Code = "EXECUTION_ERROR",
                Message = "An error occurred during program execution",
                Details = _environment.IsDevelopment() ? ex.Message : null,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Create a new task specification.
    /// </summary>
    /// <param name="request">Task creation request</param>
    /// <response code="201">Task created successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="409">Task with same name already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await _createTaskValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "Request validation failed",
                    Details = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }),
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            _logger.LogInformation("Creating task {TaskName}", request.Name);

            // Create task specification from request
            var taskSpec = new Core.Models.TaskSpecification
            {
                Name = request.Name,
                Description = request.Description,
                InputSchema = request.InputSchema,
                OutputSchema = request.OutputSchema,
                Examples = request.Examples ?? Array.Empty<JsonDocument>(),
                AccuracyTarget = request.AccuracyTarget,
                LatencyTargetMs = request.LatencyTargetMs,
                SamplingRate = request.SamplingRate
            };

            // Create task via service
            var createdTask = await _taskService.CreateTaskAsync(taskSpec, default);

            // Build response
            var response = new TaskResponse
            {
                Id = createdTask.Id,
                Name = createdTask.Name,
                Description = createdTask.Description,
                InputSchema = createdTask.InputSchema,
                OutputSchema = createdTask.OutputSchema,
                Examples = createdTask.Examples,
                AccuracyTarget = createdTask.AccuracyTarget,
                LatencyTargetMs = createdTask.LatencyTargetMs,
                SamplingRate = createdTask.SamplingRate,
                ActiveVersion = null,
                TotalVersions = 0,
                CreatedAt = createdTask.CreatedAt,
                UpdatedAt = createdTask.UpdatedAt
            };

            return CreatedAtAction(
                nameof(GetTask),
                new { taskId = createdTask.Id },
                response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Task creation conflict for {TaskName}", request.Name);
            return Conflict(new ErrorResponse
            {
                Code = "TASK_ALREADY_EXISTS",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task {TaskName}", request.Name);
            return StatusCode(500, new ErrorResponse
            {
                Code = "TASK_CREATION_ERROR",
                Message = "An error occurred while creating the task",
                Details = _environment.IsDevelopment() ? ex.Message : null,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get task details by ID.
    /// </summary>
    /// <param name="taskId">Task identifier</param>
    /// <response code="200">Task found</response>
    /// <response code="404">Task not found</response>
    [HttpGet("{taskId}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTask([FromRoute] Guid taskId)
    {
        try
        {
            _logger.LogInformation("Getting task {TaskId}", taskId);

            // Get task with artifact info via service
            var taskInfo = await _taskService.GetTaskWithActiveArtifactAsync(taskId, default);

            if (taskInfo == null)
            {
                return NotFound(new ErrorResponse
                {
                    Code = "TASK_NOT_FOUND",
                    Message = $"Task with ID {taskId} not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            // Build response
            var response = new TaskResponse
            {
                Id = taskInfo.Task.Id,
                Name = taskInfo.Task.Name,
                Description = taskInfo.Task.Description,
                InputSchema = taskInfo.Task.InputSchema,
                OutputSchema = taskInfo.Task.OutputSchema,
                Examples = taskInfo.Task.Examples,
                AccuracyTarget = taskInfo.Task.AccuracyTarget,
                LatencyTargetMs = taskInfo.Task.LatencyTargetMs,
                SamplingRate = taskInfo.Task.SamplingRate,
                ActiveVersion = taskInfo.ActiveVersion,
                TotalVersions = taskInfo.TotalVersions,
                CreatedAt = taskInfo.Task.CreatedAt,
                UpdatedAt = taskInfo.Task.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task {TaskId}", taskId);
            return StatusCode(500, new ErrorResponse
            {
                Code = "TASK_RETRIEVAL_ERROR",
                Message = "An error occurred while retrieving the task",
                Details = _environment.IsDevelopment() ? ex.Message : null,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get all program artifacts for a task.
    /// </summary>
    /// <param name="taskId">Task identifier</param>
    /// <response code="200">Artifacts retrieved successfully</response>
    /// <response code="404">Task not found</response>
    [HttpGet("{taskId}/artifacts")]
    [ProducesResponseType(typeof(IEnumerable<Core.Models.ProgramArtifact>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArtifacts([FromRoute] Guid taskId)
    {
        try
        {
            _logger.LogInformation("Getting artifacts for task {TaskId}", taskId);

            // Get artifacts via service
            var artifacts = await _executionService.GetArtifactsAsync(taskId, default);

            return Ok(artifacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting artifacts for task {TaskId}", taskId);
            return StatusCode(500, new ErrorResponse
            {
                Code = "ARTIFACT_RETRIEVAL_ERROR",
                Message = "An error occurred while retrieving artifacts",
                Details = _environment.IsDevelopment() ? ex.Message : null,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get a specific program artifact version for a task.
    /// </summary>
    /// <param name="taskId">Task identifier</param>
    /// <param name="version">Program version</param>
    /// <response code="200">Artifact found</response>
    /// <response code="404">Task or artifact not found</response>
    [HttpGet("{taskId}/artifacts/{version}")]
    [ProducesResponseType(typeof(Core.Models.ProgramArtifact), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArtifactVersion(
        [FromRoute] Guid taskId,
        [FromRoute] int version)
    {
        try
        {
            _logger.LogInformation("Getting artifact version {Version} for task {TaskId}", version, taskId);

            // Get artifact version via service
            var artifact = await _executionService.GetArtifactVersionAsync(taskId, version, default);

            if (artifact == null)
            {
                return NotFound(new ErrorResponse
                {
                    Code = "ARTIFACT_NOT_FOUND",
                    Message = $"Artifact version {version} for task {taskId} not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(artifact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting artifact version {Version} for task {TaskId}", version, taskId);
            return StatusCode(500, new ErrorResponse
            {
                Code = "ARTIFACT_RETRIEVAL_ERROR",
                Message = "An error occurred while retrieving the artifact",
                Details = _environment.IsDevelopment() ? ex.Message : null,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
