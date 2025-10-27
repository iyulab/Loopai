using System.Text.Json;
using Loopai.Core.CodeBeaker;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Loopai.Core.Tests.CodeBeaker;

/// <summary>
/// Integration tests for CodeBeaker batch executor.
/// Requires CodeBeaker server running on ws://localhost:5000/ws/jsonrpc
/// </summary>
public class CodeBeakerBatchExecutorTests : IAsyncLifetime
{
    private readonly CodeBeakerBatchExecutor _batchExecutor;
    private readonly CodeBeakerSessionPool _sessionPool;
    private readonly Mock<IProgramArtifactRepository> _artifactRepositoryMock;
    private readonly Mock<IExecutionRecordRepository> _executionRepositoryMock;
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly ILogger<CodeBeakerBatchExecutor> _logger;

    public CodeBeakerBatchExecutorTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<CodeBeakerBatchExecutor>();

        var sessionPoolLogger = loggerFactory.CreateLogger<CodeBeakerSessionPool>();
        var clientLogger = loggerFactory.CreateLogger<CodeBeakerClient>();

        // Create real CodeBeaker client and session pool
        var options = Microsoft.Extensions.Options.Options.Create(new Models.CodeBeakerOptions
        {
            WebSocketUrl = "ws://localhost:5000/ws/jsonrpc",
            SessionPoolSize = 10,
            SessionIdleTimeoutMinutes = 30,
            SessionMaxLifetimeMinutes = 120
        });

        var client = new CodeBeakerClient(options, clientLogger);
        _sessionPool = new CodeBeakerSessionPool(client, options, sessionPoolLogger);

        // Create mocks for repositories
        _artifactRepositoryMock = new Mock<IProgramArtifactRepository>();
        _executionRepositoryMock = new Mock<IExecutionRecordRepository>();
        _taskRepositoryMock = new Mock<ITaskRepository>();

