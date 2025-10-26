using Loopai.Core.Models;

namespace Loopai.Core.Interfaces;

/// <summary>
/// Repository interface for task specification persistence.
/// </summary>
public interface ITaskRepository
{
    /// <summary>
    /// Get a task by ID.
    /// </summary>
    Task<TaskSpecification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a task by name.
    /// </summary>
    Task<TaskSpecification?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all tasks.
    /// </summary>
    Task<IEnumerable<TaskSpecification>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new task.
    /// </summary>
    Task<TaskSpecification> CreateAsync(TaskSpecification task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing task.
    /// </summary>
    Task<TaskSpecification> UpdateAsync(TaskSpecification task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a task by ID.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a task exists by name.
    /// </summary>
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
}
