using Loopai.CloudApi.Data;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Loopai.CloudApi.Repositories;

/// <summary>
/// Entity Framework Core implementation of IExecutionRecordRepository.
/// </summary>
public class EfExecutionRecordRepository : IExecutionRecordRepository
{
    private readonly LoopaiDbContext _context;

    public EfExecutionRecordRepository(LoopaiDbContext context)
    {
        _context = context;
    }

    public async Task<ExecutionRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExecutionRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ExecutionRecord>> GetByTaskIdAsync(
        Guid taskId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ExecutionRecords
            .AsNoTracking()
            .Where(r => r.TaskId == taskId)
            .OrderByDescending(r => r.ExecutedAt);

        if (limit.HasValue)
        {
            query = (IOrderedQueryable<ExecutionRecord>)query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ExecutionRecord>> GetByProgramIdAsync(
        Guid programId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ExecutionRecords
            .AsNoTracking()
            .Where(r => r.ProgramId == programId)
            .OrderByDescending(r => r.ExecutedAt);

        if (limit.HasValue)
        {
            query = (IOrderedQueryable<ExecutionRecord>)query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ExecutionRecord>> GetSampledRecordsAsync(
        Guid taskId,
        DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ExecutionRecords
            .AsNoTracking()
            .Where(r => r.TaskId == taskId && r.SampledForValidation);

        if (since.HasValue)
        {
            query = query.Where(r => r.ExecutedAt >= since.Value);
        }

        return await query
            .OrderByDescending(r => r.ExecutedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExecutionRecord> CreateAsync(
        ExecutionRecord record,
        CancellationToken cancellationToken = default)
    {
        var created = record with
        {
            ExecutedAt = DateTime.UtcNow
        };

        _context.ExecutionRecords.Add(created);
        await _context.SaveChangesAsync(cancellationToken);

        return created;
    }

    public async Task<IEnumerable<ExecutionRecord>> CreateBatchAsync(
        IEnumerable<ExecutionRecord> records,
        CancellationToken cancellationToken = default)
    {
        var recordsList = records.ToList();
        var now = DateTime.UtcNow;

        var createdRecords = recordsList.Select(r => r with { ExecutedAt = now }).ToList();

        _context.ExecutionRecords.AddRange(createdRecords);
        await _context.SaveChangesAsync(cancellationToken);

        return createdRecords;
    }

    public async Task<ExecutionStatistics> GetStatisticsAsync(
        Guid taskId,
        DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ExecutionRecords
            .AsNoTracking()
            .Where(r => r.TaskId == taskId);

        if (since.HasValue)
        {
            query = query.Where(r => r.ExecutedAt >= since.Value);
        }

        var records = await query.ToListAsync(cancellationToken);

        var latencies = records
            .Where(r => r.LatencyMs.HasValue)
            .Select(r => r.LatencyMs!.Value)
            .OrderBy(l => l)
            .ToList();

        var stats = new ExecutionStatistics
        {
            TotalExecutions = records.Count,
            SuccessCount = records.Count(r => r.Status == ExecutionStatus.Success),
            ErrorCount = records.Count(r => r.Status == ExecutionStatus.Error),
            TimeoutCount = records.Count(r => r.Status == ExecutionStatus.Timeout),
            AverageLatencyMs = latencies.Any() ? latencies.Average() : 0,
            P95LatencyMs = latencies.Any() ? GetPercentile(latencies, 0.95) : 0,
            P99LatencyMs = latencies.Any() ? GetPercentile(latencies, 0.99) : 0,
            SampledCount = records.Count(r => r.SampledForValidation)
        };

        return stats;
    }

    private static double GetPercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;
        var index = (int)Math.Ceiling(sortedValues.Count * percentile) - 1;
        index = Math.Max(0, Math.Min(sortedValues.Count - 1, index));
        return sortedValues[index];
    }
}
