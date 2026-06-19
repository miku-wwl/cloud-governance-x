using System.Text.Json;
using FinOps.Application.Cloud;
using FinOps.Application.Tenancy;
using FinOps.Domain.CloudResources;
using FinOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace FinOps.Infrastructure.Persistence;

internal sealed class CloudResourceRepository(
    FinOpsDbContext dbContext,
    ITenantContext tenantContext) : ICloudResourceRepository
{
    public async Task<CloudResourceUpsertResult> UpsertAsync(
        IReadOnlyCollection<CloudResourceDto> resources,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.RequireCurrent().TenantId;

        if (resources.Count == 0)
        {
            return new CloudResourceUpsertResult(0, 0);
        }

        var normalizedIds = resources
            .Select(resource => CloudResource.NormalizeResourceId(resource.ResourceId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var providers = resources
            .Select(resource => ProviderConnection.NormalizeProvider(resource.Provider))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var existingResources = await dbContext.CloudResources
            .Where(resource =>
                resource.TenantId == tenantId &&
                providers.Contains(resource.Provider) &&
                normalizedIds.Contains(resource.ResourceIdNormalized))
            .ToDictionaryAsync(
                resource => CreateKey(resource.Provider, resource.ResourceIdNormalized),
                cancellationToken);

        var inserted = 0;
        var updated = 0;

        foreach (var resource in resources)
        {
            var normalizedId = CloudResource.NormalizeResourceId(resource.ResourceId);
            var provider = ProviderConnection.NormalizeProvider(resource.Provider);
            var key = CreateKey(provider, normalizedId);
            var tagsJson = JsonSerializer.Serialize(resource.Tags);

            if (existingResources.TryGetValue(key, out var existing))
            {
                existing.UpdateObservation(
                    resource.AccountId,
                    resource.ResourceName,
                    resource.ResourceType,
                    resource.Region,
                    resource.ResourceGroup,
                    tagsJson,
                    observedAt);
                updated++;
                continue;
            }

            var entity = CloudResource.Create(
                tenantId,
                provider,
                resource.AccountId,
                resource.ResourceId,
                resource.ResourceName,
                resource.ResourceType,
                resource.Region,
                resource.ResourceGroup,
                tagsJson,
                observedAt);

            dbContext.CloudResources.Add(entity);
            existingResources.Add(key, entity);
            inserted++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CloudResourceUpsertResult(inserted, updated);
    }

    private static string CreateKey(string provider, string normalizedResourceId)
    {
        return $"{provider}\n{normalizedResourceId}";
    }
}
