using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Loopai.CloudApi.DTOs;
using Loopai.Core.Interfaces;
using Loopai.Core.CodeBeaker;

namespace Loopai.CloudApi.Controllers;

/// <summary>
/// Controller for batch operations.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/batch")]
[Asp.Versioning.ApiVersion("1.0")]
[Produces("application/json")]
public class BatchController : ControllerBase
{
    private readonly IProgramExecutionService _executionService;
    private readonly CodeBeakerBatchExecutor? _codeBeakerBatchExecutor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BatchController> _logger;

    public BatchController(
        IProgramExecutionService executionService,
        IConfiguration configuration,
        ILogger<BatchController> logger,
        CodeBeakerBatchExecutor? codeBeakerBatchExecutor = null)
    {
        _executionService = executionService;
        _configuration = configuration;
        _codeBeakerBatchExecutor = codeBeakerBatchExecutor;
        _logger = logger;
    }

    /// <summary>
    /// Executes multiple inputs in batch with concurrency control.
    /// Uses CodeBeaker batch executor if available, otherwise falls back to standard execution.
    /// </summary>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(BatchExecuteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BatchExecute([FromBody] BatchExecuteRequest request)
    {
        // Validation
        var maxConcurrency = request.MaxConcurrency ?? 10;
        if (maxConcurrency < 1 || maxConcurrency > 100)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_CONCURRENCY",
                Message = "MaxConcurrency must be between 1 and 100",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Check if CodeBeaker batch executor is available and enabled
        var useCodeBeaker = _codeBeakerBatchExecutor != null &&
                           _configuration.GetValue<string>("Execution:Provider") == "CodeBeaker";

        if (useCodeBeaker)
        {
            return await ExecuteBatchWithCodeBeakerAsync(request, maxConcurrency);
        }
        else
        {
            return await ExecuteBatchStandardAsync(request, maxConcurrency);
        }
    }

