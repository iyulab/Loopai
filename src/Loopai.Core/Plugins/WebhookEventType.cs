namespace Loopai.Core.Plugins;

/// <summary>
/// Types of webhook events that can be handled.
/// </summary>
public enum WebhookEventType
{
    /// <summary>
    /// Task was created.
    /// </summary>
    TaskCreated,

    /// <summary>
    /// Task was updated.
    /// </summary>
    TaskUpdated,

    /// <summary>
    /// Task was deleted.
    /// </summary>
    TaskDeleted,

    /// <summary>
    /// Program generation started.
    /// </summary>
    ProgramGenerationStarted,

    /// <summary>
    /// Program generation completed successfully.
    /// </summary>
    ProgramGenerationCompleted,

    /// <summary>
    /// Program generation failed.
    /// </summary>
    ProgramGenerationFailed,

    /// <summary>
    /// Program execution completed successfully.
    /// </summary>
    ProgramExecutionCompleted,

    /// <summary>
    /// Program execution failed.
    /// </summary>
    ProgramExecutionFailed,

    /// <summary>
    /// Program execution timed out.
    /// </summary>
    ProgramExecutionTimeout,

    /// <summary>
    /// Validation completed.
    /// </summary>
    ValidationCompleted,

    /// <summary>
    /// Validation failed.
    /// </summary>
    ValidationFailed,

    /// <summary>
    /// Canary deployment started.
    /// </summary>
    CanaryDeploymentStarted,

    /// <summary>
    /// Canary deployment succeeded.
    /// </summary>
    CanaryDeploymentSucceeded,

    /// <summary>
    /// Canary deployment failed and rolled back.
    /// </summary>
    CanaryRollback,

    /// <summary>
    /// Program version promoted to active.
    /// </summary>
    ProgramVersionPromoted,

    /// <summary>
    /// Accuracy threshold breached.
    /// </summary>
    AccuracyThresholdBreached,

    /// <summary>
    /// Latency threshold breached.
    /// </summary>
    LatencyThresholdBreached
}
