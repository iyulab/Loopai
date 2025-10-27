using System.Collections.Concurrent;

namespace Loopai.Core.Plugins;

/// <summary>
/// Thread-safe registry for managing plugins.
/// </summary>
public class PluginRegistry : IPluginRegistry
{
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, IPlugin>> _plugins = new();

    /// <inheritdoc/>
    public void Register<T>(T plugin) where T : class, IPlugin
    {
        if (plugin == null)
            throw new ArgumentNullException(nameof(plugin));

        var type = typeof(T);
        var plugins = _plugins.GetOrAdd(type, _ => new ConcurrentDictionary<string, IPlugin>());

        if (!plugins.TryAdd(plugin.Name, plugin))
        {
            throw new InvalidOperationException(
                $"Plugin '{plugin.Name}' of type '{type.Name}' is already registered.");
        }
    }

    /// <inheritdoc/>
    public bool Unregister<T>(string name) where T : class, IPlugin
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Plugin name cannot be null or empty.", nameof(name));

        var type = typeof(T);
        if (!_plugins.TryGetValue(type, out var plugins))
            return false;

        return plugins.TryRemove(name, out _);
    }

    /// <inheritdoc/>
    public T? Resolve<T>(string name) where T : class, IPlugin
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Plugin name cannot be null or empty.", nameof(name));

        var type = typeof(T);
        if (!_plugins.TryGetValue(type, out var plugins))
            return null;

        return plugins.TryGetValue(name, out var plugin) ? plugin as T : null;
    }

    /// <inheritdoc/>
    public IEnumerable<T> List<T>(bool enabledOnly = true) where T : class, IPlugin
    {
        var type = typeof(T);
        if (!_plugins.TryGetValue(type, out var plugins))
            return Enumerable.Empty<T>();

        var results = plugins.Values.OfType<T>();

        if (enabledOnly)
            results = results.Where(p => p.IsEnabled);

        return results.OrderByDescending(p => p.Priority)
                     .ThenBy(p => p.Name);
    }

    /// <inheritdoc/>
    public int Count<T>(bool enabledOnly = true) where T : class, IPlugin
    {
        return List<T>(enabledOnly).Count();
    }

    /// <inheritdoc/>
    public bool Contains<T>(string name) where T : class, IPlugin
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Plugin name cannot be null or empty.", nameof(name));

        var type = typeof(T);
        if (!_plugins.TryGetValue(type, out var plugins))
            return false;

        return plugins.ContainsKey(name);
    }

    /// <inheritdoc/>
    public void Clear<T>() where T : class, IPlugin
    {
        var type = typeof(T);
        _plugins.TryRemove(type, out _);
    }

    /// <inheritdoc/>
    public void ClearAll()
    {
        _plugins.Clear();
    }
}
