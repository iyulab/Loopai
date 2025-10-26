using Loopai.Core.Models;

namespace Loopai.Core.Interfaces;

/// <summary>
/// Repository interface for validation result persistence.
/// </summary>
public interface IValidationResultRepository
{
    /// <summary>
    /// Get a validation result by ID.
    /// </summary>
    Task<ValidationResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all validation results for an execution.
    /// </summary>
    Task<IEnumerable<ValidationResult>> GetByExecutionIdAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all validation results for a task.
    /// </summary>
    Task<IEnumerable<ValidationResult>> GetByTaskIdAsync(
        Guid taskId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all validation results for a program.
    /// </summary>
    Task<IEnumerable<ValidationResult>> GetByProgramIdAsync(
        Guid programId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get failed validation results for a program (for improvement).
    /// </summary>
    Task<IEnumerable<ValidationResult>> GetFailedByProgramIdAsync(
        Guid programId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new validation result.
    /// </summary>
    Task<ValidationResult> CreateAsync(
        ValidationResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create multiple validation results in batch.
    /// </summary>
    Task<IEnumerable<ValidationResult>> CreateBatchAsync(
        IEnumerable<ValidationResult> results,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get validation statistics for a program.
    /// </summary>
    Task<ValidationStatistics> GetStatisticsAsync(
        Guid programId,
        DateTime? since = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Validation statistics for a program.
/// </summary>
public record ValidationStatistics
{
    public required int TotalValidations { get; init; }
    public required int ValidCount { get; init; }
    public required int InvalidCount { get; init; }
    public required double AverageScore { get; init; }
    public required double ValidationRate { get; init; } // ValidCount / TotalValidations
}
