namespace FinOps.Domain.Tenancy;

public sealed class CloudAccount
{
    private CloudAccount()
    {
    }

    private CloudAccount(
        Guid tenantId,
        Guid providerConnectionId,
        string provider,
        string externalAccountId,
        string? providerDirectoryId,
        string displayName,
        string? environment,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        ProviderConnectionId = providerConnectionId;
        Provider = ProviderConnection.NormalizeProvider(provider);
        ExternalAccountId = externalAccountId;
        ProviderDirectoryId = NormalizeOptional(providerDirectoryId);
        DisplayName = displayName;
        Environment = NormalizeOptional(environment);
        Status = CloudAccountStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ProviderConnectionId { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string ExternalAccountId { get; private set; } = string.Empty;

    public string? ProviderDirectoryId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string? Environment { get; private set; }

    public CloudAccountStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static CloudAccount Create(
        Guid tenantId,
        Guid providerConnectionId,
        string provider,
        string externalAccountId,
        string? providerDirectoryId,
        string displayName,
        string? environment,
        DateTimeOffset createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(providerConnectionId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new CloudAccount(
            tenantId,
            providerConnectionId,
            provider,
            externalAccountId.Trim(),
            providerDirectoryId,
            displayName.Trim(),
            environment,
            createdAt);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
