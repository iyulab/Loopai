using Loopai.Core.Models;
using System.Text.Json;

namespace Loopai.Core.Interfaces;

/// <summary>
/// Business logic service for program execution.
/// </summary>
public interface IProgramExecutionService
{
    /// <summary>
    /// Execute a program for a given task.
    /// </summary>
    Task<ExecutionResult> ExecuteAsync(
        Guid taskId,
        JsonDocument input,
        int? version = null,
        bool forceValidation = false,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all program artifacts for a task.
    /// </summary>
    Task<IEnumerable<ProgramArtifact>> GetArtifactsAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific version of a program artifact.
    /// </summary>
    Task<ProgramArtifact?> GetArtifactVersionAsync(
        Guid taskId,
        int version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get execution history for a task.
    /// </summary>
    Task<IEnumerable<ExecutionRecord>> GetExecutionHistoryAsync(
        Guid taskId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get execution statistics for a task.
    /// </summary>
    Task<ExecutionStatistics> GetExecutionStatisticsAsync(
        Guid taskId,
        DateTime? since = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a program execution.
/// </summary>
public record ExecutionResult
{
    public required Guid ExecutionId { get; init; }
    public required Guid TaskId { get; init; }
    public required Guid ProgramId { get; init; }
    public required int Version { get; init; }
    public required ExecutionStatus Status { get; init; }
    public JsonDocument? Output { get; init; }
    public string? ErrorMessage { get; init; }
    public required double LatencyMs { get; init; }
    public double? MemoryUsageMb { get; init; }
    public required bool SampledForValidation { get; init; }
    public required DateTime ExecutedAt { get; init; }
}
