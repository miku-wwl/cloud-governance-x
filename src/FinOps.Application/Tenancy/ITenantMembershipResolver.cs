using FinOps.Domain.Tenancy;

namespace FinOps.Application.Tenancy;

public sealed record TenantMembership(
    Guid TenantId,
    string Issuer,
    string Subject,
    MembershipRole Role);

public interface ITenantMembershipResolver
{
    Task<bool> IsActiveTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveMembershipAsync(
        Guid tenantId,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default);

    Task<TenantMembership?> ResolveActiveMembershipAsync(
        Guid tenantId,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default);

    Task<bool> IsActiveCloudAccountAsync(
        Guid tenantId,
        Guid cloudAccountId,
        CancellationToken cancellationToken = default);
}
