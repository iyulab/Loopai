namespace Loopai.CloudApi.DTOs;

/// <summary>
/// Response containing validation summary for a program.
/// </summary>
public record ValidationSummaryResponse
{
    public required Guid ProgramId { get; init; }
    public required ValidationStatisticsDto Statistics { get; init; }
    public required ImprovementRecommendationsDto Recommendations { get; init; }
}

/// <summary>
/// Validation statistics DTO.
/// </summary>
public record ValidationStatisticsDto
{
    public required int TotalValidations { get; init; }
    public required int ValidCount { get; init; }
    public required int InvalidCount { get; init; }
    public required double AverageScore { get; init; }
    public required double ValidationRate { get; init; }
}

/// <summary>
/// Program improvement recommendations DTO.
/// </summary>
public record ImprovementRecommendationsDto
{
    public required bool ShouldImprove { get; init; }
    public required double ValidationRate { get; init; }
    public required int FailedValidationsCount { get; init; }
    public required List<string> CommonErrors { get; init; }
    public required List<string> SuggestedFixes { get; init; }
    public required string Confidence { get; init; }
}

/// <summary>
/// Response for program improvement result.
/// </summary>
public record ImprovementResultResponse
{
    public required bool Success { get; init; }
    public Guid? NewProgramId { get; init; }
    public int? NewVersion { get; init; }
    public required List<string> ChangesApplied { get; init; }
}

/// <summary>
/// Request to compare two program versions.
/// </summary>
public record CompareProgramsRequest
{
    public required Guid ControlProgramId { get; init; }
    public required Guid TreatmentProgramId { get; init; }
    public ABTestConfigurationDto? Configuration { get; init; }
}

/// <summary>
/// A/B test configuration DTO.
/// </summary>
public record ABTestConfigurationDto
{
    public int MinimumSampleSize { get; init; } = 100;
    public double RequiredConfidence { get; init; } = 0.95;
    public double MaxDegradationThreshold { get; init; } = 0.05;
    public double MinImprovementThreshold { get; init; } = 0.02;
    public int TestDurationHours { get; init; } = 24;
}

/// <summary>
/// Response for A/B test comparison.
/// </summary>
public record ABTestResultResponse
{
    public required Guid TaskId { get; init; }
    public required Guid ControlProgramId { get; init; }
    public required Guid TreatmentProgramId { get; init; }
    public required ComparisonStatisticsDto Statistics { get; init; }
    public required string Recommendation { get; init; }
    public required string Confidence { get; init; }
    public required DateTime ComparedAt { get; init; }
}

/// <summary>
/// Comparison statistics DTO.
/// </summary>
public record ComparisonStatisticsDto
{
    public required ProgramMetricsDto ControlMetrics { get; init; }
    public required ProgramMetricsDto TreatmentMetrics { get; init; }
    public required double PerformanceDelta { get; init; }
    public required double PValue { get; init; }
    public required bool IsSignificant { get; init; }
    public required int ControlSampleSize { get; init; }
    public required int TreatmentSampleSize { get; init; }
}

/// <summary>
/// Program metrics DTO.
/// </summary>
public record ProgramMetricsDto
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
/// Request to start canary deployment.
/// </summary>
public record StartCanaryRequest
{
    public required Guid TaskId { get; init; }
    public required Guid NewProgramId { get; init; }
}

/// <summary>
/// Request to rollback canary deployment.
/// </summary>
public record RollbackCanaryRequest
{
    public required string Reason { get; init; }
}

/// <summary>
/// Response for canary deployment operations.
/// </summary>
public record CanaryDeploymentResponse
{
    public required Guid Id { get; init; }
    public required Guid TaskId { get; init; }
    public required Guid NewProgramId { get; init; }
    public required Guid CurrentProgramId { get; init; }
    public required string CurrentStage { get; init; }
    public required double CurrentPercentage { get; init; }
    public required string Status { get; init; }
    public string? StatusReason { get; init; }
    public required List<CanaryStageHistoryDto> History { get; init; }
    public required DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

/// <summary>
/// Canary stage history DTO.
/// </summary>
public record CanaryStageHistoryDto
{
    public required string Stage { get; init; }
    public required double Percentage { get; init; }
    public required string Action { get; init; }
    public string? Reason { get; init; }
    public required DateTime Timestamp { get; init; }
}
