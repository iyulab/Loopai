using System.Text.Json;
using System.Text.Json.Serialization;

namespace Loopai.CloudApi.DTOs;

/// <summary>
/// Request to execute a program for a specific task.
/// </summary>
public record ExecuteRequest
{
    /// <summary>
    /// Task identifier to execute.
    /// </summary>
    [JsonPropertyName("task_id")]
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Optional program version to execute. If null, uses active version.
    /// </summary>
    [JsonPropertyName("version")]
    public int? Version { get; init; }

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

    /// <summary>
    /// Execution timeout in milliseconds. If null, uses default (5000ms).
    /// </summary>
    [JsonPropertyName("timeout_ms")]
    public int? TimeoutMs { get; init; }
}
