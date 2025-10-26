using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.Extensions.Logging;

namespace Loopai.CloudApi.Services;

/// <summary>
/// Service for A/B testing and canary deployment of program versions.
/// </summary>
public class ABTestingService : IABTestingService
{
    private readonly IProgramArtifactRepository _artifactRepository;
    private readonly IExecutionRecordRepository _executionRepository;
    private readonly IValidationResultRepository _validationRepository;
    private readonly ICanaryDeploymentRepository _canaryRepository;
    private readonly ILogger<ABTestingService> _logger;

    // Canary deployment stages
    private static readonly Dictionary<CanaryStage, double> StagePercentages = new()
    {
        { CanaryStage.NotStarted, 0.0 },
        { CanaryStage.Stage1_5Percent, 0.05 },
        { CanaryStage.Stage2_25Percent, 0.25 },
        { CanaryStage.Stage3_50Percent, 0.50 },
        { CanaryStage.Stage4_100Percent, 1.0 },
        { CanaryStage.Completed, 1.0 }
    };

    public ABTestingService(
        IProgramArtifactRepository artifactRepository,
        IExecutionRecordRepository executionRepository,
        IValidationResultRepository validationRepository,
        ICanaryDeploymentRepository canaryRepository,
        ILogger<ABTestingService> logger)
    {
        _artifactRepository = artifactRepository;
        _executionRepository = executionRepository;
        _validationRepository = validationRepository;
        _canaryRepository = canaryRepository;
        _logger = logger;
    }

    public async Task<ABTestResult> CompareVersionsAsync(
        Guid controlProgramId,
        Guid treatmentProgramId,
        ABTestConfiguration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        configuration ??= new ABTestConfiguration();

        _logger.LogInformation(
            "Comparing programs: Control={ControlId}, Treatment={TreatmentId}",
            controlProgramId, treatmentProgramId);

        // Get execution records for both programs
        var controlExecutions = await _executionRepository.GetByProgramIdAsync(
            controlProgramId,
            limit: configuration.MinimumSampleSize * 2,
            cancellationToken);

        var treatmentExecutions = await _executionRepository.GetByProgramIdAsync(
            treatmentProgramId,
            limit: configuration.MinimumSampleSize * 2,
            cancellationToken);

        var controlList = controlExecutions.ToList();
        var treatmentList = treatmentExecutions.ToList();

        // Calculate metrics for each variant
        var controlMetrics = await CalculateMetricsAsync(
            controlProgramId,
            controlList,
            cancellationToken);

        var treatmentMetrics = await CalculateMetricsAsync(
            treatmentProgramId,
            treatmentList,
            cancellationToken);

        // Perform statistical comparison
        var statistics = CompareStatistics(
            controlMetrics,
            treatmentMetrics,
            controlList.Count,
            treatmentList.Count,
            configuration);

        // Determine recommendation
        var (recommendation, confidence) = DetermineRecommendation(
            statistics,
            configuration);

        _logger.LogInformation(
            "A/B Test Results: Delta={Delta:P2}, Recommendation={Recommendation}, Confidence={Confidence}",
            statistics.PerformanceDelta, recommendation, confidence);

        return new ABTestResult
        {
            TaskId = (await _artifactRepository.GetByIdAsync(controlProgramId, cancellationToken))!.TaskId,
            ControlProgramId = controlProgramId,
            TreatmentProgramId = treatmentProgramId,
            Configuration = configuration,
            Statistics = statistics,
            Recommendation = recommendation,
            Confidence = confidence
        };
    }

    public async Task<CanaryDeployment> StartCanaryDeploymentAsync(
        Guid taskId,
        Guid newProgramId,
        CancellationToken cancellationToken = default)
    {
        // Get current active program
        var currentProgram = await _artifactRepository.GetActiveByTaskIdAsync(taskId, cancellationToken);
        if (currentProgram == null)
        {
            throw new InvalidOperationException($"No active program found for task {taskId}");
        }

        // Check if there's already an active canary
        var existingCanary = await GetActiveCanaryAsync(taskId, cancellationToken);
        if (existingCanary != null)
        {
            throw new InvalidOperationException($"Canary deployment already in progress for task {taskId}");
        }

        _logger.LogInformation(
            "Starting canary deployment: Task={TaskId}, New={NewProgramId}, Current={CurrentProgramId}",
            taskId, newProgramId, currentProgram.Id);

        var canary = new CanaryDeployment
        {
            TaskId = taskId,
            NewProgramId = newProgramId,
            CurrentProgramId = currentProgram.Id,
            CurrentStage = CanaryStage.Stage1_5Percent,
            CurrentPercentage = 0.05,
            Status = CanaryStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            History = new List<CanaryStageHistory>
            {
                new CanaryStageHistory
                {
                    Stage = CanaryStage.Stage1_5Percent,
                    Percentage = 0.05,
                    Action = "started",
                    Reason = "Canary deployment initiated"
                }
            }
        };

        // Update program deployment percentages
        await UpdateDeploymentPercentagesAsync(canary, cancellationToken);

        // Store canary deployment in database
        await _canaryRepository.CreateAsync(canary, cancellationToken);

        return canary;
    }

