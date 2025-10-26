using System.Text.Json;
using System.Text.Json.Serialization;

namespace Loopai.CloudApi.DTOs;

/// <summary>
/// Response containing task specification details.
/// </summary>
public record TaskResponse
{
    /// <summary>
    /// Unique task identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Task name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Task description.
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
    /// Example input-output pairs.
    /// </summary>
    [JsonPropertyName("examples")]
    public required IReadOnlyList<JsonDocument> Examples { get; init; }

    /// <summary>
    /// Target accuracy (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("accuracy_target")]
    public required double AccuracyTarget { get; init; }

    /// <summary>
    /// Target latency in milliseconds.
    /// </summary>
    [JsonPropertyName("latency_target_ms")]
    public required int LatencyTargetMs { get; init; }

    /// <summary>
    /// Sampling rate for validation (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("sampling_rate")]
    public required double SamplingRate { get; init; }

    /// <summary>
    /// Active program version (null if no active version).
    /// </summary>
    [JsonPropertyName("active_version")]
    public int? ActiveVersion { get; init; }

    /// <summary>
    /// Total number of program versions for this task.
    /// </summary>
    [JsonPropertyName("total_versions")]
    public required int TotalVersions { get; init; }

    /// <summary>
    /// Timestamp when task was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Timestamp when task was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public required DateTime UpdatedAt { get; init; }
}
