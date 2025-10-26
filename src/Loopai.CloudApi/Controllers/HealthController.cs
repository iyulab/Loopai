using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Diagnostics;
using Loopai.CloudApi.Data;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Loopai.CloudApi.Controllers;

/// <summary>
/// Health check and system information endpoints.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Asp.Versioning.ApiVersion("1.0")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly LoopaiDbContext _dbContext;
    private readonly IConnectionMultiplexer? _redis;

    public HealthController(
        ILogger<HealthController> logger,
        LoopaiDbContext dbContext,
        IConnectionMultiplexer? redis = null)
    {
        _logger = logger;
        _dbContext = dbContext;
        _redis = redis;
    }

    /// <summary>
    /// Basic health check endpoint.
    /// </summary>
    /// <response code="200">Service is healthy</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new HealthResponse
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Version = GetVersion(),
            Environment = GetEnvironmentName()
        });
    }

    /// <summary>
    /// Detailed health check with dependencies.
    /// </summary>
    /// <response code="200">Service and dependencies are healthy</response>
    /// <response code="503">One or more dependencies are unhealthy</response>
    [HttpGet("detailed")]
    [ProducesResponseType(typeof(DetailedHealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DetailedHealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDetailedHealth()
    {
        var checks = new Dictionary<string, ComponentHealth>
        {
            ["api"] = new ComponentHealth
            {
                Status = "healthy",
                ResponseTime = 1
            },
            ["database"] = await CheckDatabaseAsync(),
            ["redis"] = CheckRedis()
        };

        var allHealthy = checks.All(c => c.Value.Status == "healthy");
        var response = new DetailedHealthResponse
        {
            Status = allHealthy ? "healthy" : "degraded",
            Timestamp = DateTime.UtcNow,
            Version = GetVersion(),
            Environment = GetEnvironmentName(),
            Components = checks
        };

        return allHealthy ? Ok(response) : StatusCode(503, response);
    }

    /// <summary>
    /// Readiness probe for Kubernetes.
    /// </summary>
    /// <response code="200">Service is ready to accept traffic</response>
    /// <response code="503">Service is not ready</response>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            // Check database connectivity
            var dbHealth = await CheckDatabaseAsync();
            var isDatabaseReady = dbHealth.Status == "healthy";

            // Check Redis connectivity (optional)
            var redisHealth = CheckRedis();
            var isRedisReady = redisHealth.Status == "healthy" || _redis == null;

            var isReady = isDatabaseReady && isRedisReady;

            if (isReady)
            {
                return Ok(new {
                    status = "ready",
                    database = isDatabaseReady,
                    redis = isRedisReady
                });
            }

            return StatusCode(503, new {
                status = "not_ready",
                database = isDatabaseReady,
                redis = isRedisReady
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new { status = "not_ready", error = ex.Message });
        }
    }

    /// <summary>
    /// Liveness probe for Kubernetes.
    /// </summary>
    /// <response code="200">Service is alive</response>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetLiveness()
    {
        return Ok(new { status = "alive" });
    }

    private static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "unknown";
    }

    private string GetEnvironmentName()
    {
        return HttpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .EnvironmentName;
    }

    private async Task<ComponentHealth> CheckDatabaseAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Test connectivity by checking if we can query the database
            await _dbContext.Database.CanConnectAsync();
            stopwatch.Stop();

            return new ComponentHealth
            {
                Status = "healthy",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                Message = "Database connection successful"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database health check failed");

            return new ComponentHealth
            {
                Status = "unhealthy",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                Message = $"Database connection failed: {ex.Message}"
            };
        }
    }

    private ComponentHealth CheckRedis()
    {
        if (_redis == null)
        {
            return new ComponentHealth
            {
                Status = "healthy",
                ResponseTime = 0,
                Message = "Redis not configured"
            };
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = _redis.GetDatabase();
            var pingResult = db.Ping();
            stopwatch.Stop();

            return new ComponentHealth
            {
                Status = "healthy",
                ResponseTime = (long)pingResult.TotalMilliseconds,
                Message = "Redis connection successful"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Redis health check failed");

            return new ComponentHealth
            {
                Status = "unhealthy",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                Message = $"Redis connection failed: {ex.Message}"
            };
        }
    }

    private record HealthResponse
    {
        public required string Status { get; init; }
        public required DateTime Timestamp { get; init; }
        public required string Version { get; init; }
        public required string Environment { get; init; }
    }

    private record DetailedHealthResponse
    {
        public required string Status { get; init; }
        public required DateTime Timestamp { get; init; }
        public required string Version { get; init; }
        public required string Environment { get; init; }
        public required Dictionary<string, ComponentHealth> Components { get; init; }
    }

    private record ComponentHealth
    {
        public required string Status { get; init; }
        public long? ResponseTime { get; init; }
        public string? Message { get; init; }
    }
}