        _batchExecutor = new CodeBeakerBatchExecutor(
            _sessionPool,
            _artifactRepositoryMock.Object,
            _executionRepositoryMock.Object,
            _taskRepositoryMock.Object,
            _logger
        );
    }

    public async Task InitializeAsync()
    {
        // No initialization needed
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _sessionPool.DisposeAsync();
    }

    [Fact(Skip = "Requires CodeBeaker server running on localhost:5000")]
    public async Task ExecuteBatchAsync_PythonCode_ShouldExecuteSuccessfully()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        var task = new LoopaiTask
        {
            Id = taskId,
            Name = "Test Task",
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

        var batchItems = new[]
        {
            new BatchItem { Id = "item1", Input = JsonDocument.Parse("{\"value\": 10}"), ForceValidation = false },
            new BatchItem { Id = "item2", Input = JsonDocument.Parse("{\"value\": 20}"), ForceValidation = false },
            new BatchItem { Id = "item3", Input = JsonDocument.Parse("{\"value\": 30}"), ForceValidation = false }
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _artifactRepositoryMock
            .Setup(x => x.GetActiveByTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);

        _executionRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ExecutionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _batchExecutor.ExecuteBatchAsync(
            taskId,
            batchItems,
            version: null,
            maxConcurrency: 3,
            stopOnFirstError: false,
            timeoutMs: 30000
        );

        // Assert
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);

        Assert.Equal(20, result.Items[0].Output!.RootElement.GetProperty("output").GetInt32());
        Assert.Equal(40, result.Items[1].Output!.RootElement.GetProperty("output").GetInt32());
        Assert.Equal(60, result.Items[2].Output!.RootElement.GetProperty("output").GetInt32());

        // Verify session pool stats
        Assert.NotNull(result.SessionPoolStats);
        Assert.True(result.SessionPoolStats.TotalSessions > 0);

        _logger.LogInformation(
            "Batch completed: {SuccessCount}/{TotalItems} in {Duration}ms (avg: {AvgLatency}ms)",
            result.SuccessCount, result.TotalItems, result.TotalDurationMs, result.AvgLatencyMs);
    }

    [Fact(Skip = "Requires CodeBeaker server running on localhost:5000")]
    public async Task ExecuteBatchAsync_MultipleLanguages_ShouldExecuteSuccessfully()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var pythonProgramId = Guid.NewGuid();
        var jsProgramId = Guid.NewGuid();

        var task = new LoopaiTask
        {
            Id = taskId,
            Name = "Multi-Language Test",
            SamplingRate = 0.0
        };

        var pythonArtifact = new ProgramArtifact
        {
            Id = pythonProgramId,
            TaskId = taskId,
            Language = "python",
            Code = "result = {'language': 'python', 'value': input_data['value'] * 2}",
            Version = 1
        };

        var jsArtifact = new ProgramArtifact
        {
            Id = jsProgramId,
            TaskId = taskId,
            Language = "javascript",
            Code = "result = {language: 'javascript', value: input_data.value * 3};",
            Version = 2
        };

        var batchItems = new[]
        {
            new BatchItem { Id = "item1", Input = JsonDocument.Parse("{\"value\": 10}"), ForceValidation = false },
            new BatchItem { Id = "item2", Input = JsonDocument.Parse("{\"value\": 20}"), ForceValidation = false }
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // First call returns Python artifact, second call returns JS artifact
        var artifactCalls = 0;
        _artifactRepositoryMock
            .Setup(x => x.GetActiveByTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => artifactCalls++ == 0 ? pythonArtifact : jsArtifact);

        _executionRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ExecutionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - Execute Python batch
        var pythonResult = await _batchExecutor.ExecuteBatchAsync(
            taskId, batchItems, version: null, maxConcurrency: 2,
            stopOnFirstError: false, timeoutMs: 30000);

        // Act - Execute JavaScript batch
        var jsResult = await _batchExecutor.ExecuteBatchAsync(
            taskId, batchItems, version: null, maxConcurrency: 2,
            stopOnFirstError: false, timeoutMs: 30000);

        // Assert Python
        Assert.Equal(2, pythonResult.SuccessCount);
        Assert.Equal("python", pythonResult.Items[0].Output!.RootElement.GetProperty("language").GetString());

        // Assert JavaScript
        Assert.Equal(2, jsResult.SuccessCount);
        Assert.Equal("javascript", jsResult.Items[0].Output!.RootElement.GetProperty("language").GetString());

        // Verify session pool created sessions for both languages
        var stats = _sessionPool.GetStatistics();
        Assert.True(stats.TotalSessions >= 2, "Should have sessions for both Python and JavaScript");
    }

    [Fact(Skip = "Requires CodeBeaker server running on localhost:5000")]
    public async Task ExecuteBatchAsync_SessionReuse_ShouldReuseIdleSessions()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        var task = new LoopaiTask
        {
            Id = taskId,
            Name = "Session Reuse Test",
            SamplingRate = 0.0
        };

        var artifact = new ProgramArtifact
        {
            Id = programId,
            TaskId = taskId,
            Language = "python",
            Code = "result = {'iteration': input_data['iteration']}",
            Version = 1
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _artifactRepositoryMock
            .Setup(x => x.GetActiveByTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);

        _executionRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ExecutionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - Execute first batch
        var batch1Items = new[]
        {
            new BatchItem { Id = "item1", Input = JsonDocument.Parse("{\"iteration\": 1}"), ForceValidation = false },
            new BatchItem { Id = "item2", Input = JsonDocument.Parse("{\"iteration\": 1}"), ForceValidation = false }
        };

        var result1 = await _batchExecutor.ExecuteBatchAsync(
            taskId, batch1Items, version: null, maxConcurrency: 2,
            stopOnFirstError: false, timeoutMs: 30000);

        var statsAfterBatch1 = _sessionPool.GetStatistics();

        // Act - Execute second batch (should reuse sessions)
        var batch2Items = new[]
        {
            new BatchItem { Id = "item3", Input = JsonDocument.Parse("{\"iteration\": 2}"), ForceValidation = false },
            new BatchItem { Id = "item4", Input = JsonDocument.Parse("{\"iteration\": 2}"), ForceValidation = false }
        };

        var result2 = await _batchExecutor.ExecuteBatchAsync(
            taskId, batch2Items, version: null, maxConcurrency: 2,
            stopOnFirstError: false, timeoutMs: 30000);

        var statsAfterBatch2 = _sessionPool.GetStatistics();

        // Assert
        Assert.Equal(2, result1.SuccessCount);
        Assert.Equal(2, result2.SuccessCount);

        // Verify session reuse - total sessions should not increase significantly
        Assert.True(
            statsAfterBatch2.TotalSessions <= statsAfterBatch1.TotalSessions + 1,
            $"Expected session reuse. Batch1: {statsAfterBatch1.TotalSessions}, Batch2: {statsAfterBatch2.TotalSessions}");

        _logger.LogInformation(
            "Session reuse test: Batch1 sessions={Batch1Sessions}, Batch2 sessions={Batch2Sessions}",
            statsAfterBatch1.TotalSessions, statsAfterBatch2.TotalSessions);
    }

    [Fact(Skip = "Requires CodeBeaker server running on localhost:5000")]
    public async Task ExecuteBatchAsync_WithConcurrencyLimit_ShouldRespectLimit()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        var task = new LoopaiTask
        {
            Id = taskId,
            Name = "Concurrency Test",
            SamplingRate = 0.0
        };

        // Slow code that sleeps for 1 second
        var artifact = new ProgramArtifact
        {
            Id = programId,
            TaskId = taskId,
            Language = "python",
            Code = @"
import time
time.sleep(1)
result = {'value': input_data['value']}
",
            Version = 1
        };

        var batchItems = Enumerable.Range(1, 10).Select(i => new BatchItem
        {
            Id = $"item{i}",
            Input = JsonDocument.Parse($"{{\"value\": {i}}}"),
            ForceValidation = false
        }).ToArray();

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _artifactRepositoryMock
            .Setup(x => x.GetActiveByTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);

        _executionRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ExecutionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - Execute with concurrency limit of 3
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _batchExecutor.ExecuteBatchAsync(
            taskId,
            batchItems,
            version: null,
            maxConcurrency: 3,
            stopOnFirstError: false,
            timeoutMs: 60000
        );
        stopwatch.Stop();

        // Assert
        Assert.Equal(10, result.TotalItems);
        Assert.Equal(10, result.SuccessCount);

        // With concurrency=3 and 10 items taking 1s each, should take ~4 seconds (10/3 = 3.33 batches)
        // Add buffer for overhead
        Assert.True(stopwatch.Elapsed.TotalSeconds >= 3.0, $"Too fast: {stopwatch.Elapsed.TotalSeconds}s");
        Assert.True(stopwatch.Elapsed.TotalSeconds <= 8.0, $"Too slow: {stopwatch.Elapsed.TotalSeconds}s");

        _logger.LogInformation(
            "Concurrency test: {Items} items with limit=3 took {Duration}ms",
            result.TotalItems, result.TotalDurationMs);
    }

    [Fact(Skip = "Requires CodeBeaker server running on localhost:5000")]
    public async Task ExecuteBatchAsync_WithStopOnFirstError_ShouldStopAfterFirstFailure()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        var task = new LoopaiTask
        {
            Id = taskId,
            Name = "Stop On Error Test",
            SamplingRate = 0.0
        };

        // Code that fails if value is negative
        var artifact = new ProgramArtifact
        {
            Id = programId,
            TaskId = taskId,
            Language = "python",
            Code = @"
if input_data['value'] < 0:
    raise ValueError('Negative value not allowed')
result = {'value': input_data['value'] * 2}
",
            Version = 1
        };

        var batchItems = new[]
        {
            new BatchItem { Id = "item1", Input = JsonDocument.Parse("{\"value\": 10}"), ForceValidation = false },
            new BatchItem { Id = "item2", Input = JsonDocument.Parse("{\"value\": -5}"), ForceValidation = false },
            new BatchItem { Id = "item3", Input = JsonDocument.Parse("{\"value\": 20}"), ForceValidation = false },
            new BatchItem { Id = "item4", Input = JsonDocument.Parse("{\"value\": 30}"), ForceValidation = false }
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _artifactRepositoryMock
            .Setup(x => x.GetActiveByTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);

        _executionRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ExecutionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _batchExecutor.ExecuteBatchAsync(
            taskId,
            batchItems,
            version: null,
            maxConcurrency: 1,
            stopOnFirstError: true,
            timeoutMs: 30000
        );

        // Assert
        // Should stop after processing item1 and item2 (first error)
        Assert.True(result.TotalItems <= 3, $"Should process at most 3 items, processed {result.TotalItems}");
        Assert.True(result.FailureCount >= 1, "Should have at least 1 failure");

        _logger.LogInformation(
            "Stop on error test: Processed {TotalItems} items, {FailureCount} failures",
            result.TotalItems, result.FailureCount);
    }

    [Fact(Skip = "Requires CodeBeaker server running on localhost:5000")]
    public async Task ExecuteBatchAsync_WithErrorInCode_ShouldReturnFailureResult()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        var task = new LoopaiTask
        {
            Id = taskId,
            Name = "Error Handling Test",
            SamplingRate = 0.0
        };

        // Code with syntax error
        var artifact = new ProgramArtifact
        {
            Id = programId,
            TaskId = taskId,
            Language = "python",
            Code = "result = {'invalid': undefined_variable}",  // undefined_variable will cause error
            Version = 1
        };

        var batchItems = new[]
        {
            new BatchItem { Id = "item1", Input = JsonDocument.Parse("{\"value\": 10}"), ForceValidation = false }
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _artifactRepositoryMock
            .Setup(x => x.GetActiveByTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);

        _executionRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ExecutionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _batchExecutor.ExecuteBatchAsync(
            taskId,
            batchItems,
            version: null,
            maxConcurrency: 1,
            stopOnFirstError: false,
            timeoutMs: 30000
        );

        // Assert
        Assert.Equal(1, result.TotalItems);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.False(result.Items[0].Success);
        Assert.NotNull(result.Items[0].ErrorMessage);

        _logger.LogInformation(
            "Error handling test: Error message = {ErrorMessage}",
            result.Items[0].ErrorMessage);
    }

    [Fact(Skip = "Requires CodeBeaker server running on localhost:5000")]
    public async Task ExecuteBatchAsync_WithSampling_ShouldMarkSampledItems()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        var task = new LoopaiTask
        {
            Id = taskId,
            Name = "Sampling Test",
            SamplingRate = 1.0  // 100% sampling rate
        };

        var artifact = new ProgramArtifact
        {
            Id = programId,
            TaskId = taskId,
            Language = "python",
            Code = "result = {'value': input_data['value'] * 2}",
            Version = 1
        };

        var batchItems = new[]
        {
            new BatchItem { Id = "item1", Input = JsonDocument.Parse("{\"value\": 10}"), ForceValidation = false },
            new BatchItem { Id = "item2", Input = JsonDocument.Parse("{\"value\": 20}"), ForceValidation = true },  // Force validation
            new BatchItem { Id = "item3", Input = JsonDocument.Parse("{\"value\": 30}"), ForceValidation = false }
        };

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _artifactRepositoryMock
            .Setup(x => x.GetActiveByTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);

        _executionRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ExecutionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _batchExecutor.ExecuteBatchAsync(
            taskId,
            batchItems,
            version: null,
            maxConcurrency: 3,
            stopOnFirstError: false,
            timeoutMs: 30000
        );

        // Assert
        Assert.Equal(3, result.SuccessCount);

        // All items should be sampled (100% rate)
        Assert.True(result.Items[0].SampledForValidation, "Item1 should be sampled (100% rate)");
        Assert.True(result.Items[1].SampledForValidation, "Item2 should be sampled (ForceValidation=true)");
        Assert.True(result.Items[2].SampledForValidation, "Item3 should be sampled (100% rate)");

        _logger.LogInformation(
            "Sampling test: {SampledCount}/{TotalCount} items sampled",
            result.Items.Count(i => i.SampledForValidation), result.TotalItems);
    }

    [Fact(Skip = "Requires CodeBeaker server running on localhost:5000")]
    public async Task ExecuteBatchAsync_SessionPoolStatistics_ShouldBeAccurate()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        var task = new LoopaiTask
        {
            Id = taskId,
            Name = "Statistics Test",
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

        var batchItems = Enumerable.Range(1, 5).Select(i => new BatchItem
        {
            Id = $"item{i}",
            Input = JsonDocument.Parse($"{{\"value\": {i}}}"),
            ForceValidation = false
        }).ToArray();

        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _artifactRepositoryMock
            .Setup(x => x.GetActiveByTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);

        _executionRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ExecutionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _batchExecutor.ExecuteBatchAsync(
            taskId,
            batchItems,
            version: null,
            maxConcurrency: 3,
            stopOnFirstError: false,
            timeoutMs: 30000
        );

        // Assert
        Assert.NotNull(result.SessionPoolStats);
        Assert.True(result.SessionPoolStats.TotalSessions > 0, "Should have created sessions");
        Assert.True(result.SessionPoolStats.IdleSessions >= 0, "Idle sessions should be non-negative");
        Assert.True(result.SessionPoolStats.ActiveSessions >= 0, "Active sessions should be non-negative");
        Assert.Equal(
            result.SessionPoolStats.TotalSessions,
            result.SessionPoolStats.IdleSessions + result.SessionPoolStats.ActiveSessions,
            "Total = Idle + Active");

        _logger.LogInformation(
            "Statistics: Total={Total}, Active={Active}, Idle={Idle}, Available={Available}",
            result.SessionPoolStats.TotalSessions,
            result.SessionPoolStats.ActiveSessions,
            result.SessionPoolStats.IdleSessions,
            result.SessionPoolStats.AvailableSlots);
    }
}
