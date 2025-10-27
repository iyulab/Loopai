using System.Text.Json;
using Loopai.Client.Models;

namespace Loopai.Client;

/// <summary>
/// Interface for Loopai client operations.
/// </summary>
public interface ILoopaiClient : IDisposable
{
    /// <summary>
    /// Creates a new task specification.
    /// </summary>
    /// <param name="name">Task name (unique identifier).</param>
    /// <param name="description">Natural language task description.</param>
    /// <param name="inputSchema">JSON schema for input validation.</param>
    /// <param name="outputSchema">JSON schema for output validation.</param>
    /// <param name="examples">Example input-output pairs.</param>
    /// <param name="accuracyTarget">Target accuracy (0.0 to 1.0).</param>
    /// <param name="latencyTargetMs">Target latency in milliseconds.</param>
    /// <param name="samplingRate">Sampling rate for validation (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created task response.</returns>
    Task<JsonDocument> CreateTaskAsync(
        string name,
        string description,
        JsonDocument inputSchema,
        JsonDocument outputSchema,
        IEnumerable<JsonDocument>? examples = null,
        double accuracyTarget = 0.9,
        int latencyTargetMs = 10,
        double samplingRate = 0.1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets task details by ID.
    /// </summary>
    /// <param name="taskId">Task identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task response.</returns>
    Task<JsonDocument> GetTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a program for the specified task.
    /// </summary>
    /// <param name="taskId">Task identifier.</param>
    /// <param name="input">Input data for program execution.</param>
    /// <param name="version">Optional program version (null = active version).</param>
    /// <param name="timeoutMs">Execution timeout in milliseconds.</param>
    /// <param name="forceValidation">Force sampling for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution response.</returns>
    Task<JsonDocument> ExecuteAsync(
        Guid taskId,
        JsonDocument input,
        int? version = null,
        int? timeoutMs = null,
        bool forceValidation = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a program with simplified input.
    /// </summary>
    /// <param name="taskId">Task identifier.</param>
    /// <param name="input">Input object to serialize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution response.</returns>
    Task<JsonDocument> ExecuteAsync(
        Guid taskId,
        object input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes multiple inputs in batch.
    /// </summary>
    /// <param name="request">Batch execution request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Batch execution response.</returns>
    Task<BatchExecuteResponse> BatchExecuteAsync(
        BatchExecuteRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes multiple inputs in batch with simplified parameters.
    /// </summary>
    /// <param name="taskId">Task identifier.</param>
    /// <param name="inputs">Input objects to execute.</param>
    /// <param name="maxConcurrency">Maximum concurrent executions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Batch execution response.</returns>
    Task<BatchExecuteResponse> BatchExecuteAsync(
        Guid taskId,
        IEnumerable<object> inputs,
        int maxConcurrency = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets health status of the Loopai API.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health response.</returns>
    Task<HealthResponse> GetHealthAsync(
        CancellationToken cancellationToken = default);
}
