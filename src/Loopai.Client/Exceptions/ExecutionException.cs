namespace Loopai.Client.Exceptions;

/// <summary>
/// Exception thrown when program execution fails.
/// </summary>
public class ExecutionException : LoopaiException
{
    /// <summary>
    /// Execution ID if available.
    /// </summary>
    public Guid? ExecutionId { get; }

    /// <summary>
    /// Creates a new execution exception.
    /// </summary>
    public ExecutionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new execution exception with execution ID.
    /// </summary>
    public ExecutionException(string message, Guid executionId) : base(message)
    {
        ExecutionId = executionId;
    }

    /// <summary>
    /// Creates a new execution exception with status code.
    /// </summary>
    public ExecutionException(string message, int statusCode) : base(message, statusCode)
    {
    }

    /// <summary>
    /// Creates a new execution exception with inner exception.
    /// </summary>
    public ExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
