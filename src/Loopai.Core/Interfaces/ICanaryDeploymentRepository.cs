using Loopai.Core.Models;

namespace Loopai.Core.Interfaces;

/// <summary>
/// Repository for persisting canary deployment state.
/// </summary>
public interface ICanaryDeploymentRepository
{
    /// <summary>
    /// Create a new canary deployment.
    /// </summary>
    Task<CanaryDeployment> CreateAsync(CanaryDeployment canaryDeployment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing canary deployment.
    /// </summary>
    Task<CanaryDeployment> UpdateAsync(CanaryDeployment canaryDeployment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a canary deployment by ID.
    /// </summary>
    Task<CanaryDeployment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active canary deployment for a task.
    /// </summary>
    Task<CanaryDeployment?> GetActiveByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all canary deployments for a task.
    /// </summary>
    Task<IEnumerable<CanaryDeployment>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a canary deployment.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
