namespace Loopai.Core.Plugins;

/// <summary>
/// Result from a sampler plugin execution.
/// </summary>
public class SamplingDecision
{
    /// <summary>
    /// Indicates whether this execution should be sampled.
    /// </summary>
    public required bool ShouldSample { get; init; }

    /// <summary>
    /// Reason for the sampling decision.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Name of the sampler that made this decision.
    /// </summary>
    public string? SamplerType { get; init; }

    /// <summary>
    /// Confidence score for this decision (0.0 to 1.0).
    /// </summary>
    public double? ConfidenceScore { get; init; }

    /// <summary>
    /// Additional metadata from the sampler.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
