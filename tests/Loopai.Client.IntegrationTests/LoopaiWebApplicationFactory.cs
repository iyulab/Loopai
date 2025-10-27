using Loopai.CloudApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Loopai.Client.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration testing Loopai Cloud API.
/// </summary>
public class LoopaiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Configure test-specific services if needed
            // For example, use in-memory database instead of PostgreSQL
        });

        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Configure test host
        builder.UseEnvironment("Testing");
        return base.CreateHost(builder);
    }
}
