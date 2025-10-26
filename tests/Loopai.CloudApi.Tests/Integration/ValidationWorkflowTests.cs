using System.Text.Json;
using Loopai.CloudApi.Services;
using Loopai.CloudApi.Tests.Mocks;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Loopai.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Loopai.CloudApi.Tests.Integration;

/// <summary>
/// Integration tests for validation and improvement workflow.
/// </summary>
public class ValidationWorkflowTests : IDisposable
{
    private readonly InMemoryTaskRepository _taskRepo;
    private readonly InMemoryProgramArtifactRepository _artifactRepo;
    private readonly InMemoryExecutionRecordRepository _executionRepo;
    private readonly InMemoryValidationResultRepository _validationRepo;
    private readonly MockProgramGeneratorService _mockGenerator;
    private readonly Mock<ILogger<SchemaOutputValidator>> _validatorLogger;
    private readonly Mock<ILogger<ProgramImprovementService>> _improvementLogger;
    private readonly Mock<ILogger<ValidationService>> _validationServiceLogger;
    private readonly SchemaOutputValidator _outputValidator;
    private readonly ProgramImprovementService _improvementService;
    private readonly ValidationService _validationService;

    public ValidationWorkflowTests()
    {
        _taskRepo = new InMemoryTaskRepository();
        _artifactRepo = new InMemoryProgramArtifactRepository();
        _executionRepo = new InMemoryExecutionRecordRepository();
        _validationRepo = new InMemoryValidationResultRepository();
        _mockGenerator = new MockProgramGeneratorService();

        _validatorLogger = new Mock<ILogger<SchemaOutputValidator>>();
        _improvementLogger = new Mock<ILogger<ProgramImprovementService>>();
        _validationServiceLogger = new Mock<ILogger<ValidationService>>();

        _outputValidator = new SchemaOutputValidator(
            _taskRepo,
            _executionRepo,
            _validatorLogger.Object
        );

        _improvementService = new ProgramImprovementService(
            _artifactRepo,
            _validationRepo,
            _executionRepo,
            _taskRepo,
            _mockGenerator,
            _improvementLogger.Object
        );

        _validationService = new ValidationService(
            _validationRepo,
            _outputValidator,
            _improvementService,
            _validationServiceLogger.Object
        );
    }

    [Fact]
    public async Task ValidateAndRecord_ValidOutput_CreatesValidationResult()
    {
        // Arrange
        var task = await CreateTestTaskAsync();
        var program = await CreateTestProgramAsync(task.Id);
        var execution = await CreateSuccessfulExecutionAsync(task.Id, program.Id, "{\"result\": 42}");

        // Act
        var result = await _validationService.ValidateAndRecordAsync(
            execution.Id,
            task.Id,
            program.Id,
            triggerImprovementIfNeeded: false
        );

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.True(result.IsValid);
        Assert.Equal(1.0, result.ValidationScore);
        Assert.Empty(result.Errors);

        // Verify it was saved
        var saved = await _validationRepo.GetByIdAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal(execution.Id, saved.ExecutionId);
    }

    [Fact]
    public async Task ValidateAndRecord_InvalidOutput_CreatesErrorRecord()
    {
        // Arrange
        var task = await CreateTestTaskAsync();
        var program = await CreateTestProgramAsync(task.Id);
        var execution = await CreateSuccessfulExecutionAsync(task.Id, program.Id, "{\"result\": \"wrong type\"}");

        // Act
        var result = await _validationService.ValidateAndRecordAsync(
            execution.Id,
            task.Id,
            program.Id,
            triggerImprovementIfNeeded: false
        );

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Equal(0.0, result.ValidationScore);
    }

