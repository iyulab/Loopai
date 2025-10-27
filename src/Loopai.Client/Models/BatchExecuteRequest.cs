using System.Text.Json;

namespace Loopai.Client.Models;

/// <summary>
/// Request model for batch execution.
/// </summary>
public record BatchExecuteRequest
{
    /// <summary>
    /// Task identifier.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Batch items to execute.
    /// </summary>
    public required IEnumerable<BatchExecuteItem> Items { get; init; }

    /// <summary>
    /// Program version (null = active version).
    /// </summary>
    public int? Version { get; init; }

    /// <summary>
    /// Maximum concurrent executions.
    /// </summary>
    public int? MaxConcurrency { get; init; } = 10;

    /// <summary>
    /// Stop batch on first error.
    /// </summary>
    public bool StopOnFirstError { get; init; } = false;

    /// <summary>
    /// Timeout per item in milliseconds.
    /// </summary>
    public int? TimeoutMs { get; init; }
}

/// <summary>
/// Individual item in batch execution request.
/// </summary>
public record BatchExecuteItem
{
    /// <summary>
    /// Client-provided correlation ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Input data for execution.
    /// </summary>
    public required JsonDocument Input { get; init; }

    /// <summary>
    /// Force sampling for validation.
    /// </summary>
    public bool ForceValidation { get; init; } = false;
}
