using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Loopai.Core.Services;

/// <summary>
/// Implementation of program execution business logic with integrated generation and execution.
/// </summary>
public class ProgramExecutionService : IProgramExecutionService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProgramArtifactRepository _artifactRepository;
    private readonly IExecutionRecordRepository _executionRepository;
    private readonly IProgramGeneratorService? _generatorService;
    private readonly IEdgeRuntimeService? _runtimeService;
    private readonly ILogger<ProgramExecutionService> _logger;

    public ProgramExecutionService(
        ITaskRepository taskRepository,
        IProgramArtifactRepository artifactRepository,
        IExecutionRecordRepository executionRepository,
        ILogger<ProgramExecutionService> logger,
        IProgramGeneratorService? generatorService = null,
        IEdgeRuntimeService? runtimeService = null)
    {
        _taskRepository = taskRepository;
        _artifactRepository = artifactRepository;
        _executionRepository = executionRepository;
        _generatorService = generatorService;
        _runtimeService = runtimeService;
        _logger = logger;
    }

    public async Task<ExecutionResult> ExecuteAsync(
        Guid taskId,
        JsonDocument input,
        int? version = null,
        bool forceValidation = false,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Executing program for task {TaskId}, version {Version}, force_validation: {ForceValidation}",
            taskId, version, forceValidation);

        // Get task
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task == null)
        {
            throw new KeyNotFoundException($"Task with ID {taskId} not found");
        }

        // Get program artifact
        ProgramArtifact? artifact;
        if (version.HasValue)
        {
            artifact = await _artifactRepository.GetByTaskIdAndVersionAsync(taskId, version.Value, cancellationToken);
            if (artifact == null)
            {
                throw new KeyNotFoundException($"Program artifact version {version} for task {taskId} not found");
            }
        }
        else
        {
            artifact = await _artifactRepository.GetActiveByTaskIdAsync(taskId, cancellationToken);
            if (artifact == null)
            {
                // No active artifact exists - try to generate one if generator service is available
                if (_generatorService != null)
                {
                    _logger.LogInformation("No active artifact found for task {TaskId}, generating new program", taskId);
                    artifact = await GenerateAndSaveProgramAsync(taskId, cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException($"No active program artifact found for task {taskId} and no generator service configured");
                }
            }
        }

        // Determine if this execution should be sampled
        var shouldSample = forceValidation || ShouldSampleExecution(task.SamplingRate);

        // Execute the program
        var executionId = Guid.NewGuid();
        ExecutionStatus status;
        JsonDocument? output = null;
        string? errorMessage = null;

        if (_runtimeService != null)
        {
            try
            {
                var runtimeResult = await _runtimeService.ExecuteAsync(
                    artifact.Code,
                    artifact.Language,
                    input,
                    timeoutMs,
                    cancellationToken);

                if (runtimeResult.Success)
                {
                    status = ExecutionStatus.Success;
                    output = runtimeResult.Output;
                }
                else
                {
                    status = ExecutionStatus.Error;
                    errorMessage = runtimeResult.Error ?? "Unknown execution error";
                    _logger.LogWarning("Program execution failed: {Error}", errorMessage);
                }
            }
            catch (Exception ex)
            {
                status = ExecutionStatus.Error;
                errorMessage = $"Runtime error: {ex.Message}";
                _logger.LogError(ex, "Exception during program execution");
            }
        }
        else
        {
            // Fallback to placeholder if no runtime service configured
            _logger.LogWarning("No edge runtime service configured, using placeholder result");
            status = ExecutionStatus.Success;
            output = JsonDocument.Parse("{\"result\": \"placeholder\"}");
        }

        stopwatch.Stop();
        var latencyMs = stopwatch.Elapsed.TotalMilliseconds;

        // Create execution record
        var record = new ExecutionRecord
        {
            Id = executionId,
            ProgramId = artifact.Id,
            TaskId = taskId,
            InputData = input,
            OutputData = output,
            Status = status,
            LatencyMs = latencyMs,
            SampledForValidation = shouldSample,
            ExecutedAt = DateTime.UtcNow
        };

        await _executionRepository.CreateAsync(record, cancellationToken);

        _logger.LogInformation(
            "Executed program {ProgramId} for task {TaskId} in {LatencyMs}ms, status: {Status}",
            artifact.Id, taskId, latencyMs, status);

        return new ExecutionResult
        {
            ExecutionId = executionId,
            TaskId = taskId,
            ProgramId = artifact.Id,
            Version = artifact.Version,
            Status = status,
            Output = output,
            ErrorMessage = errorMessage,
            LatencyMs = latencyMs,
            SampledForValidation = shouldSample,
            ExecutedAt = record.ExecutedAt
        };
    }

    public async Task<IEnumerable<ProgramArtifact>> GetArtifactsAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting artifacts for task {TaskId}", taskId);
        return await _artifactRepository.GetByTaskIdAsync(taskId, cancellationToken);
    }

    public async Task<ProgramArtifact?> GetArtifactVersionAsync(
        Guid taskId,
        int version,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting artifact version {Version} for task {TaskId}", version, taskId);
        return await _artifactRepository.GetByTaskIdAndVersionAsync(taskId, version, cancellationToken);
    }

    public async Task<IEnumerable<ExecutionRecord>> GetExecutionHistoryAsync(
        Guid taskId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting execution history for task {TaskId}, limit: {Limit}", taskId, limit);
        return await _executionRepository.GetByTaskIdAsync(taskId, limit, cancellationToken);
    }

    public async Task<ExecutionStatistics> GetExecutionStatisticsAsync(
        Guid taskId,
        DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting execution statistics for task {TaskId}, since: {Since}", taskId, since);
        return await _executionRepository.GetStatisticsAsync(taskId, since, cancellationToken);
    }

    private static bool ShouldSampleExecution(double samplingRate)
    {
        if (samplingRate <= 0) return false;
        if (samplingRate >= 1.0) return true;

        return Random.Shared.NextDouble() < samplingRate;
    }

    private async Task<ProgramArtifact> GenerateAndSaveProgramAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        if (_generatorService == null)
        {
            throw new InvalidOperationException("Program generator service not available");
        }

        // Generate program
        var generationResult = await _generatorService.GenerateProgramAsync(
            taskId,
            examples: null,
            constraints: null,
            cancellationToken);

        if (!generationResult.Success || string.IsNullOrEmpty(generationResult.Code))
        {
            throw new InvalidOperationException(
                $"Program generation failed: {generationResult.ErrorMessage ?? "Unknown error"}");
        }

        // Get latest version for this task
        var latestVersion = await _artifactRepository.GetLatestVersionAsync(taskId, cancellationToken);
        var newVersion = latestVersion + 1;

        // Create new artifact
        var artifact = new ProgramArtifact
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            Version = newVersion,
            Code = generationResult.Code,
            Language = generationResult.Language,
            Status = ProgramStatus.Active,
            ComplexityMetrics = new ComplexityMetrics
            {
                LinesOfCode = generationResult.LinesOfCode,
                CyclomaticComplexity = generationResult.CyclomaticComplexity
            },
            DeploymentPercentage = 100.0, // Fully deployed by default
            CreatedAt = DateTime.UtcNow,
            DeployedAt = DateTime.UtcNow
        };

        await _artifactRepository.CreateAsync(artifact, cancellationToken);

        _logger.LogInformation(
            "Generated and saved program artifact {ProgramId} version {Version} for task {TaskId}",
            artifact.Id, artifact.Version, taskId);

        return artifact;
    }
}
