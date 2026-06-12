namespace FinOps.Domain.Costs;

public sealed class CloudCostDaily
{
    public const string UnassignedResourceGroup = "(unassigned)";

    private CloudCostDaily()
    {
    }

    private CloudCostDaily(
        string provider,
        string accountId,
        DateOnly usageDate,
        string serviceName,
        string resourceGroup,
        decimal cost,
        string currency,
        string rawJson)
    {
        Id = Guid.NewGuid();
        Provider = provider;
        AccountId = accountId;
        UsageDate = usageDate;
        ServiceName = serviceName;
        ResourceGroup = resourceGroup;
        Cost = cost;
        Currency = currency;
        RawJson = rawJson;
    }

    public Guid Id { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string AccountId { get; private set; } = string.Empty;

    public DateOnly UsageDate { get; private set; }

    public string ServiceName { get; private set; } = string.Empty;

    public string ResourceGroup { get; private set; } = UnassignedResourceGroup;

    public decimal Cost { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public string RawJson { get; private set; } = "{}";

    public static CloudCostDaily Create(
        string provider,
        string accountId,
        DateOnly usageDate,
        string serviceName,
        string? resourceGroup,
        decimal cost,
        string currency,
        string rawJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(rawJson);

        return new CloudCostDaily(
            provider.Trim(),
            accountId.Trim(),
            usageDate,
            serviceName.Trim(),
            NormalizeResourceGroup(resourceGroup),
            cost,
            currency.Trim().ToUpperInvariant(),
            rawJson);
    }

    public void Update(decimal cost, string rawJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawJson);
        Cost = cost;
        RawJson = rawJson;
    }

    public static string NormalizeResourceGroup(string? resourceGroup)
    {
        return string.IsNullOrWhiteSpace(resourceGroup)
            ? UnassignedResourceGroup
            : resourceGroup.Trim();
    }
}
