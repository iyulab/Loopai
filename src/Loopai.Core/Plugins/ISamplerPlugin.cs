using Loopai.Core.Models;

namespace Loopai.Core.Plugins;

/// <summary>
/// Interface for sampler plugins that determine which executions to sample for validation.
/// </summary>
public interface ISamplerPlugin : IPlugin
{
    /// <summary>
    /// Determines whether an execution should be sampled for validation.
    /// </summary>
    /// <param name="execution">Execution record to evaluate for sampling.</param>
    /// <param name="context">Sampling context with configuration and statistics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sampling decision indicating whether to sample this execution.</returns>
    Task<SamplingDecision> ShouldSampleAsync(
        ExecutionRecord execution,
        SamplingContext context,
        CancellationToken cancellationToken = default);
}