    [Fact]
    public async Task ImprovementPipeline_MultipleFailures_TriggersImprovement()
    {
        // Arrange
        var task = await CreateTestTaskAsync();
        var program = await CreateTestProgramAsync(task.Id);

        // Configure improved program
        var improvedCode = "async function main(input) { return { result: input.value * 2 }; }";
        _mockGenerator.ConfigureProgram(task.Id, improvedCode);

        // Create multiple failed validations
        for (int i = 0; i < 6; i++)
        {
            var execution = await CreateSuccessfulExecutionAsync(
                task.Id,
                program.Id,
                "{\"result\": \"bad output\"}"
            );

            await _validationService.ValidateAndRecordAsync(
                execution.Id,
                task.Id,
                program.Id,
                triggerImprovementIfNeeded: false
            );
        }

        // Verify failures recorded
        var stats = await _validationRepo.GetStatisticsAsync(program.Id);
        Assert.Equal(6, stats.TotalValidations);
        Assert.Equal(0, stats.ValidCount);
        Assert.Equal(6, stats.InvalidCount);

        // Act - trigger improvement
        var shouldImprove = await _improvementService.ShouldImproveAsync(program.Id);
        Assert.True(shouldImprove);

        var improvementResult = await _improvementService.ImproveFromValidationFailuresAsync(program.Id);

        // Assert
        Assert.True(improvementResult.Success);
        Assert.NotNull(improvementResult.NewProgramId);
        Assert.NotNull(improvementResult.NewVersion);

        // Verify new program was created
        var newProgram = await _artifactRepo.GetByIdAsync(improvementResult.NewProgramId.Value);
        Assert.NotNull(newProgram);
        Assert.Equal(task.Id, newProgram.TaskId);
        Assert.Equal(2, newProgram.Version); // Version incremented
        Assert.Equal(ProgramStatus.Draft, newProgram.Status);
        Assert.Equal(0.0, newProgram.DeploymentPercentage);
    }

    [Fact]
    public async Task ImprovementPipeline_InsufficientFailures_DoesNotImprove()
    {
        // Arrange
        var task = await CreateTestTaskAsync();
        var program = await CreateTestProgramAsync(task.Id);

        // Create only 3 failed validations (below threshold of 5)
        for (int i = 0; i < 3; i++)
        {
            var execution = await CreateSuccessfulExecutionAsync(
                task.Id,
                program.Id,
                "{\"result\": \"bad output\"}"
            );

            await _validationService.ValidateAndRecordAsync(
                execution.Id,
                task.Id,
                program.Id,
                triggerImprovementIfNeeded: false
            );
        }

        // Act
        var shouldImprove = await _improvementService.ShouldImproveAsync(program.Id);

        // Assert
        Assert.False(shouldImprove);
    }

    [Fact]
    public async Task ImprovementPipeline_HighValidationRate_DoesNotImprove()
    {
        // Arrange
        var task = await CreateTestTaskAsync();
        var program = await CreateTestProgramAsync(task.Id);

        // Create 8 successful and 2 failed validations (80% success rate)
        for (int i = 0; i < 8; i++)
        {
            var execution = await CreateSuccessfulExecutionAsync(
                task.Id,
                program.Id,
                "{\"result\": 42}"
            );

            await _validationService.ValidateAndRecordAsync(
                execution.Id,
                task.Id,
                program.Id,
                triggerImprovementIfNeeded: false
            );
        }

        for (int i = 0; i < 2; i++)
        {
            var execution = await CreateSuccessfulExecutionAsync(
                task.Id,
                program.Id,
                "{\"result\": \"bad\"}"
            );

            await _validationService.ValidateAndRecordAsync(
                execution.Id,
                task.Id,
                program.Id,
                triggerImprovementIfNeeded: false
            );
        }

        // Act
        var shouldImprove = await _improvementService.ShouldImproveAsync(program.Id);

        // Assert
        Assert.False(shouldImprove); // 80% > 70% threshold
    }

    [Fact]
    public async Task GetRecommendations_WithFailures_ReturnsAnalysis()
    {
        // Arrange
        var task = await CreateTestTaskAsync();
        var program = await CreateTestProgramAsync(task.Id);

        // Create multiple failures
        for (int i = 0; i < 7; i++)
        {
            var execution = await CreateSuccessfulExecutionAsync(
                task.Id,
                program.Id,
                "{\"result\": \"wrong\"}"
            );

            await _validationService.ValidateAndRecordAsync(
                execution.Id,
                task.Id,
                program.Id,
                triggerImprovementIfNeeded: false
            );
        }

        // Act
        var recommendations = await _improvementService.GetRecommendationsAsync(program.Id);

        // Assert
        Assert.True(recommendations.ShouldImprove);
        Assert.Equal(7, recommendations.FailedValidationsCount);
        Assert.Equal(0.0, recommendations.ValidationRate);
        Assert.NotEmpty(recommendations.CommonErrors);
        Assert.NotEmpty(recommendations.SuggestedFixes);
        Assert.Equal("medium", recommendations.Confidence); // 7 validations
    }

