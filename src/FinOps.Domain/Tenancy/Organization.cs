namespace FinOps.Domain.Tenancy;

public sealed class Organization
{
    private Organization()
    {
    }

    private Organization(string displayName, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        DisplayName = displayName;
        Status = OrganizationStatus.Active;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public OrganizationStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Organization Create(
        string displayName,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new Organization(displayName.Trim(), createdAt);
    }
}
