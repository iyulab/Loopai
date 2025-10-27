using System.Text.Json;

namespace Loopai.Core.Plugins;

/// <summary>
/// Context provided to validator plugins during validation.
/// </summary>
public class ValidationContext
{
    /// <summary>
    /// Task identifier being validated.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Input schema for the task.
    /// </summary>
    public required JsonDocument InputSchema { get; init; }

    /// <summary>
    /// Output schema for the task.
    /// </summary>
    public required JsonDocument OutputSchema { get; init; }

    /// <summary>
    /// Expected output for validation (if available).
    /// </summary>
    public JsonDocument? ExpectedOutput { get; init; }

    /// <summary>
    /// Custom configuration for the validator plugin.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Configuration { get; init; }

    /// <summary>
    /// Additional metadata for validation.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
