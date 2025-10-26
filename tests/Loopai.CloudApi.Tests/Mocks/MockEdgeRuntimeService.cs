using System.Text.Json;
using Loopai.Core.Interfaces;

namespace Loopai.CloudApi.Tests.Mocks;

/// <summary>
/// Mock implementation of IEdgeRuntimeService for testing.
/// </summary>
public class MockEdgeRuntimeService : IEdgeRuntimeService
{
    private readonly Dictionary<string, Func<JsonDocument, JsonDocument>> _executors = new();
    private bool _shouldFail = false;
    private string? _errorMessage = null;
    private int _executionDelay = 0;
    private bool _shouldTimeout = false;

    public void ConfigureExecutor(string code, Func<JsonDocument, JsonDocument> executor)
    {
        _executors[code] = executor;
    }

    public void ConfigureFailure(string errorMessage)
    {
        _shouldFail = true;
        _errorMessage = errorMessage;
    }

    public void ConfigureDelay(int delayMs)
    {
        _executionDelay = delayMs;
    }

    public void ConfigureTimeout()
    {
        _shouldTimeout = true;
    }

    public void Reset()
    {
        _executors.Clear();
        _shouldFail = false;
        _errorMessage = null;
        _executionDelay = 0;
        _shouldTimeout = false;
    }

    public async Task<EdgeExecutionResult> ExecuteAsync(
        string code,
        string language,
        JsonDocument input,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        if (_executionDelay > 0)
        {
            await Task.Delay(_executionDelay, cancellationToken);
        }

        if (_shouldTimeout)
        {
            return new EdgeExecutionResult
            {
                Success = false,
                Error = "Execution timeout",
                ExecutionTimeMs = timeoutMs ?? 5000,
                MemoryUsedBytes = 0,
                StandardError = "Timeout occurred"
            };
        }

        if (_shouldFail)
        {
            return new EdgeExecutionResult
            {
                Success = false,
                Error = _errorMessage ?? "Mock execution failure",
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                MemoryUsedBytes = 0,
                StandardError = _errorMessage
            };
        }

        // Try to find configured executor
        if (_executors.TryGetValue(code, out var executor))
        {
            try
            {
                var output = executor(input);
                return new EdgeExecutionResult
                {
                    Success = true,
                    Output = output,
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                    MemoryUsedBytes = 1024,
                    StandardOutput = "Mock execution successful"
                };
            }
            catch (Exception ex)
            {
                return new EdgeExecutionResult
                {
                    Success = false,
                    Error = ex.Message,
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                    MemoryUsedBytes = 0,
                    StandardError = ex.ToString()
                };
            }
        }

        // Default: passthrough with processed flag
        var defaultOutput = JsonDocument.Parse($$"""
        {
            "input": {{input.RootElement.GetRawText()}},
            "processed": true,
            "timestamp": "{{DateTime.UtcNow:O}}"
        }
        """);

        return new EdgeExecutionResult
        {
            Success = true,
            Output = defaultOutput,
            ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
            MemoryUsedBytes = 2048,
            StandardOutput = "Default mock execution"
        };
    }
}
