using Microsoft.Extensions.Logging;

namespace Loopai.Client;

/// <summary>
/// Configuration options for Loopai client.
/// </summary>
public class LoopaiClientOptions
{
    /// <summary>
    /// Base URL of the Loopai API.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8080";

    /// <summary>
    /// API key for authentication (optional for local development).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// HTTP request timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of retry attempts for transient failures.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Enable telemetry and logging.
    /// </summary>
    public bool EnableTelemetry { get; set; } = true;

    /// <summary>
    /// Logger instance for client operations.
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Delay between retries (exponential backoff base).
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Enable detailed logging of HTTP requests/responses.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new ArgumentException("BaseUrl cannot be null or empty.", nameof(BaseUrl));

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            throw new ArgumentException("BaseUrl must be a valid absolute URI.", nameof(BaseUrl));

        if (Timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be greater than zero.", nameof(Timeout));

        if (MaxRetries < 0 || MaxRetries > 10)
            throw new ArgumentOutOfRangeException(nameof(MaxRetries), "MaxRetries must be between 0 and 10.");

        if (RetryDelay <= TimeSpan.Zero)
            throw new ArgumentException("RetryDelay must be greater than zero.", nameof(RetryDelay));
    }
}
