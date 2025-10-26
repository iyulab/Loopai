using System.Text.Json;
using Loopai.Core.Models;

namespace Loopai.Core.Interfaces;

/// <summary>
/// Service for intelligent sampling strategy decisions.
/// </summary>
public interface ISamplingStrategyService
{
    /// <summary>
    /// Decides whether to sample an execution based on configured strategy.
    /// </summary>
    Task<SamplingDecision> ShouldSampleAsync(
        Guid taskId,
        JsonDocument input,
        SamplingConfiguration? configuration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records execution feedback for adaptive sampling.
    /// </summary>
    Task RecordFeedbackAsync(
        AdaptiveSamplingFeedback feedback,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sampling statistics for a task.
    /// </summary>
    Task<SamplingStatistics> GetStatisticsAsync(
        Guid taskId,
        DateTime? since = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies edge cases in input based on schema and patterns.
    /// </summary>
    Task<List<EdgeCasePattern>> IdentifyEdgeCasesAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates diversity metrics for sampled inputs.
    /// </summary>
    Task<DiversityMetrics> CalculateDiversityAsync(
        Guid taskId,
        IEnumerable<JsonDocument> inputs,
        CancellationToken cancellationToken = default);
}
