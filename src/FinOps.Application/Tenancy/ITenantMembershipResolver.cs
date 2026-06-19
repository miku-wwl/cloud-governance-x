namespace FinOps.Application.Tenancy;

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
}
