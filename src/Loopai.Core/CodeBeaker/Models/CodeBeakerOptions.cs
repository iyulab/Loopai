namespace Loopai.Core.CodeBeaker.Models;

/// <summary>
/// Configuration options for CodeBeaker integration.
/// </summary>
public class CodeBeakerOptions
{
    /// <summary>
    /// WebSocket endpoint URL for CodeBeaker JSON-RPC API.
    /// </summary>
    public string WebSocketUrl { get; set; } = "ws://localhost:5000/ws/jsonrpc";

    /// <summary>
    /// Session idle timeout in minutes.
    /// </summary>
    public int SessionIdleTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Session maximum lifetime in minutes.
    /// </summary>
    public int SessionMaxLifetimeMinutes { get; set; } = 120;

    /// <summary>
    /// Maximum number of sessions in the pool.
    /// </summary>
    public int SessionPoolSize { get; set; } = 10;

    /// <summary>
    /// Memory limit per session in MB.
    /// </summary>
    public int? MemoryLimitMB { get; set; } = 512;

    /// <summary>
    /// CPU shares per session.
    /// </summary>
    public long? CpuShares { get; set; } = 1024;

    /// <summary>
    /// Default execution timeout in milliseconds.
    /// </summary>
    public int DefaultTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Enable automatic session cleanup.
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;

    /// <summary>
    /// Session cleanup interval in minutes.
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum concurrent executions per session.
    /// </summary>
    public int MaxConcurrentExecutionsPerSession { get; set; } = 1;

    /// <summary>
    /// WebSocket connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// WebSocket keep-alive interval in seconds.
    /// </summary>
    public int KeepAliveIntervalSeconds { get; set; } = 120;

    /// <summary>
    /// Maximum retry attempts for failed operations.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delay in milliseconds.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Supported languages mapping (Loopai language -> CodeBeaker language).
    /// </summary>
    public Dictionary<string, string> LanguageMapping { get; set; } = new()
    {
        { "python", "python" },
        { "javascript", "javascript" },
        { "typescript", "javascript" },
        { "go", "go" },
        { "csharp", "csharp" },
        { "dotnet", "csharp" }
    };
}
