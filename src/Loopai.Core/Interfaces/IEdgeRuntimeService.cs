using System.Text.Json;

namespace Loopai.Core.Interfaces;

/// <summary>
/// Service for executing programs in an edge runtime environment (Deno/Bun).
/// </summary>
public interface IEdgeRuntimeService
{
    /// <summary>
    /// Executes a program with the given input.
    /// </summary>
    /// <param name="code">Program source code</param>
    /// <param name="language">Programming language</param>
    /// <param name="input">Input data</param>
    /// <param name="timeoutMs">Execution timeout in milliseconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result</returns>
    Task<EdgeExecutionResult> ExecuteAsync(
        string code,
        string language,
        JsonDocument input,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of edge runtime execution.
/// </summary>
public record EdgeExecutionResult
{
    public required bool Success { get; init; }
    public JsonDocument? Output { get; init; }
    public string? Error { get; init; }
    public required int ExecutionTimeMs { get; init; }
    public required int MemoryUsedBytes { get; init; }
    public string? StandardOutput { get; init; }
    public string? StandardError { get; init; }
}
