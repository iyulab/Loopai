using System.Text.Json;
using System.Text.Json.Serialization;

namespace Loopai.CloudApi.DTOs;

/// <summary>
/// Request to validate multiple executions in batch.
/// </summary>
public record BatchValidateRequest
{
    /// <summary>
    /// Task ID to validate against.
    /// </summary>
    [JsonPropertyName("task_id")]
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Batch of items to validate.
    /// </summary>
    [JsonPropertyName("items")]
    public required IEnumerable<BatchValidateItem> Items { get; init; }
}

/// <summary>
/// Individual item in a batch validation request.
/// </summary>
public record BatchValidateItem
{
    /// <summary>
    /// Client-provided correlation ID for this item.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Input data that was executed.
    /// </summary>
    [JsonPropertyName("input")]
    public required JsonDocument Input { get; init; }

    /// <summary>
    /// Actual output from program execution.
    /// </summary>
    [JsonPropertyName("output")]
    public required JsonDocument Output { get; init; }

    /// <summary>
    /// Expected output for validation.
    /// </summary>
    [JsonPropertyName("expected_output")]
    public required JsonDocument ExpectedOutput { get; init; }
}

/// <summary>
/// Response from batch validation.
/// </summary>
public record BatchValidateResponse
{
    /// <summary>
    /// Unique batch validation identifier.
    /// </summary>
    [JsonPropertyName("batch_id")]
    public required Guid BatchId { get; init; }

    /// <summary>
    /// Task ID that was validated.
    /// </summary>
    [JsonPropertyName("task_id")]
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Total number of items validated.
    /// </summary>
    [JsonPropertyName("total_items")]
    public required int TotalItems { get; init; }

    /// <summary>
    /// Number of valid items.
    /// </summary>
    [JsonPropertyName("valid_count")]
    public required int ValidCount { get; init; }

    /// <summary>
    /// Number of invalid items.
    /// </summary>
    [JsonPropertyName("invalid_count")]
    public required int InvalidCount { get; init; }

    /// <summary>
    /// Accuracy rate (valid / total).
    /// </summary>
    [JsonPropertyName("accuracy_rate")]
    public required double AccuracyRate { get; init; }

    /// <summary>
    /// Individual validation results.
    /// </summary>
    [JsonPropertyName("results")]
    public required IEnumerable<BatchValidateResult> Results { get; init; }

    /// <summary>
    /// Total duration for batch validation in milliseconds.
    /// </summary>
    [JsonPropertyName("total_duration_ms")]
    public required double TotalDurationMs { get; init; }
}

/// <summary>
/// Result for a single item in batch validation.
/// </summary>
public record BatchValidateResult
{
    /// <summary>
    /// Client-provided correlation ID (matches request item ID).
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Whether validation passed.
    /// </summary>
    [JsonPropertyName("is_valid")]
    public required bool IsValid { get; init; }

    /// <summary>
    /// Validation message.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Confidence score (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence_score")]
    public double? ConfidenceScore { get; init; }
}
