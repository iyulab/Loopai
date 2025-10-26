using System.Text.Json;

namespace Loopai.CloudApi.Models;

/// <summary>
/// Response from the Python Generator Service.
/// </summary>
public record ProgramGenerationResponse
{
    /// <summary>
    /// Whether program generation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Generated program source code.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Programming language of the generated code.
    /// </summary>
    public string Language { get; init; } = "typescript";

    /// <summary>
    /// Estimated complexity metrics.
    /// </summary>
    public GeneratedComplexityMetrics? Complexity { get; init; }

    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Additional metadata from the generator.
    /// </summary>
    public JsonDocument? Metadata { get; init; }

    /// <summary>
    /// Generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Complexity metrics from the generator service.
/// </summary>
public record GeneratedComplexityMetrics
{
    public int LinesOfCode { get; init; }
    public int CyclomaticComplexity { get; init; }
    public int EstimatedTokens { get; init; }
}
