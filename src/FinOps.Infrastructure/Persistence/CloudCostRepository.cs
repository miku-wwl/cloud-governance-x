using FinOps.Application.Cloud;
using FinOps.Domain.Costs;
using Microsoft.EntityFrameworkCore;

namespace FinOps.Infrastructure.Persistence;

internal sealed class CloudCostRepository(FinOpsDbContext dbContext) : ICloudCostRepository
{
    public async Task<CloudCostUpsertResult> UpsertAsync(
        IReadOnlyCollection<CloudCostDailyDto> costs,
        CancellationToken cancellationToken = default)
    {
        if (costs.Count == 0)
        {
            return new CloudCostUpsertResult(0, 0);
        }

        var providers = costs.Select(cost => cost.Provider).Distinct().ToArray();
        var accounts = costs.Select(cost => cost.AccountId).Distinct().ToArray();
        var from = costs.Min(cost => cost.UsageDate);
        var to = costs.Max(cost => cost.UsageDate);

        var existing = await dbContext.CloudCosts
            .Where(cost =>
                providers.Contains(cost.Provider) &&
                accounts.Contains(cost.AccountId) &&
                cost.UsageDate >= from &&
                cost.UsageDate <= to)
            .ToDictionaryAsync(
                cost => CreateKey(
                    cost.Provider,
                    cost.AccountId,
                    cost.UsageDate,
                    cost.ServiceName,
                    cost.ResourceGroup,
                    cost.Currency),
                cancellationToken);

        var inserted = 0;
        var updated = 0;

        foreach (var cost in costs)
        {
            var resourceGroup = CloudCostDaily.NormalizeResourceGroup(cost.ResourceGroup);
            var key = CreateKey(
                cost.Provider,
                cost.AccountId,
                cost.UsageDate,
                cost.ServiceName,
                resourceGroup,
                cost.Currency);

            if (existing.TryGetValue(key, out var entity))
            {
                entity.Update(cost.Cost, cost.RawJson);
                updated++;
                continue;
            }

            entity = CloudCostDaily.Create(
                cost.Provider,
                cost.AccountId,
                cost.UsageDate,
                cost.ServiceName,
                resourceGroup,
                cost.Cost,
                cost.Currency,
                cost.RawJson);
            dbContext.CloudCosts.Add(entity);
            existing.Add(key, entity);
            inserted++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CloudCostUpsertResult(inserted, updated);
    }

    private static string CreateKey(
        string provider,
        string accountId,
        DateOnly usageDate,
        string serviceName,
        string resourceGroup,
        string currency)
    {
        return string.Join(
            '\n',
            provider,
            accountId,
            usageDate.ToString("yyyy-MM-dd"),
            serviceName.Trim(),
            CloudCostDaily.NormalizeResourceGroup(resourceGroup),
            currency.Trim().ToUpperInvariant());
    }
}
