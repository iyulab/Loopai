namespace Loopai.Core.Plugins;

/// <summary>
/// Base interface for all Loopai plugins.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Unique plugin identifier.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Human-readable plugin description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Plugin version (semantic versioning recommended).
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Plugin author or organization.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Indicates whether the plugin is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Plugin priority for execution order (higher = earlier).
    /// </summary>
    int Priority { get; set; }
}
