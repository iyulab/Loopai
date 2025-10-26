using System.Text.Json;
using System.Text.Json.Serialization;

namespace Loopai.Core.Models;

/// <summary>
/// Program execution record model.
/// </summary>
public record ExecutionRecord
{
    /// <summary>
    /// Unique execution identifier.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Reference to program artifact.
    /// </summary>
    [JsonPropertyName("program_id")]
    public required Guid ProgramId { get; init; }

    /// <summary>
    /// Reference to task specification.
    /// </summary>
    [JsonPropertyName("task_id")]
    public required Guid TaskId { get; init; }

    // Input/Output
    /// <summary>
    /// Input data provided to the program.
    /// </summary>
    [JsonPropertyName("input")]
    public required JsonDocument InputData { get; init; }

    /// <summary>
    /// Output data produced by the program.
    /// </summary>
    [JsonPropertyName("output")]
    public JsonDocument? OutputData { get; init; }

    // Execution details
    /// <summary>
    /// Execution latency in milliseconds.
    /// </summary>
    [JsonPropertyName("latency_ms")]
    public double? LatencyMs { get; init; }

    /// <summary>
    /// Memory usage in megabytes.
    /// </summary>
    [JsonPropertyName("memory_usage_mb")]
    public double? MemoryUsageMb { get; init; }

    /// <summary>
    /// Execution status.
    /// </summary>
    public ExecutionStatus Status { get; init; } = ExecutionStatus.Success;

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; init; }

    // Sampling
    /// <summary>
    /// Whether this execution was sampled for validation.
    /// </summary>
    [JsonPropertyName("sampled_for_validation")]
    public bool SampledForValidation { get; init; } = false;

    /// <summary>
    /// Reference to validation record (if sampled).
    /// </summary>
    [JsonPropertyName("validation_id")]
    public Guid? ValidationId { get; init; }

    /// <summary>
    /// Timestamp when execution occurred.
    /// </summary>
    [JsonPropertyName("executed_at")]
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
}
