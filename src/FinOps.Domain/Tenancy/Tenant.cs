namespace FinOps.Domain.Tenancy;

public sealed class Tenant
{
    private Tenant()
    {
    }

    private Tenant(
        Guid organizationId,
        string slug,
        string displayName,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Slug = NormalizeSlug(slug);
        DisplayName = displayName;
        Status = TenantStatus.Active;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public TenantStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Tenant Create(
        Guid organizationId,
        string slug,
        string displayName,
        DateTimeOffset createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(organizationId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new Tenant(
            organizationId,
            slug,
            displayName.Trim(),
            createdAt);
    }

    public static string NormalizeSlug(string slug)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        if (
            normalized.Length is < 3 or > 63 ||
            !normalized.All(character =>
                char.IsAsciiLetterOrDigit(character) || character == '-')
        )
        {
            throw new ArgumentException(
                "Tenant slug must be 3-63 lowercase letters, digits, or hyphens.",
                nameof(slug));
        }

        if (normalized[0] == '-' || normalized[^1] == '-')
        {
            throw new ArgumentException(
                "Tenant slug cannot start or end with a hyphen.",
                nameof(slug));
        }

        return normalized;
    }
}
