namespace Loopai.CloudApi.Configuration;

/// <summary>
/// Configuration settings for the Edge Runtime (Deno/Bun).
/// </summary>
public class EdgeRuntimeSettings
{
    /// <summary>
    /// Type of edge runtime to use: "deno" or "bun".
    /// </summary>
    public string RuntimeType { get; set; } = "deno";

    /// <summary>
    /// Path to the runtime executable (e.g., "deno" or "bun").
    /// </summary>
    public string ExecutablePath { get; set; } = "deno";

    /// <summary>
    /// Execution timeout in milliseconds.
    /// </summary>
    public int ExecutionTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Maximum memory limit in MB for program execution.
    /// </summary>
    public int MaxMemoryMb { get; set; } = 128;

    /// <summary>
    /// Working directory for temporary program files.
    /// </summary>
    public string WorkingDirectory { get; set; } = "./temp/programs";

    /// <summary>
    /// Whether to clean up temporary program files after execution.
    /// </summary>
    public bool CleanupTempFiles { get; set; } = true;
}
