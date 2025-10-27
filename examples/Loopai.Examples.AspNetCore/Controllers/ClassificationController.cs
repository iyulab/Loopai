using Microsoft.AspNetCore.Mvc;
using Loopai.Client;
using Loopai.Client.Exceptions;
using System.Text.Json;

namespace Loopai.Examples.AspNetCore.Controllers;

/// <summary>
/// Example controller demonstrating Loopai client usage.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ClassificationController : ControllerBase
{
    private readonly ILoopaiClient _loopai;
    private readonly ILogger<ClassificationController> _logger;

    public ClassificationController(
        ILoopaiClient loopai,
        ILogger<ClassificationController> logger)
    {
        _loopai = loopai;
        _logger = logger;
    }

    /// <summary>
    /// Classifies text using a Loopai task.
    /// </summary>
    /// <param name="request">Classification request.</param>
    /// <returns>Classification result.</returns>
    [HttpPost("classify")]
    [ProducesResponseType(typeof(ClassifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Classify([FromBody] ClassifyRequest request)
    {
        try
        {
            _logger.LogInformation("Classifying text: {Text}", request.Text);

            var result = await _loopai.ExecuteAsync(
                request.TaskId,
                new { text = request.Text }
            );

            var output = result.RootElement.GetProperty("output").GetString();
            var latencyMs = result.RootElement.GetProperty("latency_ms").GetDouble();

            _logger.LogInformation(
                "Classification result: {Output} (latency: {Latency}ms)",
                output,
                latencyMs
            );

            return Ok(new ClassifyResponse
            {
                Classification = output!,
                LatencyMs = latencyMs
            });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                Errors = ex.Errors
            });
        }
        catch (ExecutionException ex)
        {
            _logger.LogError(ex, "Execution failed");
            return StatusCode(500, new ErrorResponse
            {
                Message = ex.Message,
                ExecutionId = ex.ExecutionId
            });
        }
        catch (LoopaiException ex)
        {
            _logger.LogError(ex, "Loopai API error");
            return StatusCode(ex.StatusCode ?? 500, new ErrorResponse
            {
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Creates a new classification task.
    /// </summary>
    /// <param name="request">Task creation request.</param>
    /// <returns>Created task details.</returns>
    [HttpPost("tasks")]
    [ProducesResponseType(typeof(JsonDocument), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        try
        {
            _logger.LogInformation("Creating task: {Name}", request.Name);

            var inputSchema = JsonDocument.Parse(request.InputSchema);
            var outputSchema = JsonDocument.Parse(request.OutputSchema);

            var task = await _loopai.CreateTaskAsync(
                name: request.Name,
                description: request.Description,
                inputSchema: inputSchema,
                outputSchema: outputSchema,
                accuracyTarget: request.AccuracyTarget,
                latencyTargetMs: request.LatencyTargetMs
            );

            var taskId = task.RootElement.GetProperty("id").GetGuid();

            _logger.LogInformation("Task created: {TaskId}", taskId);

            return CreatedAtAction(
                nameof(GetTask),
                new { taskId },
                task
            );
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Task creation validation failed");
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                Errors = ex.Errors
            });
        }
        catch (LoopaiException ex)
        {
            _logger.LogError(ex, "Task creation failed");
            return StatusCode(ex.StatusCode ?? 500, new ErrorResponse
            {
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets task details by ID.
    /// </summary>
    /// <param name="taskId">Task identifier.</param>
    /// <returns>Task details.</returns>
    [HttpGet("tasks/{taskId}")]
    [ProducesResponseType(typeof(JsonDocument), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTask(Guid taskId)
    {
        try
        {
            var task = await _loopai.GetTaskAsync(taskId);
            return Ok(task);
        }
        catch (LoopaiException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
        catch (LoopaiException ex)
        {
            _logger.LogError(ex, "Failed to get task");
            return StatusCode(ex.StatusCode ?? 500, new ErrorResponse
            {
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Health()
    {
        try
        {
            var health = await _loopai.GetHealthAsync();
            return Ok(new
            {
                status = health.Status,
                version = health.Version,
                timestamp = health.Timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { status = "unhealthy", error = ex.Message });
        }
    }
}

/// <summary>
/// Request to classify text.
/// </summary>
public record ClassifyRequest
{
    /// <summary>
    /// Task ID to use for classification.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Text to classify.
    /// </summary>
    public required string Text { get; init; }
}

/// <summary>
/// Classification result.
/// </summary>
public record ClassifyResponse
{
    /// <summary>
    /// Classification result.
    /// </summary>
    public required string Classification { get; init; }

    /// <summary>
    /// Execution latency in milliseconds.
    /// </summary>
    public required double LatencyMs { get; init; }
}

/// <summary>
/// Request to create a task.
/// </summary>
public record CreateTaskRequest
{
    /// <summary>
    /// Task name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Task description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// JSON schema for input validation (as JSON string).
    /// </summary>
    public required string InputSchema { get; init; }

    /// <summary>
    /// JSON schema for output validation (as JSON string).
    /// </summary>
    public required string OutputSchema { get; init; }

    /// <summary>
    /// Target accuracy (0.0 to 1.0).
    /// </summary>
    public double AccuracyTarget { get; init; } = 0.9;

    /// <summary>
    /// Target latency in milliseconds.
    /// </summary>
    public int LatencyTargetMs { get; init; } = 10;
}

/// <summary>
/// Error response.
/// </summary>
public record ErrorResponse
{
    /// <summary>
    /// Error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Validation errors if available.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// Execution ID if available.
    /// </summary>
    public Guid? ExecutionId { get; init; }
}
