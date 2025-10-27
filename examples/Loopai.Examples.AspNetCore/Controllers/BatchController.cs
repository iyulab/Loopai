using Loopai.Client;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Loopai.Examples.AspNetCore.Controllers;

/// <summary>
/// Example controller demonstrating Loopai batch execution.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BatchController : ControllerBase
{
    private readonly ILoopaiClient _loopai;
    private readonly ILogger<BatchController> _logger;

    public BatchController(ILoopaiClient loopai, ILogger<BatchController> logger)
    {
        _loopai = loopai;
        _logger = logger;
    }

    /// <summary>
    /// Example: Batch spam classification.
    /// POST /api/batch/classify
    /// Body: { "taskId": "guid", "emails": ["email1", "email2", ...] }
    /// </summary>
    [HttpPost("classify")]
    public async Task<IActionResult> BatchClassify([FromBody] BatchClassifyRequest request)
    {
        try
        {
            _logger.LogInformation("Starting batch classification for {Count} emails", request.Emails.Count());

            // Prepare inputs
            var inputs = request.Emails.Select(email => new { text = email });

            // Execute batch
            var result = await _loopai.BatchExecuteAsync(
                request.TaskId,
                inputs,
                maxConcurrency: 10);

            _logger.LogInformation(
                "Batch completed: {SuccessCount}/{TotalItems} succeeded in {Duration}ms (avg: {AvgLatency}ms)",
                result.SuccessCount,
                result.TotalItems,
                result.TotalDurationMs,
                result.AvgLatencyMs);

            // Process results
            var classifications = result.Results.Select(r => new
            {
                r.Id,
                r.Success,
                Classification = r.Success ? r.Output?.RootElement.GetProperty("output").GetString() : null,
                Error = r.ErrorMessage,
                LatencyMs = r.LatencyMs
            }).ToList();

            return Ok(new
            {
                batchId = result.BatchId,
                totalItems = result.TotalItems,
                successCount = result.SuccessCount,
                failureCount = result.FailureCount,
                totalDurationMs = result.TotalDurationMs,
                avgLatencyMs = result.AvgLatencyMs,
                classifications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch classification failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Example: Batch sentiment analysis with custom IDs.
    /// POST /api/batch/sentiment
    /// </summary>
    [HttpPost("sentiment")]
    public async Task<IActionResult> BatchSentiment([FromBody] BatchSentimentRequest request)
    {
        try
        {
            // Prepare batch items with custom IDs
            var batchItems = request.Texts.Select((text, index) => new Client.Models.BatchExecuteItem
            {
                Id = $"sentiment-{index}",
                Input = JsonDocument.Parse(JsonSerializer.Serialize(new { text })),
                ForceValidation = false
            });

            var batchRequest = new Client.Models.BatchExecuteRequest
            {
                TaskId = request.TaskId,
                Items = batchItems.ToList(),
                MaxConcurrency = 5,
                StopOnFirstError = false,
                TimeoutMs = 30000
            };

            var result = await _loopai.BatchExecuteAsync(batchRequest);

            return Ok(new
            {
                batchId = result.BatchId,
                results = result.Results.Select(r => new
                {
                    id = r.Id,
                    success = r.Success,
                    sentiment = r.Success ? r.Output?.RootElement.GetProperty("output").GetString() : null,
                    error = r.ErrorMessage,
                    latencyMs = r.LatencyMs,
                    sampled = r.SampledForValidation
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch sentiment analysis failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    public record BatchClassifyRequest(Guid TaskId, IEnumerable<string> Emails);
    public record BatchSentimentRequest(Guid TaskId, IEnumerable<string> Texts);
}
