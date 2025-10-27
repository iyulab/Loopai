namespace Loopai.Core.Plugins;

/// <summary>
/// Context provided to webhook handler plugins during event handling.
/// </summary>
public class WebhookHandlerContext
{
    /// <summary>
    /// Custom configuration for the webhook handler plugin.
    /// </summary>
    public required IReadOnlyDictionary<string, object> Configuration { get; init; }

    /// <summary>
    /// Retry attempt number (0 for first attempt).
    /// </summary>
    public int RetryAttempt { get; init; }

    /// <summary>
    /// Maximum number of retry attempts allowed.
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Indicates whether this is a test/dry-run execution.
    /// </summary>
    public bool IsDryRun { get; init; }

    /// <summary>
    /// Additional metadata for the webhook handler.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
