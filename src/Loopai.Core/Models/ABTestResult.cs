using System.Text.Json;

namespace Loopai.Core.Models;

/// <summary>
/// Result of A/B testing between two program versions.
/// </summary>
public record ABTestResult
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid TaskId { get; init; }
    public required Guid ControlProgramId { get; init; }
    public required Guid TreatmentProgramId { get; init; }

    /// <summary>
    /// Test configuration at the time of comparison.
    /// </summary>
    public required ABTestConfiguration Configuration { get; init; }

    /// <summary>
    /// Statistical comparison results.
    /// </summary>
    public required ComparisonStatistics Statistics { get; init; }

    /// <summary>
    /// Recommended action based on test results.
    /// </summary>
    public required string Recommendation { get; init; } // "promote", "rollback", "continue", "manual_review"

    /// <summary>
    /// Confidence level in the recommendation.
    /// </summary>
    public required string Confidence { get; init; } // "high", "medium", "low"

    public DateTime ComparedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Configuration for A/B testing.
/// </summary>
public record ABTestConfiguration
{
    /// <summary>
    /// Minimum sample size for statistical significance.
    /// </summary>
    public int MinimumSampleSize { get; init; } = 100;

    /// <summary>
    /// Required confidence level (e.g., 0.95 for 95%).
    /// </summary>
    public double RequiredConfidence { get; init; } = 0.95;

    /// <summary>
    /// Maximum allowed performance degradation (e.g., 0.05 for 5%).
    /// </summary>
    public double MaxDegradationThreshold { get; init; } = 0.05;

    /// <summary>
    /// Minimum improvement to justify promotion (e.g., 0.02 for 2%).
    /// </summary>
    public double MinImprovementThreshold { get; init; } = 0.02;

    /// <summary>
    /// Test duration in hours.
    /// </summary>
    public int TestDurationHours { get; init; } = 24;
}

/// <summary>
/// Statistical comparison between control and treatment groups.
/// </summary>
public record ComparisonStatistics
{
    public required ProgramMetrics ControlMetrics { get; init; }
    public required ProgramMetrics TreatmentMetrics { get; init; }

    /// <summary>
    /// Relative performance difference (-1.0 to +1.0, where positive is improvement).
    /// </summary>
    public required double PerformanceDelta { get; init; }

    /// <summary>
    /// Statistical significance p-value.
    /// </summary>
    public required double PValue { get; init; }

    /// <summary>
    /// Whether the difference is statistically significant.
    /// </summary>
    public required bool IsSignificant { get; init; }

    /// <summary>
    /// Sample sizes for each variant.
    /// </summary>
    public required int ControlSampleSize { get; init; }
    public required int TreatmentSampleSize { get; init; }
}

/// <summary>
/// Aggregated metrics for a program variant.
/// </summary>
public record ProgramMetrics
{
    public required double AverageLatencyMs { get; init; }
    public required double P50LatencyMs { get; init; }
    public required double P95LatencyMs { get; init; }
    public required double P99LatencyMs { get; init; }

    public required double ValidationRate { get; init; }
    public required double ErrorRate { get; init; }

    public required int TotalExecutions { get; init; }
    public required int SuccessfulExecutions { get; init; }
    public required int FailedExecutions { get; init; }
}

/// <summary>
/// Canary deployment progression tracking.
/// </summary>
public record CanaryDeployment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid TaskId { get; init; }
    public required Guid NewProgramId { get; init; }
    public required Guid CurrentProgramId { get; init; }

    public required CanaryStage CurrentStage { get; init; }
    public required double CurrentPercentage { get; init; }

    public required CanaryStatus Status { get; init; }

    /// <summary>
    /// Reason for current status (e.g., why it was paused or rolled back).
    /// </summary>
    public string? StatusReason { get; init; }

    /// <summary>
    /// Progression history through stages.
    /// </summary>
    public IReadOnlyList<CanaryStageHistory> History { get; init; } = Array.Empty<CanaryStageHistory>();

    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

/// <summary>
/// Canary deployment stages.
/// </summary>
public enum CanaryStage
{
    NotStarted = 0,
    Stage1_5Percent = 1,
    Stage2_25Percent = 2,
    Stage3_50Percent = 3,
    Stage4_100Percent = 4,
    Completed = 5
}

/// <summary>
/// Canary deployment status.
/// </summary>
public enum CanaryStatus
{
    InProgress,
    Paused,
    RolledBack,
    Completed,
    Failed
}

/// <summary>
/// History entry for canary stage progression.
/// </summary>
public record CanaryStageHistory
{
    public required CanaryStage Stage { get; init; }
    public required double Percentage { get; init; }
    public required string Action { get; init; } // "promoted", "paused", "rolled_back"
    public string? Reason { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