    public async Task<CanaryDeployment> ProgressCanaryAsync(
        Guid canaryId,
        CancellationToken cancellationToken = default)
    {
        var canary = await _canaryRepository.GetByIdAsync(canaryId, cancellationToken);
        if (canary == null)
        {
            throw new InvalidOperationException($"Canary deployment {canaryId} not found");
        }

        if (canary.Status != CanaryStatus.InProgress)
        {
            throw new InvalidOperationException($"Canary deployment is not in progress (status: {canary.Status})");
        }

        // Evaluate whether to progress
        var evaluation = await EvaluateCanaryProgressionAsync(canaryId, cancellationToken);

        if (!evaluation.ShouldProgress)
        {
            _logger.LogWarning(
                "Canary progression blocked: {Reason}",
                evaluation.Reason);

            if (evaluation.Recommendation == "rollback")
            {
                return await RollbackCanaryAsync(canaryId, evaluation.Reason!, cancellationToken);
            }

            // Pause canary
            var pausedCanary = canary with
            {
                Status = CanaryStatus.Paused,
                StatusReason = evaluation.Reason
            };

            await _canaryRepository.UpdateAsync(pausedCanary, cancellationToken);

            return pausedCanary;
        }

        // Progress to next stage
        var nextStage = GetNextStage(canary.CurrentStage);
        var nextPercentage = StagePercentages[nextStage];

        _logger.LogInformation(
            "Progressing canary: Stage={CurrentStage} → {NextStage}, Percentage={Percentage:P0}",
            canary.CurrentStage, nextStage, nextPercentage);

        var newHistory = new List<CanaryStageHistory>(canary.History)
        {
            new CanaryStageHistory
            {
                Stage = nextStage,
                Percentage = nextPercentage,
                Action = "promoted",
                Reason = "Metrics acceptable, progressing to next stage"
            }
        };

        var isCompleted = nextStage == CanaryStage.Stage4_100Percent;

        var updatedCanary = canary with
        {
            CurrentStage = nextStage,
            CurrentPercentage = nextPercentage,
            Status = isCompleted ? CanaryStatus.Completed : CanaryStatus.InProgress,
            CompletedAt = isCompleted ? DateTime.UtcNow : null,
            History = newHistory
        };

        // Update deployment percentages
        await UpdateDeploymentPercentagesAsync(updatedCanary, cancellationToken);

        if (isCompleted)
        {
            // Set new program as active and old as retired
            await ActivateNewProgramAsync(updatedCanary, cancellationToken);
        }

        await _canaryRepository.UpdateAsync(updatedCanary, cancellationToken);

        return updatedCanary;
    }

    public async Task<CanaryDeployment> RollbackCanaryAsync(
        Guid canaryId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var canary = await _canaryRepository.GetByIdAsync(canaryId, cancellationToken);
        if (canary == null)
        {
            throw new InvalidOperationException($"Canary deployment {canaryId} not found");
        }

        _logger.LogWarning(
            "Rolling back canary deployment: {CanaryId}, Reason: {Reason}",
            canaryId, reason);

        var newHistory = new List<CanaryStageHistory>(canary.History)
        {
            new CanaryStageHistory
            {
                Stage = canary.CurrentStage,
                Percentage = canary.CurrentPercentage,
                Action = "rolled_back",
                Reason = reason
            }
        };

        var rolledBackCanary = canary with
        {
            Status = CanaryStatus.RolledBack,
            StatusReason = reason,
            CompletedAt = DateTime.UtcNow,
            History = newHistory
        };

        // Restore 100% to current program
        var currentProgram = await _artifactRepository.GetByIdAsync(canary.CurrentProgramId, cancellationToken);
        if (currentProgram != null)
        {
            var updated = currentProgram with { DeploymentPercentage = 1.0 };
            await _artifactRepository.UpdateAsync(updated, cancellationToken);
        }

        // Set new program to 0%
        var newProgram = await _artifactRepository.GetByIdAsync(canary.NewProgramId, cancellationToken);
        if (newProgram != null)
        {
            var updated = newProgram with { DeploymentPercentage = 0.0 };
            await _artifactRepository.UpdateAsync(updated, cancellationToken);
        }

        await _canaryRepository.UpdateAsync(rolledBackCanary, cancellationToken);

        return rolledBackCanary;
    }

