using System.Text.Json;
using Loopai.Core.Models;

namespace Loopai.Core.Interfaces;

/// <summary>
/// Service for improving programs based on validation feedback.
/// </summary>
public interface IProgramImprovementService
{
    /// <summary>
    /// Analyzes failed validations and generates improved program.
    /// </summary>
    /// <param name="programId">Program artifact to improve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Improvement result with new program or failure reason</returns>
    Task<ImprovementResult> ImproveFromValidationFailuresAsync(
        Guid programId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if a program should be improved based on validation metrics.
    /// </summary>
    /// <param name="programId">Program artifact ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if improvement is recommended</returns>
    Task<bool> ShouldImproveAsync(
        Guid programId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets improvement recommendations for a program.
    /// </summary>
    /// <param name="programId">Program artifact ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Improvement recommendations</returns>
    Task<ImprovementRecommendations> GetRecommendationsAsync(
        Guid programId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of program improvement attempt.
/// </summary>
public record ImprovementResult
{
    public required bool Success { get; init; }
    public Guid? NewProgramId { get; init; }
    public int? NewVersion { get; init; }
    public string? FailureReason { get; init; }
    public IReadOnlyList<string> ChangesApplied { get; init; } = Array.Empty<string>();
    public JsonDocument? Metadata { get; init; }
}

/// <summary>
/// Improvement recommendations for a program.
/// </summary>
public record ImprovementRecommendations
{
    public required bool ShouldImprove { get; init; }
    public required double ValidationRate { get; init; }
    public required int FailedValidationsCount { get; init; }
    public IReadOnlyList<string> CommonErrors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SuggestedFixes { get; init; } = Array.Empty<string>();
    public required string Confidence { get; init; } // "high", "medium", "low"
}
