using System.Text.Json;
using Loopai.CloudApi.Tests.Mocks;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Loopai.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Loopai.CloudApi.Tests.Integration;

/// <summary>
/// End-to-end workflow tests covering the complete program lifecycle.
/// </summary>
public class EndToEndWorkflowTests : IDisposable
{
    private readonly InMemoryTaskRepository _taskRepo;
    private readonly InMemoryProgramArtifactRepository _artifactRepo;
    private readonly InMemoryExecutionRecordRepository _executionRepo;
    private readonly Mock<ILogger<TaskService>> _taskServiceLogger;
    private readonly Mock<ILogger<ProgramExecutionService>> _executionServiceLogger;
    private readonly MockProgramGeneratorService _mockGenerator;
    private readonly MockEdgeRuntimeService _mockRuntime;
    private readonly TaskService _taskService;
    private readonly ProgramExecutionService _executionService;

    public EndToEndWorkflowTests()
    {
        _taskRepo = new InMemoryTaskRepository();
        _artifactRepo = new InMemoryProgramArtifactRepository();
        _executionRepo = new InMemoryExecutionRecordRepository();
        _taskServiceLogger = new Mock<ILogger<TaskService>>();
        _executionServiceLogger = new Mock<ILogger<ProgramExecutionService>>();
        _mockGenerator = new MockProgramGeneratorService();
        _mockRuntime = new MockEdgeRuntimeService();

        _taskService = new TaskService(_taskRepo, _artifactRepo, _taskServiceLogger.Object);
        _executionService = new ProgramExecutionService(
            _taskRepo,
            _artifactRepo,
            _executionRepo,
            _executionServiceLogger.Object,
            _mockGenerator,
            _mockRuntime
        );
    }

    [Fact]
    public async Task CompleteWorkflow_CreateTaskAndExecute_Success()
    {
        // Step 1: Create a task
        var task = new TaskSpecification
        {
            Name = "double-number",
            Description = "Doubles the input number",
            InputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "value": { "type": "number" }
                }
            }
            """),
            OutputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "result": { "type": "number" }
                }
            }
            """),
            SamplingRate = 1.0
        };

        var createdTask = await _taskService.CreateTaskAsync(task);
        Assert.NotEqual(Guid.Empty, createdTask.Id);

        // Step 2: Configure mock generator to create a program
        var programCode = @"
