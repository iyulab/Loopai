using Loopai.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace Loopai.Core.CodeBeaker;

/// <summary>
/// Batch executor optimized for CodeBeaker session pooling.
/// Distributes batch items across available sessions for parallel execution.
/// </summary>
public class CodeBeakerBatchExecutor
{
    private readonly CodeBeakerSessionPool _sessionPool;
    private readonly IProgramArtifactRepository _artifactRepository;
    private readonly IExecutionRecordRepository _executionRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ILogger<CodeBeakerBatchExecutor> _logger;

    public CodeBeakerBatchExecutor(
        CodeBeakerSessionPool sessionPool,
        IProgramArtifactRepository artifactRepository,
        IExecutionRecordRepository executionRepository,
        ITaskRepository taskRepository,
        ILogger<CodeBeakerBatchExecutor> logger)
    {
        _sessionPool = sessionPool;
        _artifactRepository = artifactRepository;
        _executionRepository = executionRepository;
        _taskRepository = taskRepository;
        _logger = logger;
    }

    /// <summary>
    /// Execute batch items using CodeBeaker session pool.
    /// </summary>
    public async Task<BatchExecutionResult> ExecuteBatchAsync(
        Guid taskId,
        IEnumerable<BatchItem> items,
        int? version = null,
        int maxConcurrency = 10,
        bool stopOnFirstError = false,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        var batchId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting CodeBeaker batch execution {BatchId} for task {TaskId} with {ItemCount} items, concurrency: {MaxConcurrency}",
            batchId, taskId, items.Count(), maxConcurrency);

