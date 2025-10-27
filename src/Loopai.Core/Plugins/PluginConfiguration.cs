namespace Loopai.Core.Plugins;

/// <summary>
/// Configuration for a specific plugin instance.
/// </summary>
public class PluginConfiguration
{
    /// <summary>
    /// Plugin name (must match registered plugin).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Plugin type (validator, sampler, webhook-handler).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Indicates whether the plugin is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Plugin execution priority (higher = earlier execution).
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Custom configuration settings for the plugin.
    /// </summary>
    public Dictionary<string, object> Configuration { get; init; } = new();

    /// <summary>
    /// Additional metadata for the plugin.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Collection of plugin configurations.
/// </summary>
public class PluginsConfiguration
{
    /// <summary>
    /// Validator plugin configurations.
    /// </summary>
    public List<PluginConfiguration> Validators { get; init; } = new();

    /// <summary>
    /// Sampler plugin configurations.
    /// </summary>
    public List<PluginConfiguration> Samplers { get; init; } = new();

    /// <summary>
    /// Webhook handler plugin configurations.
    /// </summary>
    public List<PluginConfiguration> WebhookHandlers { get; init; } = new();
}
