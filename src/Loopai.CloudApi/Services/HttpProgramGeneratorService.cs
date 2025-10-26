using System.Text.Json;
using Loopai.CloudApi.Configuration;
using Loopai.CloudApi.Models;
using Loopai.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace Loopai.CloudApi.Services;

/// <summary>
/// HTTP client-based implementation of IProgramGeneratorService.
/// Communicates with the Python Generator Service for program synthesis.
/// </summary>
public class HttpProgramGeneratorService : IProgramGeneratorService
{
    private readonly HttpClient _httpClient;
    private readonly GeneratorServiceSettings _settings;
    private readonly ITaskService _taskService;
    private readonly ILogger<HttpProgramGeneratorService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpProgramGeneratorService(
        HttpClient httpClient,
        IOptions<GeneratorServiceSettings> settings,
        ITaskService taskService,
        ILogger<HttpProgramGeneratorService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _taskService = taskService;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<ProgramGenerationResult> GenerateProgramAsync(
        Guid taskId,
        IReadOnlyList<(JsonDocument Input, JsonDocument Output)>? examples = null,
        string? constraints = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get task specification
            var task = await _taskService.GetTaskAsync(taskId, cancellationToken);
            if (task == null)
            {
                _logger.LogError("Task not found: {TaskId}", taskId);
                return new ProgramGenerationResult
                {
                    Success = false,
                    Code = null,
                    Language = "typescript",
                    LinesOfCode = 0,
                    CyclomaticComplexity = 0,
                    EstimatedTokens = 0,
                    ErrorMessage = $"Task not found: {taskId}"
                };
            }

            // Build request
            var examplePairs = examples?.Select(e => new ExamplePair
            {
                Input = e.Input,
                Output = e.Output
            }).ToList() ?? new List<ExamplePair>();

            var request = new ProgramGenerationRequest
            {
                TaskId = taskId,
                TaskName = task.Name,
                InputSchema = task.InputSchema,
                OutputSchema = task.OutputSchema,
                Description = task.Description,
                Examples = examplePairs,
                Constraints = constraints,
                TargetRuntime = "deno"
            };

            if (_settings.EnableDetailedLogging)
            {
                _logger.LogInformation("Sending program generation request for task {TaskId}: {TaskName}",
                    taskId, task.Name);
            }

            // Send HTTP request with retry logic
            var response = await SendWithRetryAsync(request, cancellationToken);

            if (response == null || !response.Success)
            {
                _logger.LogWarning("Program generation failed for task {TaskId}: {Error}",
                    taskId, response?.ErrorMessage ?? "Unknown error");

                return new ProgramGenerationResult
                {
                    Success = false,
                    Code = null,
                    Language = "typescript",
                    LinesOfCode = 0,
                    CyclomaticComplexity = 0,
                    EstimatedTokens = 0,
                    ErrorMessage = response?.ErrorMessage ?? "Program generation failed"
                };
            }

            _logger.LogInformation("Successfully generated program for task {TaskId}, {Lines} lines of code",
                taskId, response.Complexity?.LinesOfCode ?? 0);

            return new ProgramGenerationResult
            {
                Success = true,
                Code = response.Code,
                Language = response.Language,
                LinesOfCode = response.Complexity?.LinesOfCode ?? 0,
                CyclomaticComplexity = response.Complexity?.CyclomaticComplexity ?? 0,
                EstimatedTokens = response.Complexity?.EstimatedTokens ?? 0,
                Metadata = response.Metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating program for task {TaskId}", taskId);
            return new ProgramGenerationResult
            {
                Success = false,
                Code = null,
                Language = "typescript",
                LinesOfCode = 0,
                CyclomaticComplexity = 0,
                EstimatedTokens = 0,
                ErrorMessage = $"Generation error: {ex.Message}"
            };
        }
    }

    private async Task<ProgramGenerationResponse?> SendWithRetryAsync(
        ProgramGenerationRequest request,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < _settings.MaxRetryAttempts)
        {
            attempt++;
            try
            {
                var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/generate", content, cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Generator service returned {StatusCode}: {Error}",
                        httpResponse.StatusCode, errorBody);

                    if (attempt < _settings.MaxRetryAttempts)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                        continue;
                    }

                    return new ProgramGenerationResponse
                    {
                        Success = false,
                        ErrorMessage = $"HTTP {httpResponse.StatusCode}: {errorBody}"
                    };
                }

                var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                var response = JsonSerializer.Deserialize<ProgramGenerationResponse>(responseBody, _jsonOptions);

                return response;
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "HTTP request failed (attempt {Attempt}/{Max})",
                    attempt, _settings.MaxRetryAttempts);

                if (attempt < _settings.MaxRetryAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                }
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout
                lastException = ex;
                _logger.LogWarning("Request timeout (attempt {Attempt}/{Max})",
                    attempt, _settings.MaxRetryAttempts);

                if (attempt < _settings.MaxRetryAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                }
            }
        }

        return new ProgramGenerationResponse
        {
            Success = false,
            ErrorMessage = $"Failed after {_settings.MaxRetryAttempts} attempts: {lastException?.Message}"
        };
    }
}
