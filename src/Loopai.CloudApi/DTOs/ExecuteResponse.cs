using System.Text.Json;
using System.Text.Json.Serialization;
using Loopai.Core.Models;

namespace Loopai.CloudApi.DTOs;

/// <summary>
/// Response from program execution.
/// </summary>
public record ExecuteResponse
{
    /// <summary>
    /// Unique execution identifier.
    /// </summary>
    [JsonPropertyName("execution_id")]
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Task identifier that was executed.
    /// </summary>
    [JsonPropertyName("task_id")]
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Program artifact identifier that was executed.
    /// </summary>
    [JsonPropertyName("program_id")]
    public required Guid ProgramId { get; init; }

    /// <summary>
    /// Program version that was executed.
    /// </summary>
    [JsonPropertyName("version")]
    public required int Version { get; init; }

    /// <summary>
    /// Execution status.
    /// </summary>
    [JsonPropertyName("status")]
    public required ExecutionStatus Status { get; init; }

    /// <summary>
    /// Output data from program execution (null if error/timeout).
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
    /// Memory usage in megabytes.
    /// </summary>
    [JsonPropertyName("memory_usage_mb")]
    public double? MemoryUsageMb { get; init; }

    /// <summary>
    /// Whether this execution was sampled for validation.
    /// </summary>
    [JsonPropertyName("sampled_for_validation")]
    public required bool SampledForValidation { get; init; }

    /// <summary>
    /// Timestamp when execution occurred.
    /// </summary>
    [JsonPropertyName("executed_at")]
    public required DateTime ExecutedAt { get; init; }
}
