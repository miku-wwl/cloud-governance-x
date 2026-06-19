using FinOps.Application.Cloud;
using FinOps.Application.Tenancy;
using FinOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace FinOps.Infrastructure.Persistence;

internal sealed class CloudCostQueryRepository(
    IDbContextFactory<FinOpsDbContext> dbContextFactory,
    ITenantContext tenantContext) : ICloudCostQueryRepository
{
    public async Task<IReadOnlyList<CloudCostDailyPointDto>> GetDailyAsync(
        string provider,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.RequireCurrent().TenantId;
        var normalizedProvider = ProviderConnection.NormalizeProvider(provider);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await dbContext.CloudCosts
            .AsNoTracking()
            .Where(cost =>
                cost.TenantId == tenantId &&
                cost.Provider == normalizedProvider &&
                cost.UsageDate >= from &&
                cost.UsageDate <= to)
            .GroupBy(cost => new
            {
                cost.UsageDate,
                cost.Currency
            })
            .Select(group => new
            {
                group.Key.UsageDate,
                Cost = group.Sum(cost => cost.Cost),
                group.Key.Currency
            })
            .OrderBy(row => row.UsageDate)
            .ThenBy(row => row.Currency)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new CloudCostDailyPointDto(
                row.UsageDate,
                row.Cost,
                row.Currency))
            .ToArray();
    }

    public Task<IReadOnlyList<CloudCostAggregateDto>> GetByServiceAsync(
        string provider,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        return GetBreakdownAsync(
            provider,
            from,
            to,
            groupByService: true,
            cancellationToken);
    }

    public Task<IReadOnlyList<CloudCostAggregateDto>> GetByResourceGroupAsync(
        string provider,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        return GetBreakdownAsync(
            provider,
            from,
            to,
            groupByService: false,
            cancellationToken);
    }

    private async Task<IReadOnlyList<CloudCostAggregateDto>> GetBreakdownAsync(
        string provider,
        DateOnly from,
        DateOnly to,
        bool groupByService,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.RequireCurrent().TenantId;
        var normalizedProvider = ProviderConnection.NormalizeProvider(provider);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var costs = dbContext.CloudCosts
            .AsNoTracking()
            .Where(cost =>
                cost.TenantId == tenantId &&
                cost.Provider == normalizedProvider &&
                cost.UsageDate >= from &&
                cost.UsageDate <= to);

        if (groupByService)
        {
            var rows = await costs
                .GroupBy(cost => new
                {
                    Name = cost.ServiceName,
                    cost.Currency
                })
                .Select(group => new
                {
                    group.Key.Name,
                    Cost = group.Sum(cost => cost.Cost),
                    group.Key.Currency
                })
                .OrderByDescending(item => item.Cost)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken);

            return rows
                .Select(row => new CloudCostAggregateDto(
                    row.Name,
                    row.Cost,
                    row.Currency))
                .ToArray();
        }

        var resourceGroupRows = await costs
                .GroupBy(cost => new
                {
                    Name = cost.ResourceGroup,
                    cost.Currency
                })
                .Select(group => new
                {
                    group.Key.Name,
                    Cost = group.Sum(cost => cost.Cost),
                    group.Key.Currency
                })
                .OrderByDescending(item => item.Cost)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken);

        return resourceGroupRows
            .Select(row => new CloudCostAggregateDto(
                row.Name,
                row.Cost,
                row.Currency))
            .ToArray();
    }
}
