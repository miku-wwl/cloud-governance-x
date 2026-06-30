using FinOps.Domain.Tenancy;

namespace FinOps.Application.Tenancy;

public enum TenantContextSource
{
    HttpUser,
    BackgroundJob
}

public sealed record TrustedTenantContext
{
    private TrustedTenantContext(
        Guid tenantId,
        TenantContextSource source,
        string? issuer,
        string? subject,
        MembershipRole? role)
    {
        TenantId = tenantId;
        Source = source;
        Issuer = issuer;
        Subject = subject;
        Role = role;
    }

    public Guid TenantId { get; }

    public TenantContextSource Source { get; }

    public string? Issuer { get; }

    public string? Subject { get; }

    public MembershipRole? Role { get; }

    public static TrustedTenantContext ForHttpUser(
        Guid tenantId,
        string issuer,
        string subject,
        MembershipRole role = MembershipRole.Auditor)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "Membership role is not supported.");
        }

        return new TrustedTenantContext(
            tenantId,
            TenantContextSource.HttpUser,
            issuer.Trim(),
            subject.Trim(),
            role);
    }

    public static TrustedTenantContext ForBackgroundJob(Guid tenantId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        return new TrustedTenantContext(
            tenantId,
            TenantContextSource.BackgroundJob,
            issuer: null,
            subject: null,
            role: null);
    }
}

public interface ITenantContext
{
    TrustedTenantContext? Current { get; }

    TrustedTenantContext RequireCurrent();
}

public interface ITenantContextInitializer
{
    void Initialize(TrustedTenantContext context);
}

public sealed class TenantContext : ITenantContext, ITenantContextInitializer
{
    public TrustedTenantContext? Current { get; private set; }

    void ITenantContextInitializer.Initialize(TrustedTenantContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Current is not null)
        {
            throw new InvalidOperationException(
                "Tenant context has already been initialized for this scope.");
        }

        Current = context;
    }

    public TrustedTenantContext RequireCurrent() =>
        Current ?? throw new InvalidOperationException(
            "A trusted tenant context is required for this operation.");
}
