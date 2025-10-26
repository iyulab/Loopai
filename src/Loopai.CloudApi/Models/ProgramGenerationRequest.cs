using System.Text.Json;

namespace Loopai.CloudApi.Models;

/// <summary>
/// Request to the Python Generator Service for program synthesis.
/// </summary>
public record ProgramGenerationRequest
{
    /// <summary>
    /// Task specification ID.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Task name for reference.
    /// </summary>
    public required string TaskName { get; init; }

    /// <summary>
    /// Input data schema.
    /// </summary>
    public required JsonDocument InputSchema { get; init; }

    /// <summary>
    /// Output data schema.
    /// </summary>
    public required JsonDocument OutputSchema { get; init; }

    /// <summary>
    /// Task description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Example input-output pairs for few-shot learning.
    /// </summary>
    public IReadOnlyList<ExamplePair> Examples { get; init; } = Array.Empty<ExamplePair>();

    /// <summary>
    /// Additional constraints or requirements.
    /// </summary>
    public string? Constraints { get; init; }

    /// <summary>
    /// Target runtime environment (deno, bun).
    /// </summary>
    public string TargetRuntime { get; init; } = "deno";
}

/// <summary>
/// Example input-output pair for program synthesis.
/// </summary>
public record ExamplePair
{
    public required JsonDocument Input { get; init; }
    public required JsonDocument Output { get; init; }
}
