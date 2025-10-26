using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Loopai.CloudApi.Data.Entities;

/// <summary>
/// Entity for persisting canary deployment state.
/// </summary>
[Table("CanaryDeployments")]
public class CanaryDeploymentEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TaskId { get; set; }

    [Required]
    public Guid NewProgramId { get; set; }

    public Guid? CurrentProgramId { get; set; }

    [Required]
    [MaxLength(50)]
    public string CurrentStage { get; set; } = string.Empty;

    [Required]
    public double CurrentPercentage { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? StatusReason { get; set; }

    [Required]
    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    // Navigation property for history
    public ICollection<CanaryStageHistoryEntity> History { get; set; } = new List<CanaryStageHistoryEntity>();
}

/// <summary>
/// Entity for canary stage history records.
/// </summary>
[Table("CanaryStageHistory")]
public class CanaryStageHistoryEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid CanaryDeploymentId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Stage { get; set; } = string.Empty;

    [Required]
    public double Percentage { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Reason { get; set; }

    [Required]
    public DateTime Timestamp { get; set; }

    // Navigation property
    [ForeignKey(nameof(CanaryDeploymentId))]
    public CanaryDeploymentEntity CanaryDeployment { get; set; } = null!;
}
