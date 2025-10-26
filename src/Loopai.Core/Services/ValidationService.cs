using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.Extensions.Logging;

namespace Loopai.Core.Services;

/// <summary>
/// Service for coordinating validation and improvement workflows.
/// </summary>
public class ValidationService
{
    private readonly IValidationResultRepository _validationRepository;
    private readonly IOutputValidator _outputValidator;
    private readonly IProgramImprovementService _improvementService;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(
        IValidationResultRepository validationRepository,
        IOutputValidator outputValidator,
        IProgramImprovementService improvementService,
        ILogger<ValidationService> logger)
    {
        _validationRepository = validationRepository;
        _outputValidator = outputValidator;
        _improvementService = improvementService;
        _logger = logger;
    }

    /// <summary>
    /// Validates an execution and optionally triggers improvement if needed.
    /// </summary>
    public async Task<ValidationResult> ValidateAndRecordAsync(
        Guid executionId,
        Guid taskId,
        Guid programId,
        bool triggerImprovementIfNeeded = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating execution {ExecutionId}", executionId);

        // Perform validation
        var validation = await _outputValidator.ValidateExecutionAsync(executionId, cancellationToken);

        // Create validation result record
        var validationResult = new ValidationResult
        {
            ExecutionId = executionId,
            TaskId = taskId,
            ProgramId = programId,
            IsValid = validation.IsValid,
            ValidationScore = validation.Score,
            Errors = validation.Errors,
            ValidationMethod = validation.Method,
            ValidatedAt = DateTime.UtcNow
        };

        // Save validation result
        await _validationRepository.CreateAsync(validationResult, cancellationToken);

        _logger.LogInformation(
            "Validation completed for execution {ExecutionId}: Valid={IsValid}, Score={Score}",
            executionId, validation.IsValid, validation.Score);

        // Check if improvement should be triggered
        if (triggerImprovementIfNeeded && !validation.IsValid)
        {
            var shouldImprove = await _improvementService.ShouldImproveAsync(programId, cancellationToken);

            if (shouldImprove)
            {
                _logger.LogInformation(
                    "Triggering program improvement for {ProgramId} due to validation failures",
                    programId);

                // Trigger improvement asynchronously (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var result = await _improvementService.ImproveFromValidationFailuresAsync(
                            programId,
                            CancellationToken.None);

                        if (result.Success)
                        {
                            _logger.LogInformation(
                                "Successfully created improved program {NewProgramId} version {NewVersion}",
                                result.NewProgramId, result.NewVersion);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Program improvement failed: {Reason}",
                                result.FailureReason);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in background improvement task");
                    }
                }, CancellationToken.None);
            }
        }

        return validationResult;
    }

    /// <summary>
    /// Validates multiple sampled executions in batch.
    /// </summary>
    public async Task<IEnumerable<ValidationResult>> ValidateBatchAsync(
        IEnumerable<ExecutionRecord> executions,
        bool triggerImprovementIfNeeded = false,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ValidationResult>();

        foreach (var execution in executions)
        {
            if (execution.Status == ExecutionStatus.Success && execution.OutputData != null)
            {
                var result = await ValidateAndRecordAsync(
                    execution.Id,
                    execution.TaskId,
                    execution.ProgramId,
                    triggerImprovementIfNeeded,
                    cancellationToken);

                results.Add(result);
            }
        }

        return results;
    }

    /// <summary>
    /// Gets validation summary for a program.
    /// </summary>
    public async Task<ValidationSummary> GetValidationSummaryAsync(
        Guid programId,
        CancellationToken cancellationToken = default)
    {
        var stats = await _validationRepository.GetStatisticsAsync(programId, since: null, cancellationToken);
        var recommendations = await _improvementService.GetRecommendationsAsync(programId, cancellationToken);

        return new ValidationSummary
        {
            ProgramId = programId,
            Statistics = stats,
            Recommendations = recommendations
        };
    }
}

/// <summary>
/// Summary of validation status and recommendations.
/// </summary>
public record ValidationSummary
{
    public required Guid ProgramId { get; init; }
    public required ValidationStatistics Statistics { get; init; }
    public required ImprovementRecommendations Recommendations { get; init; }
}
