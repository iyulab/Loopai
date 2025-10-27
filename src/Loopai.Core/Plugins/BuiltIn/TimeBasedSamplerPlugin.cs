using Loopai.Core.Models;

namespace Loopai.Core.Plugins.BuiltIn;

/// <summary>
/// Built-in sampler that samples based on time intervals.
/// </summary>
public class TimeBasedSamplerPlugin : ISamplerPlugin
{
    private readonly TimeSpan _interval;

    /// <summary>
    /// Creates a new time-based sampler with the specified interval.
    /// </summary>
    /// <param name="interval">Minimum time interval between samples.</param>
    public TimeBasedSamplerPlugin(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("Interval must be greater than zero.", nameof(interval));

        _interval = interval;
    }

    /// <summary>
    /// Creates a new time-based sampler with default 5-minute interval.
    /// </summary>
    public TimeBasedSamplerPlugin() : this(TimeSpan.FromMinutes(5))
    {
    }

    /// <inheritdoc/>
    public string Name => "time-based-sampler";

    /// <inheritdoc/>
    public string Description => $"Samples execution every {_interval.TotalMinutes:F0} minutes";

    /// <inheritdoc/>
    public string Version => "1.0.0";

    /// <inheritdoc/>
    public string Author => "Loopai";

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public int Priority { get; set; } = 50;

    /// <inheritdoc/>
    public async Task<SamplingDecision> ShouldSampleAsync(
        ExecutionRecord execution,
        SamplingContext context,
        CancellationToken cancellationToken = default)
    {
        // Get last sample time
        DateTime? lastSampleTime = context.LastSampleTime;

        // If GetLastSampleTimeAsync is provided, use it
        if (lastSampleTime == null && context.GetLastSampleTimeAsync != null)
        {
            lastSampleTime = await context.GetLastSampleTimeAsync();
        }

        // If no previous sample, sample this one
        if (lastSampleTime == null)
        {
            return new SamplingDecision
            {
                ShouldSample = true,
                Reason = "No previous sample found - sampling first execution",
                SamplerType = Name,
                ConfidenceScore = 1.0
            };
        }

        // Check if enough time has passed since last sample
        var timeSinceLastSample = DateTime.UtcNow - lastSampleTime.Value;
        var shouldSample = timeSinceLastSample >= _interval;

        return new SamplingDecision
        {
            ShouldSample = shouldSample,
            Reason = shouldSample
                ? $"Time interval reached: {timeSinceLastSample.TotalMinutes:F1} minutes since last sample"
                : $"Time interval not reached: {timeSinceLastSample.TotalMinutes:F1} / {_interval.TotalMinutes:F0} minutes",
            SamplerType = Name,
            ConfidenceScore = shouldSample ? 1.0 : 0.0,
            Metadata = new Dictionary<string, object>
            {
                ["interval_minutes"] = _interval.TotalMinutes,
                ["time_since_last_sample_minutes"] = timeSinceLastSample.TotalMinutes
            }
        };
    }
}
