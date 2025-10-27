namespace Loopai.Core.Plugins;

/// <summary>
/// Context provided to sampler plugins during sampling decision.
/// </summary>
public class SamplingContext
{
    /// <summary>
    /// Task identifier being sampled.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Configured sampling rate for the task (0.0 to 1.0).
    /// </summary>
    public required double SamplingRate { get; init; }

    /// <summary>
    /// Total number of executions for this task.
    /// </summary>
    public required long TotalExecutions { get; init; }

    /// <summary>
    /// Number of executions sampled so far.
    /// </summary>
    public required long SampledExecutions { get; init; }

    /// <summary>
    /// Timestamp of last sampling for this task.
    /// </summary>
    public DateTime? LastSampleTime { get; init; }

    /// <summary>
    /// Custom configuration for the sampler plugin.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Configuration { get; init; }

    /// <summary>
    /// Additional metadata for sampling decision.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Gets the last sample time asynchronously (for implementations requiring async access).
    /// </summary>
    public Func<Task<DateTime?>>? GetLastSampleTimeAsync { get; init; }
}
