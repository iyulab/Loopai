using Loopai.Core.Models;

namespace Loopai.Core.Interfaces;

/// <summary>
/// Repository interface for execution record persistence.
/// </summary>
public interface IExecutionRecordRepository
{
    /// <summary>
    /// Get an execution record by ID.
    /// </summary>
    Task<ExecutionRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all execution records for a task.
    /// </summary>
    Task<IEnumerable<ExecutionRecord>> GetByTaskIdAsync(
        Guid taskId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all execution records for a program.
    /// </summary>
    Task<IEnumerable<ExecutionRecord>> GetByProgramIdAsync(
        Guid programId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get sampled execution records for validation.
    /// </summary>
    Task<IEnumerable<ExecutionRecord>> GetSampledRecordsAsync(
        Guid taskId,
        DateTime? since = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new execution record.
    /// </summary>
    Task<ExecutionRecord> CreateAsync(ExecutionRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create multiple execution records in batch.
    /// </summary>
    Task<IEnumerable<ExecutionRecord>> CreateBatchAsync(
        IEnumerable<ExecutionRecord> records,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get execution statistics for a task.
    /// </summary>
    Task<ExecutionStatistics> GetStatisticsAsync(
        Guid taskId,
        DateTime? since = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Execution statistics aggregation.
/// </summary>
public record ExecutionStatistics
{
    public required int TotalExecutions { get; init; }
    public required int SuccessCount { get; init; }
    public required int ErrorCount { get; init; }
    public required int TimeoutCount { get; init; }
    public required double AverageLatencyMs { get; init; }
    public required double P95LatencyMs { get; init; }
    public required double P99LatencyMs { get; init; }
    public required int SampledCount { get; init; }
}