    private async Task<IActionResult> ExecuteBatchWithCodeBeakerAsync(
        BatchExecuteRequest request,
        int maxConcurrency)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting CodeBeaker batch execution for task {TaskId} with {ItemCount} items (concurrency: {MaxConcurrency})",
            request.TaskId,
            request.Items.Count(),
            maxConcurrency
        );

        try
        {
            // Convert DTO items to batch items
            var batchItems = request.Items.Select(item => new BatchItem
            {
                Id = item.Id,
                Input = item.Input,
                ForceValidation = item.ForceValidation
            });

            // Execute using CodeBeaker batch executor
            var result = await _codeBeakerBatchExecutor!.ExecuteBatchAsync(
                request.TaskId,
                batchItems,
                request.Version,
                maxConcurrency,
                request.StopOnFirstError,
                request.TimeoutMs,
                HttpContext.RequestAborted
            );

            stopwatch.Stop();

            // Convert to API response
            var response = new BatchExecuteResponse
            {
                BatchId = result.BatchId,
                TaskId = result.TaskId,
                Version = result.Version,
                TotalItems = result.TotalItems,
                SuccessCount = result.SuccessCount,
                FailureCount = result.FailureCount,
                TotalDurationMs = result.TotalDurationMs,
                AvgLatencyMs = result.AvgLatencyMs,
                Results = result.Items.Select(item => new BatchExecuteResult
                {
                    Id = item.ItemId,
                    ExecutionId = item.ExecutionId,
                    Success = item.Success,
                    Output = item.Output,
                    ErrorMessage = item.ErrorMessage,
                    LatencyMs = item.LatencyMs,
                    SampledForValidation = item.SampledForValidation
                }).ToList(),
                StartedAt = result.StartedAt,
                CompletedAt = result.CompletedAt
            };

            _logger.LogInformation(
                "CodeBeaker batch {BatchId} completed: {SuccessCount}/{TotalItems} succeeded in {Duration}ms (avg: {AvgLatency}ms) " +
                "- Sessions: {TotalSessions} total, {ActiveSessions} active, {IdleSessions} idle",
                result.BatchId,
                result.SuccessCount,
                result.TotalItems,
                result.TotalDurationMs,
                result.AvgLatencyMs,
                result.SessionPoolStats?.TotalSessions ?? 0,
                result.SessionPoolStats?.ActiveSessions ?? 0,
                result.SessionPoolStats?.IdleSessions ?? 0
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "CodeBeaker batch execution failed after {Ms}ms", stopwatch.ElapsedMilliseconds);
            return StatusCode(500, new ErrorResponse
            {
                Code = "BATCH_EXECUTION_ERROR",
                Message = $"CodeBeaker batch execution failed: {ex.Message}",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    private async Task<IActionResult> ExecuteBatchStandardAsync(
        BatchExecuteRequest request,
        int maxConcurrency)
    {
        var batchId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting standard batch execution {BatchId} for task {TaskId} with {ItemCount} items",
            batchId,
            request.TaskId,
            request.Items.Count()
        );

        try
        {
            var semaphore = new SemaphoreSlim(maxConcurrency);
            var results = new List<BatchExecuteResult>();
            var tasks = new List<Task<BatchExecuteResult>>();

            foreach (var item in request.Items)
            {
                if (request.StopOnFirstError && results.Any(r => !r.Success))
                {
                    _logger.LogWarning("Stopping batch execution due to error (StopOnFirstError=true)");
                    break;
                }

                tasks.Add(ExecuteItemAsync(
                    item,
                    request.TaskId,
                    request.Version,
                    request.TimeoutMs,
                    semaphore,
                    HttpContext.RequestAborted
                ));
            }

            results.AddRange(await Task.WhenAll(tasks));
            stopwatch.Stop();

            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);
            var avgLatency = results.Any() ? results.Average(r => r.LatencyMs) : 0;

            var response = new BatchExecuteResponse
            {
                BatchId = batchId,
                TaskId = request.TaskId,
                Version = request.Version ?? 0,
                TotalItems = results.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                TotalDurationMs = stopwatch.Elapsed.TotalMilliseconds,
                AvgLatencyMs = avgLatency,
                Results = results,
                StartedAt = startTime,
                CompletedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Standard batch {BatchId} completed: {SuccessCount}/{TotalItems} succeeded in {Duration}ms",
                batchId,
                successCount,
                results.Count,
                stopwatch.ElapsedMilliseconds
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Standard batch execution {BatchId} failed", batchId);
            return StatusCode(500, new ErrorResponse
            {
                Code = "BATCH_EXECUTION_ERROR",
                Message = $"Batch execution failed: {ex.Message}",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Validates multiple executions in batch.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(BatchValidateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public Task<IActionResult> BatchValidate([FromBody] BatchValidateRequest request)
    {
        var batchId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting batch validation {BatchId} for task {TaskId} with {ItemCount} items",
            batchId,
            request.TaskId,
            request.Items.Count()
        );

        try
        {
            var results = new List<BatchValidateResult>();

            foreach (var item in request.Items)
            {
                // Simple validation: compare output with expected output
                var isValid = CompareJsonDocuments(item.Output, item.ExpectedOutput);

                results.Add(new BatchValidateResult
                {
                    Id = item.Id,
                    IsValid = isValid,
                    Message = isValid ? "Valid" : "Output does not match expected output",
                    ConfidenceScore = isValid ? 1.0 : 0.0
                });
            }

            stopwatch.Stop();

            var validCount = results.Count(r => r.IsValid);
            var invalidCount = results.Count(r => !r.IsValid);
            var accuracyRate = results.Any() ? (double)validCount / results.Count : 0.0;

            var response = new BatchValidateResponse
            {
                BatchId = batchId,
                TaskId = request.TaskId,
                TotalItems = results.Count,
                ValidCount = validCount,
                InvalidCount = invalidCount,
                AccuracyRate = accuracyRate,
                Results = results,
                TotalDurationMs = stopwatch.Elapsed.TotalMilliseconds
            };

            _logger.LogInformation(
                "Batch validation {BatchId} completed: {ValidCount}/{TotalItems} valid ({AccuracyRate:P2})",
                batchId,
                validCount,
                results.Count,
                accuracyRate
            );

            return Task.FromResult<IActionResult>(Ok(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch validation {BatchId} failed", batchId);
            return Task.FromResult<IActionResult>(StatusCode(500, new ErrorResponse
            {
                Code = "BATCH_VALIDATION_ERROR",
                Message = $"Batch validation failed: {ex.Message}",
                TraceId = HttpContext.TraceIdentifier
            }));
        }
    }

    private async Task<BatchExecuteResult> ExecuteItemAsync(
        BatchExecuteItem item,
        Guid taskId,
        int? version,
        int? timeoutMs,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _executionService.ExecuteAsync(
                taskId,
                item.Input,
                version,
                item.ForceValidation,
                timeoutMs
            );

            stopwatch.Stop();

            return new BatchExecuteResult
            {
                Id = item.Id,
                ExecutionId = result.ExecutionId,
                Success = result.Status == Core.Models.ExecutionStatus.Success,
                Output = result.Output,
                ErrorMessage = result.ErrorMessage,
                LatencyMs = stopwatch.Elapsed.TotalMilliseconds,
                SampledForValidation = result.SampledForValidation
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Batch item {ItemId} execution failed", item.Id);

            return new BatchExecuteResult
            {
                Id = item.Id,
                Success = false,
                ErrorMessage = ex.Message,
                LatencyMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        finally
        {
            semaphore.Release();
        }
    }

    private bool CompareJsonDocuments(System.Text.Json.JsonDocument doc1, System.Text.Json.JsonDocument doc2)
    {
        try
        {
            var json1 = doc1.RootElement.GetRawText();
            var json2 = doc2.RootElement.GetRawText();

            // Normalize and compare
            return System.Text.Json.JsonDocument.Parse(json1).RootElement.ToString() ==
                   System.Text.Json.JsonDocument.Parse(json2).RootElement.ToString();
        }
        catch
        {
            return false;
        }
    }
}
