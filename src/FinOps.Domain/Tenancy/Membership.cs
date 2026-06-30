namespace FinOps.Domain.Tenancy;

public sealed class Membership
{
    private Membership()
    {
    }

    private Membership(
        Guid tenantId,
        string issuer,
        string subject,
        SubjectType subjectType,
        string displayName,
        DateTimeOffset createdAt,
        MembershipRole role)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Issuer = issuer;
        Subject = subject;
        SubjectType = subjectType;
        DisplayName = displayName;
        Role = role;
        Status = MembershipStatus.Invited;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Issuer { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public SubjectType SubjectType { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public MembershipRole Role { get; private set; }

    public MembershipStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Membership Create(
        Guid tenantId,
        string issuer,
        string subject,
        SubjectType subjectType,
        string displayName,
        DateTimeOffset createdAt,
        MembershipRole role = MembershipRole.Auditor)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (!Enum.IsDefined(subjectType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(subjectType),
                subjectType,
                "Membership subject type is not supported.");
        }
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "Membership role is not supported.");
        }

        return new Membership(
            tenantId,
            issuer.Trim(),
            subject.Trim(),
            subjectType,
            displayName.Trim(),
            createdAt,
            role);
    }

    public void Activate(DateTimeOffset updatedAt)
    {
        Status = MembershipStatus.Active;
        UpdatedAt = updatedAt;
    }
}
