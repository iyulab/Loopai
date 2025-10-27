using System.Text.Json;
using System.Text.Json.Serialization;

namespace Loopai.CloudApi.DTOs;

/// <summary>
/// Request to execute multiple inputs in batch.
/// </summary>
public record BatchExecuteRequest
{
    /// <summary>
    /// Task ID to execute.
    /// </summary>
    [JsonPropertyName("task_id")]
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Optional program version to execute. If null, uses active version.
    /// </summary>
    [JsonPropertyName("version")]
    public int? Version { get; init; }

    /// <summary>
    /// Batch of items to execute.
    /// </summary>
    [JsonPropertyName("items")]
    public required IEnumerable<BatchExecuteItem> Items { get; init; }

    /// <summary>
    /// Maximum concurrent executions (default: 10).
    /// </summary>
    [JsonPropertyName("max_concurrency")]
    public int? MaxConcurrency { get; init; }

    /// <summary>
    /// Whether to stop on first error (default: false).
    /// </summary>
    [JsonPropertyName("stop_on_first_error")]
    public bool StopOnFirstError { get; init; } = false;

    /// <summary>
    /// Execution timeout in milliseconds for each item.
    /// </summary>
    [JsonPropertyName("timeout_ms")]
    public int? TimeoutMs { get; init; }
}

/// <summary>
/// Individual item in a batch execution request.
/// </summary>
public record BatchExecuteItem
{
    /// <summary>
    /// Client-provided correlation ID for this item.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Input data for program execution.
    /// </summary>
    [JsonPropertyName("input")]
    public required JsonDocument Input { get; init; }

    /// <summary>
    /// Whether to force sampling for validation (overrides sampling rate).
    /// </summary>
    [JsonPropertyName("force_validation")]
    public bool ForceValidation { get; init; } = false;
}

/// <summary>
/// Response from batch execution.
/// </summary>
public record BatchExecuteResponse
{
    /// <summary>
    /// Unique batch execution identifier.
    /// </summary>
    [JsonPropertyName("batch_id")]
    public required Guid BatchId { get; init; }

    /// <summary>
    /// Task ID that was executed.
    /// </summary>
    [JsonPropertyName("task_id")]
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Program version that was executed.
    /// </summary>
    [JsonPropertyName("version")]
    public required int Version { get; init; }

    /// <summary>
    /// Total number of items in the batch.
    /// </summary>
    [JsonPropertyName("total_items")]
    public required int TotalItems { get; init; }

    /// <summary>
    /// Number of successful executions.
    /// </summary>
    [JsonPropertyName("success_count")]
    public required int SuccessCount { get; init; }

    /// <summary>
    /// Number of failed executions.
    /// </summary>
    [JsonPropertyName("failure_count")]
    public required int FailureCount { get; init; }

    /// <summary>
    /// Total duration for batch execution in milliseconds.
    /// </summary>
    [JsonPropertyName("total_duration_ms")]
    public required double TotalDurationMs { get; init; }

    /// <summary>
    /// Average latency per item in milliseconds.
    /// </summary>
    [JsonPropertyName("avg_latency_ms")]
    public required double AvgLatencyMs { get; init; }

    /// <summary>
    /// Individual execution results.
    /// </summary>
    [JsonPropertyName("results")]
    public required IEnumerable<BatchExecuteResult> Results { get; init; }

    /// <summary>
    /// Timestamp when batch execution started.
    /// </summary>
    [JsonPropertyName("started_at")]
    public required DateTime StartedAt { get; init; }

    /// <summary>
    /// Timestamp when batch execution completed.
    /// </summary>
    [JsonPropertyName("completed_at")]
    public required DateTime CompletedAt { get; init; }
}

/// <summary>
/// Result for a single item in batch execution.
/// </summary>
public record BatchExecuteResult
{
    /// <summary>
    /// Client-provided correlation ID (matches request item ID).
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Execution ID from the database.
    /// </summary>
    [JsonPropertyName("execution_id")]
    public Guid? ExecutionId { get; init; }

    /// <summary>
    /// Whether execution succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// Output data from program execution (null if error).
    /// </summary>
    [JsonPropertyName("output")]
    public JsonDocument? Output { get; init; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Execution latency in milliseconds.
    /// </summary>
    [JsonPropertyName("latency_ms")]
    public required double LatencyMs { get; init; }

    /// <summary>
    /// Whether this execution was sampled for validation.
    /// </summary>
    [JsonPropertyName("sampled_for_validation")]
    public bool SampledForValidation { get; init; }
}
