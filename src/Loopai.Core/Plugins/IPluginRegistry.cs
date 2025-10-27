namespace Loopai.Core.Plugins;

/// <summary>
/// Registry for managing and discovering plugins.
/// </summary>
public interface IPluginRegistry
{
    /// <summary>
    /// Registers a plugin in the registry.
    /// </summary>
    /// <typeparam name="T">Plugin interface type.</typeparam>
    /// <param name="plugin">Plugin instance to register.</param>
    void Register<T>(T plugin) where T : class, IPlugin;

    /// <summary>
    /// Unregisters a plugin from the registry.
    /// </summary>
    /// <typeparam name="T">Plugin interface type.</typeparam>
    /// <param name="name">Plugin name to unregister.</param>
    /// <returns>True if plugin was unregistered, false if not found.</returns>
    bool Unregister<T>(string name) where T : class, IPlugin;

    /// <summary>
    /// Resolves a plugin by name and type.
    /// </summary>
    /// <typeparam name="T">Plugin interface type.</typeparam>
    /// <param name="name">Plugin name to resolve.</param>
    /// <returns>Plugin instance if found, null otherwise.</returns>
    T? Resolve<T>(string name) where T : class, IPlugin;

    /// <summary>
    /// Lists all registered plugins of a specific type.
    /// </summary>
    /// <typeparam name="T">Plugin interface type.</typeparam>
    /// <param name="enabledOnly">If true, only returns enabled plugins.</param>
    /// <returns>Collection of registered plugins.</returns>
    IEnumerable<T> List<T>(bool enabledOnly = true) where T : class, IPlugin;

    /// <summary>
    /// Gets the count of registered plugins of a specific type.
    /// </summary>
    /// <typeparam name="T">Plugin interface type.</typeparam>
    /// <param name="enabledOnly">If true, only counts enabled plugins.</param>
    /// <returns>Number of registered plugins.</returns>
    int Count<T>(bool enabledOnly = true) where T : class, IPlugin;

    /// <summary>
    /// Checks if a plugin is registered.
    /// </summary>
    /// <typeparam name="T">Plugin interface type.</typeparam>
    /// <param name="name">Plugin name to check.</param>
    /// <returns>True if plugin is registered, false otherwise.</returns>
    bool Contains<T>(string name) where T : class, IPlugin;

    /// <summary>
    /// Clears all registered plugins of a specific type.
    /// </summary>
    /// <typeparam name="T">Plugin interface type.</typeparam>
    void Clear<T>() where T : class, IPlugin;

    /// <summary>
    /// Clears all registered plugins.
    /// </summary>
    void ClearAll();
}