    [Fact]
    public async Task ValidationSummary_CombinesStatsAndRecommendations()
    {
        // Arrange
        var task = await CreateTestTaskAsync();
        var program = await CreateTestProgramAsync(task.Id);

        // Create mixed results
        for (int i = 0; i < 4; i++)
        {
            var execution = await CreateSuccessfulExecutionAsync(
                task.Id,
                program.Id,
                "{\"result\": 42}"
            );

            await _validationService.ValidateAndRecordAsync(
                execution.Id,
                task.Id,
                program.Id
            );
        }

        for (int i = 0; i < 6; i++)
        {
            var execution = await CreateSuccessfulExecutionAsync(
                task.Id,
                program.Id,
                "{\"result\": \"bad\"}"
            );

            await _validationService.ValidateAndRecordAsync(
                execution.Id,
                task.Id,
                program.Id
            );
        }

        // Act
        var summary = await _validationService.GetValidationSummaryAsync(program.Id);

        // Assert
        Assert.Equal(program.Id, summary.ProgramId);
        Assert.Equal(10, summary.Statistics.TotalValidations);
        Assert.Equal(4, summary.Statistics.ValidCount);
        Assert.Equal(6, summary.Statistics.InvalidCount);
        Assert.Equal(0.4, summary.Statistics.ValidationRate);
        Assert.True(summary.Recommendations.ShouldImprove); // 40% < 70%
    }

    public void Dispose()
    {
        _mockGenerator.Reset();
    }

