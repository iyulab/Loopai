namespace Loopai.Core.Models;

/// <summary>
/// Program complexity metrics.
/// </summary>
public record ComplexityMetrics
{
    /// <summary>
    /// Cyclomatic complexity score.
    /// </summary>
    public int? CyclomaticComplexity { get; init; }

    /// <summary>
    /// Lines of code count.
    /// </summary>
    public int? LinesOfCode { get; init; }

    /// <summary>
    /// Estimated execution latency in milliseconds.
    /// </summary>
    public double? EstimatedLatencyMs { get; init; }
}
