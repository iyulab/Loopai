namespace Loopai.Core.Plugins;

/// <summary>
/// Result from a validator plugin execution.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates whether validation passed.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Name of the validator that produced this result.
    /// </summary>
    public required string ValidatorType { get; init; }

    /// <summary>
    /// Validation message (error message if invalid, success message if valid).
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Detailed validation errors (field-level errors if applicable).
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// Confidence score (0.0 to 1.0).
    /// </summary>
    public double? ConfidenceScore { get; init; }

    /// <summary>
    /// Time taken for validation in milliseconds.
    /// </summary>
    public double? DurationMs { get; init; }

    /// <summary>
    /// Additional metadata from the validator.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
