using Loopai.Core.Models;

namespace Loopai.Core.Interfaces;

/// <summary>
/// Service for A/B testing and canary deployment of program versions.
/// </summary>
public interface IABTestingService
{
    /// <summary>
    /// Compares two program versions using A/B testing methodology.
    /// </summary>
    Task<ABTestResult> CompareVersionsAsync(
        Guid controlProgramId,
        Guid treatmentProgramId,
        ABTestConfiguration? configuration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a canary deployment for a new program version.
    /// </summary>
    Task<CanaryDeployment> StartCanaryDeploymentAsync(
        Guid taskId,
        Guid newProgramId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Progresses canary deployment to the next stage if metrics are acceptable.
    /// </summary>
    Task<CanaryDeployment> ProgressCanaryAsync(
        Guid canaryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a canary deployment to the previous version.
    /// </summary>
    Task<CanaryDeployment> RollbackCanaryAsync(
        Guid canaryId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current canary deployment for a task, if any.
    /// </summary>
    Task<CanaryDeployment?> GetActiveCanaryAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether a canary deployment should progress based on current metrics.
    /// </summary>
    Task<CanaryEvaluationResult> EvaluateCanaryProgressionAsync(
        Guid canaryId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of canary deployment evaluation.
/// </summary>
public record CanaryEvaluationResult
{
    public required bool ShouldProgress { get; init; }
    public required string Recommendation { get; init; } // "progress", "pause", "rollback"
    public string? Reason { get; init; }
    public ComparisonStatistics? Statistics { get; init; }
}
