using System.Diagnostics;
using System.Text.Json;
using Loopai.Core.CodeBeaker;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Loopai.Core.Tests.CodeBeaker;

/// <summary>
/// Performance benchmarks for CodeBeaker batch execution vs standard execution.
/// Requires CodeBeaker server running on ws://localhost:5000/ws/jsonrpc
/// </summary>
public class CodeBeakerBatchBenchmark : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly CodeBeakerBatchExecutor _batchExecutor;
    private readonly CodeBeakerSessionPool _sessionPool;
    private readonly Mock<IProgramArtifactRepository> _artifactRepositoryMock;
    private readonly Mock<IExecutionRecordRepository> _executionRepositoryMock;
    private readonly Mock<ITaskRepository> _taskRepositoryMock;

    public CodeBeakerBatchBenchmark(ITestOutputHelper output)
    {
        _output = output;

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<CodeBeakerBatchExecutor>();
        var sessionPoolLogger = loggerFactory.CreateLogger<CodeBeakerSessionPool>();
        var clientLogger = loggerFactory.CreateLogger<CodeBeakerClient>();

        var options = Microsoft.Extensions.Options.Options.Create(new Models.CodeBeakerOptions
        {
            WebSocketUrl = "ws://localhost:5000/ws/jsonrpc",
            SessionPoolSize = 20,
            SessionIdleTimeoutMinutes = 30,
            SessionMaxLifetimeMinutes = 120
        });

        var client = new CodeBeakerClient(options, clientLogger);
        _sessionPool = new CodeBeakerSessionPool(client, options, sessionPoolLogger);

        _artifactRepositoryMock = new Mock<IProgramArtifactRepository>();
        _executionRepositoryMock = new Mock<IExecutionRecordRepository>();
        _taskRepositoryMock = new Mock<ITaskRepository>();

        _batchExecutor = new CodeBeakerBatchExecutor(
            _sessionPool,
            _artifactRepositoryMock.Object,
            _executionRepositoryMock.Object,
            _taskRepositoryMock.Object,
            logger
        );
    }

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _sessionPool.DisposeAsync();
    }

    [Fact(Skip = "Performance benchmark - run manually")]
    public async Task Benchmark_SmallBatch_ComparePerformance()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        var task = new LoopaiTask
        {
            Id = taskId,
            Name = "Small Batch Benchmark",
            SamplingRate = 0.0
        };

        var artifact = new ProgramArtifact
        {
            Id = programId,
            TaskId = taskId,
            Language = "python",
            Code = "result = {'output': input_data['value'] * 2}",
            Version = 1
        };

        var batchItems = Enumerable.Range(1, 10).Select(i => new BatchItem
        {
            Id = $"item{i}",
            Input = JsonDocument.Parse($"{{\"value\": {i}}}"),
            ForceValidation = false
        }).ToArray();

        SetupMocks(task, artifact);

        _output.WriteLine("=== Small Batch Benchmark (10 items) ===\n");

        // Act - First run (cold start with session creation)
        var sw1 = Stopwatch.StartNew();
        var result1 = await _batchExecutor.ExecuteBatchAsync(
            taskId, batchItems, version: null, maxConcurrency: 5,
            stopOnFirstError: false, timeoutMs: 60000);
        sw1.Stop();

        _output.WriteLine($"Run 1 (Cold Start):");
        _output.WriteLine($"  Total Duration: {result1.TotalDurationMs:F2}ms");
        _output.WriteLine($"  Avg Latency: {result1.AvgLatencyMs:F2}ms");
        _output.WriteLine($"  Sessions Created: {result1.SessionPoolStats?.TotalSessions ?? 0}");
        _output.WriteLine($"  Success Rate: {result1.SuccessCount}/{result1.TotalItems}\n");

        // Act - Second run (warm start with session reuse)
        var sw2 = Stopwatch.StartNew();
        var result2 = await _batchExecutor.ExecuteBatchAsync(
            taskId, batchItems, version: null, maxConcurrency: 5,
            stopOnFirstError: false, timeoutMs: 60000);
        sw2.Stop();

        _output.WriteLine($"Run 2 (Warm Start - Session Reuse):");
        _output.WriteLine($"  Total Duration: {result2.TotalDurationMs:F2}ms");
        _output.WriteLine($"  Avg Latency: {result2.AvgLatencyMs:F2}ms");
        _output.WriteLine($"  Sessions: {result2.SessionPoolStats?.TotalSessions ?? 0}");
        _output.WriteLine($"  Success Rate: {result2.SuccessCount}/{result2.TotalItems}\n");

        // Assert - Warm start should be faster
        var improvement = ((result1.TotalDurationMs - result2.TotalDurationMs) / result1.TotalDurationMs) * 100;
        _output.WriteLine($"Performance Improvement: {improvement:F1}%");

        Assert.True(result2.TotalDurationMs < result1.TotalDurationMs * 1.2,
            "Warm start should not be significantly slower than cold start");
    }

    [Fact(Skip = "Performance benchmark - run manually")]
    public async Task Benchmark_MediumBatch_TestConcurrency()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        var task = new LoopaiTask
        {
            Id = taskId,
            Name = "Medium Batch Benchmark",
            SamplingRate = 0.0
        };

        var artifact = new ProgramArtifact
        {
            Id = programId,
            TaskId = taskId,
            Language = "python",
            Code = @"
import time
time.sleep(0.1)  # 100ms processing time
result = {'output': input_data['value'] * 2}
",
            Version = 1
        };

        var batchItems = Enumerable.Range(1, 50).Select(i => new BatchItem
        {
            Id = $"item{i}",
            Input = JsonDocument.Parse($"{{\"value\": {i}}}"),
            ForceValidation = false
        }).ToArray();

        SetupMocks(task, artifact);

        _output.WriteLine("=== Medium Batch Benchmark (50 items, 100ms each) ===\n");

        // Test different concurrency levels
        var concurrencyLevels = new[] { 1, 5, 10, 20 };

        foreach (var concurrency in concurrencyLevels)
        {
            var sw = Stopwatch.StartNew();
            var result = await _batchExecutor.ExecuteBatchAsync(
                taskId, batchItems, version: null, maxConcurrency: concurrency,
                stopOnFirstError: false, timeoutMs: 120000);
            sw.Stop();

            var throughput = result.TotalItems / (result.TotalDurationMs / 1000.0);

            _output.WriteLine($"Concurrency Level: {concurrency}");
            _output.WriteLine($"  Total Duration: {result.TotalDurationMs:F2}ms ({sw.ElapsedMilliseconds}ms wall time)");
            _output.WriteLine($"  Avg Latency: {result.AvgLatencyMs:F2}ms");
            _output.WriteLine($"  Throughput: {throughput:F2} items/sec");
            _output.WriteLine($"  Sessions: {result.SessionPoolStats?.TotalSessions ?? 0}");
            _output.WriteLine($"  Success Rate: {result.SuccessCount}/{result.TotalItems}\n");

            Assert.Equal(50, result.SuccessCount);
        }
    }

    [Fact(Skip = "Performance benchmark - run manually")]
    public async Task Benchmark_LargeBatch_TestScalability()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        var task = new LoopaiTask
        {
            Id = taskId,
            Name = "Large Batch Benchmark",
            SamplingRate = 0.1  // 10% sampling
        };

        var artifact = new ProgramArtifact
        {
            Id = programId,
            TaskId = taskId,
            Language = "python",
            Code = "result = {'output': input_data['value'] * 2, 'squared': input_data['value'] ** 2}",
            Version = 1
        };

        var batchItems = Enumerable.Range(1, 200).Select(i => new BatchItem
        {
            Id = $"item{i}",
            Input = JsonDocument.Parse($"{{\"value\": {i}}}"),
            ForceValidation = false
        }).ToArray();

        SetupMocks(task, artifact);

        _output.WriteLine("=== Large Batch Benchmark (200 items) ===\n");

        // Act
        var sw = Stopwatch.StartNew();
        var result = await _batchExecutor.ExecuteBatchAsync(
            taskId, batchItems, version: null, maxConcurrency: 15,
            stopOnFirstError: false, timeoutMs: 180000);
        sw.Stop();

        var throughput = result.TotalItems / (result.TotalDurationMs / 1000.0);
        var sampledCount = result.Items.Count(i => i.SampledForValidation);

        _output.WriteLine($"Total Duration: {result.TotalDurationMs:F2}ms");
        _output.WriteLine($"Wall Time: {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"Avg Latency: {result.AvgLatencyMs:F2}ms");
        _output.WriteLine($"Min Latency: {result.Items.Min(i => i.LatencyMs):F2}ms");
        _output.WriteLine($"Max Latency: {result.Items.Max(i => i.LatencyMs):F2}ms");
        _output.WriteLine($"Throughput: {throughput:F2} items/sec");
        _output.WriteLine($"Sessions: {result.SessionPoolStats?.TotalSessions ?? 0}");
        _output.WriteLine($"Success Rate: {result.SuccessCount}/{result.TotalItems}");
        _output.WriteLine($"Sampled for Validation: {sampledCount}/{result.TotalItems} ({(sampledCount * 100.0 / result.TotalItems):F1}%)");

        // Assert
        Assert.Equal(200, result.SuccessCount);
        Assert.True(throughput > 10, $"Throughput should be >10 items/sec, got {throughput:F2}");
    }

    [Fact(Skip = "Performance benchmark - run manually")]
    public async Task Benchmark_MultiLanguage_ComparePython_vs_JavaScript()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var pythonProgramId = Guid.NewGuid();
        var jsProgramId = Guid.NewGuid();

        var task = new LoopaiTask
        {
            Id = taskId,
            Name = "Multi-Language Benchmark",
            SamplingRate = 0.0
        };

        var pythonArtifact = new ProgramArtifact
        {
            Id = pythonProgramId,
            TaskId = taskId,
            Language = "python",
            Code = "result = {'language': 'python', 'output': input_data['value'] * 2}",
            Version = 1
        };

        var jsArtifact = new ProgramArtifact
        {
            Id = jsProgramId,
            TaskId = taskId,
            Language = "javascript",
            Code = "result = {language: 'javascript', output: input_data.value * 2};",
            Version = 2
        };

        var batchItems = Enumerable.Range(1, 30).Select(i => new BatchItem
        {
            Id = $"item{i}",
            Input = JsonDocument.Parse($"{{\"value\": {i}}}"),
            ForceValidation = false
        }).ToArray();

        _output.WriteLine("=== Multi-Language Benchmark (30 items each) ===\n");

        // Test Python
        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _artifactRepositoryMock
            .Setup(x => x.GetActiveByTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pythonArtifact);

        _executionRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ExecutionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var swPython = Stopwatch.StartNew();
        var pythonResult = await _batchExecutor.ExecuteBatchAsync(
            taskId, batchItems, version: null, maxConcurrency: 10,
            stopOnFirstError: false, timeoutMs: 60000);
        swPython.Stop();

        _output.WriteLine("Python Execution:");
        _output.WriteLine($"  Total Duration: {pythonResult.TotalDurationMs:F2}ms");
        _output.WriteLine($"  Avg Latency: {pythonResult.AvgLatencyMs:F2}ms");
        _output.WriteLine($"  Sessions: {pythonResult.SessionPoolStats?.TotalSessions ?? 0}");
        _output.WriteLine($"  Success Rate: {pythonResult.SuccessCount}/{pythonResult.TotalItems}\n");

        // Test JavaScript
        _artifactRepositoryMock
            .Setup(x => x.GetActiveByTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsArtifact);

        var swJs = Stopwatch.StartNew();
        var jsResult = await _batchExecutor.ExecuteBatchAsync(
            taskId, batchItems, version: null, maxConcurrency: 10,
            stopOnFirstError: false, timeoutMs: 60000);
        swJs.Stop();

        _output.WriteLine("JavaScript Execution:");
        _output.WriteLine($"  Total Duration: {jsResult.TotalDurationMs:F2}ms");
        _output.WriteLine($"  Avg Latency: {jsResult.AvgLatencyMs:F2}ms");
        _output.WriteLine($"  Sessions: {jsResult.SessionPoolStats?.TotalSessions ?? 0}");
        _output.WriteLine($"  Success Rate: {jsResult.SuccessCount}/{jsResult.TotalItems}\n");

        // Compare
        var pythonFaster = pythonResult.AvgLatencyMs < jsResult.AvgLatencyMs;
        var difference = Math.Abs(pythonResult.AvgLatencyMs - jsResult.AvgLatencyMs);
        var percentDiff = (difference / Math.Min(pythonResult.AvgLatencyMs, jsResult.AvgLatencyMs)) * 100;

        _output.WriteLine($"Comparison:");
        _output.WriteLine($"  {(pythonFaster ? "Python" : "JavaScript")} is faster by {difference:F2}ms ({percentDiff:F1}%)");

        // Assert both succeeded
        Assert.Equal(30, pythonResult.SuccessCount);
        Assert.Equal(30, jsResult.SuccessCount);
    }

    [Fact(Skip = "Performance benchmark - run manually")]
    public async Task Benchmark_SessionReuse_MeasureOverhead()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        var task = new LoopaiTask
        {
            Id = taskId,
            Name = "Session Reuse Overhead",
            SamplingRate = 0.0
        };

        var artifact = new ProgramArtifact
        {
            Id = programId,
            TaskId = taskId,
            Language = "python",
            Code = "result = {'value': input_data['value']}",
            Version = 1
        };

        SetupMocks(task, artifact);

        _output.WriteLine("=== Session Reuse Overhead Benchmark ===\n");

        // Run multiple small batches to measure session reuse overhead
        var results = new List<BatchExecutionResult>();

        for (int batch = 1; batch <= 5; batch++)
        {
            var batchItems = Enumerable.Range(1, 5).Select(i => new BatchItem
            {
                Id = $"batch{batch}_item{i}",
                Input = JsonDocument.Parse($"{{\"value\": {i}}}"),
                ForceValidation = false
            }).ToArray();

            var result = await _batchExecutor.ExecuteBatchAsync(
                taskId, batchItems, version: null, maxConcurrency: 5,
                stopOnFirstError: false, timeoutMs: 30000);

            results.Add(result);

            _output.WriteLine($"Batch {batch}:");
            _output.WriteLine($"  Total Duration: {result.TotalDurationMs:F2}ms");
            _output.WriteLine($"  Avg Latency: {result.AvgLatencyMs:F2}ms");
            _output.WriteLine($"  Sessions: Total={result.SessionPoolStats?.TotalSessions ?? 0}, " +
                            $"Active={result.SessionPoolStats?.ActiveSessions ?? 0}, " +
                            $"Idle={result.SessionPoolStats?.IdleSessions ?? 0}");
        }

        _output.WriteLine($"\n=== Summary ===");
        _output.WriteLine($"First Batch (Cold): {results[0].AvgLatencyMs:F2}ms");
        _output.WriteLine($"Last Batch (Warm): {results[^1].AvgLatencyMs:F2}ms");
        _output.WriteLine($"Improvement: {((results[0].AvgLatencyMs - results[^1].AvgLatencyMs) / results[0].AvgLatencyMs * 100):F1}%");
        _output.WriteLine($"Final Session Count: {results[^1].SessionPoolStats?.TotalSessions ?? 0}");

        // Assert all batches succeeded
        Assert.True(results.All(r => r.SuccessCount == 5), "All batches should succeed");
    }

    [Fact(Skip = "Performance benchmark - run manually")]
    public async Task Benchmark_ErrorRecovery_MeasureImpact()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        var task = new LoopaiTask
        {
            Id = taskId,
            Name = "Error Recovery Benchmark",
            SamplingRate = 0.0
        };

        // Code that fails 20% of the time
        var artifact = new ProgramArtifact
        {
            Id = programId,
            TaskId = taskId,
            Language = "python",
            Code = @"
if input_data['value'] % 5 == 0:
    raise ValueError('Multiple of 5')
result = {'value': input_data['value'] * 2}
",
            Version = 1
        };

        var batchItems = Enumerable.Range(1, 50).Select(i => new BatchItem
        {
            Id = $"item{i}",
            Input = JsonDocument.Parse($"{{\"value\": {i}}}"),
            ForceValidation = false
        }).ToArray();

        SetupMocks(task, artifact);

        _output.WriteLine("=== Error Recovery Impact Benchmark (50 items, 20% error rate) ===\n");

        // Act
        var sw = Stopwatch.StartNew();
        var result = await _batchExecutor.ExecuteBatchAsync(
            taskId, batchItems, version: null, maxConcurrency: 10,
            stopOnFirstError: false, timeoutMs: 60000);
        sw.Stop();

        var successRate = (result.SuccessCount * 100.0) / result.TotalItems;
        var errorRate = (result.FailureCount * 100.0) / result.TotalItems;

        _output.WriteLine($"Total Duration: {result.TotalDurationMs:F2}ms");
        _output.WriteLine($"Success Rate: {result.SuccessCount}/{result.TotalItems} ({successRate:F1}%)");
        _output.WriteLine($"Error Rate: {result.FailureCount}/{result.TotalItems} ({errorRate:F1}%)");
        _output.WriteLine($"Avg Latency (Success): {result.Items.Where(i => i.Success).Average(i => i.LatencyMs):F2}ms");
        _output.WriteLine($"Avg Latency (Failure): {result.Items.Where(i => !i.Success).Average(i => i.LatencyMs):F2}ms");

        // Assert expected error rate (10 out of 50 items should fail)
        Assert.Equal(10, result.FailureCount);
        Assert.Equal(40, result.SuccessCount);
    }

    private void SetupMocks(LoopaiTask task, ProgramArtifact artifact)
    {
        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _artifactRepositoryMock
            .Setup(x => x.GetActiveByTaskIdAsync(task.TaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);

        _executionRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ExecutionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}
