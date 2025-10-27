using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Loopai.Client.Exceptions;
using Loopai.Client.Models;

namespace Loopai.Client;

/// <summary>
/// Official .NET client for Loopai API.
/// </summary>
public class LoopaiClient : ILoopaiClient
{
    private readonly HttpClient _httpClient;
    private readonly LoopaiClientOptions _options;
    private readonly ILogger? _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;
    private bool _disposed;

    /// <summary>
    /// Creates a new Loopai client with options.
    /// </summary>
    public LoopaiClient(LoopaiClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();

        _logger = options.Logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/')),
            Timeout = options.Timeout
        };

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        }

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Loopai.Client/0.1.0");

        _retryPipeline = options.MaxRetries > 0 ? BuildRetryPipeline() : null!;
    }

    /// <summary>
    /// Creates a new Loopai client with IOptions pattern.
    /// </summary>
    public LoopaiClient(IOptions<LoopaiClientOptions> options) : this(options.Value)
    {
    }

    /// <summary>
    /// Creates a new Loopai client with base URL and optional API key.
    /// </summary>
    public LoopaiClient(string baseUrl, string? apiKey = null)
        : this(new LoopaiClientOptions { BaseUrl = baseUrl, ApiKey = apiKey })
    {
    }

    /// <inheritdoc/>
    public async Task<JsonDocument> CreateTaskAsync(
        string name,
        string description,
        JsonDocument inputSchema,
        JsonDocument outputSchema,
        IEnumerable<JsonDocument>? examples = null,
        double accuracyTarget = 0.9,
        int latencyTargetMs = 10,
        double samplingRate = 0.1,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            name,
            description,
            input_schema = inputSchema,
            output_schema = outputSchema,
            examples = examples ?? Array.Empty<JsonDocument>(),
            accuracy_target = accuracyTarget,
            latency_target_ms = latencyTargetMs,
            sampling_rate = samplingRate
        };

        return await PostAsync<JsonDocument>("/api/v1/tasks", request, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<JsonDocument> GetTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<JsonDocument>($"/api/v1/tasks/{taskId}", cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<JsonDocument> ExecuteAsync(
        Guid taskId,
        JsonDocument input,
        int? version = null,
        int? timeoutMs = null,
        bool forceValidation = false,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            task_id = taskId,
            input,
            version,
            timeout_ms = timeoutMs,
            force_validation = forceValidation
        };

        return await PostAsync<JsonDocument>("/api/v1/tasks/execute", request, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<JsonDocument> ExecuteAsync(
        Guid taskId,
        object input,
        CancellationToken cancellationToken = default)
    {
        var inputJson = JsonSerializer.SerializeToDocument(input);
        return await ExecuteAsync(taskId, inputJson, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BatchExecuteResponse> BatchExecuteAsync(
        BatchExecuteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var payload = new
        {
            task_id = request.TaskId,
            items = request.Items.Select(item => new
            {
                id = item.Id,
                input = item.Input,
                force_validation = item.ForceValidation
            }),
            version = request.Version,
            max_concurrency = request.MaxConcurrency,
            stop_on_first_error = request.StopOnFirstError,
            timeout_ms = request.TimeoutMs
        };

        return await PostAsync<BatchExecuteResponse>("/api/v1/batch/execute", payload, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BatchExecuteResponse> BatchExecuteAsync(
        Guid taskId,
        IEnumerable<object> inputs,
        int maxConcurrency = 10,
        CancellationToken cancellationToken = default)
    {
        var items = inputs.Select((input, index) => new BatchExecuteItem
        {
            Id = index.ToString(),
            Input = JsonSerializer.SerializeToDocument(input),
            ForceValidation = false
        });

        var request = new BatchExecuteRequest
        {
            TaskId = taskId,
            Items = items.ToList(),
            MaxConcurrency = maxConcurrency
        };

        return await BatchExecuteAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<HealthResponse> GetHealthAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<HealthResponse>("/health", cancellationToken);
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        _logger?.LogDebug("GET {Path}", path);

        var response = _retryPipeline != null
            ? await _retryPipeline.ExecuteAsync(
                async ct => await _httpClient.GetAsync(path, ct),
                cancellationToken)
            : await _httpClient.GetAsync(path, cancellationToken);

        return await HandleResponseAsync<T>(response);
    }

    private async Task<T> PostAsync<T>(string path, object payload, CancellationToken cancellationToken)
    {
        _logger?.LogDebug("POST {Path}", path);

        if (_options.EnableDetailedLogging)
        {
            var json = JsonSerializer.Serialize(payload);
            _logger?.LogTrace("Request payload: {Payload}", json);
        }

        var response = _retryPipeline != null
            ? await _retryPipeline.ExecuteAsync(
                async ct => await _httpClient.PostAsJsonAsync(path, payload, ct),
                cancellationToken)
            : await _httpClient.PostAsJsonAsync(path, payload, cancellationToken);

        return await HandleResponseAsync<T>(response);
    }

    private async Task<T> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<T>();
            if (result == null)
                throw new LoopaiException("Response deserialization returned null.");

            return result;
        }

        var statusCode = (int)response.StatusCode;
        var errorContent = await response.Content.ReadAsStringAsync();

        _logger?.LogError("HTTP {StatusCode}: {Error}", statusCode, errorContent);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ValidationException($"Validation failed: {errorContent}", statusCode, null!);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new LoopaiException($"Resource not found: {errorContent}", statusCode);
        }

        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            throw new ExecutionException($"Server error: {errorContent}", statusCode);
        }

        throw new LoopaiException($"HTTP {statusCode}: {errorContent}", statusCode);
    }

    private ResiliencePipeline<HttpResponseMessage> BuildRetryPipeline()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = _options.MaxRetries,
                Delay = _options.RetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => r.StatusCode == HttpStatusCode.RequestTimeout ||
                                      r.StatusCode == HttpStatusCode.TooManyRequests ||
                                      (int)r.StatusCode >= 500),
                OnRetry = args =>
                {
                    _logger?.LogWarning(
                        "Retry attempt {Attempt} after {Delay}ms. Status: {Status}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Result?.StatusCode);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Disposes the HTTP client.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _httpClient?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
