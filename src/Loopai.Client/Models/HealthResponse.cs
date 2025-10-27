using System.Text.Json.Serialization;

namespace Loopai.Client.Models;

/// <summary>
/// Response from health check endpoint.
/// </summary>
public record HealthResponse
{
    /// <summary>
    /// Overall health status.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// API version.
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>
    /// Current timestamp from server.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Individual component health checks.
    /// </summary>
    [JsonPropertyName("checks")]
    public IReadOnlyDictionary<string, string>? Checks { get; init; }
}
