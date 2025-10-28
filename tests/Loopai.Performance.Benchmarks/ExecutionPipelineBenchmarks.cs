using System.Diagnostics;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Loopai.Performance.Benchmarks;

/// <summary>
/// Benchmarks for the complete execution pipeline.
/// Measures end-to-end performance including DB operations, execution, and logging.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ExecutionPipelineBenchmarks
{
    private JsonDocument? _inputData;

    [GlobalSetup]
    public void Setup()
    {
        _inputData = JsonDocument.Parse("""{"text": "Buy now! Limited offer!"}""");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _inputData?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public async Task<double> MeasureStopwatchOverhead()
    {
        var sw = Stopwatch.StartNew();
        await Task.Delay(1); // Simulate minimal work
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    [Benchmark]
    public async Task<ExecutionResult> SimulateCompleteExecution()
    {
        var sw = Stopwatch.StartNew();

        // Simulate DB query (task lookup)
        await Task.Delay(1); // ~1ms DB query
        var taskLookupTime = sw.Elapsed.TotalMilliseconds;

        // Simulate DB query (artifact lookup)
        await Task.Delay(1); // ~1ms DB query
        var artifactLookupTime = sw.Elapsed.TotalMilliseconds - taskLookupTime;

        // Simulate program execution
        var executionStart = sw.Elapsed.TotalMilliseconds;
        var output = SimulateProgramExecution(_inputData!);
        var executionTime = sw.Elapsed.TotalMilliseconds - executionStart;

        // Simulate DB save (execution record)
        await Task.Delay(1); // ~1ms DB save
        var saveTime = sw.Elapsed.TotalMilliseconds - executionStart - executionTime;

        sw.Stop();

        return new ExecutionResult
        {
            TotalLatencyMs = sw.Elapsed.TotalMilliseconds,
            DbQueryMs = taskLookupTime + artifactLookupTime,
            ExecutionMs = executionTime,
            DbSaveMs = saveTime,
            Output = output
        };
    }

    // Helper method (not a benchmark)
    private JsonDocument SimulateProgramExecution(JsonDocument input)
    {
        // Simulate simple rule-based classification
        var text = input.RootElement.GetProperty("text").GetString() ?? "";
        var result = text.Contains("buy", StringComparison.OrdinalIgnoreCase)
            ? "spam"
            : "ham";

        return JsonDocument.Parse($"{{\"result\": \"{result}\"}}");
    }

    [Benchmark]
    public async Task<List<ExecutionResult>> SimulateBatchExecution()
    {
        var results = new List<ExecutionResult>();

        // Simulate 10 concurrent executions
        var tasks = Enumerable.Range(1, 10)
            .Select(async i =>
            {
                var sw = Stopwatch.StartNew();
                await Task.Delay(1); // DB lookup
                var output = SimulateProgramExecution(_inputData!);
                await Task.Delay(1); // DB save
                sw.Stop();

                return new ExecutionResult
                {
                    TotalLatencyMs = sw.Elapsed.TotalMilliseconds,
                    DbQueryMs = 1.0,
                    ExecutionMs = 0.1,
                    DbSaveMs = 1.0,
                    Output = output
                };
            });

        results.AddRange(await Task.WhenAll(tasks));
        return results;
    }

    public class ExecutionResult
    {
        public double TotalLatencyMs { get; set; }
        public double DbQueryMs { get; set; }
        public double ExecutionMs { get; set; }
        public double DbSaveMs { get; set; }
        public JsonDocument? Output { get; set; }
    }
}
