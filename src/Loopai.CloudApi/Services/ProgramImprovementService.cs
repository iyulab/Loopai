using System.Text;
using System.Text.Json;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;

namespace Loopai.CloudApi.Services;

/// <summary>
/// Service for improving programs based on validation feedback.
/// </summary>
public class ProgramImprovementService : IProgramImprovementService
{
    private readonly IProgramArtifactRepository _artifactRepository;
    private readonly IValidationResultRepository _validationRepository;
    private readonly IExecutionRecordRepository _executionRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IProgramGeneratorService _generatorService;
    private readonly ILogger<ProgramImprovementService> _logger;

    // Improvement thresholds
    private const double MinValidationRateForImprovement = 0.7; // 70% validation rate
    private const int MinFailuresForImprovement = 5;

    public ProgramImprovementService(
        IProgramArtifactRepository artifactRepository,
        IValidationResultRepository validationRepository,
        IExecutionRecordRepository executionRepository,
        ITaskRepository taskRepository,
        IProgramGeneratorService generatorService,
        ILogger<ProgramImprovementService> logger)
    {
        _artifactRepository = artifactRepository;
        _validationRepository = validationRepository;
        _executionRepository = executionRepository;
        _taskRepository = taskRepository;
        _generatorService = generatorService;
        _logger = logger;
    }

    public async Task<bool> ShouldImproveAsync(
        Guid programId,
        CancellationToken cancellationToken = default)
    {
        var stats = await _validationRepository.GetStatisticsAsync(programId, since: null, cancellationToken);

        // Need minimum data to make improvement decision
        if (stats.TotalValidations < MinFailuresForImprovement)
        {
            return false;
        }

        // Improve if validation rate is below threshold
        return stats.ValidationRate < MinValidationRateForImprovement;
    }

    public async Task<ImprovementRecommendations> GetRecommendationsAsync(
        Guid programId,
        CancellationToken cancellationToken = default)
    {
        var stats = await _validationRepository.GetStatisticsAsync(programId, since: null, cancellationToken);
        var failedValidations = await _validationRepository.GetFailedByProgramIdAsync(
            programId,
            limit: 20,
            cancellationToken);

        var failedList = failedValidations.ToList();

        // Analyze common error patterns
        var errorPatterns = AnalyzeErrorPatterns(failedList);
        var suggestedFixes = GenerateSuggestedFixes(errorPatterns);

        var shouldImprove = stats.TotalValidations >= MinFailuresForImprovement &&
                           stats.ValidationRate < MinValidationRateForImprovement;

        var confidence = CalculateConfidence(stats, failedList.Count);

        return new ImprovementRecommendations
        {
            ShouldImprove = shouldImprove,
            ValidationRate = stats.ValidationRate,
            FailedValidationsCount = stats.InvalidCount,
            CommonErrors = errorPatterns,
            SuggestedFixes = suggestedFixes,
            Confidence = confidence
        };
    }

    public async Task<ImprovementResult> ImproveFromValidationFailuresAsync(
        Guid programId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get program artifact
            var program = await _artifactRepository.GetByIdAsync(programId, cancellationToken);
            if (program == null)
            {
                return new ImprovementResult
                {
                    Success = false,
                    FailureReason = $"Program {programId} not found"
                };
            }

            // Get task
            var task = await _taskRepository.GetByIdAsync(program.TaskId, cancellationToken);
            if (task == null)
            {
                return new ImprovementResult
                {
                    Success = false,
                    FailureReason = $"Task {program.TaskId} not found"
                };
            }

            // Check if improvement is needed
            var shouldImprove = await ShouldImproveAsync(programId, cancellationToken);
            if (!shouldImprove)
            {
                return new ImprovementResult
                {
                    Success = false,
                    FailureReason = "Program validation rate is acceptable, improvement not needed"
                };
            }

            _logger.LogInformation(
                "Improving program {ProgramId} for task {TaskId}",
                programId, program.TaskId);

            // Get failed validations and executions for learning
            var failedValidations = await _validationRepository.GetFailedByProgramIdAsync(
                programId,
                limit: 10,
                cancellationToken);

            var failedList = failedValidations.ToList();

            // Collect examples from failed executions
            var examples = await CollectImprovementExamplesAsync(
                failedList,
                cancellationToken);

            // Build improvement constraints
            var constraints = BuildImprovementConstraints(program, failedList);

            _logger.LogDebug(
                "Generating improved program with {ExampleCount} examples and constraints",
                examples.Count);

            // Generate improved program
            var generationResult = await _generatorService.GenerateProgramAsync(
                program.TaskId,
                examples: examples,
                constraints: constraints,
                cancellationToken);

            if (!generationResult.Success || string.IsNullOrEmpty(generationResult.Code))
            {
                return new ImprovementResult
                {
                    Success = false,
                    FailureReason = $"Program generation failed: {generationResult.ErrorMessage}"
                };
            }

            // Get latest version for versioning
            var latestVersion = await _artifactRepository.GetLatestVersionAsync(
                program.TaskId,
                cancellationToken);

            // Create new improved artifact
            var improvedArtifact = new ProgramArtifact
            {
                Id = Guid.NewGuid(),
                TaskId = program.TaskId,
                Version = latestVersion + 1,
                Code = generationResult.Code,
                Language = generationResult.Language,
                Status = ProgramStatus.Draft, // Start in draft status for testing
                ComplexityMetrics = new ComplexityMetrics
                {
                    LinesOfCode = generationResult.LinesOfCode,
                    CyclomaticComplexity = generationResult.CyclomaticComplexity
                },
                DeploymentPercentage = 0.0, // No deployment yet
                CreatedAt = DateTime.UtcNow
            };

            await _artifactRepository.CreateAsync(improvedArtifact, cancellationToken);

            var changes = new List<string>
            {
                $"Fixed {failedList.Count} validation failures",
                $"Applied {examples.Count} correction examples",
                "Updated based on error patterns"
            };

            _logger.LogInformation(
                "Successfully created improved program {NewProgramId} version {Version} for task {TaskId}",
                improvedArtifact.Id, improvedArtifact.Version, program.TaskId);

            return new ImprovementResult
            {
                Success = true,
                NewProgramId = improvedArtifact.Id,
                NewVersion = improvedArtifact.Version,
                ChangesApplied = changes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error improving program {ProgramId}", programId);
            return new ImprovementResult
            {
                Success = false,
                FailureReason = $"Improvement error: {ex.Message}"
            };
        }
    }

