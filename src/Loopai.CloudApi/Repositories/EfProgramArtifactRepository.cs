using Loopai.CloudApi.Data;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Loopai.CloudApi.Repositories;

/// <summary>
/// Entity Framework Core implementation of IProgramArtifactRepository.
/// </summary>
public class EfProgramArtifactRepository : IProgramArtifactRepository
{
    private readonly LoopaiDbContext _context;

    public EfProgramArtifactRepository(LoopaiDbContext context)
    {
        _context = context;
    }

    public async Task<ProgramArtifact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProgramArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ProgramArtifact>> GetByTaskIdAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProgramArtifacts
            .AsNoTracking()
            .Where(a => a.TaskId == taskId)
            .OrderBy(a => a.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProgramArtifact?> GetByTaskIdAndVersionAsync(
        Guid taskId,
        int version,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProgramArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.TaskId == taskId && a.Version == version, cancellationToken);
    }

    public async Task<ProgramArtifact?> GetActiveByTaskIdAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProgramArtifacts
            .AsNoTracking()
            .Where(a => a.TaskId == taskId && a.Status == ProgramStatus.Active)
            .OrderByDescending(a => a.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProgramArtifact> CreateAsync(
        ProgramArtifact artifact,
        CancellationToken cancellationToken = default)
    {
        var created = artifact with
        {
            CreatedAt = DateTime.UtcNow
        };

        _context.ProgramArtifacts.Add(created);
        await _context.SaveChangesAsync(cancellationToken);

        return created;
    }

    public async Task<ProgramArtifact> UpdateAsync(
        ProgramArtifact artifact,
        CancellationToken cancellationToken = default)
    {
        _context.ProgramArtifacts.Update(artifact);
        await _context.SaveChangesAsync(cancellationToken);

        return artifact;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var artifact = await _context.ProgramArtifacts.FindAsync(new object[] { id }, cancellationToken);
        if (artifact == null)
        {
            return false;
        }

        _context.ProgramArtifacts.Remove(artifact);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<int> GetLatestVersionAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var latestVersion = await _context.ProgramArtifacts
            .Where(a => a.TaskId == taskId)
            .MaxAsync(a => (int?)a.Version, cancellationToken);

        return latestVersion ?? 0;
    }
}
