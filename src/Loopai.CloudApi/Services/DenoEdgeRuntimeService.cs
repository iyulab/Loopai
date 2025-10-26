using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Loopai.CloudApi.Configuration;
using Loopai.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace Loopai.CloudApi.Services;

/// <summary>
/// Deno-based edge runtime service for program execution.
/// </summary>
public class DenoEdgeRuntimeService : IEdgeRuntimeService
{
    private readonly EdgeRuntimeSettings _settings;
    private readonly ILogger<DenoEdgeRuntimeService> _logger;

    public DenoEdgeRuntimeService(
        IOptions<EdgeRuntimeSettings> settings,
        ILogger<DenoEdgeRuntimeService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Ensure working directory exists
        Directory.CreateDirectory(_settings.WorkingDirectory);
    }

    public async Task<EdgeExecutionResult> ExecuteAsync(
        string code,
        string language,
        JsonDocument input,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        var executionTimeout = timeoutMs ?? _settings.ExecutionTimeoutMs;
        var startTime = DateTime.UtcNow;
        var tempFilePath = string.Empty;

        try
        {
            // Create temporary program file
            tempFilePath = Path.Combine(
                _settings.WorkingDirectory,
                $"program_{Guid.NewGuid()}.ts"
            );

            // Wrap the program with input/output handling
            var wrappedCode = WrapProgramCode(code, input);
            await File.WriteAllTextAsync(tempFilePath, wrappedCode, cancellationToken);

            _logger.LogDebug("Executing program at {Path} with timeout {Timeout}ms",
                tempFilePath, executionTimeout);

            // Execute using Deno
            var result = await ExecuteDenoAsync(tempFilePath, executionTimeout, cancellationToken);

            var executionTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            return new EdgeExecutionResult
            {
                Success = result.Success,
                Output = result.Output,
                Error = result.Error,
                ExecutionTimeMs = executionTime,
                MemoryUsedBytes = result.MemoryUsed,
                StandardOutput = result.StdOut,
                StandardError = result.StdErr
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing program");
            var executionTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            return new EdgeExecutionResult
            {
                Success = false,
                Error = $"Execution error: {ex.Message}",
                ExecutionTimeMs = executionTime,
                MemoryUsedBytes = 0,
                StandardError = ex.ToString()
            };
        }
        finally
        {
            // Clean up temporary file
            if (_settings.CleanupTempFiles && !string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary file: {Path}", tempFilePath);
                }
            }
        }
    }

    private string WrapProgramCode(string code, JsonDocument input)
    {
        var inputJson = input.RootElement.GetRawText();

        return $@"
// Auto-generated wrapper for program execution
const input = {inputJson};

// User program
{code}

// Execute and capture output
try {{
    const result = await main(input);
    console.log('__OUTPUT_START__');
    console.log(JSON.stringify(result));
    console.log('__OUTPUT_END__');
}} catch (error) {{
    console.error('__ERROR_START__');
    console.error(error.message);
    console.error('__ERROR_END__');
    Deno.exit(1);
}}
";
    }

    private async Task<DenoExecutionResult> ExecuteDenoAsync(
        string filePath,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = _settings.ExecutablePath,
            Arguments = $"run --allow-all --quiet {filePath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        var stdOutBuilder = new StringBuilder();
        var stdErrBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) stdOutBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) stdErrBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Timeout or cancellation
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch { }

            return new DenoExecutionResult
            {
                Success = false,
                Error = cancellationToken.IsCancellationRequested
                    ? "Execution cancelled"
                    : $"Execution timeout after {timeoutMs}ms",
                StdOut = stdOutBuilder.ToString(),
                StdErr = stdErrBuilder.ToString(),
                MemoryUsed = 0
            };
        }

        var stdOut = stdOutBuilder.ToString();
        var stdErr = stdErrBuilder.ToString();

        // Parse output
        if (process.ExitCode == 0)
        {
            var output = ExtractOutput(stdOut);
            if (output != null)
            {
                return new DenoExecutionResult
                {
                    Success = true,
                    Output = output,
                    StdOut = stdOut,
                    StdErr = stdErr,
                    MemoryUsed = EstimateMemoryUsage(stdOut)
                };
            }
        }

        // Execution failed
        var error = ExtractError(stdErr) ?? $"Program exited with code {process.ExitCode}";
        return new DenoExecutionResult
        {
            Success = false,
            Error = error,
            StdOut = stdOut,
            StdErr = stdErr,
            MemoryUsed = 0
        };
    }

    private static JsonDocument? ExtractOutput(string stdOut)
    {
        try
        {
            var startIndex = stdOut.IndexOf("__OUTPUT_START__", StringComparison.Ordinal);
            var endIndex = stdOut.IndexOf("__OUTPUT_END__", StringComparison.Ordinal);

            if (startIndex >= 0 && endIndex > startIndex)
            {
                var outputText = stdOut.Substring(
                    startIndex + "__OUTPUT_START__".Length,
                    endIndex - startIndex - "__OUTPUT_START__".Length
                ).Trim();

                return JsonDocument.Parse(outputText);
            }
        }
        catch (JsonException)
        {
            // Invalid JSON output
        }

        return null;
    }

    private static string? ExtractError(string stdErr)
    {
        var startIndex = stdErr.IndexOf("__ERROR_START__", StringComparison.Ordinal);
        var endIndex = stdErr.IndexOf("__ERROR_END__", StringComparison.Ordinal);

        if (startIndex >= 0 && endIndex > startIndex)
        {
            return stdErr.Substring(
                startIndex + "__ERROR_START__".Length,
                endIndex - startIndex - "__ERROR_START__".Length
            ).Trim();
        }

        return stdErr.Length > 0 ? stdErr : null;
    }

    private static int EstimateMemoryUsage(string stdOut)
    {
        // Simple estimation based on output size
        // In production, could use process memory metrics
        return stdOut.Length * 2; // Rough estimate
    }

    private record DenoExecutionResult
    {
        public required bool Success { get; init; }
        public JsonDocument? Output { get; init; }
        public string? Error { get; init; }
        public required string StdOut { get; init; }
        public required string StdErr { get; init; }
        public required int MemoryUsed { get; init; }
    }
}
