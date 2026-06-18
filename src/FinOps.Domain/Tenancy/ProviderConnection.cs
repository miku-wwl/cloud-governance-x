namespace FinOps.Domain.Tenancy;

public sealed class ProviderConnection
{
    private ProviderConnection()
    {
    }

    private ProviderConnection(
        Guid tenantId,
        string provider,
        string displayName,
        string credentialReference,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Provider = NormalizeProvider(provider);
        DisplayName = displayName;
        CredentialReference = credentialReference;
        Status = ProviderConnectionStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string CredentialReference { get; private set; } = string.Empty;

    public ProviderConnectionStatus Status { get; private set; }

    public DateTimeOffset? LastValidatedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ProviderConnection Create(
        Guid tenantId,
        string provider,
        string displayName,
        string credentialReference,
        DateTimeOffset createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialReference);

        return new ProviderConnection(
            tenantId,
            provider,
            displayName.Trim(),
            credentialReference.Trim(),
            createdAt);
    }

    public static string NormalizeProvider(string provider) =>
        provider.Trim().ToLowerInvariant();
}
