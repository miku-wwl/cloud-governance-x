namespace FinOps.Domain.CloudResources;

public sealed class CloudResource
{
    private CloudResource()
    {
    }

    private CloudResource(
        string provider,
        string accountId,
        string resourceId,
        string resourceName,
        string resourceType,
        string region,
        string? resourceGroup,
        string tagsJson,
        DateTimeOffset observedAt)
    {
        Id = Guid.NewGuid();
        Provider = provider;
        AccountId = accountId;
        ResourceId = resourceId;
        ResourceIdNormalized = NormalizeResourceId(resourceId);
        ResourceName = resourceName;
        ResourceType = resourceType;
        Region = region;
        ResourceGroup = resourceGroup;
        TagsJson = tagsJson;
        FirstSeenAt = observedAt;
        LastSeenAt = observedAt;
    }

    public Guid Id { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string AccountId { get; private set; } = string.Empty;

    public string ResourceId { get; private set; } = string.Empty;

    public string ResourceIdNormalized { get; private set; } = string.Empty;

    public string ResourceName { get; private set; } = string.Empty;

    public string ResourceType { get; private set; } = string.Empty;

    public string Region { get; private set; } = string.Empty;

    public string? ResourceGroup { get; private set; }

    public string TagsJson { get; private set; } = "{}";

    public DateTimeOffset FirstSeenAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public static CloudResource Create(
        string provider,
        string accountId,
        string resourceId,
        string resourceName,
        string resourceType,
        string region,
        string? resourceGroup,
        string tagsJson,
        DateTimeOffset observedAt)
    {
        return new CloudResource(
            provider,
            accountId,
            resourceId,
            resourceName,
            resourceType,
            region,
            resourceGroup,
            tagsJson,
            observedAt);
    }

    public void UpdateObservation(
        string accountId,
        string resourceName,
        string resourceType,
        string region,
        string? resourceGroup,
        string tagsJson,
        DateTimeOffset observedAt)
    {
        AccountId = accountId;
        ResourceName = resourceName;
        ResourceType = resourceType;
        Region = region;
        ResourceGroup = resourceGroup;
        TagsJson = tagsJson;
        LastSeenAt = observedAt;
    }

    public static string NormalizeResourceId(string resourceId)
    {
        return resourceId.Trim().ToUpperInvariant();
    }
}
