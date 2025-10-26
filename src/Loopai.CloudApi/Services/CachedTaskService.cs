using Loopai.CloudApi.Configuration;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.Extensions.Options;

namespace Loopai.CloudApi.Services;

/// <summary>
/// Cached decorator for ITaskService implementing cache-aside pattern.
/// </summary>
public class CachedTaskService : ITaskService
{
    private readonly ITaskService _inner;
    private readonly ICacheService _cache;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<CachedTaskService> _logger;

    public CachedTaskService(
        ITaskService inner,
        ICacheService cache,
        IOptions<CacheSettings> cacheSettings,
        ILogger<CachedTaskService> logger)
    {
        _inner = inner;
        _cache = cache;
        _cacheSettings = cacheSettings.Value;
        _logger = logger;
    }

    public async Task<TaskSpecification?> GetTaskAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_cacheSettings.Enabled)
        {
            return await _inner.GetTaskAsync(id, cancellationToken);
        }

        var cacheKey = $"task:{id}";

        // Try to get from cache
        var cached = await _cache.GetAsync<TaskSpecification>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // Cache miss - get from database
        var task = await _inner.GetTaskAsync(id, cancellationToken);
        if (task != null)
        {
            var ttl = TimeSpan.FromMinutes(_cacheSettings.TaskMetadataTtlMinutes);
            await _cache.SetAsync(cacheKey, task, ttl, cancellationToken);
        }

        return task;
    }

    public async Task<TaskSpecification?> GetTaskByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!_cacheSettings.Enabled)
        {
            return await _inner.GetTaskByNameAsync(name, cancellationToken);
        }

        var cacheKey = $"task:name:{name}";

        // Try to get from cache
        var cached = await _cache.GetAsync<TaskSpecification>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // Cache miss - get from database
        var task = await _inner.GetTaskByNameAsync(name, cancellationToken);
        if (task != null)
        {
            var ttl = TimeSpan.FromMinutes(_cacheSettings.TaskMetadataTtlMinutes);
            await _cache.SetAsync(cacheKey, task, ttl, cancellationToken);

            // Also cache by ID for consistency
            var idCacheKey = $"task:{task.Id}";
            await _cache.SetAsync(idCacheKey, task, ttl, cancellationToken);
        }

        return task;
    }

    public async Task<IEnumerable<TaskSpecification>> GetAllTasksAsync(CancellationToken cancellationToken = default)
    {
        // Don't cache GetAll - could be large and frequently changing
        return await _inner.GetAllTasksAsync(cancellationToken);
    }

    public async Task<TaskSpecification> CreateTaskAsync(
        TaskSpecification task,
        CancellationToken cancellationToken = default)
    {
        var created = await _inner.CreateTaskAsync(task, cancellationToken);

        if (_cacheSettings.Enabled)
        {
            // Proactively cache the newly created task
            var ttl = TimeSpan.FromMinutes(_cacheSettings.TaskMetadataTtlMinutes);
            var idCacheKey = $"task:{created.Id}";
            var nameCacheKey = $"task:name:{created.Name}";

            await _cache.SetAsync(idCacheKey, created, ttl, cancellationToken);
            await _cache.SetAsync(nameCacheKey, created, ttl, cancellationToken);
        }

        return created;
    }

    public async Task<TaskSpecification> UpdateTaskAsync(
        TaskSpecification task,
        CancellationToken cancellationToken = default)
    {
        var updated = await _inner.UpdateTaskAsync(task, cancellationToken);

        if (_cacheSettings.Enabled)
        {
            // Invalidate cache on update
            var idCacheKey = $"task:{updated.Id}";
            var nameCacheKey = $"task:name:{updated.Name}";

            await _cache.RemoveAsync(idCacheKey, cancellationToken);
            await _cache.RemoveAsync(nameCacheKey, cancellationToken);
        }

        return updated;
    }

    public async Task<bool> DeleteTaskAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Get task name before deletion for cache invalidation
        TaskSpecification? task = null;
        if (_cacheSettings.Enabled)
        {
            task = await _inner.GetTaskAsync(id, cancellationToken);
        }

        var deleted = await _inner.DeleteTaskAsync(id, cancellationToken);

        if (deleted && _cacheSettings.Enabled && task != null)
        {
            // Invalidate cache on delete
            var idCacheKey = $"task:{id}";
            var nameCacheKey = $"task:name:{task.Name}";

            await _cache.RemoveAsync(idCacheKey, cancellationToken);
            await _cache.RemoveAsync(nameCacheKey, cancellationToken);
        }

        return deleted;
    }

    public async Task<TaskWithArtifactInfo?> GetTaskWithActiveArtifactAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!_cacheSettings.Enabled)
        {
            return await _inner.GetTaskWithActiveArtifactAsync(id, cancellationToken);
        }

        var cacheKey = $"task:artifact-info:{id}";

        // Try to get from cache
        var cached = await _cache.GetAsync<TaskWithArtifactInfo>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // Cache miss - get from database
        var taskInfo = await _inner.GetTaskWithActiveArtifactAsync(id, cancellationToken);
        if (taskInfo != null)
        {
            // Use shorter TTL for artifact info as it changes more frequently
            var ttl = TimeSpan.FromMinutes(_cacheSettings.ActiveArtifactTtlMinutes);
            await _cache.SetAsync(cacheKey, taskInfo, ttl, cancellationToken);
        }

        return taskInfo;
    }
}
