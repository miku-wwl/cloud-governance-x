using FinOps.Domain.Auditing;

namespace FinOps.Tests.Domain;

public sealed class AuthorizationAuditEventTests
{
    [Fact]
    public void Record_rejects_invalid_status_code()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AuthorizationAuditEvent.Record(
                DateTimeOffset.UtcNow,
                "CostSync",
                "Tenant",
                Guid.NewGuid(),
                cloudAccountId: null,
                isAllowed: false,
                isHighPrivilege: true,
                "Denied",
                "issuer",
                "subject",
                "POST",
                "/api/admin/sync/azure/costs",
                99,
                "trace"));
    }

    [Fact]
    public void Record_normalizes_and_truncates_non_secret_fields()
    {
        var auditEvent = AuthorizationAuditEvent.Record(
            DateTimeOffset.Parse("2026-06-30T01:00:00Z"),
            " CostSync ",
            " Tenant ",
            Guid.Parse("70000000-0000-0000-0000-000000000029"),
            cloudAccountId: null,
            isAllowed: true,
            isHighPrivilege: true,
            new string('r', 600),
            " issuer ",
            " subject ",
            "POST",
            "/api/admin/sync/azure/costs",
            200,
            " trace ");

        Assert.Equal("CostSync", auditEvent.Permission);
        Assert.Equal("Tenant", auditEvent.ScopeKind);
        Assert.True(auditEvent.IsAllowed);
        Assert.True(auditEvent.IsHighPrivilege);
        Assert.Equal(512, auditEvent.Reason.Length);
        Assert.Equal("issuer", auditEvent.ActorIssuer);
        Assert.Equal("subject", auditEvent.ActorSubject);
        Assert.Equal("trace", auditEvent.CorrelationId);
    }
}
