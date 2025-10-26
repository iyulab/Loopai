namespace Loopai.CloudApi.Configuration;

/// <summary>
/// Configuration settings for the Python Generator Service.
/// </summary>
public class GeneratorServiceSettings
{
    /// <summary>
    /// Base URL of the Python Generator Service.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8000";

    /// <summary>
    /// Timeout for program generation requests in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for failed requests.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Whether to enable detailed logging of generator requests/responses.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}
