using Loopai.CloudApi.Data;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Loopai.CloudApi.Repositories;

/// <summary>
/// Entity Framework Core implementation of IValidationResultRepository.
/// </summary>
public class EfValidationResultRepository : IValidationResultRepository
{
    private readonly LoopaiDbContext _context;

    public EfValidationResultRepository(LoopaiDbContext context)
    {
        _context = context;
    }

    public async Task<ValidationResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ValidationResults
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ValidationResult>> GetByExecutionIdAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ValidationResults
            .AsNoTracking()
            .Where(v => v.ExecutionId == executionId)
            .OrderByDescending(v => v.ValidatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ValidationResult>> GetByTaskIdAsync(
        Guid taskId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ValidationResults
            .AsNoTracking()
            .Where(v => v.TaskId == taskId)
            .OrderByDescending(v => v.ValidatedAt);

        if (limit.HasValue)
        {
            query = (IOrderedQueryable<ValidationResult>)query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ValidationResult>> GetByProgramIdAsync(
        Guid programId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ValidationResults
            .AsNoTracking()
            .Where(v => v.ProgramId == programId)
            .OrderByDescending(v => v.ValidatedAt);

        if (limit.HasValue)
        {
            query = (IOrderedQueryable<ValidationResult>)query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ValidationResult>> GetFailedByProgramIdAsync(
        Guid programId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ValidationResults
            .AsNoTracking()
            .Where(v => v.ProgramId == programId && !v.IsValid)
            .OrderByDescending(v => v.ValidatedAt);

        if (limit.HasValue)
        {
            query = (IOrderedQueryable<ValidationResult>)query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<ValidationResult> CreateAsync(
        ValidationResult result,
        CancellationToken cancellationToken = default)
    {
        _context.ValidationResults.Add(result);
        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<IEnumerable<ValidationResult>> CreateBatchAsync(
        IEnumerable<ValidationResult> results,
        CancellationToken cancellationToken = default)
    {
        var resultsList = results.ToList();
        _context.ValidationResults.AddRange(resultsList);
        await _context.SaveChangesAsync(cancellationToken);
        return resultsList;
    }

    public async Task<ValidationStatistics> GetStatisticsAsync(
        Guid programId,
        DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ValidationResults
            .AsNoTracking()
            .Where(v => v.ProgramId == programId);

        if (since.HasValue)
        {
            query = query.Where(v => v.ValidatedAt >= since.Value);
        }

        var results = await query.ToListAsync(cancellationToken);

        if (results.Count == 0)
        {
            return new ValidationStatistics
            {
                TotalValidations = 0,
                ValidCount = 0,
                InvalidCount = 0,
                AverageScore = 0.0,
                ValidationRate = 0.0
            };
        }

        var validCount = results.Count(r => r.IsValid);
        var invalidCount = results.Count - validCount;
        var averageScore = results.Average(r => r.ValidationScore);
        var validationRate = (double)validCount / results.Count;

        return new ValidationStatistics
        {
            TotalValidations = results.Count,
            ValidCount = validCount,
            InvalidCount = invalidCount,
            AverageScore = averageScore,
            ValidationRate = validationRate
        };
    }
}
