using System.Text.Json.Serialization;

namespace Loopai.Core.Models;

/// <summary>
/// Result of program output validation.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Unique validation ID.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Execution record being validated.
    /// </summary>
    [JsonPropertyName("execution_id")]
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Task ID.
    /// </summary>
    [JsonPropertyName("task_id")]
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Program artifact ID.
    /// </summary>
    [JsonPropertyName("program_id")]
    public required Guid ProgramId { get; init; }

    /// <summary>
    /// Whether the output is valid.
    /// </summary>
    [JsonPropertyName("is_valid")]
    public required bool IsValid { get; init; }

    /// <summary>
    /// Validation score (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("validation_score")]
    public double ValidationScore { get; init; }

    /// <summary>
    /// Validation errors if any.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();

    /// <summary>
    /// Validation method used.
    /// </summary>
    [JsonPropertyName("validation_method")]
    public string ValidationMethod { get; init; } = "schema";

    /// <summary>
    /// Timestamp when validation was performed.
    /// </summary>
    [JsonPropertyName("validated_at")]
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Validation error detail.
/// </summary>
public record ValidationError
{
    /// <summary>
    /// Error type (e.g., "schema", "type", "constraint").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// JSON path to the error location.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Expected value or format.
    /// </summary>
    public string? Expected { get; init; }

    /// <summary>
    /// Actual value received.
    /// </summary>
    public string? Actual { get; init; }
}

/// <summary>
/// Validation method types.
/// </summary>
public enum ValidationMethod
{
    /// <summary>
    /// JSON schema validation.
    /// </summary>
    Schema,

    /// <summary>
    /// LLM-based semantic validation.
    /// </summary>
    Semantic,

    /// <summary>
    /// Example-based validation.
    /// </summary>
    ExampleBased,

    /// <summary>
    /// Custom validation logic.
    /// </summary>
    Custom
}
