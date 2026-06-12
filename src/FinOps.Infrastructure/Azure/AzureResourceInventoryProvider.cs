using System.Text.Json;
using Azure.ResourceManager;
using Azure.ResourceManager.ResourceGraph;
using Azure.ResourceManager.ResourceGraph.Models;
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
        var subscriptionIds = new List<string>();

        await foreach (var subscription in armClient
            .GetSubscriptions()
            .GetAllAsync(cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(subscription.Data.SubscriptionId))
            {
                subscriptionIds.Add(subscription.Data.SubscriptionId);
            }
        }

        if (subscriptionIds.Count == 0)
        {
            return [];
        }

        var tenant = await GetTenantAsync(cancellationToken);
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
                GetString(item, "subscriptionId"),
                GetString(item, "id"),
                GetString(item, "name"),
                GetString(item, "type"),
                GetString(item, "location"),
                GetNullableString(item, "resourceGroup"),
                GetTags(item)));
        }

        return resources;
    }

    private async Task<global::Azure.ResourceManager.Resources.TenantResource> GetTenantAsync(
        CancellationToken cancellationToken)
    {
        await foreach (var tenant in armClient.GetTenants().GetAllAsync(cancellationToken))
        {
            return tenant;
        }

        throw new InvalidOperationException("No Azure tenant is available to query Resource Graph.");
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
