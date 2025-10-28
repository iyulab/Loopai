using Loopai.Core.CodeBeaker.Models;
using Loopai.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Loopai.Core.CodeBeaker;

/// <summary>
/// CodeBeaker implementation of IEdgeRuntimeService.
/// Executes programs using CodeBeaker's session-based execution.
/// </summary>
public class CodeBeakerRuntimeService : IEdgeRuntimeService
{
    private readonly CodeBeakerSessionPool _sessionPool;
    private readonly ILogger<CodeBeakerRuntimeService> _logger;

    private static readonly Dictionary<string, (string Command, string Extension)> LanguageConfig = new()
    {
        { "python", ("python3", ".py") },
        { "javascript", ("deno", ".js") },  // Use Deno for JavaScript (supports WriteFile command)
        { "typescript", ("deno", ".ts") },  // Use Deno for TypeScript (native support)
        { "go", ("go", ".go") },
        { "csharp", ("dotnet", ".cs") },
        { "dotnet", ("dotnet", ".cs") }
    };

    public CodeBeakerRuntimeService(
        CodeBeakerSessionPool sessionPool,
        ILogger<CodeBeakerRuntimeService> logger)
    {
        _sessionPool = sessionPool;
        _logger = logger;
    }

    public async Task<EdgeExecutionResult> ExecuteAsync(
        string code,
        string language,
        JsonDocument input,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug("Executing {Language} code via CodeBeaker", language);

        // Normalize language name
        var normalizedLanguage = language.ToLowerInvariant();
        if (!LanguageConfig.TryGetValue(normalizedLanguage, out var config))
        {
            return new EdgeExecutionResult
            {
                Success = false,
                Error = $"Unsupported language: {language}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                MemoryUsedBytes = 0
            };
        }

        CodeBeakerSession? session = null;
        try
        {
            // Acquire session from pool
            session = await _sessionPool.AcquireSessionAsync(normalizedLanguage, cancellationToken);

            // Prepare script with input injection
            // CodeBeaker path strategy (UPDATED - workaround for CodeBeaker bug):
            // - WriteFileCommand/ReadFileCommand paths: Use /workspace/ virtual paths
            //   (DenoRuntime converts via GetFullPath(), other runtimes should too but don't yet)
            // - ExecuteShellCommand script path: Use relative paths
            //   (Only DenoRuntime converts /workspace/ paths in ExecuteShellCommand args; PythonRuntime doesn't)
            //   TODO: Submit PR to CodeBeaker to add path conversion to PythonRuntime/other runtimes
            // - Wrapped code paths: Use relative paths (code executes WITH working directory = workspace)
            //   All runtimes set WorkingDirectory to actual workspace, so relative paths work

            // Paths for file commands (WriteFileCommand/ReadFileCommand)
            string scriptPath = $"/workspace/program{config.Extension}";
            string inputPath = "/workspace/input.json";
            string outputPath = "/workspace/output.json";

            // Paths for execute command and wrapped code (relative paths)
            string executeScriptPath = $"program{config.Extension}";
            var wrappedInputPath = "input.json";
            var wrappedOutputPath = "output.json";

            _logger.LogDebug(
                "Using /workspace/ for file commands, relative paths for execute and wrapped code - {Language} runtime",
                normalizedLanguage);

            // Write input to file
            var inputJson = JsonSerializer.Serialize(
                JsonDocument.Parse(input.RootElement.GetRawText()).RootElement,
                new JsonSerializerOptions { WriteIndented = false });

            await ExecuteCommandAsync(session, new WriteFileCommand
            {
                Path = inputPath,
                Content = inputJson
            }, cancellationToken);

            // Write program code (use /workspace/ paths consistent with file operations)
            var wrappedCode = WrapCodeWithInputOutput(code, normalizedLanguage, wrappedInputPath, wrappedOutputPath);
            await ExecuteCommandAsync(session, new WriteFileCommand
            {
                Path = scriptPath,
                Content = wrappedCode
            }, cancellationToken);

            // Execute program (use relative path for execute command)
            var executeCommand = CreateExecuteCommand(normalizedLanguage, executeScriptPath, config.Command);

            _logger.LogDebug(
                "ExecuteShellCommand - CommandName: {CommandName}, Args: {Args}",
                executeCommand.CommandName,
                string.Join(" ", executeCommand.Args ?? new List<string>()));

            var executeResult = await ExecuteCommandAsync(session, executeCommand, cancellationToken);

            session.ExecutionCount++;

            // Log execution details for debugging
            _logger.LogDebug(
                "Execution result - Success: {Success}, Result: {Result}",
                executeResult.Success,
                executeResult.Result?.ToString() ?? "null");

            if (!executeResult.Success)
            {
                stopwatch.Stop();

                // Extract stderr from result if available
                var stderr = ExtractStderr(executeResult.Result);

                _logger.LogError(
                    "Code execution failed - Error: {Error}, Stderr: {Stderr}",
                    executeResult.Error, stderr);

                return new EdgeExecutionResult
                {
                    Success = false,
                    Error = executeResult.Error ?? "Execution failed",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    MemoryUsedBytes = 0,
                    StandardError = stderr ?? executeResult.Error
                };
            }

            // Even if success=true, check for stderr output
            var executionStderr = ExtractStderr(executeResult.Result);
            if (!string.IsNullOrEmpty(executionStderr))
            {
                _logger.LogWarning(
                    "Code execution had stderr output: {Stderr}",
                    executionStderr);
            }

            // Read output
            var readResult = await ExecuteCommandAsync(session, new ReadFileCommand
            {
                Path = outputPath
            }, cancellationToken);

            stopwatch.Stop();

            if (!readResult.Success || readResult.Result == null)
            {
                return new EdgeExecutionResult
                {
                    Success = false,
                    Error = "Failed to read output",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    MemoryUsedBytes = 0
                };
            }

            // Parse output JSON
            var outputJson = GetResultContent(readResult.Result);
            if (outputJson == null)
            {
                return new EdgeExecutionResult
                {
                    Success = false,
                    Error = "Failed to parse output JSON",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    MemoryUsedBytes = 0
                };
            }

            var outputDocument = JsonDocument.Parse(outputJson);

            _logger.LogInformation(
                "Executed {Language} code successfully in {Ms}ms via session {SessionId}",
                language, stopwatch.ElapsedMilliseconds, session.SessionId);

            return new EdgeExecutionResult
            {
                Success = true,
                Output = outputDocument,
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                MemoryUsedBytes = 0  // CodeBeaker doesn't expose memory usage
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Error executing {Language} code via CodeBeaker", language);

            return new EdgeExecutionResult
            {
                Success = false,
                Error = $"Execution error: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                MemoryUsedBytes = 0,
                StandardError = ex.ToString()
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

    private async Task<CommandResult> ExecuteCommandAsync(
        CodeBeakerSession session,
        CodeBeakerCommand command,
        CancellationToken cancellationToken)
    {
        return await _sessionPool.ExecuteCommandAsync(session, command, cancellationToken);
    }

    private static string WrapCodeWithInputOutput(
        string code,
        string language,
        string inputPath,
        string outputPath)
    {
        return language switch
        {
            "python" => WrapPythonCode(code, inputPath, outputPath),
            "javascript" or "typescript" => WrapJavaScriptCode(code, inputPath, outputPath),
            "go" => WrapGoCode(code, inputPath, outputPath),
            "csharp" or "dotnet" => WrapCSharpCode(code, inputPath, outputPath),
            _ => code
        };
    }

    private static string WrapPythonCode(string code, string inputPath, string outputPath)
    {
        return $@"
import json

# Read input
with open('{inputPath}', 'r') as f:
    input_data = json.load(f)

# User code
{code}

# Write output (assumes 'result' variable exists)
with open('{outputPath}', 'w') as f:
    json.dump(result, f)
";
    }

    private static string WrapJavaScriptCode(string code, string inputPath, string outputPath)
    {
        // Deno-compatible wrapper (uses Deno APIs instead of Node's require)
        return $@"
// Read input using Deno API
const inputText = await Deno.readTextFile('{inputPath}');
const input_data = JSON.parse(inputText);

// User code
{code}

// Write output (assumes 'result' variable exists)
const outputText = JSON.stringify(result);
await Deno.writeTextFile('{outputPath}', outputText);
";
    }

    private static string WrapGoCode(string code, string inputPath, string outputPath)
    {
        return $@"
package main

import (
    ""encoding/json""
    ""os""
)

func main() {{
    // Read input
    inputFile, _ := os.Open(""{inputPath}"")
    defer inputFile.Close()
    var input_data map[string]interface{{}}
    json.NewDecoder(inputFile).Decode(&input_data)

    // User code
    {code}

    // Write output (assumes 'result' variable exists)
    outputFile, _ := os.Create(""{outputPath}"")
    defer outputFile.Close()
    json.NewEncoder(outputFile).Encode(result)
}}
";
    }

    private static string WrapCSharpCode(string code, string inputPath, string outputPath)
    {
        return $@"
using System;
using System.IO;
using System.Text.Json;

// Read input
var inputJson = File.ReadAllText(""{inputPath}"");
var input_data = JsonSerializer.Deserialize<JsonElement>(inputJson);

// User code
{code}

// Write output (assumes 'result' variable exists)
var outputJson = JsonSerializer.Serialize(result);
File.WriteAllText(""{outputPath}"", outputJson);
";
    }

    private static ExecuteShellCommand CreateExecuteCommand(
        string language,
        string scriptPath,
        string command)
    {
        return language switch
        {
            "python" => new ExecuteShellCommand
            {
                CommandName = command,
                Args = new List<string> { scriptPath }
            },
            "javascript" or "typescript" => new ExecuteShellCommand
            {
                CommandName = command,
                Args = new List<string> { scriptPath }
            },
            "go" => new ExecuteShellCommand
            {
                CommandName = command,
                Args = new List<string> { "run", scriptPath }
            },
            "csharp" or "dotnet" => new ExecuteShellCommand
            {
                CommandName = command,
                Args = new List<string> { "script", scriptPath }
            },
            _ => throw new NotSupportedException($"Language {language} not supported")
        };
    }

    private static string? GetResultContent(object result)
    {
        if (result is JsonElement jsonElement)
        {
            // If it's already a string, return it directly
            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                return jsonElement.GetString();
            }

            // If it's an object, try to extract "content" property
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                if (jsonElement.TryGetProperty("content", out var contentProp))
                {
                    return contentProp.GetString();
                }
            }
        }

        // If result is already a string, return it
        if (result is string str)
        {
            return str;
        }

        // Try to serialize and extract content
        try
        {
            var json = JsonSerializer.Serialize(result);
            var doc = JsonDocument.Parse(json);

            // Check if root is a string
            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                return doc.RootElement.GetString();
            }

            // Try to extract "content" property
            if (doc.RootElement.TryGetProperty("content", out var content))
            {
                return content.GetString();
            }
        }
        catch
        {
            // If serialization fails, return null
        }

        return null;
    }

    private static string? ExtractStderr(object? result)
    {
        if (result == null)
        {
            return null;
        }

        try
        {
            if (result is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
            {
                if (jsonElement.TryGetProperty("stderr", out var stderrProp))
                {
                    return stderrProp.GetString();
                }
                if (jsonElement.TryGetProperty("error", out var errorProp))
                {
                    return errorProp.GetString();
                }
            }

            // Try to serialize and extract stderr
            var json = JsonSerializer.Serialize(result);
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("stderr", out var stderr))
            {
                return stderr.GetString();
            }
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                return error.GetString();
            }
        }
        catch
        {
            // If extraction fails, return null
        }

        return null;
    }
}
