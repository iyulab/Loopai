namespace Loopai.CloudApi.Configuration;

/// <summary>
/// Cache configuration settings.
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Time-to-live for task metadata cache in minutes.
    /// </summary>
    public int TaskMetadataTtlMinutes { get; set; } = 60;

    /// <summary>
    /// Time-to-live for active artifact cache in minutes.
    /// </summary>
    public int ActiveArtifactTtlMinutes { get; set; } = 10;

    /// <summary>
    /// Time-to-live for execution statistics cache in minutes.
    /// </summary>
    public int ExecutionStatsTtlMinutes { get; set; } = 5;

    /// <summary>
    /// Whether caching is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
