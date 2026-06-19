using FinOps.Application.Tenancy;
using FinOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace FinOps.Infrastructure.Persistence;

internal sealed class TenantMembershipResolver(FinOpsDbContext dbContext) :
    ITenantMembershipResolver
{
    public Task<bool> IsActiveTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);

        return dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant =>
                    tenant.Id == tenantId &&
                    tenant.Status == TenantStatus.Active,
                cancellationToken);
    }

    public Task<bool> HasActiveMembershipAsync(
        Guid tenantId,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        return dbContext.Memberships
            .AsNoTracking()
            .AnyAsync(
                membership =>
                    membership.TenantId == tenantId &&
                    membership.Issuer == issuer &&
                    membership.Subject == subject &&
                    membership.Status == MembershipStatus.Active &&
                    dbContext.Tenants.Any(tenant =>
                        tenant.Id == membership.TenantId &&
                        tenant.Status == TenantStatus.Active),
                cancellationToken);
    }
}
