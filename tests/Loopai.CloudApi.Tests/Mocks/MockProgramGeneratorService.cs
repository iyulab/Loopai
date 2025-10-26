using System.Text.Json;
using Loopai.Core.Interfaces;

namespace Loopai.CloudApi.Tests.Mocks;

/// <summary>
/// Mock implementation of IProgramGeneratorService for testing.
/// </summary>
public class MockProgramGeneratorService : IProgramGeneratorService
{
    private readonly Dictionary<Guid, string> _taskPrograms = new();
    private bool _shouldFail = false;
    private string? _errorMessage = null;
    private int _generationDelay = 0;

    public void ConfigureProgram(Guid taskId, string code)
    {
        _taskPrograms[taskId] = code;
    }

    public void ConfigureFailure(string errorMessage)
    {
        _shouldFail = true;
        _errorMessage = errorMessage;
    }

    public void ConfigureDelay(int delayMs)
    {
        _generationDelay = delayMs;
    }

    public void Reset()
    {
        _taskPrograms.Clear();
        _shouldFail = false;
        _errorMessage = null;
        _generationDelay = 0;
    }

    public async Task<ProgramGenerationResult> GenerateProgramAsync(
        Guid taskId,
        IReadOnlyList<(JsonDocument Input, JsonDocument Output)>? examples = null,
        string? constraints = null,
        CancellationToken cancellationToken = default)
    {
        if (_generationDelay > 0)
        {
            await Task.Delay(_generationDelay, cancellationToken);
        }

        if (_shouldFail)
        {
            return new ProgramGenerationResult
            {
                Success = false,
                Code = null,
                Language = "typescript",
                LinesOfCode = 0,
                CyclomaticComplexity = 0,
                EstimatedTokens = 0,
                ErrorMessage = _errorMessage ?? "Mock generation failure"
            };
        }

        // Get configured program or generate a simple one
        var code = _taskPrograms.TryGetValue(taskId, out var configuredCode)
            ? configuredCode
            : GenerateDefaultProgram(taskId, examples);

        return new ProgramGenerationResult
        {
            Success = true,
            Code = code,
            Language = "typescript",
            LinesOfCode = code.Split('\n').Length,
            CyclomaticComplexity = 1,
            EstimatedTokens = code.Length / 4,
            Metadata = JsonDocument.Parse("{\"mock\": true}")
        };
    }

    private static string GenerateDefaultProgram(
        Guid taskId,
        IReadOnlyList<(JsonDocument Input, JsonDocument Output)>? examples)
    {
        // Generate a simple passthrough program
        return @"
async function main(input: any): Promise<any> {
    // Auto-generated mock program
    return {
        ...input,
        processed: true,
        timestamp: new Date().toISOString()
    };
}
";
    }
}