    // Helper methods
    private async Task<TaskSpecification> CreateTestTaskAsync()
    {
        var task = new TaskSpecification
        {
            Id = Guid.NewGuid(),
            Name = "test-task",
            Description = "Test task",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "result": { "type": "number" }
                },
                "required": ["result"]
            }
            """)
        };

        return await _taskRepo.CreateAsync(task);
    }

    private async Task<ProgramArtifact> CreateTestProgramAsync(Guid taskId)
    {
        var program = new ProgramArtifact
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            Version = 1,
            Code = "async function main(input) { return { result: 42 }; }",
            Language = "typescript",
            Status = ProgramStatus.Active
        };

        return await _artifactRepo.CreateAsync(program);
    }

    private async Task<ExecutionRecord> CreateSuccessfulExecutionAsync(
        Guid taskId,
        Guid programId,
        string outputJson)
    {
        var execution = new ExecutionRecord
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ProgramId = programId,
            Status = ExecutionStatus.Success,
            InputData = JsonDocument.Parse("{\"value\": 21}"),
            OutputData = JsonDocument.Parse(outputJson),
            LatencyMs = 100,
            ExecutedAt = DateTime.UtcNow
        };

        return await _executionRepo.CreateAsync(execution);
    }

    // In-memory repositories
    private class InMemoryTaskRepository : ITaskRepository
    {
        private readonly Dictionary<Guid, TaskSpecification> _tasks = new();

        public Task<TaskSpecification> CreateAsync(TaskSpecification task, CancellationToken cancellationToken = default)
        {
            var created = task with { Id = task.Id == Guid.Empty ? Guid.NewGuid() : task.Id };
            _tasks[created.Id] = created;
            return Task.FromResult(created);
        }

        public Task<TaskSpecification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _tasks.TryGetValue(id, out var task);
            return Task.FromResult(task);
        }

        public Task<TaskSpecification?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<IEnumerable<TaskSpecification>> GetAllAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<TaskSpecification> UpdateAsync(TaskSpecification task, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private class InMemoryProgramArtifactRepository : IProgramArtifactRepository
    {
        private readonly Dictionary<Guid, ProgramArtifact> _artifacts = new();

        public Task<ProgramArtifact> CreateAsync(ProgramArtifact artifact, CancellationToken cancellationToken = default)
        {
            var created = artifact with { Id = artifact.Id == Guid.Empty ? Guid.NewGuid() : artifact.Id };
            _artifacts[created.Id] = created;
            return Task.FromResult(created);
        }

        public Task<ProgramArtifact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _artifacts.TryGetValue(id, out var artifact);
            return Task.FromResult(artifact);
        }

        public Task<int> GetLatestVersionAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var maxVersion = _artifacts.Values
                .Where(a => a.TaskId == taskId)
                .Max(a => (int?)a.Version) ?? 0;
            return Task.FromResult(maxVersion);
        }

        public Task<ProgramArtifact?> GetActiveByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<IEnumerable<ProgramArtifact>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<ProgramArtifact?> GetByTaskIdAndVersionAsync(Guid taskId, int version, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<ProgramArtifact> UpdateAsync(ProgramArtifact artifact, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private class InMemoryExecutionRecordRepository : IExecutionRecordRepository
    {
        private readonly Dictionary<Guid, ExecutionRecord> _records = new();

        public Task<ExecutionRecord> CreateAsync(ExecutionRecord record, CancellationToken cancellationToken = default)
        {
            var created = record with { Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id };
            _records[created.Id] = created;
            return Task.FromResult(created);
        }

        public Task<ExecutionRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _records.TryGetValue(id, out var record);
            return Task.FromResult(record);
        }

        public Task<IEnumerable<ExecutionRecord>> CreateBatchAsync(IEnumerable<ExecutionRecord> records, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<IEnumerable<ExecutionRecord>> GetByTaskIdAsync(Guid taskId, int? limit = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<IEnumerable<ExecutionRecord>> GetByProgramIdAsync(Guid programId, int? limit = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<IEnumerable<ExecutionRecord>> GetSampledRecordsAsync(Guid taskId, DateTime? since = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<ExecutionStatistics> GetStatisticsAsync(Guid taskId, DateTime? since = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private class InMemoryValidationResultRepository : IValidationResultRepository
    {
        private readonly List<ValidationResult> _results = new();

        public Task<ValidationResult> CreateAsync(ValidationResult result, CancellationToken cancellationToken = default)
        {
            var created = result with { Id = result.Id == Guid.Empty ? Guid.NewGuid() : result.Id };
            _results.Add(created);
            return Task.FromResult(created);
        }

        public Task<ValidationResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var result = _results.FirstOrDefault(r => r.Id == id);
            return Task.FromResult(result);
        }

        public Task<IEnumerable<ValidationResult>> GetFailedByProgramIdAsync(
            Guid programId,
            int? limit = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<ValidationResult> query = _results
                .Where(r => r.ProgramId == programId && !r.IsValid)
                .OrderByDescending(r => r.ValidatedAt);

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            return Task.FromResult<IEnumerable<ValidationResult>>(query.ToList());
        }

        public Task<ValidationStatistics> GetStatisticsAsync(
            Guid programId,
            DateTime? since = null,
            CancellationToken cancellationToken = default)
        {
            var results = _results
                .Where(r => r.ProgramId == programId && (!since.HasValue || r.ValidatedAt >= since.Value))
                .ToList();

            if (results.Count == 0)
            {
                return Task.FromResult(new ValidationStatistics
                {
                    TotalValidations = 0,
                    ValidCount = 0,
                    InvalidCount = 0,
                    AverageScore = 0.0,
                    ValidationRate = 0.0
                });
            }

            var validCount = results.Count(r => r.IsValid);
            return Task.FromResult(new ValidationStatistics
            {
                TotalValidations = results.Count,
                ValidCount = validCount,
                InvalidCount = results.Count - validCount,
                AverageScore = results.Average(r => r.ValidationScore),
                ValidationRate = (double)validCount / results.Count
            });
        }

        public Task<IEnumerable<ValidationResult>> CreateBatchAsync(IEnumerable<ValidationResult> results, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<IEnumerable<ValidationResult>> GetByExecutionIdAsync(Guid executionId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<IEnumerable<ValidationResult>> GetByTaskIdAsync(Guid taskId, int? limit = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<IEnumerable<ValidationResult>> GetByProgramIdAsync(Guid programId, int? limit = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