    public async Task<CanaryDeployment?> GetActiveCanaryAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return await _canaryRepository.GetActiveByTaskIdAsync(taskId, cancellationToken);
    }

    public async Task<CanaryEvaluationResult> EvaluateCanaryProgressionAsync(
        Guid canaryId,
        CancellationToken cancellationToken = default)
    {
        var canary = await _canaryRepository.GetByIdAsync(canaryId, cancellationToken);
        if (canary == null)
        {
            throw new InvalidOperationException($"Canary deployment {canaryId} not found");
        }

        // Compare current performance with new program
        var comparison = await CompareVersionsAsync(
            canary.CurrentProgramId,
            canary.NewProgramId,
            new ABTestConfiguration
            {
                MinimumSampleSize = 50, // Lower threshold for canary evaluation
                MaxDegradationThreshold = 0.1, // 10% degradation threshold
                MinImprovementThreshold = 0.0 // Any improvement is good
            },
            cancellationToken);

        var stats = comparison.Statistics;

        // Check for significant degradation
        if (stats.IsSignificant && stats.PerformanceDelta < -0.1)
        {
            return new CanaryEvaluationResult
            {
                ShouldProgress = false,
                Recommendation = "rollback",
                Reason = $"Significant performance degradation detected: {stats.PerformanceDelta:P2}",
                Statistics = stats
            };
        }

        // Check if we have enough data
        if (stats.TreatmentSampleSize < 50)
        {
            return new CanaryEvaluationResult
            {
                ShouldProgress = false,
                Recommendation = "pause",
                Reason = $"Insufficient sample size: {stats.TreatmentSampleSize} (need 50+)",
                Statistics = stats
            };
        }

        // Check error rates
        if (stats.TreatmentMetrics.ErrorRate > stats.ControlMetrics.ErrorRate * 1.5)
        {
            return new CanaryEvaluationResult
            {
                ShouldProgress = false,
                Recommendation = "rollback",
                Reason = $"Error rate too high: {stats.TreatmentMetrics.ErrorRate:P2} vs {stats.ControlMetrics.ErrorRate:P2}",
                Statistics = stats
            };
        }

        // All checks passed
        return new CanaryEvaluationResult
        {
            ShouldProgress = true,
            Recommendation = "progress",
            Reason = "Metrics are acceptable",
            Statistics = stats
        };
    }

    private async Task<ProgramMetrics> CalculateMetricsAsync(
        Guid programId,
        List<ExecutionRecord> executions,
        CancellationToken cancellationToken)
    {
        var successfulExecutions = executions
            .Where(e => e.Status == ExecutionStatus.Success)
            .ToList();

        var failedExecutions = executions.Count - successfulExecutions.Count;

        // Calculate latency percentiles
        var latencies = successfulExecutions
            .Where(e => e.LatencyMs.HasValue)
            .Select(e => (double)e.LatencyMs!.Value)
            .OrderBy(l => l)
            .ToList();

        var avgLatency = latencies.Any() ? latencies.Average() : 0;
        var p50 = latencies.Any() ? GetPercentile(latencies, 0.5) : 0;
        var p95 = latencies.Any() ? GetPercentile(latencies, 0.95) : 0;
        var p99 = latencies.Any() ? GetPercentile(latencies, 0.99) : 0;

        // Get validation statistics
        var validationStats = await _validationRepository.GetStatisticsAsync(
            programId,
            since: DateTime.UtcNow.AddHours(-24),
            cancellationToken);

        return new ProgramMetrics
        {
            AverageLatencyMs = avgLatency,
            P50LatencyMs = p50,
            P95LatencyMs = p95,
            P99LatencyMs = p99,
            ValidationRate = validationStats.ValidationRate,
            ErrorRate = executions.Any() ? (double)failedExecutions / executions.Count : 0,
            TotalExecutions = executions.Count,
            SuccessfulExecutions = successfulExecutions.Count,
            FailedExecutions = failedExecutions
        };
    }

    private static ComparisonStatistics CompareStatistics(
        ProgramMetrics control,
        ProgramMetrics treatment,
        int controlSampleSize,
        int treatmentSampleSize,
        ABTestConfiguration configuration)
    {
        // Calculate performance delta (composite metric)
        // Positive delta = improvement, negative = degradation
        var latencyDelta = control.AverageLatencyMs > 0
            ? (control.AverageLatencyMs - treatment.AverageLatencyMs) / control.AverageLatencyMs
            : 0;

        var validationDelta = treatment.ValidationRate - control.ValidationRate;
        var errorDelta = control.ErrorRate - treatment.ErrorRate;

        // Weighted composite performance delta
        var performanceDelta = (latencyDelta * 0.3) + (validationDelta * 0.5) + (errorDelta * 0.2);

        // Simple statistical significance test (t-test approximation)
        // In production, use proper statistical library
        var pooledStd = Math.Sqrt(
            (control.AverageLatencyMs + treatment.AverageLatencyMs) / 2);

        var standardError = pooledStd * Math.Sqrt(
            (1.0 / controlSampleSize) + (1.0 / treatmentSampleSize));

        var tStat = standardError > 0
            ? Math.Abs(control.AverageLatencyMs - treatment.AverageLatencyMs) / standardError
            : 0;

        // Approximate p-value (simplified)
        var pValue = tStat > 1.96 ? 0.01 : 0.1; // Rough approximation
        var isSignificant = pValue < (1 - configuration.RequiredConfidence);

        return new ComparisonStatistics
        {
            ControlMetrics = control,
            TreatmentMetrics = treatment,
            PerformanceDelta = performanceDelta,
            PValue = pValue,
            IsSignificant = isSignificant,
            ControlSampleSize = controlSampleSize,
            TreatmentSampleSize = treatmentSampleSize
        };
    }

    private static (string recommendation, string confidence) DetermineRecommendation(
        ComparisonStatistics stats,
        ABTestConfiguration configuration)
    {
        // Insufficient data
        if (stats.ControlSampleSize < configuration.MinimumSampleSize ||
            stats.TreatmentSampleSize < configuration.MinimumSampleSize)
        {
            return ("continue", "low");
        }

        // Significant degradation
        if (stats.IsSignificant && stats.PerformanceDelta < -configuration.MaxDegradationThreshold)
        {
            return ("rollback", "high");
        }

        // Significant improvement
        if (stats.IsSignificant && stats.PerformanceDelta > configuration.MinImprovementThreshold)
        {
            return ("promote", "high");
        }

        // No significant difference
        if (!stats.IsSignificant)
        {
            return ("continue", "medium");
        }

        // Minor improvement but not enough to justify change
        if (stats.PerformanceDelta > 0 && stats.PerformanceDelta < configuration.MinImprovementThreshold)
        {
            return ("continue", "medium");
        }

        return ("manual_review", "low");
    }

    private static double GetPercentile(List<double> sortedValues, double percentile)
    {
        if (!sortedValues.Any()) return 0;

        var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        index = Math.Max(0, Math.Min(sortedValues.Count - 1, index));

        return sortedValues[index];
    }

    private static CanaryStage GetNextStage(CanaryStage current)
    {
        return current switch
        {
            CanaryStage.NotStarted => CanaryStage.Stage1_5Percent,
            CanaryStage.Stage1_5Percent => CanaryStage.Stage2_25Percent,
            CanaryStage.Stage2_25Percent => CanaryStage.Stage3_50Percent,
            CanaryStage.Stage3_50Percent => CanaryStage.Stage4_100Percent,
            CanaryStage.Stage4_100Percent => CanaryStage.Completed,
            _ => throw new InvalidOperationException($"Cannot progress from stage {current}")
        };
    }

    private async Task UpdateDeploymentPercentagesAsync(
        CanaryDeployment canary,
        CancellationToken cancellationToken)
    {
        var newPercentage = canary.CurrentPercentage;
        var currentPercentage = 1.0 - newPercentage;

        // Update new program
        var newProgram = await _artifactRepository.GetByIdAsync(canary.NewProgramId, cancellationToken);
        if (newProgram != null)
        {
            var updated = newProgram with { DeploymentPercentage = newPercentage };
            await _artifactRepository.UpdateAsync(updated, cancellationToken);
        }

        // Update current program
        var currentProgram = await _artifactRepository.GetByIdAsync(canary.CurrentProgramId, cancellationToken);
        if (currentProgram != null)
        {
            var updated = currentProgram with { DeploymentPercentage = currentPercentage };
            await _artifactRepository.UpdateAsync(updated, cancellationToken);
        }
    }

    private async Task ActivateNewProgramAsync(
        CanaryDeployment canary,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Activating new program: {NewProgramId} for task {TaskId}",
            canary.NewProgramId, canary.TaskId);

        // Set new program to Active with 100% deployment
        var newProgram = await _artifactRepository.GetByIdAsync(canary.NewProgramId, cancellationToken);
        if (newProgram != null)
        {
            var activated = newProgram with
            {
                Status = ProgramStatus.Active,
                DeploymentPercentage = 1.0
            };
            await _artifactRepository.UpdateAsync(activated, cancellationToken);
        }

        // Set old program to Deprecated with 0% deployment
        var currentProgram = await _artifactRepository.GetByIdAsync(canary.CurrentProgramId, cancellationToken);
        if (currentProgram != null)
        {
            var deprecated = currentProgram with
            {
                Status = ProgramStatus.Deprecated,
                DeploymentPercentage = 0.0
            };
            await _artifactRepository.UpdateAsync(deprecated, cancellationToken);
        }
    }
}
