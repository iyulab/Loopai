using System.Text.Json;

namespace Loopai.Core.Interfaces;

/// <summary>
/// Service for generating programs using AI-powered synthesis.
/// </summary>
public interface IProgramGeneratorService
{
    /// <summary>
    /// Generates a program for the specified task.
    /// </summary>
    /// <param name="taskId">Task specification ID</param>
    /// <param name="examples">Optional example input-output pairs</param>
    /// <param name="constraints">Optional additional constraints</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated program code and metadata</returns>
    Task<ProgramGenerationResult> GenerateProgramAsync(
        Guid taskId,
        IReadOnlyList<(JsonDocument Input, JsonDocument Output)>? examples = null,
        string? constraints = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of program generation.
/// </summary>
public record ProgramGenerationResult
{
    public required bool Success { get; init; }
    public required string? Code { get; init; }
    public required string Language { get; init; }
    public required int LinesOfCode { get; init; }
    public required int CyclomaticComplexity { get; init; }
    public required int EstimatedTokens { get; init; }
    public string? ErrorMessage { get; init; }
    public JsonDocument? Metadata { get; init; }
}
