using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.Extensions.Logging;

namespace Loopai.Core.Services;

/// <summary>
/// Implementation of task management business logic.
/// </summary>
public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProgramArtifactRepository _artifactRepository;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ITaskRepository taskRepository,
        IProgramArtifactRepository artifactRepository,
        ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository;
        _artifactRepository = artifactRepository;
        _logger = logger;
    }

    public async Task<TaskSpecification?> GetTaskAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting task {TaskId}", id);
        return await _taskRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<TaskSpecification?> GetTaskByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting task by name {TaskName}", name);
        return await _taskRepository.GetByNameAsync(name, cancellationToken);
    }

    public async Task<IEnumerable<TaskSpecification>> GetAllTasksAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all tasks");
        return await _taskRepository.GetAllAsync(cancellationToken);
    }

    public async Task<TaskSpecification> CreateTaskAsync(
        TaskSpecification task,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating task {TaskName}", task.Name);

        // Check if task with same name already exists
        var existing = await _taskRepository.GetByNameAsync(task.Name, cancellationToken);
        if (existing != null)
        {
            _logger.LogWarning("Task with name {TaskName} already exists", task.Name);
            throw new InvalidOperationException($"Task with name '{task.Name}' already exists");
        }

        var created = await _taskRepository.CreateAsync(task, cancellationToken);
        _logger.LogInformation("Created task {TaskId} with name {TaskName}", created.Id, created.Name);

        return created;
    }

    public async Task<TaskSpecification> UpdateTaskAsync(
        TaskSpecification task,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating task {TaskId}", task.Id);

        // Check if task exists
        var existing = await _taskRepository.GetByIdAsync(task.Id, cancellationToken);
        if (existing == null)
        {
            _logger.LogWarning("Task {TaskId} not found for update", task.Id);
            throw new KeyNotFoundException($"Task with ID {task.Id} not found");
        }

        var updated = await _taskRepository.UpdateAsync(task, cancellationToken);
        _logger.LogInformation("Updated task {TaskId}", updated.Id);

        return updated;
    }

    public async Task<bool> DeleteTaskAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting task {TaskId}", id);

        var deleted = await _taskRepository.DeleteAsync(id, cancellationToken);

        if (deleted)
        {
            _logger.LogInformation("Deleted task {TaskId}", id);
        }
        else
        {
            _logger.LogWarning("Task {TaskId} not found for deletion", id);
        }

        return deleted;
    }

    public async Task<TaskWithArtifactInfo?> GetTaskWithActiveArtifactAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting task {TaskId} with artifact info", id);

        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (task == null)
        {
            return null;
        }

        var artifacts = await _artifactRepository.GetByTaskIdAsync(id, cancellationToken);
        var artifactsList = artifacts.ToList();

        var activeArtifact = artifactsList
            .Where(a => a.Status == ProgramStatus.Active)
            .OrderByDescending(a => a.Version)
            .FirstOrDefault();

        return new TaskWithArtifactInfo
        {
            Task = task,
            ActiveVersion = activeArtifact?.Version,
            TotalVersions = artifactsList.Count,
            LastExecutedAt = artifactsList
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault()?.CreatedAt
        };
    }
}
