using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Loopai.Client;

/// <summary>
/// Extension methods for registering Loopai client in DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Loopai client to the service collection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddLoopaiClient(
        this IServiceCollection services,
        Action<LoopaiClientOptions> configure)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        services.Configure(configure);
        services.AddSingleton<ILoopaiClient, LoopaiClient>();

        return services;
    }

    /// <summary>
    /// Adds Loopai client to the service collection with configuration binding.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration section.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddLoopaiClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        services.Configure<LoopaiClientOptions>(configuration);
        services.AddSingleton<ILoopaiClient, LoopaiClient>();

        return services;
    }

    /// <summary>
    /// Adds Loopai client to the service collection with base URL and API key.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="baseUrl">Base URL of the Loopai API.</param>
    /// <param name="apiKey">API key for authentication (optional).</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddLoopaiClient(
        this IServiceCollection services,
        string baseUrl,
        string? apiKey = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));

        return services.AddLoopaiClient(options =>
        {
            options.BaseUrl = baseUrl;
            options.ApiKey = apiKey;
        });
    }
}
