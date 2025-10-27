using Loopai.Core.Models;

namespace Loopai.Core.Plugins.BuiltIn;

/// <summary>
/// Built-in sampler that samples a fixed percentage of executions.
/// </summary>
public class PercentageSamplerPlugin : ISamplerPlugin
{
    private readonly double _samplingRate;
    private readonly Random _random = new();

    /// <summary>
    /// Creates a new percentage-based sampler.
    /// </summary>
    /// <param name="samplingRate">Sampling rate (0.0 to 1.0).</param>
    public PercentageSamplerPlugin(double samplingRate)
    {
        if (samplingRate < 0.0 || samplingRate > 1.0)
            throw new ArgumentException("Sampling rate must be between 0.0 and 1.0.", nameof(samplingRate));

        _samplingRate = samplingRate;
    }

    /// <summary>
    /// Creates a new percentage-based sampler with 10% sampling rate.
    /// </summary>
    public PercentageSamplerPlugin() : this(0.1)
    {
    }

    /// <inheritdoc/>
    public string Name => "percentage-sampler";

    /// <inheritdoc/>
    public string Description => $"Samples {_samplingRate * 100:F0}% of executions randomly";

    /// <inheritdoc/>
    public string Version => "1.0.0";

    /// <inheritdoc/>
    public string Author => "Loopai";

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public int Priority { get; set; } = 100;

    /// <inheritdoc/>
    public Task<SamplingDecision> ShouldSampleAsync(
        ExecutionRecord execution,
        SamplingContext context,
        CancellationToken cancellationToken = default)
    {
        // Use configured sampling rate if available, otherwise use plugin rate
        var rate = context.SamplingRate > 0 ? context.SamplingRate : _samplingRate;

        // Generate random number and compare with sampling rate
        var randomValue = _random.NextDouble();
        var shouldSample = randomValue < rate;

        return Task.FromResult(new SamplingDecision
        {
            ShouldSample = shouldSample,
            Reason = shouldSample
                ? $"Random sample selected (value: {randomValue:F4} < rate: {rate:F4})"
                : $"Random sample not selected (value: {randomValue:F4} >= rate: {rate:F4})",
            SamplerType = Name,
            ConfidenceScore = shouldSample ? 1.0 : 0.0,
            Metadata = new Dictionary<string, object>
            {
                ["sampling_rate"] = rate,
                ["random_value"] = randomValue,
                ["total_executions"] = context.TotalExecutions,
                ["sampled_executions"] = context.SampledExecutions,
                ["actual_sampling_rate"] = context.TotalExecutions > 0
                    ? (double)context.SampledExecutions / context.TotalExecutions
                    : 0.0
            }
        });
    }
}
