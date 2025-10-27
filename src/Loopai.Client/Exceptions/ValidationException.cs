namespace Loopai.Client.Exceptions;

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : LoopaiException
{
    /// <summary>
    /// Validation errors if available.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    /// <summary>
    /// Creates a new validation exception.
    /// </summary>
    public ValidationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new validation exception with errors.
    /// </summary>
    public ValidationException(string message, IReadOnlyDictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }

    /// <summary>
    /// Creates a new validation exception with status code and errors.
    /// </summary>
    public ValidationException(string message, int statusCode, IReadOnlyDictionary<string, string[]> errors)
        : base(message, statusCode)
    {
        Errors = errors;
    }
}