        try
        {
            // Get task and artifact
            var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
            {
                throw new KeyNotFoundException($"Task {taskId} not found");
            }

            var artifact = version.HasValue
                ? await _artifactRepository.GetByTaskIdAndVersionAsync(taskId, version.Value, cancellationToken)
                : await _artifactRepository.GetActiveByTaskIdAsync(taskId, cancellationToken);

            if (artifact == null)
            {
                throw new InvalidOperationException($"No program artifact found for task {taskId}");
            }

            // Execute batch items with session pooling
            var results = await ExecuteBatchItemsWithPoolingAsync(
                items,
                artifact,
                task.SamplingRate,
                maxConcurrency,
                stopOnFirstError,
                timeoutMs,
                cancellationToken);

            stopwatch.Stop();

            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count - successCount;
            var avgLatency = results.Any() ? results.Average(r => r.LatencyMs) : 0;

            _logger.LogInformation(
                "CodeBeaker batch {BatchId} completed: {SuccessCount}/{TotalCount} succeeded in {TotalMs}ms (avg: {AvgMs}ms)",
                batchId, successCount, results.Count, stopwatch.ElapsedMilliseconds, avgLatency);

            // Get session pool statistics
            var poolStats = _sessionPool.GetStatistics();
            _logger.LogDebug(
                "Session pool stats: Total={Total}, Active={Active}, Idle={Idle}, Available={Available}",
                poolStats.TotalSessions, poolStats.ActiveSessions, poolStats.IdleSessions, poolStats.AvailableSlots);

            return new BatchExecutionResult
            {
                BatchId = batchId,
                TaskId = taskId,
                ProgramId = artifact.Id,
                Version = artifact.Version,
                TotalItems = results.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                TotalDurationMs = stopwatch.Elapsed.TotalMilliseconds,
                AvgLatencyMs = avgLatency,
                Items = results,
                StartedAt = startTime,
                CompletedAt = DateTime.UtcNow,
                SessionPoolStats = new SessionPoolStatsSnapshot
                {
                    TotalSessions = poolStats.TotalSessions,
                    ActiveSessions = poolStats.ActiveSessions,
                    IdleSessions = poolStats.IdleSessions,
                    AvailableSlots = poolStats.AvailableSlots,
                    AverageExecutionCount = poolStats.AverageExecutionCount
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "CodeBeaker batch {BatchId} failed after {Ms}ms", batchId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task<List<BatchItemResult>> ExecuteBatchItemsWithPoolingAsync(
        IEnumerable<BatchItem> items,
        Core.Models.ProgramArtifact artifact,
        double samplingRate,
        int maxConcurrency,
        bool stopOnFirstError,
        int? timeoutMs,
        CancellationToken cancellationToken)
    {
        var results = new ConcurrentBag<BatchItemResult>();
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = new List<Task>();
        var hasError = false;

        foreach (var item in items)
        {
            if (stopOnFirstError && hasError)
            {
                _logger.LogWarning("Stopping batch due to error (StopOnFirstError=true)");
                break;
            }

            await semaphore.WaitAsync(cancellationToken);

            var task = Task.Run(async () =>
            {
                try
                {
                    var result = await ExecuteBatchItemAsync(
                        item,
                        artifact,
                        samplingRate,
                        timeoutMs,
                        cancellationToken);

                    results.Add(result);

                    if (!result.Success)
                    {
                        hasError = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch item {ItemId} failed", item.Id);
                    results.Add(new BatchItemResult
                    {
                        ItemId = item.Id,
                        ExecutionId = Guid.NewGuid(),
                        Success = false,
                        ErrorMessage = ex.Message,
                        LatencyMs = 0,
                        SampledForValidation = false
                    });
                    hasError = true;
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        return results.OrderBy(r => r.ItemId).ToList();
    }

    private async Task<BatchItemResult> ExecuteBatchItemAsync(
        BatchItem item,
        Core.Models.ProgramArtifact artifact,
        double samplingRate,
        int? timeoutMs,
        CancellationToken cancellationToken)
    {
        var executionId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        CodeBeakerSession? session = null;

        try
        {
            // Acquire session from pool (will reuse if available)
            session = await _sessionPool.AcquireSessionAsync(artifact.Language, cancellationToken);

            _logger.LogDebug(
                "Executing batch item {ItemId} using session {SessionId} (execution #{Count})",
                item.Id, session.SessionId, session.ExecutionCount + 1);

            // Execute using CodeBeaker runtime
            // Note: Creating logger for runtime service - in production, use ILoggerFactory
            var runtimeLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CodeBeakerRuntimeService>.Instance;
            var runtimeService = new CodeBeakerRuntimeService(_sessionPool, runtimeLogger);
            var runtimeResult = await runtimeService.ExecuteAsync(
                artifact.Code,
                artifact.Language,
                item.Input,
                timeoutMs,
                cancellationToken);

            stopwatch.Stop();

            // Determine if sampled
            var shouldSample = item.ForceValidation || ShouldSampleExecution(samplingRate);

            // Create execution record
            var record = new Core.Models.ExecutionRecord
            {
                Id = executionId,
                ProgramId = artifact.Id,
                TaskId = artifact.TaskId,
                InputData = item.Input,
                OutputData = runtimeResult.Output,
                Status = runtimeResult.Success
                    ? Core.Models.ExecutionStatus.Success
                    : Core.Models.ExecutionStatus.Error,
                LatencyMs = stopwatch.Elapsed.TotalMilliseconds,
                SampledForValidation = shouldSample,
                ExecutedAt = DateTime.UtcNow
            };

            await _executionRepository.CreateAsync(record, cancellationToken);

            _logger.LogDebug(
                "Batch item {ItemId} completed in {Ms}ms using session {SessionId}",
                item.Id, stopwatch.ElapsedMilliseconds, session.SessionId);

            return new BatchItemResult
            {
                ItemId = item.Id,
                ExecutionId = executionId,
                Success = runtimeResult.Success,
                Output = runtimeResult.Output,
                ErrorMessage = runtimeResult.Error,
                LatencyMs = stopwatch.Elapsed.TotalMilliseconds,
                SampledForValidation = shouldSample,
                SessionId = session.SessionId
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Batch item {ItemId} failed after {Ms}ms", item.Id, stopwatch.ElapsedMilliseconds);

            return new BatchItemResult
            {
                ItemId = item.Id,
                ExecutionId = executionId,
                Success = false,
                ErrorMessage = ex.Message,
                LatencyMs = stopwatch.Elapsed.TotalMilliseconds,
                SampledForValidation = false,
                SessionId = session?.SessionId
            };
        }
        finally
        {
            if (session != null)
            {
                _sessionPool.ReleaseSession(session);
            }
        }
    }

    private static bool ShouldSampleExecution(double samplingRate)
    {
        if (samplingRate <= 0) return false;
        if (samplingRate >= 1.0) return true;
        return Random.Shared.NextDouble() < samplingRate;
    }
}

/// <summary>
/// Input item for batch execution.
/// </summary>
public record BatchItem
{
    public required string Id { get; init; }
    public required JsonDocument Input { get; init; }
    public bool ForceValidation { get; init; }
}

/// <summary>
/// Result of batch item execution.
/// </summary>
public record BatchItemResult
{
    public required string ItemId { get; init; }
    public required Guid ExecutionId { get; init; }
    public required bool Success { get; init; }
    public JsonDocument? Output { get; init; }
    public string? ErrorMessage { get; init; }
    public required double LatencyMs { get; init; }
    public required bool SampledForValidation { get; init; }
    public string? SessionId { get; init; }
}

/// <summary>
/// Result of batch execution.
/// </summary>
public record BatchExecutionResult
{
    public required Guid BatchId { get; init; }
    public required Guid TaskId { get; init; }
    public required Guid ProgramId { get; init; }
    public required int Version { get; init; }
    public required int TotalItems { get; init; }
    public required int SuccessCount { get; init; }
    public required int FailureCount { get; init; }
    public required double TotalDurationMs { get; init; }
    public required double AvgLatencyMs { get; init; }
    public required List<BatchItemResult> Items { get; init; }
    public required DateTime StartedAt { get; init; }
    public required DateTime CompletedAt { get; init; }
    public SessionPoolStatsSnapshot? SessionPoolStats { get; init; }
}

/// <summary>
/// Snapshot of session pool statistics.
/// </summary>
public record SessionPoolStatsSnapshot
{
    public int TotalSessions { get; init; }
    public int ActiveSessions { get; init; }
    public int IdleSessions { get; init; }
    public int AvailableSlots { get; init; }
    public double AverageExecutionCount { get; init; }
}
