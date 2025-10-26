using Loopai.CloudApi.Data;
using Loopai.CloudApi.Data.Entities;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Loopai.CloudApi.Repositories;

/// <summary>
/// Entity Framework Core implementation of canary deployment repository.
/// </summary>
public class EfCanaryDeploymentRepository : ICanaryDeploymentRepository
{
    private readonly LoopaiDbContext _context;

    public EfCanaryDeploymentRepository(LoopaiDbContext context)
    {
        _context = context;
    }

    public async Task<CanaryDeployment> CreateAsync(CanaryDeployment canaryDeployment, CancellationToken cancellationToken = default)
    {
        var entity = MapToEntity(canaryDeployment);
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.CanaryDeployments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDomain(entity);
    }

    public async Task<CanaryDeployment> UpdateAsync(CanaryDeployment canaryDeployment, CancellationToken cancellationToken = default)
    {
        var entity = await _context.CanaryDeployments
            .Include(c => c.History)
            .FirstOrDefaultAsync(c => c.Id == canaryDeployment.Id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"Canary deployment {canaryDeployment.Id} not found");
        }

        // Update properties
        entity.CurrentStage = canaryDeployment.CurrentStage.ToString();
        entity.CurrentPercentage = canaryDeployment.CurrentPercentage;
        entity.Status = canaryDeployment.Status.ToString();
        entity.StatusReason = canaryDeployment.StatusReason;
        entity.CompletedAt = canaryDeployment.CompletedAt;
        entity.UpdatedAt = DateTime.UtcNow;

        // Update history
        entity.History.Clear();
        foreach (var historyItem in canaryDeployment.History)
        {
            entity.History.Add(new CanaryStageHistoryEntity
            {
                Id = Guid.NewGuid(),
                CanaryDeploymentId = entity.Id,
                Stage = historyItem.Stage.ToString(),
                Percentage = historyItem.Percentage,
                Action = historyItem.Action,
                Reason = historyItem.Reason,
                Timestamp = historyItem.Timestamp
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDomain(entity);
    }

    public async Task<CanaryDeployment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.CanaryDeployments
            .Include(c => c.History)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity == null ? null : MapToDomain(entity);
    }

    public async Task<CanaryDeployment?> GetActiveByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.CanaryDeployments
            .Include(c => c.History)
            .Where(c => c.TaskId == taskId && c.Status == "InProgress")
            .OrderByDescending(c => c.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? null : MapToDomain(entity);
    }

    public async Task<IEnumerable<CanaryDeployment>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var entities = await _context.CanaryDeployments
            .Include(c => c.History)
            .Where(c => c.TaskId == taskId)
            .OrderByDescending(c => c.StartedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.CanaryDeployments.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        _context.CanaryDeployments.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static CanaryDeploymentEntity MapToEntity(CanaryDeployment canary)
    {
        var entity = new CanaryDeploymentEntity
        {
            Id = canary.Id,
            TaskId = canary.TaskId,
            NewProgramId = canary.NewProgramId,
            CurrentProgramId = canary.CurrentProgramId,
            CurrentStage = canary.CurrentStage.ToString(),
            CurrentPercentage = canary.CurrentPercentage,
            Status = canary.Status.ToString(),
            StatusReason = canary.StatusReason,
            StartedAt = canary.StartedAt,
            CompletedAt = canary.CompletedAt
        };

        foreach (var historyItem in canary.History)
        {
            entity.History.Add(new CanaryStageHistoryEntity
            {
                Id = Guid.NewGuid(),
                CanaryDeploymentId = entity.Id,
                Stage = historyItem.Stage.ToString(),
                Percentage = historyItem.Percentage,
                Action = historyItem.Action,
                Reason = historyItem.Reason,
                Timestamp = historyItem.Timestamp
            });
        }

        return entity;
    }

    private static CanaryDeployment MapToDomain(CanaryDeploymentEntity entity)
    {
        return new CanaryDeployment
        {
            Id = entity.Id,
            TaskId = entity.TaskId,
            NewProgramId = entity.NewProgramId,
            CurrentProgramId = entity.CurrentProgramId ?? Guid.Empty,
            CurrentStage = Enum.Parse<CanaryStage>(entity.CurrentStage),
            CurrentPercentage = entity.CurrentPercentage,
            Status = Enum.Parse<CanaryStatus>(entity.Status),
            StatusReason = entity.StatusReason,
            History = entity.History.Select(h => new CanaryStageHistory
            {
                Stage = Enum.Parse<CanaryStage>(h.Stage),
                Percentage = h.Percentage,
                Action = h.Action,
                Reason = h.Reason,
                Timestamp = h.Timestamp
            }).ToList(),
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt
        };
    }
}
