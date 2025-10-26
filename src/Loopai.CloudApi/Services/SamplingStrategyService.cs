using System.Text.Json;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.Extensions.Logging;

namespace Loopai.CloudApi.Services;

/// <summary>
/// Service for intelligent sampling strategy decisions.
/// </summary>
public class SamplingStrategyService : ISamplingStrategyService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IExecutionRecordRepository _executionRepository;
    private readonly ILogger<SamplingStrategyService> _logger;

    // In-memory storage for adaptive feedback (in production, use database)
    private static readonly List<AdaptiveSamplingFeedback> _feedbackHistory = new();
    private static readonly Dictionary<Guid, int> _executionCounts = new();
    private static readonly Dictionary<Guid, int> _sampledCounts = new();

    public SamplingStrategyService(
        ITaskRepository taskRepository,
        IExecutionRecordRepository executionRepository,
        ILogger<SamplingStrategyService> logger)
    {
        _taskRepository = taskRepository;
        _executionRepository = executionRepository;
        _logger = logger;
    }

    public async Task<SamplingDecision> ShouldSampleAsync(
        Guid taskId,
        JsonDocument input,
        SamplingConfiguration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        // Get task to access default sampling configuration
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task == null)
        {
            return new SamplingDecision
            {
                ShouldSample = false,
                SamplingProbability = 0.0,
                Reason = "Task not found",
                StrategyUsed = SamplingStrategyType.Random
            };
        }

        configuration ??= new SamplingConfiguration
        {
            Strategy = SamplingStrategyType.Random,
            SamplingRate = task.SamplingRate
        };

        // Increment execution count
        if (!_executionCounts.ContainsKey(taskId))
        {
            _executionCounts[taskId] = 0;
            _sampledCounts[taskId] = 0;
        }
        _executionCounts[taskId]++;

        // Apply strategy-specific logic
        var decision = configuration.Strategy switch
        {
            SamplingStrategyType.Random => ApplyRandomSampling(configuration.SamplingRate, taskId),
            SamplingStrategyType.Stratified => await ApplyStratifiedSamplingAsync(taskId, input, configuration, cancellationToken),
            SamplingStrategyType.EdgeCase => await ApplyEdgeCaseSamplingAsync(taskId, input, configuration, cancellationToken),
            SamplingStrategyType.Adaptive => await ApplyAdaptiveSamplingAsync(taskId, input, configuration, cancellationToken),
            SamplingStrategyType.DiversityBased => await ApplyDiversitySamplingAsync(taskId, input, configuration, cancellationToken),
            _ => ApplyRandomSampling(configuration.SamplingRate, taskId)
        };

        if (decision.ShouldSample)
        {
            _sampledCounts[taskId]++;
        }

        _logger.LogDebug(
            "Sampling decision for task {TaskId}: Sample={ShouldSample}, Strategy={Strategy}, Reason={Reason}",
            taskId, decision.ShouldSample, decision.StrategyUsed, decision.Reason);

        return decision;
    }

    public Task RecordFeedbackAsync(
        AdaptiveSamplingFeedback feedback,
        CancellationToken cancellationToken = default)
    {
        _feedbackHistory.Add(feedback);

        _logger.LogDebug(
            "Recorded sampling feedback for task {TaskId}: Failure={WasFailure}, Reason={Reason}",
            feedback.TaskId, feedback.WasFailure, feedback.FailureReason);

        return Task.CompletedTask;
    }

    public async Task<SamplingStatistics> GetStatisticsAsync(
        Guid taskId,
        DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        var totalExecutions = _executionCounts.GetValueOrDefault(taskId, 0);
        var sampledExecutions = _sampledCounts.GetValueOrDefault(taskId, 0);

        var actualRate = totalExecutions > 0
            ? (double)sampledExecutions / totalExecutions
            : 0.0;

        // Get recent sampled executions for diversity calculation
        var executions = await _executionRepository.GetByTaskIdAsync(taskId, limit: 100, cancellationToken);
        var sampledInputs = executions
            .Where(e => e.InputData != null)
            .Select(e => e.InputData!)
            .ToList();

        var diversity = await CalculateDiversityAsync(taskId, sampledInputs, cancellationToken);

        return new SamplingStatistics
        {
            TaskId = taskId,
            TotalExecutions = totalExecutions,
            SampledExecutions = sampledExecutions,
            ActualSamplingRate = actualRate,
            Strategy = SamplingStrategyType.Random, // Default for now
            Diversity = diversity
        };
    }

    public async Task<List<EdgeCasePattern>> IdentifyEdgeCasesAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task == null)
        {
            return new List<EdgeCasePattern>();
        }

        var patterns = new List<EdgeCasePattern>();

        // Analyze input schema to identify edge cases
        var schema = task.InputSchema.RootElement;

        if (schema.TryGetProperty("properties", out var properties))
        {
            foreach (var prop in properties.EnumerateObject())
            {
                var propName = prop.Name;
                var propSchema = prop.Value;

                if (propSchema.TryGetProperty("type", out var typeElement))
                {
                    var type = typeElement.GetString();

                    // Generate edge cases based on type
                    patterns.AddRange(GenerateEdgeCasesForType(propName, type));
                }
            }
        }

        return patterns;
    }

    public Task<DiversityMetrics> CalculateDiversityAsync(
        Guid taskId,
        IEnumerable<JsonDocument> inputs,
        CancellationToken cancellationToken = default)
    {
        var inputList = inputs.ToList();

        if (!inputList.Any())
        {
            return Task.FromResult(new DiversityMetrics
            {
                FeatureCoverage = 0.0,
                SpaceCoverage = 0.0,
                DiversityScore = 0.0,
                DistinctPatterns = 0
            });
        }

        // Calculate feature coverage: how many different properties are used
        var allProperties = new HashSet<string>();
        var propertyUsageCount = new Dictionary<string, int>();

        foreach (var input in inputList)
        {
            foreach (var prop in input.RootElement.EnumerateObject())
            {
                allProperties.Add(prop.Name);
                propertyUsageCount[prop.Name] = propertyUsageCount.GetValueOrDefault(prop.Name, 0) + 1;
            }
        }

        var featureCoverage = allProperties.Any()
            ? propertyUsageCount.Count / (double)allProperties.Count
            : 0.0;

        // Calculate distinct patterns based on JSON structure similarity
        var distinctPatterns = CalculateDistinctPatterns(inputList);

        // Space coverage: ratio of distinct patterns to total samples
        var spaceCoverage = inputList.Count > 0
            ? distinctPatterns / (double)inputList.Count
            : 0.0;

        // Diversity score: weighted combination
        var diversityScore = (featureCoverage * 0.4) + (spaceCoverage * 0.6);

        return Task.FromResult(new DiversityMetrics
        {
            FeatureCoverage = featureCoverage,
            SpaceCoverage = spaceCoverage,
            DiversityScore = diversityScore,
            DistinctPatterns = distinctPatterns
        });
    }

    private static SamplingDecision ApplyRandomSampling(double samplingRate, Guid taskId)
    {
        var random = new Random();
        var shouldSample = random.NextDouble() < samplingRate;

        return new SamplingDecision
        {
            ShouldSample = shouldSample,
            SamplingProbability = samplingRate,
            Reason = $"Random sampling with rate {samplingRate:P0}",
            StrategyUsed = SamplingStrategyType.Random
        };
    }

    private async Task<SamplingDecision> ApplyStratifiedSamplingAsync(
        Guid taskId,
        JsonDocument input,
        SamplingConfiguration configuration,
        CancellationToken cancellationToken)
    {
        // Identify partition based on input characteristics
        var partition = IdentifyPartition(input);

        // Check if this partition is under-sampled
        var shouldSample = IsPartitionUnderSampled(taskId, partition, configuration.SamplingRate);

        return new SamplingDecision
        {
            ShouldSample = shouldSample,
            SamplingProbability = configuration.SamplingRate,
            Reason = $"Stratified sampling - partition: {partition}",
            StrategyUsed = SamplingStrategyType.Stratified,
            Metadata = new Dictionary<string, object> { { "partition", partition } }
        };
    }

    private async Task<SamplingDecision> ApplyEdgeCaseSamplingAsync(
        Guid taskId,
        JsonDocument input,
        SamplingConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var edgeCases = await IdentifyEdgeCasesAsync(taskId, cancellationToken);
        var isEdgeCase = IsInputEdgeCase(input, edgeCases);

        // Always sample edge cases, apply normal rate to regular inputs
        var shouldSample = isEdgeCase || new Random().NextDouble() < configuration.SamplingRate;
        var probability = isEdgeCase ? 1.0 : configuration.SamplingRate;

        return new SamplingDecision
        {
            ShouldSample = shouldSample,
            SamplingProbability = probability,
            Reason = isEdgeCase ? "Edge case detected - always sample" : "Regular input - random sampling",
            StrategyUsed = SamplingStrategyType.EdgeCase,
            Metadata = new Dictionary<string, object> { { "is_edge_case", isEdgeCase } }
        };
    }

    private async Task<SamplingDecision> ApplyAdaptiveSamplingAsync(
        Guid taskId,
        JsonDocument input,
        SamplingConfiguration configuration,
        CancellationToken cancellationToken)
    {
        // Get recent feedback for this task
        var recentFeedback = _feedbackHistory
            .Where(f => f.TaskId == taskId)
            .OrderByDescending(f => f.RecordedAt)
            .Take(50)
            .ToList();

        if (!recentFeedback.Any())
        {
            // No feedback yet, use random sampling
            return ApplyRandomSampling(configuration.SamplingRate, taskId);
        }

        // Calculate failure rate
        var failureRate = recentFeedback.Count(f => f.WasFailure) / (double)recentFeedback.Count;

        // Check if input is similar to recent failures
        var similarToFailures = IsSimilarToFailures(input, recentFeedback);

        // Increase sampling rate for inputs similar to failures
        var adjustedRate = similarToFailures
            ? Math.Min(1.0, configuration.SamplingRate * 2.0) // Double the rate
            : configuration.SamplingRate;

        var shouldSample = new Random().NextDouble() < adjustedRate;

        return new SamplingDecision
        {
            ShouldSample = shouldSample,
            SamplingProbability = adjustedRate,
            Reason = similarToFailures
                ? $"Similar to failure patterns - increased rate to {adjustedRate:P0}"
                : $"Normal adaptive sampling - rate {adjustedRate:P0}",
            StrategyUsed = SamplingStrategyType.Adaptive,
            Metadata = new Dictionary<string, object>
            {
                { "failure_rate", failureRate },
                { "similar_to_failures", similarToFailures }
            }
        };
    }

    private async Task<SamplingDecision> ApplyDiversitySamplingAsync(
        Guid taskId,
        JsonDocument input,
        SamplingConfiguration configuration,
        CancellationToken cancellationToken)
    {
        // Get recent sampled inputs
        var executions = await _executionRepository.GetByTaskIdAsync(taskId, limit: 50, cancellationToken);
        var recentInputs = executions
            .Where(e => e.InputData != null)
            .Select(e => e.InputData!)
            .ToList();

        if (!recentInputs.Any())
        {
            // No history, always sample for diversity
            return new SamplingDecision
            {
                ShouldSample = true,
                SamplingProbability = 1.0,
                Reason = "No sampling history - building diversity",
                StrategyUsed = SamplingStrategyType.DiversityBased
            };
        }

        // Check if input is sufficiently different from recent samples
        var isDiverse = IsDifferentFromRecent(input, recentInputs);

        // Higher probability for diverse inputs
        var adjustedRate = isDiverse
            ? Math.Min(1.0, configuration.SamplingRate * 1.5)
            : configuration.SamplingRate * 0.5;

        var shouldSample = new Random().NextDouble() < adjustedRate;

        return new SamplingDecision
        {
            ShouldSample = shouldSample,
            SamplingProbability = adjustedRate,
            Reason = isDiverse
                ? "Diverse input detected - increased sampling"
                : "Similar to recent samples - reduced sampling",
            StrategyUsed = SamplingStrategyType.DiversityBased,
            Metadata = new Dictionary<string, object> { { "is_diverse", isDiverse } }
        };
    }

    private static string IdentifyPartition(JsonDocument input)
    {
        // Simple partitioning based on input structure
        var propCount = input.RootElement.EnumerateObject().Count();

        return propCount switch
        {
            0 => "empty",
            1 => "simple",
            <= 5 => "moderate",
            _ => "complex"
        };
    }

    private static bool IsPartitionUnderSampled(Guid taskId, string partition, double targetRate)
    {
        // Simplified: in production, track per-partition sampling rates
        return new Random().NextDouble() < targetRate;
    }

    private static bool IsInputEdgeCase(JsonDocument input, List<EdgeCasePattern> edgeCases)
    {
        // Check for common edge case patterns
        foreach (var prop in input.RootElement.EnumerateObject())
        {
            var value = prop.Value;

            // Null or undefined
            if (value.ValueKind == JsonValueKind.Null)
            {
                return true;
            }

            // Empty strings
            if (value.ValueKind == JsonValueKind.String && value.GetString() == "")
            {
                return true;
            }

            // Empty arrays
            if (value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0)
            {
                return true;
            }

            // Extreme numbers
            if (value.ValueKind == JsonValueKind.Number)
            {
                var num = value.GetDouble();
                if (num == 0 || Math.Abs(num) > 1_000_000 || Math.Abs(num) < 0.0001)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsSimilarToFailures(JsonDocument input, List<AdaptiveSamplingFeedback> feedback)
    {
        var failures = feedback.Where(f => f.WasFailure).ToList();
        if (!failures.Any())
        {
            return false;
        }

        // Check if input has similar structure to any failure
        var inputProps = input.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();

        foreach (var failure in failures.Take(10))
        {
            var failureProps = failure.Input.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();
            var overlap = inputProps.Intersect(failureProps).Count();
            var similarity = inputProps.Any() ? overlap / (double)inputProps.Count : 0;

            if (similarity > 0.7) // 70% property overlap
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDifferentFromRecent(JsonDocument input, List<JsonDocument> recentInputs)
    {
        if (!recentInputs.Any())
        {
            return true;
        }

        var inputProps = input.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();

        // Check similarity with recent inputs
        foreach (var recent in recentInputs.Take(10))
        {
            var recentProps = recent.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();
            var overlap = inputProps.Intersect(recentProps).Count();
            var similarity = inputProps.Any() ? overlap / (double)inputProps.Count : 0;

            if (similarity > 0.8) // Very similar to a recent input
            {
                return false;
            }
        }

        return true; // Sufficiently different
    }

    private static List<EdgeCasePattern> GenerateEdgeCasesForType(string propName, string? type)
    {
        var patterns = new List<EdgeCasePattern>();

        switch (type)
        {
            case "string":
                patterns.Add(new EdgeCasePattern
                {
                    PatternName = $"{propName}_empty",
                    Description = "Empty string",
                    ExampleInput = JsonDocument.Parse($"{{\"{propName}\": \"\"}}"),
                    Category = "empty"
                });
                patterns.Add(new EdgeCasePattern
                {
                    PatternName = $"{propName}_whitespace",
                    Description = "Whitespace-only string",
                    ExampleInput = JsonDocument.Parse($"{{\"{propName}\": \"   \"}}"),
                    Category = "special"
                });
                break;

            case "number":
            case "integer":
                patterns.Add(new EdgeCasePattern
                {
                    PatternName = $"{propName}_zero",
                    Description = "Zero value",
                    ExampleInput = JsonDocument.Parse($"{{\"{propName}\": 0}}"),
                    Category = "boundary"
                });
                patterns.Add(new EdgeCasePattern
                {
                    PatternName = $"{propName}_negative",
                    Description = "Negative number",
                    ExampleInput = JsonDocument.Parse($"{{\"{propName}\": -1}}"),
                    Category = "boundary"
                });
                patterns.Add(new EdgeCasePattern
                {
                    PatternName = $"{propName}_large",
                    Description = "Large number",
                    ExampleInput = JsonDocument.Parse($"{{\"{propName}\": 999999999}}"),
                    Category = "extreme"
                });
                break;

            case "array":
                patterns.Add(new EdgeCasePattern
                {
                    PatternName = $"{propName}_empty_array",
                    Description = "Empty array",
                    ExampleInput = JsonDocument.Parse($"{{\"{propName}\": []}}"),
                    Category = "empty"
                });
                break;

            case "object":
                patterns.Add(new EdgeCasePattern
                {
                    PatternName = $"{propName}_empty_object",
                    Description = "Empty object",
                    ExampleInput = JsonDocument.Parse($"{{\"{propName}\": {{}}}}"),
                    Category = "empty"
                });
                break;
        }

        // Null case for all types
        patterns.Add(new EdgeCasePattern
        {
            PatternName = $"{propName}_null",
            Description = "Null value",
            ExampleInput = JsonDocument.Parse($"{{\"{propName}\": null}}"),
            Category = "null"
        });

        return patterns;
    }

    private static int CalculateDistinctPatterns(List<JsonDocument> inputs)
    {
        if (!inputs.Any())
        {
            return 0;
        }

        // Group inputs by structural similarity (property names and types)
        var patterns = new HashSet<string>();

        foreach (var input in inputs)
        {
            var signature = GetInputSignature(input);
            patterns.Add(signature);
        }

        return patterns.Count;
    }

    private static string GetInputSignature(JsonDocument input)
    {
        // Create a signature based on property names and types
        var props = input.RootElement.EnumerateObject()
            .Select(p => $"{p.Name}:{p.Value.ValueKind}")
            .OrderBy(s => s);

        return string.Join("|", props);
    }
}
