namespace Loopai.Client.Exceptions;

/// <summary>
/// Base exception for all Loopai client errors.
/// </summary>
public class LoopaiException : Exception
{
    /// <summary>
    /// HTTP status code if available.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Creates a new Loopai exception.
    /// </summary>
    public LoopaiException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new Loopai exception with HTTP status code.
    /// </summary>
    public LoopaiException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Creates a new Loopai exception with inner exception.
    /// </summary>
    public LoopaiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a new Loopai exception with status code and inner exception.
    /// </summary>
    public LoopaiException(string message, int statusCode, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
