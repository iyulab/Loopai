using Loopai.CloudApi.Data;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Loopai.CloudApi.Repositories;

/// <summary>
/// Entity Framework Core implementation of ITaskRepository.
/// </summary>
public class EfTaskRepository : ITaskRepository
{
    private readonly LoopaiDbContext _context;

    public EfTaskRepository(LoopaiDbContext context)
    {
        _context = context;
    }

    public async Task<TaskSpecification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<TaskSpecification?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<TaskSpecification>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskSpecification> CreateAsync(TaskSpecification task, CancellationToken cancellationToken = default)
    {
        var created = task with
        {
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(created);
        await _context.SaveChangesAsync(cancellationToken);

        return created;
    }

    public async Task<TaskSpecification> UpdateAsync(TaskSpecification task, CancellationToken cancellationToken = default)
    {
        var updated = task with
        {
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tasks.Update(updated);
        await _context.SaveChangesAsync(cancellationToken);

        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks.FindAsync(new object[] { id }, cancellationToken);
        if (task == null)
        {
            return false;
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AnyAsync(t => t.Name == name, cancellationToken);
    }
}
