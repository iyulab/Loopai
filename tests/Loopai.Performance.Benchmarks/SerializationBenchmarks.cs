using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Loopai.Performance.Benchmarks;

/// <summary>
/// Benchmarks for JSON serialization/deserialization performance.
/// Tests the overhead of converting input/output data.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SerializationBenchmarks
{
    private readonly string _simpleJson = """{"text": "Hello World"}""";
    private readonly string _complexJson = """
    {
        "text": "This is a longer text with multiple words",
        "metadata": {
            "id": 123,
            "tags": ["spam", "email", "test"],
            "timestamp": "2025-10-28T10:00:00Z"
        },
        "nested": {
            "level1": {
                "level2": {
                    "value": 42
                }
            }
        }
    }
    """;

    private JsonDocument? _simpleDoc;
    private JsonDocument? _complexDoc;
    private object _simpleObject = new { text = "Hello World" };
    private object _complexObject = new
    {
        text = "This is a longer text with multiple words",
        metadata = new
        {
            id = 123,
            tags = new[] { "spam", "email", "test" },
            timestamp = "2025-10-28T10:00:00Z"
        },
        nested = new
        {
            level1 = new
            {
                level2 = new
                {
                    value = 42
                }
            }
        }
    };

    [GlobalSetup]
    public void Setup()
    {
        _simpleDoc = JsonDocument.Parse(_simpleJson);
        _complexDoc = JsonDocument.Parse(_complexJson);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _simpleDoc?.Dispose();
        _complexDoc?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public JsonDocument ParseSimpleJson()
    {
        return JsonDocument.Parse(_simpleJson);
    }

    [Benchmark]
    public JsonDocument ParseComplexJson()
    {
        return JsonDocument.Parse(_complexJson);
    }

    [Benchmark]
    public string SerializeSimpleObject()
    {
        return JsonSerializer.Serialize(_simpleObject);
    }

    [Benchmark]
    public string SerializeComplexObject()
    {
        return JsonSerializer.Serialize(_complexObject);
    }

    [Benchmark]
    public string GetRawTextSimple()
    {
        return _simpleDoc!.RootElement.GetRawText();
    }

    [Benchmark]
    public string GetRawTextComplex()
    {
        return _complexDoc!.RootElement.GetRawText();
    }
}
