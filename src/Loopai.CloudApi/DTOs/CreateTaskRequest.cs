using System.Text.Json;
using System.Text.Json.Serialization;

namespace Loopai.CloudApi.DTOs;

/// <summary>
/// Request to create a new task specification.
/// </summary>
public record CreateTaskRequest
{
    /// <summary>
    /// Task name (unique identifier).
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Natural language task description.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>
    /// JSON schema for input validation.
    /// </summary>
    [JsonPropertyName("input_schema")]
    public required JsonDocument InputSchema { get; init; }

    /// <summary>
    /// JSON schema for output validation.
    /// </summary>
    [JsonPropertyName("output_schema")]
    public required JsonDocument OutputSchema { get; init; }

    /// <summary>
    /// Example input-output pairs for program generation.
    /// </summary>
    [JsonPropertyName("examples")]
    public IReadOnlyList<JsonDocument> Examples { get; init; } = Array.Empty<JsonDocument>();

    /// <summary>
    /// Target accuracy (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("accuracy_target")]
    public double AccuracyTarget { get; init; } = 0.9;

    /// <summary>
    /// Target latency in milliseconds.
    /// </summary>
    [JsonPropertyName("latency_target_ms")]
    public int LatencyTargetMs { get; init; } = 10;

    /// <summary>
    /// Sampling rate for validation (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("sampling_rate")]
    public double SamplingRate { get; init; } = 0.1;
}
