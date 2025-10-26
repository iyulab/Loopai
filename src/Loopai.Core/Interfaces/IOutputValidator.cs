using System.Text.Json;
using Loopai.Core.Models;

namespace Loopai.Core.Interfaces;

/// <summary>
/// Service for validating program outputs.
/// </summary>
public interface IOutputValidator
{
    /// <summary>
    /// Validates program output against expected schema and constraints.
    /// </summary>
    /// <param name="taskId">Task specification ID</param>
    /// <param name="output">Program output to validate</param>
    /// <param name="expectedOutput">Optional expected output for comparison</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<OutputValidation> ValidateOutputAsync(
        Guid taskId,
        JsonDocument output,
        JsonDocument? expectedOutput = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an execution record's output.
    /// </summary>
    /// <param name="executionId">Execution record ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<OutputValidation> ValidateExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Output validation result.
/// </summary>
public record OutputValidation
{
    public required bool IsValid { get; init; }
    public required double Score { get; init; }
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();
    public required string Method { get; init; }
}
