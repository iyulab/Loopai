using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Loopai.Performance.Benchmarks;

/// <summary>
/// Benchmarks for file I/O operations.
/// Measures the overhead of temporary file creation/deletion used in Deno runtime.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class FileIoBenchmarks
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "loopai-benchmarks");
    private readonly string _programCode = """
        async function main(input) {
            const text = input.text.toLowerCase();
            return {
                result: text.includes('buy') ? 'spam' : 'ham'
            };
        }
        """;

    [GlobalSetup]
    public void Setup()
    {
        Directory.CreateDirectory(_tempDir);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Benchmark(Baseline = true)]
    public async Task WriteAndDeleteTempFile()
    {
        var filePath = Path.Combine(_tempDir, $"program_{Guid.NewGuid()}.ts");

        // Write
        await File.WriteAllTextAsync(filePath, _programCode);

        // Delete
        File.Delete(filePath);
    }

    [Benchmark]
    public async Task WriteAndDeleteTempFileWithEncoding()
    {
        var filePath = Path.Combine(_tempDir, $"program_{Guid.NewGuid()}.ts");

        // Write with UTF8
        await File.WriteAllTextAsync(filePath, _programCode, Encoding.UTF8);

        // Delete
        File.Delete(filePath);
    }

    [Benchmark]
    public void WriteAndDeleteTempFileSync()
    {
        var filePath = Path.Combine(_tempDir, $"program_{Guid.NewGuid()}.ts");

        // Synchronous write
        File.WriteAllText(filePath, _programCode);

        // Delete
        File.Delete(filePath);
    }

    [Benchmark]
    public async Task WriteToMemoryStream()
    {
        // Alternative: keep in memory instead of file
        using var ms = new MemoryStream();
        await using var writer = new StreamWriter(ms);
        await writer.WriteAsync(_programCode);
        await writer.FlushAsync();

        // Read back
        ms.Position = 0;
        using var reader = new StreamReader(ms);
        await reader.ReadToEndAsync();
    }

    [Benchmark]
    public async Task MultipleFileOperations()
    {
        // Simulate multiple executions
        var tasks = Enumerable.Range(1, 5)
            .Select(async i =>
            {
                var filePath = Path.Combine(_tempDir, $"program_{Guid.NewGuid()}.ts");
                await File.WriteAllTextAsync(filePath, _programCode);
                File.Delete(filePath);
            });

        await Task.WhenAll(tasks);
    }
}
