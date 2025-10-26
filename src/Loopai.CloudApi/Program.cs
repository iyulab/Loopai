using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using FluentValidation;
using Loopai.CloudApi.Configuration;
using Loopai.CloudApi.Data;
using Loopai.CloudApi.Services;
using Loopai.CloudApi.Validators;
using Loopai.CloudApi.Middleware;
using Loopai.CloudApi.Swagger;
using Loopai.Core.Interfaces;
using Loopai.Core.Services;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load rate limiting configuration
builder.Configuration.AddJsonFile("appsettings.RateLimiting.json", optional: true, reloadOnChange: true);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add configuration
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("CacheSettings"));
builder.Services.Configure<GeneratorServiceSettings>(builder.Configuration.GetSection("GeneratorService"));
builder.Services.Configure<EdgeRuntimeSettings>(builder.Configuration.GetSection("EdgeRuntime"));

// Add Entity Framework Core
builder.Services.AddDbContext<LoopaiDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });

    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add Redis distributed cache
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "Loopai:";
    });
}
else
{
    // Fallback to in-memory cache if Redis is not configured
    builder.Services.AddDistributedMemoryCache();
}

// Add controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Use snake_case for JSON property names to match Python API
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;

        // Convert enums to strings
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        // Ignore null values
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        // Pretty print in development
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    });

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<ExecuteRequestValidator>();

// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Add OpenTelemetry tracing
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "Loopai.CloudApi",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = httpContext =>
            {
                // Don't trace health checks and swagger UI
                var path = httpContext.Request.Path.Value ?? string.Empty;
                return !path.Contains("/health") && !path.Contains("/swagger");
            };
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddSqlClientInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddConsoleExporter()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317");
        }));

// Add metrics service
builder.Services.AddSingleton<MetricsService>();

// Add cache service
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// Add repositories (Entity Framework Core implementations)
builder.Services.AddScoped<Loopai.Core.Interfaces.ITaskRepository, Loopai.CloudApi.Repositories.EfTaskRepository>();
builder.Services.AddScoped<Loopai.Core.Interfaces.IProgramArtifactRepository, Loopai.CloudApi.Repositories.EfProgramArtifactRepository>();
builder.Services.AddScoped<Loopai.Core.Interfaces.IExecutionRecordRepository, Loopai.CloudApi.Repositories.EfExecutionRecordRepository>();
builder.Services.AddScoped<Loopai.Core.Interfaces.IValidationResultRepository, Loopai.CloudApi.Repositories.EfValidationResultRepository>();
builder.Services.AddScoped<Loopai.Core.Interfaces.ICanaryDeploymentRepository, Loopai.CloudApi.Repositories.EfCanaryDeploymentRepository>();

// Add HTTP client for program generator service
builder.Services.AddHttpClient<IProgramGeneratorService, HttpProgramGeneratorService>();

// Add edge runtime service
builder.Services.AddSingleton<IEdgeRuntimeService, DenoEdgeRuntimeService>();

// Add output validator
builder.Services.AddScoped<IOutputValidator, SchemaOutputValidator>();

// Add program improvement service
builder.Services.AddScoped<IProgramImprovementService, ProgramImprovementService>();

// Add validation service
builder.Services.AddScoped<ValidationService>();

// Add A/B testing service
builder.Services.AddScoped<IABTestingService, ABTestingService>();

// Add sampling strategy service
builder.Services.AddScoped<ISamplingStrategyService, SamplingStrategyService>();

// Add webhook service
builder.Services.AddSingleton<IWebhookService, WebhookService>();

// Add core services (without caching)
builder.Services.AddScoped<Loopai.Core.Services.TaskService>();

// Add program execution service with generator and runtime dependencies
builder.Services.AddScoped<Loopai.Core.Services.ProgramExecutionService>(provider =>
{
    var taskRepo = provider.GetRequiredService<Loopai.Core.Interfaces.ITaskRepository>();
    var artifactRepo = provider.GetRequiredService<Loopai.Core.Interfaces.IProgramArtifactRepository>();
    var executionRepo = provider.GetRequiredService<Loopai.Core.Interfaces.IExecutionRecordRepository>();
    var logger = provider.GetRequiredService<ILogger<Loopai.Core.Services.ProgramExecutionService>>();
    var generatorService = provider.GetService<IProgramGeneratorService>();
    var runtimeService = provider.GetService<IEdgeRuntimeService>();

    return new Loopai.Core.Services.ProgramExecutionService(
        taskRepo, artifactRepo, executionRepo, logger, generatorService, runtimeService);
});

// Add cached service decorators
builder.Services.AddScoped<Loopai.Core.Interfaces.ITaskService>(provider =>
{
    var innerService = provider.GetRequiredService<Loopai.Core.Services.TaskService>();
    var cache = provider.GetRequiredService<ICacheService>();
    var cacheSettings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CacheSettings>>();
    var logger = provider.GetRequiredService<ILogger<CachedTaskService>>();
    return new CachedTaskService(innerService, cache, cacheSettings, logger);
});

builder.Services.AddScoped<Loopai.Core.Interfaces.IProgramExecutionService>(provider =>
{
    // For now, don't cache execution service - will add in next iteration
    return provider.GetRequiredService<Loopai.Core.Services.ProgramExecutionService>();
});

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
        new Asp.Versioning.UrlSegmentApiVersionReader(),
        new Asp.Versioning.HeaderApiVersionReader("X-Api-Version"),
        new Asp.Versioning.QueryStringApiVersionReader("api-version"));
}).AddMvc().AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Swagger docs for v1
    options.SwaggerDoc("v1", new()
    {
        Title = "Loopai Cloud API",
        Version = "v1.0",
        Description = @"**REST API for Loopai program synthesis, execution, validation, and improvement**

## Features
- 🎯 Task management and program synthesis
- ⚡ Edge runtime execution (Deno)
- ✅ Automatic output validation
- 🔄 Program improvement pipeline
- 📊 A/B testing and canary deployment
- 🎲 Advanced sampling strategies

## Authentication
Currently no authentication required (development mode).

## Rate Limits
No rate limits in development mode.

## Support
For issues and questions, contact the Loopai team.",
        Contact = new()
        {
            Name = "Loopai Team",
            Email = "support@loopai.dev"
        },
        License = new()
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Enable annotations for better Swagger UI
    options.EnableAnnotations();

    // Include XML comments for API documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add example schemas
    options.SchemaFilter<ExampleSchemaFilter>();

    // Custom operation filters for better documentation
    options.OperationFilter<SwaggerDefaultValues>();
});

// Add health checks
builder.Services.AddHealthChecks();

// Add CORS (configure based on environment)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // Configure production CORS settings
            policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Global exception handler must be first
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Loopai Cloud API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

// Expose Prometheus metrics endpoint
app.UseMetricServer("/metrics");
app.UseHttpMetrics();

app.UseIpRateLimiting();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

try
{
    Log.Information("Starting Loopai Cloud API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