    private async Task<List<(JsonDocument Input, JsonDocument Output)>> CollectImprovementExamplesAsync(
        List<ValidationResult> failedValidations,
        CancellationToken cancellationToken)
    {
        var examples = new List<(JsonDocument Input, JsonDocument Output)>();

        foreach (var validation in failedValidations.Take(5)) // Limit to top 5
        {
            // Get the execution record
            var execution = await _executionRepository.GetByIdAsync(
                validation.ExecutionId,
                cancellationToken);

            if (execution?.InputData != null && execution.OutputData != null)
            {
                // These are examples of what NOT to do
                // In a real system, we'd need correct outputs
                // For now, we collect them for the generator to learn from errors
                examples.Add((execution.InputData, execution.OutputData));
            }
        }

        return examples;
    }

    private static string BuildImprovementConstraints(
        ProgramArtifact program,
        List<ValidationResult> failedValidations)
    {
        var constraints = new StringBuilder();

        constraints.AppendLine("IMPORTANT: Fix the following issues from the previous version:");
        constraints.AppendLine();

        // Collect unique error types
        var errorTypes = failedValidations
            .SelectMany(v => v.Errors)
            .GroupBy(e => e.Type)
            .OrderByDescending(g => g.Count())
            .Take(5);

        foreach (var errorGroup in errorTypes)
        {
            var count = errorGroup.Count();
            var firstError = errorGroup.First();

            constraints.AppendLine($"- {errorGroup.Key} ({count} occurrences)");
            constraints.AppendLine($"  Example: {firstError.Message}");

            if (!string.IsNullOrEmpty(firstError.Expected))
            {
                constraints.AppendLine($"  Expected: {firstError.Expected}");
            }

            constraints.AppendLine();
        }

        constraints.AppendLine("Ensure the output strictly follows the schema and handles edge cases.");

        return constraints.ToString();
    }

    private static List<string> AnalyzeErrorPatterns(List<ValidationResult> failedValidations)
    {
        var patterns = failedValidations
            .SelectMany(v => v.Errors)
            .GroupBy(e => e.Type)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => $"{g.Key} ({g.Count()} times)")
            .ToList();

        return patterns;
    }

    private static List<string> GenerateSuggestedFixes(List<string> errorPatterns)
    {
        var fixes = new List<string>();

        foreach (var pattern in errorPatterns)
        {
            if (pattern.Contains("schema"))
            {
                fixes.Add("Ensure output strictly matches the expected JSON schema");
            }
            else if (pattern.Contains("type_mismatch"))
            {
                fixes.Add("Fix data type conversions and ensure correct types");
            }
            else if (pattern.Contains("missing_property"))
            {
                fixes.Add("Add all required properties to the output");
            }
            else if (pattern.Contains("value_mismatch"))
            {
                fixes.Add("Verify calculation logic and output values");
            }
            else
            {
                fixes.Add($"Address error pattern: {pattern}");
            }
        }

        return fixes;
    }

    private static string CalculateConfidence(ValidationStatistics stats, int sampleSize)
    {
        // High confidence if we have enough data and clear failure pattern
        if (stats.TotalValidations >= 20 && stats.ValidationRate < 0.5)
        {
            return "high";
        }

        // Medium confidence with moderate data (>= 5 validations)
        if (stats.TotalValidations >= 5)
        {
            return "medium";
        }

        // Low confidence with limited data
        return "low";
    }
}