async function main(input: any): Promise<any> {
    return { result: input.value * 2 };
}";
        _mockGenerator.ConfigureProgram(createdTask.Id, programCode);

        // Step 3: Configure mock runtime to execute the program
        _mockRuntime.ConfigureExecutor(programCode, input =>
        {
            var value = input.RootElement.GetProperty("value").GetInt32();
            return JsonDocument.Parse($"{{\"result\": {value * 2}}}");
        });

        // Step 4: Execute the program (should trigger generation)
        var input = JsonDocument.Parse("{\"value\": 21}");
        var executionResult = await _executionService.ExecuteAsync(createdTask.Id, input);

        // Verify execution
        Assert.Equal(ExecutionStatus.Success, executionResult.Status);
        Assert.NotNull(executionResult.Output);
        Assert.Equal(42, executionResult.Output.RootElement.GetProperty("result").GetInt32());
        Assert.Equal(1, executionResult.Version);

        // Step 5: Verify artifact was created
        var artifacts = await _executionService.GetArtifactsAsync(createdTask.Id);
        var artifactsList = artifacts.ToList();
        Assert.Single(artifactsList);
        Assert.Equal(programCode, artifactsList[0].Code);
        Assert.Equal(ProgramStatus.Active, artifactsList[0].Status);

        // Step 6: Execute again (should use existing artifact)
        var input2 = JsonDocument.Parse("{\"value\": 50}");
        var executionResult2 = await _executionService.ExecuteAsync(createdTask.Id, input2);

        Assert.Equal(ExecutionStatus.Success, executionResult2.Status);
        Assert.Equal(100, executionResult2.Output!.RootElement.GetProperty("result").GetInt32());
        Assert.Equal(executionResult.ProgramId, executionResult2.ProgramId); // Same program used

        // Step 7: Verify execution history
        var history = await _executionService.GetExecutionHistoryAsync(createdTask.Id);
        var historyList = history.ToList();
        Assert.Equal(2, historyList.Count);

        // Step 8: Check execution statistics
        var stats = await _executionService.GetExecutionStatisticsAsync(createdTask.Id);
        Assert.Equal(2, stats.TotalExecutions);
        Assert.Equal(2, stats.SuccessCount);
        Assert.Equal(0, stats.ErrorCount);
        Assert.Equal(2, stats.SampledCount); // All sampled due to 100% sampling rate
    }

    [Fact]
    public async Task MultipleExecutions_SamplingRate_RecordsCorrectly()
    {
        // Create task with 0% sampling rate
        var task = new TaskSpecification
        {
            Name = "sampled-task",
            Description = "Task with sampling",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            SamplingRate = 0.0 // No sampling
        };

        var createdTask = await _taskService.CreateTaskAsync(task);

        // Create an artifact directly
        var artifact = new ProgramArtifact
        {
            TaskId = createdTask.Id,
            Version = 1,
            Code = "async function main(input) { return input; }",
            Status = ProgramStatus.Active
        };
        await _artifactRepo.CreateAsync(artifact);

        // Execute multiple times
        for (int i = 0; i < 10; i++)
        {
            var input = JsonDocument.Parse($"{{\"iteration\": {i}}}");
            await _executionService.ExecuteAsync(createdTask.Id, input, forceValidation: false);
        }

        // Check statistics
        var stats = await _executionService.GetExecutionStatisticsAsync(createdTask.Id);
        Assert.Equal(10, stats.TotalExecutions);
        Assert.Equal(0, stats.SampledCount); // No sampling due to 0% rate
    }

    [Fact]
    public async Task Workflow_GenerationFailure_HandlesGracefully()
    {
        // Create task
        var task = new TaskSpecification
        {
            Name = "failing-task",
            Description = "Task that fails generation",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}")
        };

        var createdTask = await _taskService.CreateTaskAsync(task);

        // Configure generator to fail
        _mockGenerator.ConfigureFailure("Service temporarily unavailable");

        // Try to execute
        var input = JsonDocument.Parse("{\"test\": true}");
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _executionService.ExecuteAsync(createdTask.Id, input)
        );

        Assert.Contains("Program generation failed", exception.Message);

        // Verify no artifacts were created
        var artifacts = await _executionService.GetArtifactsAsync(createdTask.Id);
        Assert.Empty(artifacts);
    }

    [Fact]
    public async Task Workflow_ExecutionFailure_RecordsErrorStatus()
    {
        // Create task
        var task = new TaskSpecification
        {
            Name = "error-task",
            Description = "Task that fails execution",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}")
        };

        var createdTask = await _taskService.CreateTaskAsync(task);

        // Create buggy program
        var buggyCode = "throw new Error('Bug in program');";
        var artifact = new ProgramArtifact
        {
            TaskId = createdTask.Id,
            Version = 1,
            Code = buggyCode,
            Status = ProgramStatus.Active
        };
        await _artifactRepo.CreateAsync(artifact);

        // Configure runtime to fail
        _mockRuntime.ConfigureFailure("Bug in program");

        // Execute
        var input = JsonDocument.Parse("{\"test\": true}");
        var result = await _executionService.ExecuteAsync(createdTask.Id, input);

        // Verify error was recorded
        Assert.Equal(ExecutionStatus.Error, result.Status);
        Assert.Null(result.Output);
        Assert.NotNull(result.ErrorMessage);

        // Check statistics reflect the error
        var stats = await _executionService.GetExecutionStatisticsAsync(createdTask.Id);
        Assert.Equal(1, stats.TotalExecutions);
        Assert.Equal(0, stats.SuccessCount);
        Assert.Equal(1, stats.ErrorCount);
    }

    [Fact]
    public async Task Workflow_GetTaskWithActiveArtifact_ReturnsCorrectInfo()
    {
        // Create task
        var task = new TaskSpecification
        {
            Name = "artifact-info-task",
            Description = "Task for testing artifact info",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}")
        };

        var createdTask = await _taskService.CreateTaskAsync(task);

        // Initially no active artifact
        var initialInfo = await _taskService.GetTaskWithActiveArtifactAsync(createdTask.Id);
        // Service returns task info but no active version
        Assert.NotNull(initialInfo);
        Assert.Null(initialInfo.ActiveVersion);

        // Create active artifact
        var artifact = new ProgramArtifact
        {
            TaskId = createdTask.Id,
            Version = 1,
            Code = "async function main(input) { return input; }",
            Status = ProgramStatus.Active,
            ComplexityMetrics = new ComplexityMetrics
            {
                LinesOfCode = 5,
                CyclomaticComplexity = 1
            }
        };
        await _artifactRepo.CreateAsync(artifact);

        // Now should return artifact info
        var infoWithArtifact = await _taskService.GetTaskWithActiveArtifactAsync(createdTask.Id);
        Assert.NotNull(infoWithArtifact);
        Assert.Equal(createdTask.Id, infoWithArtifact.Task.Id);
        Assert.NotNull(infoWithArtifact.ActiveVersion);
        Assert.Equal(1, infoWithArtifact.ActiveVersion);
    }

    [Fact]
    public async Task Workflow_SpecificVersionExecution_UsesCorrectVersion()
    {
        // Create task
        var task = new TaskSpecification
        {
            Name = "versioned-task",
            Description = "Task with multiple versions",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}")
        };

        var createdTask = await _taskService.CreateTaskAsync(task);

        // Create version 1
        var v1Code = "v1-code";
        var artifact1 = new ProgramArtifact
        {
            TaskId = createdTask.Id,
            Version = 1,
            Code = v1Code,
            Status = ProgramStatus.Deprecated
        };
        await _artifactRepo.CreateAsync(artifact1);

        _mockRuntime.ConfigureExecutor(v1Code, _ =>
            JsonDocument.Parse("{\"version\": 1}"));

        // Create version 2 (active)
        var v2Code = "v2-code";
        var artifact2 = new ProgramArtifact
        {
            TaskId = createdTask.Id,
            Version = 2,
            Code = v2Code,
            Status = ProgramStatus.Active
        };
        await _artifactRepo.CreateAsync(artifact2);

        _mockRuntime.ConfigureExecutor(v2Code, _ =>
            JsonDocument.Parse("{\"version\": 2}"));

        var input = JsonDocument.Parse("{}");

        // Execute with default (should use v2)
        var defaultResult = await _executionService.ExecuteAsync(createdTask.Id, input);
        Assert.Equal(2, defaultResult.Version);
        Assert.Equal(2, defaultResult.Output!.RootElement.GetProperty("version").GetInt32());

        // Execute with specific version 1
        var v1Result = await _executionService.ExecuteAsync(createdTask.Id, input, version: 1);
        Assert.Equal(1, v1Result.Version);
        Assert.Equal(1, v1Result.Output!.RootElement.GetProperty("version").GetInt32());
    }

    public void Dispose()
    {
        _mockGenerator.Reset();
        _mockRuntime.Reset();
    }

    // Helper repositories for in-memory testing
    private class InMemoryTaskRepository : ITaskRepository
    {
        private readonly Dictionary<Guid, TaskSpecification> _tasks = new();
        private readonly Dictionary<string, Guid> _nameIndex = new();

        public Task<TaskSpecification> CreateAsync(TaskSpecification task, CancellationToken cancellationToken = default)
        {
            var created = task with
            {
                Id = task.Id == Guid.Empty ? Guid.NewGuid() : task.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _tasks[created.Id] = created;
            _nameIndex[created.Name] = created.Id;
            return Task.FromResult(created);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (_tasks.TryGetValue(id, out var task))
            {
                _tasks.Remove(id);
                _nameIndex.Remove(task.Name);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tasks.ContainsKey(id));
        }

        public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_nameIndex.ContainsKey(name));
        }

        public Task<IEnumerable<TaskSpecification>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<TaskSpecification>>(_tasks.Values.ToList());
        }

        public Task<TaskSpecification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _tasks.TryGetValue(id, out var task);
            return Task.FromResult(task);
        }

        public Task<TaskSpecification?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (_nameIndex.TryGetValue(name, out var id))
            {
                return GetByIdAsync(id, cancellationToken);
            }
            return Task.FromResult<TaskSpecification?>(null);
        }

        public Task<TaskSpecification> UpdateAsync(TaskSpecification task, CancellationToken cancellationToken = default)
        {
            var updated = task with { UpdatedAt = DateTime.UtcNow };
            _tasks[updated.Id] = updated;
            _nameIndex[updated.Name] = updated.Id;
            return Task.FromResult(updated);
        }
    }

    private class InMemoryProgramArtifactRepository : IProgramArtifactRepository
    {
        private readonly Dictionary<Guid, ProgramArtifact> _artifacts = new();

        public Task<ProgramArtifact> CreateAsync(ProgramArtifact artifact, CancellationToken cancellationToken = default)
        {
            var created = artifact with
            {
                Id = artifact.Id == Guid.Empty ? Guid.NewGuid() : artifact.Id,
                CreatedAt = DateTime.UtcNow
            };
            _artifacts[created.Id] = created;
            return Task.FromResult(created);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_artifacts.Remove(id));
        }

        public Task<ProgramArtifact?> GetActiveByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var active = _artifacts.Values
                .Where(a => a.TaskId == taskId && a.Status == ProgramStatus.Active)
                .OrderByDescending(a => a.Version)
                .FirstOrDefault();
            return Task.FromResult(active);
        }

        public Task<ProgramArtifact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _artifacts.TryGetValue(id, out var artifact);
            return Task.FromResult(artifact);
        }

        public Task<IEnumerable<ProgramArtifact>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var artifacts = _artifacts.Values
                .Where(a => a.TaskId == taskId)
                .OrderByDescending(a => a.Version)
                .ToList();
            return Task.FromResult<IEnumerable<ProgramArtifact>>(artifacts);
        }

        public Task<ProgramArtifact?> GetByTaskIdAndVersionAsync(Guid taskId, int version, CancellationToken cancellationToken = default)
        {
            var artifact = _artifacts.Values
                .FirstOrDefault(a => a.TaskId == taskId && a.Version == version);
            return Task.FromResult(artifact);
        }

        public Task<int> GetLatestVersionAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var maxVersion = _artifacts.Values
                .Where(a => a.TaskId == taskId)
                .Max(a => (int?)a.Version) ?? 0;
            return Task.FromResult(maxVersion);
        }

        public Task<ProgramArtifact> UpdateAsync(ProgramArtifact artifact, CancellationToken cancellationToken = default)
        {
            _artifacts[artifact.Id] = artifact;
            return Task.FromResult(artifact);
        }
    }

    private class InMemoryExecutionRecordRepository : IExecutionRecordRepository
    {
        private readonly List<ExecutionRecord> _records = new();

        public Task<ExecutionRecord> CreateAsync(ExecutionRecord record, CancellationToken cancellationToken = default)
        {
            var created = record with
            {
                Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id,
                ExecutedAt = record.ExecutedAt == default ? DateTime.UtcNow : record.ExecutedAt
            };
            _records.Add(created);
            return Task.FromResult(created);
        }

        public Task<IEnumerable<ExecutionRecord>> CreateBatchAsync(IEnumerable<ExecutionRecord> records, CancellationToken cancellationToken = default)
        {
            var created = records.Select(r => r with
            {
                Id = r.Id == Guid.Empty ? Guid.NewGuid() : r.Id,
                ExecutedAt = r.ExecutedAt == default ? DateTime.UtcNow : r.ExecutedAt
            }).ToList();
            _records.AddRange(created);
            return Task.FromResult<IEnumerable<ExecutionRecord>>(created);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var record = _records.FirstOrDefault(r => r.Id == id);
            if (record != null)
            {
                _records.Remove(record);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<ExecutionRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var record = _records.FirstOrDefault(r => r.Id == id);
            return Task.FromResult(record);
        }

        public Task<IEnumerable<ExecutionRecord>> GetByProgramIdAsync(Guid programId, int? limit = null, CancellationToken cancellationToken = default)
        {
            var query = _records.Where(r => r.ProgramId == programId)
                .OrderByDescending(r => r.ExecutedAt);

            if (limit.HasValue)
            {
                query = (IOrderedEnumerable<ExecutionRecord>)query.Take(limit.Value);
            }

            return Task.FromResult<IEnumerable<ExecutionRecord>>(query.ToList());
        }

        public Task<IEnumerable<ExecutionRecord>> GetByTaskIdAsync(Guid taskId, int? limit = null, CancellationToken cancellationToken = default)
        {
            var query = _records.Where(r => r.TaskId == taskId)
                .OrderByDescending(r => r.ExecutedAt);

            if (limit.HasValue)
            {
                query = (IOrderedEnumerable<ExecutionRecord>)query.Take(limit.Value);
            }

            return Task.FromResult<IEnumerable<ExecutionRecord>>(query.ToList());
        }

        public Task<IEnumerable<ExecutionRecord>> GetSampledRecordsAsync(Guid taskId, DateTime? since = null, CancellationToken cancellationToken = default)
        {
            var query = _records
                .Where(r => r.TaskId == taskId && r.SampledForValidation && (!since.HasValue || r.ExecutedAt >= since.Value))
                .OrderByDescending(r => r.ExecutedAt);

            return Task.FromResult<IEnumerable<ExecutionRecord>>(query.ToList());
        }

        public Task<ExecutionStatistics> GetStatisticsAsync(Guid taskId, DateTime? since = null, CancellationToken cancellationToken = default)
        {
            var records = _records
                .Where(r => r.TaskId == taskId && (!since.HasValue || r.ExecutedAt >= since.Value))
                .ToList();

            var latencies = records.Select(r => r.LatencyMs).OrderBy(l => l).ToList();
            var p95Index = (int)(latencies.Count * 0.95);
            var p99Index = (int)(latencies.Count * 0.99);

            return Task.FromResult(new ExecutionStatistics
            {
                TotalExecutions = records.Count,
                SuccessCount = records.Count(r => r.Status == ExecutionStatus.Success),
                ErrorCount = records.Count(r => r.Status == ExecutionStatus.Error),
                TimeoutCount = records.Count(r => r.Status == ExecutionStatus.Timeout),
                AverageLatencyMs = latencies.Any() ? (latencies.Average() ?? 0.0) : 0.0,
                P95LatencyMs = latencies.Any() && p95Index < latencies.Count ? (double?)latencies[p95Index] ?? 0.0 : 0.0,
                P99LatencyMs = latencies.Any() && p99Index < latencies.Count ? (double?)latencies[p99Index] ?? 0.0 : 0.0,
                SampledCount = records.Count(r => r.SampledForValidation)
            });
        }
    }
}
