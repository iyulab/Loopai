using Loopai.Core.CodeBeaker.Models;
using Loopai.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Loopai.Core.CodeBeaker;

/// <summary>
/// Extension methods for registering CodeBeaker services.
/// </summary>
public static class CodeBeakerServiceCollectionExtensions
{
    /// <summary>
    /// Add CodeBeaker runtime services to the service collection.
    /// </summary>
    public static IServiceCollection AddCodeBeakerRuntime(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options
        services.Configure<CodeBeakerOptions>(
            configuration.GetSection("CodeBeaker"));

        // Register client as singleton (maintains WebSocket connection)
        services.AddSingleton<ICodeBeakerClient, CodeBeakerClient>();

        // Register session pool as singleton
        services.AddSingleton<CodeBeakerSessionPool>();

        // Register runtime service as IEdgeRuntimeService
        services.AddSingleton<IEdgeRuntimeService, CodeBeakerRuntimeService>();

        // Register batch executor
        services.AddSingleton<CodeBeakerBatchExecutor>();

        return services;
    }

    /// <summary>
    /// Add CodeBeaker runtime services with custom options.
    /// </summary>
    public static IServiceCollection AddCodeBeakerRuntime(
        this IServiceCollection services,
        Action<CodeBeakerOptions> configureOptions)
    {
        // Configure options
        services.Configure(configureOptions);

        // Register client as singleton (maintains WebSocket connection)
        services.AddSingleton<ICodeBeakerClient, CodeBeakerClient>();

        // Register session pool as singleton
        services.AddSingleton<CodeBeakerSessionPool>();

        // Register runtime service as IEdgeRuntimeService
        services.AddSingleton<IEdgeRuntimeService, CodeBeakerRuntimeService>();

        return services;
    }
}
