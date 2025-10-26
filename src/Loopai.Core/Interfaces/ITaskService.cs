using Loopai.Core.Models;

namespace Loopai.Core.Interfaces;

/// <summary>
/// Business logic service for task management.
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Get a task by ID.
    /// </summary>
    Task<TaskSpecification?> GetTaskAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a task by name.
    /// </summary>
    Task<TaskSpecification?> GetTaskByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all tasks.
    /// </summary>
    Task<IEnumerable<TaskSpecification>> GetAllTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new task.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if task name already exists.</exception>
    Task<TaskSpecification> CreateTaskAsync(TaskSpecification task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing task.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown if task not found.</exception>
    Task<TaskSpecification> UpdateTaskAsync(TaskSpecification task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a task.
    /// </summary>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteTaskAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get task with active program information.
    /// </summary>
    Task<TaskWithArtifactInfo?> GetTaskWithActiveArtifactAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Task with artifact metadata.
/// </summary>
public record TaskWithArtifactInfo
{
    public required TaskSpecification Task { get; init; }
    public int? ActiveVersion { get; init; }
    public int TotalVersions { get; init; }
    public DateTime? LastExecutedAt { get; init; }
}
