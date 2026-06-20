using FinOps.Application.Etl;
using FinOps.Application.Tenancy;
using FinOps.Domain.Etl;
using Microsoft.EntityFrameworkCore;

namespace FinOps.Infrastructure.Persistence;

internal sealed class EtlJobRunRepository(
    IDbContextFactory<FinOpsDbContext> dbContextFactory,
    ITenantContext tenantContext) : IEtlJobRunRepository
{
    public async Task<Guid> StartAsync(
        string jobName,
        string provider,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.RequireCurrent().TenantId;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var run = EtlJobRun.Start(tenantId, jobName, provider, startedAt);
        dbContext.EtlJobRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        return run.Id;
    }

    public async Task CompleteAsync(
        Guid id,
        DateTimeOffset finishedAt,
        int recordsProcessed,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.RequireCurrent().TenantId;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var run = await GetRequiredAsync(dbContext, tenantId, id, cancellationToken);
        run.Complete(finishedAt, recordsProcessed);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task FailAsync(
        Guid id,
        DateTimeOffset finishedAt,
        int recordsProcessed,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.RequireCurrent().TenantId;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var run = await GetRequiredAsync(dbContext, tenantId, id, cancellationToken);
        run.Fail(finishedAt, recordsProcessed, errorMessage);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EtlJobRunDto>> GetRecentAsync(
        string? jobName,
        int take,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.RequireCurrent().TenantId;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = dbContext.EtlJobRuns
            .AsNoTracking()
            .Where(run => run.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(jobName))
        {
            query = query.Where(run => run.JobName == jobName);
        }

        return await query
            .OrderByDescending(run => run.StartedAt)
            .Take(Math.Clamp(take, 1, 100))
            .Select(run => new EtlJobRunDto(
                run.Id,
                run.JobName,
                run.Provider,
                run.StartedAt,
                run.FinishedAt,
                run.Status,
                run.RecordsProcessed,
                run.ErrorMessage))
            .ToListAsync(cancellationToken);
    }

    private static async Task<EtlJobRun> GetRequiredAsync(
        FinOpsDbContext dbContext,
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.EtlJobRuns
            .SingleOrDefaultAsync(
                run => run.TenantId == tenantId && run.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException($"ETL job run '{id}' was not found.");
    }
}
