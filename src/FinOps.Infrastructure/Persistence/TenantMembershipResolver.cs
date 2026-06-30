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

    public async Task<TenantMembership?> ResolveActiveMembershipAsync(
        Guid tenantId,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        return await dbContext.Memberships
            .AsNoTracking()
            .Where(membership =>
                membership.TenantId == tenantId &&
                membership.Issuer == issuer &&
                membership.Subject == subject &&
                membership.Status == MembershipStatus.Active &&
                dbContext.Tenants.Any(tenant =>
                    tenant.Id == membership.TenantId &&
                    tenant.Status == TenantStatus.Active))
            .Select(membership => new TenantMembership(
                membership.TenantId,
                membership.Issuer,
                membership.Subject,
                membership.Role))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> IsActiveCloudAccountAsync(
        Guid tenantId,
        Guid cloudAccountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(cloudAccountId, Guid.Empty);

        return dbContext.CloudAccounts
            .AsNoTracking()
            .AnyAsync(
                account =>
                    account.TenantId == tenantId &&
                    account.Id == cloudAccountId &&
                    account.Status == CloudAccountStatus.Active,
                cancellationToken);
    }
}
