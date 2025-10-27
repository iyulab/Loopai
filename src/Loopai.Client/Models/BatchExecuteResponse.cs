using System.Text.Json;

namespace Loopai.Client.Models;

/// <summary>
/// Response model for batch execution.
/// </summary>
public record BatchExecuteResponse
{
    /// <summary>
    /// Batch execution identifier.
    /// </summary>
    public required Guid BatchId { get; init; }

    /// <summary>
    /// Task identifier.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Program version used.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Total number of items.
    /// </summary>
    public required int TotalItems { get; init; }

    /// <summary>
    /// Number of successful executions.
    /// </summary>
    public required int SuccessCount { get; init; }

    /// <summary>
    /// Number of failed executions.
    /// </summary>
    public required int FailureCount { get; init; }

    /// <summary>
    /// Total batch duration in milliseconds.
    /// </summary>
    public required double TotalDurationMs { get; init; }

    /// <summary>
    /// Average latency per item in milliseconds.
    /// </summary>
    public required double AvgLatencyMs { get; init; }

    /// <summary>
    /// Individual batch execution results.
    /// </summary>
    public required IReadOnlyList<BatchExecuteResult> Results { get; init; }

    /// <summary>
    /// Batch start timestamp.
    /// </summary>
    public required DateTime StartedAt { get; init; }

    /// <summary>
    /// Batch completion timestamp.
    /// </summary>
    public required DateTime CompletedAt { get; init; }
}

/// <summary>
/// Individual batch execution result.
/// </summary>
public record BatchExecuteResult
{
    /// <summary>
    /// Correlation ID (matches request item ID).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Execution record identifier.
    /// </summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Execution success status.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Output data (null if failed).
    /// </summary>
    public JsonDocument? Output { get; init; }

    /// <summary>
    /// Error message (null if success).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Execution latency in milliseconds.
    /// </summary>
    public required double LatencyMs { get; init; }

    /// <summary>
    /// Whether this execution was sampled for validation.
    /// </summary>
    public required bool SampledForValidation { get; init; }
}
