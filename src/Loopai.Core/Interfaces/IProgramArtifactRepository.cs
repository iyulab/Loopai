using Loopai.Core.Models;

namespace Loopai.Core.Interfaces;

/// <summary>
/// Repository interface for program artifact persistence.
/// </summary>
public interface IProgramArtifactRepository
{
    /// <summary>
    /// Get a program artifact by ID.
    /// </summary>
    Task<ProgramArtifact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all artifacts for a task.
    /// </summary>
    Task<IEnumerable<ProgramArtifact>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific version of an artifact for a task.
    /// </summary>
    Task<ProgramArtifact?> GetByTaskIdAndVersionAsync(Guid taskId, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the active (most recent validated) artifact for a task.
    /// </summary>
    Task<ProgramArtifact?> GetActiveByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new program artifact.
    /// </summary>
    Task<ProgramArtifact> CreateAsync(ProgramArtifact artifact, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing artifact.
    /// </summary>
    Task<ProgramArtifact> UpdateAsync(ProgramArtifact artifact, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an artifact by ID.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the latest version number for a task.
    /// </summary>
    Task<int> GetLatestVersionAsync(Guid taskId, CancellationToken cancellationToken = default);
}
