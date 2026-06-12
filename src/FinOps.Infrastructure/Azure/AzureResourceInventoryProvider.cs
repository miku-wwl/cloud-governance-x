using System.Text.Json;
using Azure.ResourceManager;
using Azure.ResourceManager.ResourceGraph;
using Azure.ResourceManager.ResourceGraph.Models;
using Azure.ResourceManager.Resources;
using FinOps.Application.Cloud;

namespace FinOps.Infrastructure.Azure;

internal sealed class AzureResourceInventoryProvider(ArmClient armClient)
    : ICloudResourceInventoryProvider
{
    private const string Query = """
        Resources
        | project
            id = tostring(id),
            name = tostring(name),
            type = tostring(type),
            location = tostring(location),
            resourceGroup = tostring(resourceGroup),
            subscriptionId = tostring(subscriptionId),
            tags
        | order by id asc
        """;

    public async Task<IReadOnlyList<CloudResourceDto>> GetResourcesAsync(
        CancellationToken cancellationToken = default)
    {
        var subscriptionsByTenant = new Dictionary<Guid, List<string>>();

        await foreach (var subscription in armClient
            .GetSubscriptions()
            .GetAllAsync(cancellationToken))
        {
            var subscriptionId = subscription.Data.SubscriptionId;
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                continue;
            }

            var tenantId = subscription.Data.TenantId
                ?? throw new InvalidOperationException(
                    $"Azure subscription '{subscriptionId}' does not expose a tenant ID.");

            if (!subscriptionsByTenant.TryGetValue(tenantId, out var subscriptionIds))
            {
                subscriptionIds = [];
                subscriptionsByTenant.Add(tenantId, subscriptionIds);
            }

            subscriptionIds.Add(subscriptionId);
        }

        if (subscriptionsByTenant.Count == 0)
        {
            return [];
        }

        var tenants = await GetTenantsAsync(cancellationToken);
        var resources = new List<CloudResourceDto>();

        foreach (var (tenantId, subscriptionIds) in subscriptionsByTenant)
        {
            if (!tenants.TryGetValue(tenantId, out var tenant))
            {
                throw new InvalidOperationException(
                    $"Azure tenant '{tenantId}' is not available for Resource Graph queries.");
            }

            resources.AddRange(await GetResourcesAsync(
                tenant,
                subscriptionIds,
                cancellationToken));
        }

        return resources;
    }

    private static async Task<IReadOnlyList<CloudResourceDto>> GetResourcesAsync(
        TenantResource tenant,
        IReadOnlyCollection<string> subscriptionIds,
        CancellationToken cancellationToken)
    {
        var resources = new List<CloudResourceDto>();
        string? skipToken = null;

        do
        {
            var content = new ResourceQueryContent(Query)
            {
                Options = new ResourceQueryRequestOptions
                {
                    ResultFormat = ResultFormat.ObjectArray,
                    Top = 1000,
                    SkipToken = skipToken
                }
            };

            foreach (var subscriptionId in subscriptionIds)
            {
                content.Subscriptions.Add(subscriptionId);
            }

            var response = await tenant.GetResourcesAsync(content, cancellationToken);
            resources.AddRange(ParseResources(response.Value.Data));
            skipToken = response.Value.SkipToken;
        }
        while (!string.IsNullOrWhiteSpace(skipToken));

        return resources;
    }

    internal static IReadOnlyList<CloudResourceDto> ParseResources(BinaryData data)
    {
        using var document = JsonDocument.Parse(data);
        var resources = new List<CloudResourceDto>();

        foreach (var item in document.RootElement.EnumerateArray())
        {
            resources.Add(new CloudResourceDto(
                "Azure",
                GetRequiredString(item, "subscriptionId"),
                GetRequiredString(item, "id"),
                GetRequiredString(item, "name"),
                GetRequiredString(item, "type"),
                GetString(item, "location"),
                GetNullableString(item, "resourceGroup"),
                GetTags(item)));
        }

        return resources;
    }

    private async Task<IReadOnlyDictionary<Guid, TenantResource>> GetTenantsAsync(
        CancellationToken cancellationToken)
    {
        var tenants = new Dictionary<Guid, TenantResource>();

        await foreach (var tenant in armClient.GetTenants().GetAllAsync(cancellationToken))
        {
            if (tenant.Data.TenantId is { } tenantId)
            {
                tenants[tenantId] = tenant;
            }
        }

        return tenants;
    }

    private static string GetRequiredString(JsonElement item, string propertyName)
    {
        var value = GetNullableString(item, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException(
                $"Azure Resource Graph result is missing required property '{propertyName}'.");
        }

        return value;
    }

    private static string GetString(JsonElement item, string propertyName)
    {
        return GetNullableString(item, propertyName) ?? string.Empty;
    }

    private static string? GetNullableString(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var value) &&
               value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined
            ? value.GetString()
            : null;
    }

    private static IReadOnlyDictionary<string, string> GetTags(JsonElement item)
    {
        if (!item.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string>();
        }

        return tags.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? string.Empty
                : property.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);
    }
}
