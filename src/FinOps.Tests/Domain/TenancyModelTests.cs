using FinOps.Domain.Tenancy;

namespace FinOps.Tests.Domain;

public sealed class TenancyModelTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 6, 19, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Tenant_normalizes_slug_and_requires_organization()
    {
        var tenant = Tenant.Create(
            Guid.NewGuid(),
            "  Platform-Team  ",
            "Platform Team",
            Now);

        Assert.Equal("platform-team", tenant.Slug);
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Tenant.Create(Guid.Empty, "valid-slug", "Tenant", Now));
    }

    [Fact]
    public void Tenant_rejects_invalid_slug()
    {
        Assert.Throws<ArgumentException>(() =>
            Tenant.Create(Guid.NewGuid(), "bad slug", "Tenant", Now));
        Assert.Throws<ArgumentException>(() =>
            Tenant.Create(Guid.NewGuid(), "-tenant", "Tenant", Now));
    }

    [Fact]
    public void Provider_connection_contains_only_credential_reference()
    {
        var connection = ProviderConnection.Create(
            Guid.NewGuid(),
            " Azure ",
            "Production Azure",
            "keyvault://cloud-governance/azure-prod",
            Now);

        Assert.Equal("azure", connection.Provider);
        Assert.Equal(
            "keyvault://cloud-governance/azure-prod",
            connection.CredentialReference);
        Assert.Equal(ProviderConnectionStatus.Pending, connection.Status);
    }

    [Fact]
    public void Cloud_account_preserves_business_and_provider_tenant_separation()
    {
        var tenantId = Guid.NewGuid();
        var account = CloudAccount.Create(
            tenantId,
            Guid.NewGuid(),
            " Azure ",
            "subscription-id",
            "entra-directory-id",
            "Production Subscription",
            "Production",
            Now);

        Assert.Equal(tenantId, account.TenantId);
        Assert.Equal("azure", account.Provider);
        Assert.Equal("entra-directory-id", account.ProviderDirectoryId);
        Assert.NotEqual(account.TenantId.ToString(), account.ProviderDirectoryId);
    }

    [Fact]
    public void Membership_uses_issuer_and_subject_as_external_identity()
    {
        var membership = Membership.Create(
            Guid.NewGuid(),
            "https://login.microsoftonline.com/directory/v2.0",
            "subject-id",
            SubjectType.Human,
            "Ada Lovelace",
            Now);

        Assert.Equal("subject-id", membership.Subject);
        Assert.Equal(SubjectType.Human, membership.SubjectType);
        Assert.Equal(MembershipRole.Auditor, membership.Role);
        Assert.Equal(MembershipStatus.Invited, membership.Status);
    }

    [Fact]
    public void Membership_can_be_created_with_explicit_role_and_activated()
    {
        var membership = Membership.Create(
            Guid.NewGuid(),
            "https://issuer.example",
            "subject-id",
            SubjectType.Human,
            "Operator",
            Now,
            MembershipRole.Operator);

        membership.Activate(Now.AddMinutes(1));

        Assert.Equal(MembershipRole.Operator, membership.Role);
        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.Equal(Now.AddMinutes(1), membership.UpdatedAt);
    }

    [Fact]
    public void Membership_rejects_unknown_subject_type()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Membership.Create(
                Guid.NewGuid(),
                "https://issuer.example",
                "subject-id",
                (SubjectType)999,
                "Unknown Subject",
                Now));
    }

    [Fact]
    public void Membership_rejects_unknown_role()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Membership.Create(
                Guid.NewGuid(),
                "https://issuer.example",
                "subject-id",
                SubjectType.Human,
                "Unknown Role",
                Now,
                (MembershipRole)999));
    }
}
