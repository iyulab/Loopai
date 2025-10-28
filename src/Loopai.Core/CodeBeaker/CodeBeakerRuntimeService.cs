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
            var scriptPath = $"/workspace/program{config.Extension}";
            var inputPath = "/workspace/input.json";
            var outputPath = "/workspace/output.json";

            // Write input to file
            var inputJson = JsonSerializer.Serialize(
                JsonDocument.Parse(input.RootElement.GetRawText()).RootElement,
                new JsonSerializerOptions { WriteIndented = false });

            await ExecuteCommandAsync(session, new WriteFileCommand
            {
                Path = inputPath,
                Content = inputJson
            }, cancellationToken);

            // Write program code
            var wrappedCode = WrapCodeWithInputOutput(code, normalizedLanguage, inputPath, outputPath);
            await ExecuteCommandAsync(session, new WriteFileCommand
            {
                Path = scriptPath,
                Content = wrappedCode
            }, cancellationToken);

            // Execute program
            var executeCommand = CreateExecuteCommand(normalizedLanguage, scriptPath, config.Command);
            var executeResult = await ExecuteCommandAsync(session, executeCommand, cancellationToken);

            session.ExecutionCount++;

            if (!executeResult.Success)
            {
                stopwatch.Stop();
                return new EdgeExecutionResult
                {
                    Success = false,
                    Error = executeResult.Error ?? "Execution failed",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    MemoryUsedBytes = 0,
                    StandardError = executeResult.Error
                };
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
        return $@"
const fs = require('fs');

// Read input
const input_data = JSON.parse(fs.readFileSync('{inputPath}', 'utf8'));

// User code
{code}

// Write output (assumes 'result' variable exists)
fs.writeFileSync('{outputPath}', JSON.stringify(result));
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
            if (jsonElement.TryGetProperty("content", out var contentProp))
            {
                return contentProp.GetString();
            }
        }

        // Try to serialize and extract content
        var json = JsonSerializer.Serialize(result);
        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("content", out var content))
        {
            return content.GetString();
        }

        return null;
    }
}
