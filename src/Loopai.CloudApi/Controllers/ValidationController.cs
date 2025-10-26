using Loopai.CloudApi.DTOs;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Loopai.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Loopai.CloudApi.Controllers;

/// <summary>
/// API endpoints for validation and program improvement.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/validation")]
[Asp.Versioning.ApiVersion("1.0")]
[Produces("application/json")]
public class ValidationController : ControllerBase
{
    private readonly ValidationService _validationService;
    private readonly IProgramImprovementService _improvementService;
    private readonly IABTestingService _abTestingService;
    private readonly ILogger<ValidationController> _logger;

    public ValidationController(
        ValidationService validationService,
        IProgramImprovementService improvementService,
        IABTestingService abTestingService,
        ILogger<ValidationController> logger)
    {
        _validationService = validationService;
        _improvementService = improvementService;
        _abTestingService = abTestingService;
        _logger = logger;
    }

    /// <summary>
    /// Get validation summary for a program.
    /// </summary>
    [HttpGet("programs/{programId}/summary")]
    [ProducesResponseType(typeof(ValidationSummaryResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetValidationSummary(Guid programId)
    {
        try
        {
            var summary = await _validationService.GetValidationSummaryAsync(programId);

            var response = new ValidationSummaryResponse
            {
                ProgramId = summary.ProgramId,
                Statistics = new ValidationStatisticsDto
                {
                    TotalValidations = summary.Statistics.TotalValidations,
                    ValidCount = summary.Statistics.ValidCount,
                    InvalidCount = summary.Statistics.InvalidCount,
                    AverageScore = summary.Statistics.AverageScore,
                    ValidationRate = summary.Statistics.ValidationRate
                },
                Recommendations = new ImprovementRecommendationsDto
                {
                    ShouldImprove = summary.Recommendations.ShouldImprove,
                    ValidationRate = summary.Recommendations.ValidationRate,
                    FailedValidationsCount = summary.Recommendations.FailedValidationsCount,
                    CommonErrors = summary.Recommendations.CommonErrors.ToList(),
                    SuggestedFixes = summary.Recommendations.SuggestedFixes.ToList(),
                    Confidence = summary.Recommendations.Confidence
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting validation summary for program {ProgramId}", programId);
            return NotFound(new ErrorResponse
            {
                Code = "PROGRAM_NOT_FOUND",
                Message = $"Program {programId} not found or error retrieving validation summary"
            });
        }
    }

    /// <summary>
    /// Trigger program improvement based on validation failures.
    /// </summary>
    [HttpPost("programs/{programId}/improve")]
    [ProducesResponseType(typeof(ImprovementResultResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> ImproveProgram(Guid programId)
    {
        try
        {
            _logger.LogInformation("Triggering improvement for program {ProgramId}", programId);

            var result = await _improvementService.ImproveFromValidationFailuresAsync(programId);

            if (!result.Success)
            {
                return BadRequest(new ErrorResponse
                {
                    Code = "IMPROVEMENT_FAILED",
                    Message = result.FailureReason ?? "Unknown error"
                });
            }

            var response = new ImprovementResultResponse
            {
                Success = result.Success,
                NewProgramId = result.NewProgramId,
                NewVersion = result.NewVersion,
                ChangesApplied = result.ChangesApplied.ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error improving program {ProgramId}", programId);
            return BadRequest(new ErrorResponse
            {
                Code = "IMPROVEMENT_ERROR",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Get improvement recommendations for a program.
    /// </summary>
    [HttpGet("programs/{programId}/recommendations")]
    [ProducesResponseType(typeof(ImprovementRecommendationsDto), 200)]
    public async Task<IActionResult> GetRecommendations(Guid programId)
    {
        try
        {
            var recommendations = await _improvementService.GetRecommendationsAsync(programId);

            var response = new ImprovementRecommendationsDto
            {
                ShouldImprove = recommendations.ShouldImprove,
                ValidationRate = recommendations.ValidationRate,
                FailedValidationsCount = recommendations.FailedValidationsCount,
                CommonErrors = recommendations.CommonErrors.ToList(),
                SuggestedFixes = recommendations.SuggestedFixes.ToList(),
                Confidence = recommendations.Confidence
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for program {ProgramId}", programId);
            return Ok(new ImprovementRecommendationsDto
            {
                ShouldImprove = false,
                ValidationRate = 0,
                FailedValidationsCount = 0,
                CommonErrors = new List<string>(),
                SuggestedFixes = new List<string>(),
                Confidence = "low"
            });
        }
    }

    /// <summary>
    /// Compare two program versions using A/B testing.
    /// </summary>
    [HttpPost("programs/compare")]
    [ProducesResponseType(typeof(ABTestResultResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> ComparePrograms([FromBody] CompareProgramsRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Comparing programs: Control={ControlId}, Treatment={TreatmentId}",
                request.ControlProgramId, request.TreatmentProgramId);

            var configuration = request.Configuration != null
                ? new ABTestConfiguration
                {
                    MinimumSampleSize = request.Configuration.MinimumSampleSize,
                    RequiredConfidence = request.Configuration.RequiredConfidence,
                    MaxDegradationThreshold = request.Configuration.MaxDegradationThreshold,
                    MinImprovementThreshold = request.Configuration.MinImprovementThreshold,
                    TestDurationHours = request.Configuration.TestDurationHours
                }
                : null;

            var result = await _abTestingService.CompareVersionsAsync(
                request.ControlProgramId,
                request.TreatmentProgramId,
                configuration);

            var response = new ABTestResultResponse
            {
                TaskId = result.TaskId,
                ControlProgramId = result.ControlProgramId,
                TreatmentProgramId = result.TreatmentProgramId,
                Statistics = new ComparisonStatisticsDto
                {
                    ControlMetrics = MapMetrics(result.Statistics.ControlMetrics),
                    TreatmentMetrics = MapMetrics(result.Statistics.TreatmentMetrics),
                    PerformanceDelta = result.Statistics.PerformanceDelta,
                    PValue = result.Statistics.PValue,
                    IsSignificant = result.Statistics.IsSignificant,
                    ControlSampleSize = result.Statistics.ControlSampleSize,
                    TreatmentSampleSize = result.Statistics.TreatmentSampleSize
                },
                Recommendation = result.Recommendation,
                Confidence = result.Confidence,
                ComparedAt = result.ComparedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing programs");
            return BadRequest(new ErrorResponse
            {
                Code = "COMPARISON_FAILED",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Start a canary deployment for a new program version.
    /// </summary>
    [HttpPost("programs/canary/start")]
    [ProducesResponseType(typeof(CanaryDeploymentResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> StartCanaryDeployment([FromBody] StartCanaryRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Starting canary deployment: Task={TaskId}, NewProgram={NewProgramId}",
                request.TaskId, request.NewProgramId);

            var canary = await _abTestingService.StartCanaryDeploymentAsync(
                request.TaskId,
                request.NewProgramId);

            var response = MapCanaryDeployment(canary);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "CANARY_START_FAILED",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting canary deployment");
            return BadRequest(new ErrorResponse
            {
                Code = "CANARY_START_ERROR",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Progress a canary deployment to the next stage.
    /// </summary>
    [HttpPost("programs/canary/{canaryId}/progress")]
    [ProducesResponseType(typeof(CanaryDeploymentResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> ProgressCanary(Guid canaryId)
    {
        try
        {
            _logger.LogInformation("Progressing canary deployment {CanaryId}", canaryId);

            var canary = await _abTestingService.ProgressCanaryAsync(canaryId);

            var response = MapCanaryDeployment(canary);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "CANARY_PROGRESS_FAILED",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error progressing canary deployment {CanaryId}", canaryId);
            return BadRequest(new ErrorResponse
            {
                Code = "CANARY_PROGRESS_ERROR",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Rollback a canary deployment.
    /// </summary>
    [HttpPost("programs/canary/{canaryId}/rollback")]
    [ProducesResponseType(typeof(CanaryDeploymentResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> RollbackCanary(Guid canaryId, [FromBody] RollbackCanaryRequest request)
    {
        try
        {
            _logger.LogWarning(
                "Rolling back canary deployment {CanaryId}: {Reason}",
                canaryId, request.Reason);

            var canary = await _abTestingService.RollbackCanaryAsync(canaryId, request.Reason);

            var response = MapCanaryDeployment(canary);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back canary deployment {CanaryId}", canaryId);
            return BadRequest(new ErrorResponse
            {
                Code = "CANARY_ROLLBACK_ERROR",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Get active canary deployment for a task.
    /// </summary>
    [HttpGet("tasks/{taskId}/canary")]
    [ProducesResponseType(typeof(CanaryDeploymentResponse), 200)]
    [ProducesResponseType(204)]
    public async Task<IActionResult> GetActiveCanary(Guid taskId)
    {
        try
        {
            var canary = await _abTestingService.GetActiveCanaryAsync(taskId);

            if (canary == null)
            {
                return NoContent();
            }

            var response = MapCanaryDeployment(canary);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active canary for task {TaskId}", taskId);
            return NoContent();
        }
    }

    private static ProgramMetricsDto MapMetrics(ProgramMetrics metrics)
    {
        return new ProgramMetricsDto
        {
            AverageLatencyMs = metrics.AverageLatencyMs,
            P50LatencyMs = metrics.P50LatencyMs,
            P95LatencyMs = metrics.P95LatencyMs,
            P99LatencyMs = metrics.P99LatencyMs,
            ValidationRate = metrics.ValidationRate,
            ErrorRate = metrics.ErrorRate,
            TotalExecutions = metrics.TotalExecutions,
            SuccessfulExecutions = metrics.SuccessfulExecutions,
            FailedExecutions = metrics.FailedExecutions
        };
    }

    private static CanaryDeploymentResponse MapCanaryDeployment(CanaryDeployment canary)
    {
        return new CanaryDeploymentResponse
        {
            Id = canary.Id,
            TaskId = canary.TaskId,
            NewProgramId = canary.NewProgramId,
            CurrentProgramId = canary.CurrentProgramId,
            CurrentStage = canary.CurrentStage.ToString(),
            CurrentPercentage = canary.CurrentPercentage,
            Status = canary.Status.ToString(),
            StatusReason = canary.StatusReason,
            History = canary.History.Select(h => new CanaryStageHistoryDto
            {
                Stage = h.Stage.ToString(),
                Percentage = h.Percentage,
                Action = h.Action,
                Reason = h.Reason,
                Timestamp = h.Timestamp
            }).ToList(),
            StartedAt = canary.StartedAt,
            CompletedAt = canary.CompletedAt
        };
    }
}
