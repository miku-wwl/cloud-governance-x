using FinOps.Application.Tenancy;
using FinOps.Domain.Tenancy;

namespace FinOps.Application.Authorization;

public enum FinOpsPermission
{
    TenantManage,
    ResourceRead,
    ResourceSync,
    CostRead,
    CostSync,
    EtlRunRead,
    PlatformOperate
}

public enum FinOpsAuthorizationScopeKind
{
    Tenant,
    CloudAccount,
    Platform
}

public sealed record FinOpsAuthorizationScope
{
    private FinOpsAuthorizationScope(
        FinOpsAuthorizationScopeKind kind,
        Guid? tenantId,
        Guid? cloudAccountId)
    {
        Kind = kind;
        TenantId = tenantId;
        CloudAccountId = cloudAccountId;
    }

    public FinOpsAuthorizationScopeKind Kind { get; }

    public Guid? TenantId { get; }

    public Guid? CloudAccountId { get; }

    public static FinOpsAuthorizationScope Tenant(Guid tenantId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        return new FinOpsAuthorizationScope(
            FinOpsAuthorizationScopeKind.Tenant,
            tenantId,
            cloudAccountId: null);
    }

    public static FinOpsAuthorizationScope CloudAccount(
        Guid tenantId,
        Guid cloudAccountId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(cloudAccountId, Guid.Empty);
        return new FinOpsAuthorizationScope(
            FinOpsAuthorizationScopeKind.CloudAccount,
            tenantId,
            cloudAccountId);
    }

    public static FinOpsAuthorizationScope Platform() =>
        new(
            FinOpsAuthorizationScopeKind.Platform,
            tenantId: null,
            cloudAccountId: null);
}

public sealed record FinOpsAuthorizationDecision(
    bool IsAllowed,
    string Reason)
{
    public static FinOpsAuthorizationDecision Allow(string reason) =>
        new(IsAllowed: true, reason);

    public static FinOpsAuthorizationDecision Deny(string reason) =>
        new(IsAllowed: false, reason);
}

public interface IFinOpsAuthorizationService
{
    Task<FinOpsAuthorizationDecision> AuthorizeAsync(
        FinOpsPermission permission,
        FinOpsAuthorizationScope scope,
        CancellationToken cancellationToken = default);
}

public sealed class FinOpsAuthorizationService(
    ITenantContext tenantContext,
    ITenantMembershipResolver membershipResolver) : IFinOpsAuthorizationService
{
    public async Task<FinOpsAuthorizationDecision> AuthorizeAsync(
        FinOpsPermission permission,
        FinOpsAuthorizationScope scope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (!Enum.IsDefined(permission))
        {
            throw new ArgumentOutOfRangeException(
                nameof(permission),
                permission,
                "Permission is not supported.");
        }

        var current = tenantContext.Current;
        if (current is null)
        {
            return FinOpsAuthorizationDecision.Deny(
                "A trusted tenant context is required.");
        }

        if (current.Source != TenantContextSource.HttpUser ||
            string.IsNullOrWhiteSpace(current.Issuer) ||
            string.IsNullOrWhiteSpace(current.Subject))
        {
            return FinOpsAuthorizationDecision.Deny(
                "RBAC currently requires an authenticated user membership.");
        }

        if (scope.Kind == FinOpsAuthorizationScopeKind.Platform)
        {
            return FinOpsAuthorizationDecision.Deny(
                "Tenant memberships do not grant platform scope.");
        }

        if (scope.TenantId != current.TenantId)
        {
            return FinOpsAuthorizationDecision.Deny(
                "Requested scope belongs to a different tenant.");
        }

        var membership = await membershipResolver.ResolveActiveMembershipAsync(
            current.TenantId,
            current.Issuer,
            current.Subject,
            cancellationToken);
        if (membership is null)
        {
            return FinOpsAuthorizationDecision.Deny(
                "Active membership is required.");
        }

        if (scope.Kind == FinOpsAuthorizationScopeKind.CloudAccount)
        {
            var cloudAccountId = scope.CloudAccountId ??
                throw new InvalidOperationException(
                    "Cloud account scope requires a cloud account id.");
            var isActiveCloudAccount =
                await membershipResolver.IsActiveCloudAccountAsync(
                    current.TenantId,
                    cloudAccountId,
                    cancellationToken);
            if (!isActiveCloudAccount)
            {
                return FinOpsAuthorizationDecision.Deny(
                    "Cloud account is not active in the current tenant.");
            }
        }

        return RoleAllows(membership.Role, permission)
            ? FinOpsAuthorizationDecision.Allow(
                "Membership role grants the requested permission.")
            : FinOpsAuthorizationDecision.Deny(
                "Membership role does not grant the requested permission.");
    }

    private static bool RoleAllows(
        MembershipRole role,
        FinOpsPermission permission) =>
        role switch
        {
            MembershipRole.Owner => permission != FinOpsPermission.PlatformOperate,
            MembershipRole.Administrator => permission is
                FinOpsPermission.TenantManage or
                FinOpsPermission.ResourceRead or
                FinOpsPermission.ResourceSync or
                FinOpsPermission.CostRead or
                FinOpsPermission.CostSync or
                FinOpsPermission.EtlRunRead,
            MembershipRole.Operator => permission is
                FinOpsPermission.ResourceRead or
                FinOpsPermission.ResourceSync or
                FinOpsPermission.CostRead or
                FinOpsPermission.CostSync or
                FinOpsPermission.EtlRunRead,
            MembershipRole.Analyst => permission is
                FinOpsPermission.ResourceRead or
                FinOpsPermission.CostRead,
            MembershipRole.Auditor => permission is
                FinOpsPermission.ResourceRead or
                FinOpsPermission.CostRead or
                FinOpsPermission.EtlRunRead,
            _ => false
        };
}
