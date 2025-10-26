using Prometheus;

namespace Loopai.CloudApi.Services;

/// <summary>
/// Service for collecting and exposing Prometheus metrics for the Loopai framework.
/// </summary>
public class MetricsService
{
    // Program execution metrics
    private static readonly Counter ProgramExecutionsTotal = Metrics.CreateCounter(
        "loopai_program_executions_total",
        "Total number of program executions",
        new CounterConfiguration
        {
            LabelNames = new[] { "task_id", "status" }
        });

    private static readonly Histogram ProgramExecutionDuration = Metrics.CreateHistogram(
        "loopai_program_execution_duration_seconds",
        "Program execution duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "task_id" },
            Buckets = Histogram.ExponentialBuckets(0.01, 2, 10) // 10ms to 10s
        });

    private static readonly Gauge ProgramExecutionMemoryUsage = Metrics.CreateGauge(
        "loopai_program_execution_memory_mb",
        "Program execution memory usage in MB",
        new GaugeConfiguration
        {
            LabelNames = new[] { "task_id" }
        });

    // Validation metrics
    private static readonly Counter ValidationTotal = Metrics.CreateCounter(
        "loopai_validations_total",
        "Total number of output validations",
        new CounterConfiguration
        {
            LabelNames = new[] { "task_id", "program_id", "result" }
        });

    private static readonly Gauge ValidationSuccessRate = Metrics.CreateGauge(
        "loopai_validation_success_rate",
        "Validation success rate per program",
        new GaugeConfiguration
        {
            LabelNames = new[] { "task_id", "program_id" }
        });

    // Program improvement metrics
    private static readonly Counter ProgramImprovementsTotal = Metrics.CreateCounter(
        "loopai_program_improvements_total",
        "Total number of program improvements",
        new CounterConfiguration
        {
            LabelNames = new[] { "task_id", "reason" }
        });

    private static readonly Gauge ProgramVersions = Metrics.CreateGauge(
        "loopai_program_versions",
        "Current number of program versions per task",
        new GaugeConfiguration
        {
            LabelNames = new[] { "task_id" }
        });

    // A/B testing metrics
    private static readonly Counter ABTestsTotal = Metrics.CreateCounter(
        "loopai_ab_tests_total",
        "Total number of A/B tests conducted",
        new CounterConfiguration
        {
            LabelNames = new[] { "task_id", "recommendation" }
        });

    private static readonly Gauge ABTestConfidence = Metrics.CreateGauge(
        "loopai_ab_test_confidence",
        "A/B test confidence level",
        new GaugeConfiguration
        {
            LabelNames = new[] { "task_id", "control_program_id", "treatment_program_id" }
        });

    // Canary deployment metrics
    private static readonly Gauge CanaryDeploymentStatus = Metrics.CreateGauge(
        "loopai_canary_deployment_status",
        "Current canary deployment status (0=not_started, 1=stage1, 2=stage2, 3=stage3, 4=stage4, 5=completed)",
        new GaugeConfiguration
        {
            LabelNames = new[] { "task_id", "canary_id", "status" }
        });

    private static readonly Counter CanaryRollbacksTotal = Metrics.CreateCounter(
        "loopai_canary_rollbacks_total",
        "Total number of canary rollbacks",
        new CounterConfiguration
        {
            LabelNames = new[] { "task_id", "reason" }
        });

    // Sampling metrics
    private static readonly Counter SamplingDecisionsTotal = Metrics.CreateCounter(
        "loopai_sampling_decisions_total",
        "Total number of sampling decisions",
        new CounterConfiguration
        {
            LabelNames = new[] { "task_id", "strategy", "sampled" }
        });

    private static readonly Gauge SamplingRate = Metrics.CreateGauge(
        "loopai_sampling_rate",
        "Current sampling rate per task",
        new GaugeConfiguration
        {
            LabelNames = new[] { "task_id", "strategy" }
        });

    // Task metrics
    private static readonly Gauge ActiveTasks = Metrics.CreateGauge(
        "loopai_active_tasks",
        "Number of active tasks in the system");

    // Program execution tracking
    public void RecordProgramExecution(string taskId, string status, double durationSeconds, double? memoryMb = null)
    {
        ProgramExecutionsTotal.WithLabels(taskId, status).Inc();
        ProgramExecutionDuration.WithLabels(taskId).Observe(durationSeconds);

        if (memoryMb.HasValue)
        {
            ProgramExecutionMemoryUsage.WithLabels(taskId).Set(memoryMb.Value);
        }
    }

    // Validation tracking
    public void RecordValidation(string taskId, string programId, bool isValid)
    {
        ValidationTotal.WithLabels(taskId, programId, isValid ? "valid" : "invalid").Inc();
    }

    public void UpdateValidationSuccessRate(string taskId, string programId, double rate)
    {
        ValidationSuccessRate.WithLabels(taskId, programId).Set(rate);
    }

    // Program improvement tracking
    public void RecordProgramImprovement(string taskId, string reason)
    {
        ProgramImprovementsTotal.WithLabels(taskId, reason).Inc();
    }

    public void UpdateProgramVersions(string taskId, int versionCount)
    {
        ProgramVersions.WithLabels(taskId).Set(versionCount);
    }

    // A/B testing tracking
    public void RecordABTest(string taskId, string recommendation, double confidence,
        string controlProgramId, string treatmentProgramId)
    {
        ABTestsTotal.WithLabels(taskId, recommendation).Inc();
        ABTestConfidence.WithLabels(taskId, controlProgramId, treatmentProgramId).Set(confidence);
    }

    // Canary deployment tracking
    public void UpdateCanaryStatus(string taskId, string canaryId, string status, int stageNumber)
    {
        CanaryDeploymentStatus.WithLabels(taskId, canaryId, status).Set(stageNumber);
    }

    public void RecordCanaryRollback(string taskId, string reason)
    {
        CanaryRollbacksTotal.WithLabels(taskId, reason).Inc();
    }

    // Sampling tracking
    public void RecordSamplingDecision(string taskId, string strategy, bool sampled)
    {
        SamplingDecisionsTotal.WithLabels(taskId, strategy, sampled ? "true" : "false").Inc();
    }

    public void UpdateSamplingRate(string taskId, string strategy, double rate)
    {
        SamplingRate.WithLabels(taskId, strategy).Set(rate);
    }

    // Task tracking
    public void UpdateActiveTasks(int count)
    {
        ActiveTasks.Set(count);
    }
}
