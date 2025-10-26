using System.Text.Json;

namespace Loopai.Core.Models;

/// <summary>
/// Sampling strategy types.
/// </summary>
public enum SamplingStrategyType
{
    /// <summary>
    /// Random uniform sampling.
    /// </summary>
    Random,

    /// <summary>
    /// Stratified sampling across input space partitions.
    /// </summary>
    Stratified,

    /// <summary>
    /// Edge case and boundary value focused sampling.
    /// </summary>
    EdgeCase,

    /// <summary>
    /// Adaptive sampling based on failure patterns.
    /// </summary>
    Adaptive,

    /// <summary>
    /// Diversity-maximizing sampling.
    /// </summary>
    DiversityBased
}

/// <summary>
/// Configuration for sampling strategy.
/// </summary>
public record SamplingConfiguration
{
    public required SamplingStrategyType Strategy { get; init; }
    public required double SamplingRate { get; init; }

    /// <summary>
    /// Strategy-specific parameters.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; init; }
}

/// <summary>
/// Input space partition for stratified sampling.
/// </summary>
public record InputPartition
{
    public required string PartitionKey { get; init; }
    public required JsonDocument Criteria { get; init; }
    public required double Weight { get; init; }
    public int SampleCount { get; init; }
}

/// <summary>
/// Edge case pattern for targeted sampling.
/// </summary>
public record EdgeCasePattern
{
    public required string PatternName { get; init; }
    public required string Description { get; init; }
    public required JsonDocument ExampleInput { get; init; }
    public required string Category { get; init; } // "boundary", "null", "empty", "extreme", "special"
    public double Priority { get; init; } = 1.0;
}

/// <summary>
/// Adaptive sampling feedback based on execution results.
/// </summary>
public record AdaptiveSamplingFeedback
{
    public required Guid TaskId { get; init; }
    public required JsonDocument Input { get; init; }
    public required bool WasFailure { get; init; }
    public required string FailureReason { get; init; }
    public List<string> IdentifiedPatterns { get; init; } = new();
    public DateTime RecordedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Diversity metric for input selection.
/// </summary>
public record DiversityMetrics
{
    /// <summary>
    /// Feature coverage score (0-1).
    /// </summary>
    public required double FeatureCoverage { get; init; }

    /// <summary>
    /// Input space coverage score (0-1).
    /// </summary>
    public required double SpaceCoverage { get; init; }

    /// <summary>
    /// Diversity score combining multiple factors (0-1).
    /// </summary>
    public required double DiversityScore { get; init; }

    /// <summary>
    /// Distinct input patterns identified.
    /// </summary>
    public int DistinctPatterns { get; init; }
}

/// <summary>
/// Sampling decision result.
/// </summary>
public record SamplingDecision
{
    public required bool ShouldSample { get; init; }
    public required double SamplingProbability { get; init; }
    public required string Reason { get; init; }
    public required SamplingStrategyType StrategyUsed { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Sampling statistics for monitoring.
/// </summary>
public record SamplingStatistics
{
    public required Guid TaskId { get; init; }
    public required int TotalExecutions { get; init; }
    public required int SampledExecutions { get; init; }
    public required double ActualSamplingRate { get; init; }
    public required SamplingStrategyType Strategy { get; init; }

    public Dictionary<string, int> PartitionCounts { get; init; } = new();
    public Dictionary<string, int> EdgeCaseCounts { get; init; } = new();

    public required DiversityMetrics Diversity { get; init; }
    public DateTime CalculatedAt { get; init; } = DateTime.UtcNow;
}
