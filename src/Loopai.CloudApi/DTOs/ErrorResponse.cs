using System.Text.Json.Serialization;

namespace Loopai.CloudApi.DTOs;

/// <summary>
/// Standardized error response format.
/// </summary>
public record ErrorResponse
{
    /// <summary>
    /// Error code (e.g., "TASK_NOT_FOUND", "VALIDATION_ERROR").
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Additional error details (e.g., validation errors, stack trace in dev).
    /// </summary>
    [JsonPropertyName("details")]
    public object? Details { get; init; }

    /// <summary>
    /// Unique trace identifier for error tracking.
    /// </summary>
    [JsonPropertyName("trace_id")]
    public string? TraceId { get; init; }

    /// <summary>
    /// Timestamp when error occurred.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
