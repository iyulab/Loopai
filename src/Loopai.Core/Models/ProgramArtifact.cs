using System.Text.Json.Serialization;

namespace Loopai.Core.Models;

/// <summary>
/// Generated program artifact model.
/// </summary>
public record ProgramArtifact
{
    /// <summary>
    /// Unique artifact identifier.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Reference to task specification.
    /// </summary>
    [JsonPropertyName("task_id")]
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Artifact version number (starts at 1).
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Programming language (e.g., "python", "csharp").
    /// </summary>
    public string Language { get; init; } = "python";

    /// <summary>
    /// Program source code.
    /// </summary>
    public required string Code { get; init; }

    // Metadata
    /// <summary>
    /// Synthesis strategy used to generate this program.
    /// </summary>
    [JsonPropertyName("synthesis_strategy")]
    public SynthesisStrategy SynthesisStrategy { get; init; } = SynthesisStrategy.Auto;

    /// <summary>
    /// Confidence score (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence_score")]
    public double ConfidenceScore { get; init; } = 0.5;

    /// <summary>
    /// Program complexity metrics.
    /// </summary>
    [JsonPropertyName("complexity_metrics")]
    public ComplexityMetrics ComplexityMetrics { get; init; } = new();

    // Generation context
    /// <summary>
    /// LLM provider used for generation (e.g., "openai", "anthropic").
    /// </summary>
    [JsonPropertyName("llm_provider")]
    public string LlmProvider { get; init; } = "openai";

    /// <summary>
    /// LLM model used for generation (e.g., "gpt-4", "claude-3").
    /// </summary>
    [JsonPropertyName("llm_model")]
    public string LlmModel { get; init; } = "gpt-4";

    /// <summary>
    /// Cost of program generation in USD.
    /// </summary>
    [JsonPropertyName("generation_cost")]
    public double GenerationCost { get; init; } = 0.0;

    /// <summary>
    /// Time taken for program generation in seconds.
    /// </summary>
    [JsonPropertyName("generation_time_sec")]
    public double GenerationTimeSec { get; init; } = 0.0;

    // Status
    /// <summary>
    /// Current program status.
    /// </summary>
    public ProgramStatus Status { get; init; } = ProgramStatus.Draft;

    /// <summary>
    /// Deployment percentage for gradual rollout (0.0 to 100.0).
    /// </summary>
    [JsonPropertyName("deployment_percentage")]
    public double DeploymentPercentage { get; init; } = 0.0;

    /// <summary>
    /// Timestamp when artifact was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when artifact was deployed.
    /// </summary>
    [JsonPropertyName("deployed_at")]
    public DateTime? DeployedAt { get; init; }
}
