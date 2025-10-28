using BenchmarkDotNet.Attributes;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Loopai.Performance.Benchmarks;

/// <summary>
/// Benchmarks to measure Deno process creation and execution overhead.
/// This is the PRIMARY bottleneck identified in the performance analysis.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ProcessCreationBenchmarks
{
    private string _tempDir = string.Empty;
    private string _denoPath = string.Empty;
    private string _simpleScriptPath = string.Empty;
    private string _jsonScriptPath = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "loopai_bench_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        // Find Deno executable
        _denoPath = FindDenoExecutable();
        if (string.IsNullOrEmpty(_denoPath))
        {
            throw new InvalidOperationException(
                "Deno executable not found. Please install Deno or add it to PATH.");
        }

        // Create a simple echo script
        _simpleScriptPath = Path.Combine(_tempDir, "simple.ts");
        File.WriteAllText(_simpleScriptPath, @"
console.log('Hello');
");

        // Create a JSON transformation script (realistic workload)
        _jsonScriptPath = Path.Combine(_tempDir, "json_transform.ts");
        File.WriteAllText(_jsonScriptPath, @"
const input = { text: 'Hello World' };

async function main(input: any) {
    return { result: input.text.toUpperCase() };
}

const result = await main(input);
console.log(JSON.stringify(result));
");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Baseline: Measure process creation overhead with minimal execution.
    /// This isolates the process startup cost.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<string> DenoProcessCreation_SimpleEcho()
    {
        return await ExecuteDenoScript(_simpleScriptPath);
    }

    /// <summary>
    /// Realistic workload: JSON input → transformation → JSON output.
    /// This simulates actual Loopai program execution.
    /// </summary>
    [Benchmark]
    public async Task<string> DenoProcessCreation_JsonTransform()
    {
        return await ExecuteDenoScript(_jsonScriptPath);
    }

    /// <summary>
    /// Measure creating temporary file + Deno execution + cleanup.
    /// This simulates the full DenoEdgeRuntimeService workflow.
    /// </summary>
    [Benchmark]
    public async Task<string> DenoProcessCreation_WithTempFile()
    {
        var tempScript = Path.Combine(_tempDir, $"temp_{Guid.NewGuid():N}.ts");

        try
        {
            // Write script
            await File.WriteAllTextAsync(tempScript, @"
const input = { text: 'test' };
console.log(JSON.stringify({ result: input.text }));
");

            // Execute
            return await ExecuteDenoScript(tempScript);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempScript))
            {
                File.Delete(tempScript);
            }
        }
    }

    /// <summary>
    /// For comparison: How fast would it be without process creation?
    /// This is what CodeBeaker session pooling can achieve.
    /// </summary>
    [Benchmark]
    public Task<string> NoProcessCreation_DirectExecution()
    {
        // Simulate the actual computation without process overhead
        var input = JsonDocument.Parse("{\"text\":\"Hello World\"}");
        var text = input.RootElement.GetProperty("text").GetString() ?? "";
        var result = text.ToUpperInvariant();
        var output = JsonSerializer.Serialize(new { result });

        return Task.FromResult(output);
    }

    // Helper methods

    private async Task<string> ExecuteDenoScript(string scriptPath)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = _denoPath,
            Arguments = $"run --allow-all --quiet {scriptPath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        var stdOutBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) stdOutBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();

        await process.WaitForExitAsync();

        return stdOutBuilder.ToString();
    }

    private static string FindDenoExecutable()
    {
        // Check common locations
        var candidates = new[]
        {
            "deno",  // In PATH
            "deno.exe",
            "/usr/local/bin/deno",
            "/usr/bin/deno",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".deno", "bin", "deno"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".deno", "bin", "deno.exe"),
        };

        foreach (var candidate in candidates)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    process.WaitForExit(1000);
                    if (process.ExitCode == 0)
                    {
                        return candidate;
                    }
                }
            }
            catch
            {
                // Try next candidate
                continue;
            }
        }

        return string.Empty;
    }
}
