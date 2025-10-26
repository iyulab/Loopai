using Loopai.CloudApi.Data.Entities;
using Loopai.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Loopai.CloudApi.Data;

/// <summary>
/// Entity Framework Core database context for Loopai.
/// </summary>
public class LoopaiDbContext : DbContext
{
    // Value converters for JSON documents
    private static readonly ValueConverter<JsonDocument, string> JsonDocumentConverter = new(
        v => v.RootElement.GetRawText(),
        v => ParseJsonDocument(v));

    private static readonly ValueConverter<JsonDocument?, string?> NullableJsonDocumentConverter = new(
        v => v != null ? v.RootElement.GetRawText() : null,
        v => v != null ? ParseJsonDocument(v) : null);

    private static readonly ValueConverter<IReadOnlyList<JsonDocument>, string> JsonDocumentListConverter = new(
        v => SerializeJsonDocumentList(v),
        v => ParseJsonDocumentList(v));

    private static JsonDocument ParseJsonDocument(string json) => JsonDocument.Parse(json);

    private static string SerializeJsonDocumentList(IReadOnlyList<JsonDocument> documents)
    {
        var stringList = documents.Select(d => d.RootElement.GetRawText()).ToList();
        return JsonSerializer.Serialize(stringList);
    }

    private static IReadOnlyList<JsonDocument> ParseJsonDocumentList(string json)
    {
        var stringList = JsonSerializer.Deserialize<List<string>>(json)!;
        return stringList.Select(s => ParseJsonDocument(s)).ToList().AsReadOnly();
    }

    public LoopaiDbContext(DbContextOptions<LoopaiDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Task specifications.
    /// </summary>
    public DbSet<TaskSpecification> Tasks { get; set; } = null!;

    /// <summary>
    /// Program artifacts (versioned programs).
    /// </summary>
    public DbSet<ProgramArtifact> ProgramArtifacts { get; set; } = null!;

    /// <summary>
    /// Execution records.
    /// </summary>
    public DbSet<ExecutionRecord> ExecutionRecords { get; set; } = null!;

    /// <summary>
    /// Validation results.
    /// </summary>
    public DbSet<ValidationResult> ValidationResults { get; set; } = null!;

    /// <summary>
    /// Canary deployments.
    /// </summary>
    public DbSet<CanaryDeploymentEntity> CanaryDeployments { get; set; } = null!;

    /// <summary>
    /// Canary stage history.
    /// </summary>
    public DbSet<CanaryStageHistoryEntity> CanaryStageHistory { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure TaskSpecification
        modelBuilder.Entity<TaskSpecification>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(e => e.Id);

            // Name must be unique
            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(2000);

            // JSON document properties
            entity.Property(e => e.InputSchema)
                .HasConversion(JsonDocumentConverter)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.OutputSchema)
                .HasConversion(JsonDocumentConverter)
                .HasColumnType("nvarchar(max)");

            // Examples as JSON array
            entity.Property(e => e.Examples)
                .HasConversion(JsonDocumentListConverter)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.AccuracyTarget)
                .HasPrecision(3, 2);

            entity.Property(e => e.SamplingRate)
                .HasPrecision(3, 2);
        });

        // Configure ProgramArtifact
        modelBuilder.Entity<ProgramArtifact>(entity =>
        {
            entity.ToTable("ProgramArtifacts");
            entity.HasKey(e => e.Id);

            // Composite index for TaskId + Version (must be unique)
            entity.HasIndex(e => new { e.TaskId, e.Version }).IsUnique();

            // Index for finding active artifacts
            entity.HasIndex(e => new { e.TaskId, e.Status });

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Language)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.LlmProvider)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.LlmModel)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.SynthesisStrategy)
                .HasConversion<string>()
                .HasMaxLength(50);

            // ComplexityMetrics as owned type
            entity.OwnsOne(e => e.ComplexityMetrics);
        });

        // Configure ExecutionRecord
        modelBuilder.Entity<ExecutionRecord>(entity =>
        {
            entity.ToTable("ExecutionRecords");
            entity.HasKey(e => e.Id);

            // Index for querying by task
            entity.HasIndex(e => new { e.TaskId, e.ExecutedAt });

            // Index for querying by program
            entity.HasIndex(e => new { e.ProgramId, e.ExecutedAt });

            // Index for finding sampled records
            entity.HasIndex(e => new { e.TaskId, e.SampledForValidation, e.ExecutedAt });

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.InputData)
                .HasConversion(JsonDocumentConverter)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.OutputData)
                .HasConversion(NullableJsonDocumentConverter)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(4000);

            entity.Property(e => e.LatencyMs)
                .HasPrecision(10, 2);

            entity.Property(e => e.MemoryUsageMb)
                .HasPrecision(10, 2);
        });

        // Configure ValidationResult
        modelBuilder.Entity<ValidationResult>(entity =>
        {
            entity.ToTable("ValidationResults");
            entity.HasKey(e => e.Id);

            // Index for querying by execution
            entity.HasIndex(e => e.ExecutionId);

            // Index for querying by task
            entity.HasIndex(e => new { e.TaskId, e.ValidatedAt });

            // Index for querying by program
            entity.HasIndex(e => new { e.ProgramId, e.ValidatedAt });

            // Index for finding failed validations (for improvement)
            entity.HasIndex(e => new { e.ProgramId, e.IsValid, e.ValidatedAt });

            entity.Property(e => e.ValidationMethod)
                .HasMaxLength(50);

            entity.Property(e => e.ValidationScore)
                .HasPrecision(5, 4); // 0.0000 to 1.0000

            // Store errors as JSON
            entity.Property(e => e.Errors)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<IReadOnlyList<ValidationError>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? Array.Empty<ValidationError>())
                .HasColumnType("nvarchar(max)");
        });

        // Configure CanaryDeploymentEntity
        modelBuilder.Entity<CanaryDeploymentEntity>(entity =>
        {
            entity.ToTable("CanaryDeployments");
            entity.HasKey(e => e.Id);

            // Index for finding active canaries by task
            entity.HasIndex(e => new { e.TaskId, e.Status });

            // Index for finding by program
            entity.HasIndex(e => e.NewProgramId);

            entity.Property(e => e.CurrentStage)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.StatusReason)
                .HasMaxLength(500);

            entity.Property(e => e.CurrentPercentage)
                .HasPrecision(5, 2);

            // Relationship with history
            entity.HasMany(e => e.History)
                .WithOne(h => h.CanaryDeployment)
                .HasForeignKey(h => h.CanaryDeploymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CanaryStageHistoryEntity
        modelBuilder.Entity<CanaryStageHistoryEntity>(entity =>
        {
            entity.ToTable("CanaryStageHistory");
            entity.HasKey(e => e.Id);

            // Index for querying history by canary
            entity.HasIndex(e => new { e.CanaryDeploymentId, e.Timestamp });

            entity.Property(e => e.Stage)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Reason)
                .HasMaxLength(500);

            entity.Property(e => e.Percentage)
                .HasPrecision(5, 2);
        });
    }
}
