using System.Text.Json;
using System.Text.Json.Serialization;

namespace Loopai.Core.Models;

/// <summary>
/// Task specification model.
/// </summary>
public record TaskSpecification
{
    /// <summary>
    /// Unique task identifier.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Task name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Natural language task description.
    /// </summary>
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

    /// <summary>
    /// Timestamp when task was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when task was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
