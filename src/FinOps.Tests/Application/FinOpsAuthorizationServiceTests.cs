using FinOps.Application.Authorization;
using FinOps.Application.Tenancy;
using FinOps.Domain.Tenancy;

namespace FinOps.Tests.Application;

public sealed class FinOpsAuthorizationServiceTests
{
    private const string Issuer = "https://issuer.example";
    private const string Subject = "subject-a";
    private static readonly Guid TenantId =
        Guid.Parse("30000000-0000-0000-0000-000000000027");
    private static readonly Guid OtherTenantId =
        Guid.Parse("30000000-0000-0000-0000-000000000028");
    private static readonly Guid CloudAccountId =
        Guid.Parse("40000000-0000-0000-0000-000000000027");

    [Theory]
    [InlineData(MembershipRole.Owner, FinOpsPermission.TenantManage, true)]
    [InlineData(MembershipRole.Administrator, FinOpsPermission.TenantManage, true)]
    [InlineData(MembershipRole.Operator, FinOpsPermission.TenantManage, false)]
    [InlineData(MembershipRole.Operator, FinOpsPermission.ResourceSync, true)]
    [InlineData(MembershipRole.Analyst, FinOpsPermission.CostRead, true)]
    [InlineData(MembershipRole.Analyst, FinOpsPermission.CostSync, false)]
    [InlineData(MembershipRole.Auditor, FinOpsPermission.EtlRunRead, true)]
    [InlineData(MembershipRole.Auditor, FinOpsPermission.ResourceSync, false)]
    public async Task Role_matrix_controls_tenant_permissions(
        MembershipRole role,
        FinOpsPermission permission,
        bool expectedAllowed)
    {
        var decision = await CreateService(role).AuthorizeAsync(
            permission,
            FinOpsAuthorizationScope.Tenant(TenantId));

        Assert.Equal(expectedAllowed, decision.IsAllowed);
    }

    [Fact]
    public async Task Cloud_account_scope_requires_account_in_current_tenant()
    {
        var resolver = new StubMembershipResolver(
            MembershipRole.Operator,
            activeCloudAccountIds: new HashSet<Guid> { CloudAccountId });
        var decision = await CreateService(resolver).AuthorizeAsync(
            FinOpsPermission.ResourceSync,
            FinOpsAuthorizationScope.CloudAccount(TenantId, CloudAccountId));

        Assert.True(decision.IsAllowed);
        Assert.Equal(TenantId, resolver.CloudAccountTenantId);
        Assert.Equal(CloudAccountId, resolver.CloudAccountId);
    }

    [Fact]
    public async Task Cloud_account_scope_denies_unknown_or_inactive_account()
    {
        var decision = await CreateService(MembershipRole.Operator).AuthorizeAsync(
            FinOpsPermission.ResourceSync,
            FinOpsAuthorizationScope.CloudAccount(TenantId, CloudAccountId));

        Assert.False(decision.IsAllowed);
        Assert.Contains("Cloud account", decision.Reason);
    }

    [Fact]
    public async Task Tenant_member_cannot_use_platform_scope()
    {
        var decision = await CreateService(MembershipRole.Owner).AuthorizeAsync(
            FinOpsPermission.PlatformOperate,
            FinOpsAuthorizationScope.Platform());

        Assert.False(decision.IsAllowed);
        Assert.Contains("platform", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cross_tenant_scope_is_denied_before_role_evaluation()
    {
        var resolver = new StubMembershipResolver(MembershipRole.Owner);

        var decision = await CreateService(resolver).AuthorizeAsync(
            FinOpsPermission.TenantManage,
            FinOpsAuthorizationScope.Tenant(OtherTenantId));

        Assert.False(decision.IsAllowed);
        Assert.Equal(0, resolver.ResolveMembershipCallCount);
    }

    [Fact]
    public async Task Missing_tenant_context_is_denied()
    {
        var service = new FinOpsAuthorizationService(
            new TenantContext(),
            new StubMembershipResolver(MembershipRole.Owner));

        var decision = await service.AuthorizeAsync(
            FinOpsPermission.CostRead,
            FinOpsAuthorizationScope.Tenant(TenantId));

        Assert.False(decision.IsAllowed);
        Assert.Contains("tenant context", decision.Reason);
    }

    [Fact]
    public async Task Inactive_or_unknown_membership_is_denied()
    {
        var decision = await CreateService(
                new StubMembershipResolver(membershipRole: null))
            .AuthorizeAsync(
                FinOpsPermission.CostRead,
                FinOpsAuthorizationScope.Tenant(TenantId));

        Assert.False(decision.IsAllowed);
        Assert.Contains("Active membership", decision.Reason);
    }

    [Fact]
    public async Task Background_tenant_context_does_not_bypass_rbac()
    {
        var tenantContext = new TenantContext();
        ((ITenantContextInitializer)tenantContext).Initialize(
            TrustedTenantContext.ForBackgroundJob(TenantId));
        var service = new FinOpsAuthorizationService(
            tenantContext,
            new StubMembershipResolver(MembershipRole.Owner));

        var decision = await service.AuthorizeAsync(
            FinOpsPermission.CostSync,
            FinOpsAuthorizationScope.Tenant(TenantId));

        Assert.False(decision.IsAllowed);
        Assert.Contains("authenticated user", decision.Reason);
    }

    private static FinOpsAuthorizationService CreateService(
        MembershipRole role) =>
        CreateService(new StubMembershipResolver(role));

    private static FinOpsAuthorizationService CreateService(
        StubMembershipResolver resolver)
    {
        var tenantContext = new TenantContext();
        ((ITenantContextInitializer)tenantContext).Initialize(
            TrustedTenantContext.ForHttpUser(
                TenantId,
                Issuer,
                Subject,
                resolver.Role ?? MembershipRole.Auditor));
        return new FinOpsAuthorizationService(tenantContext, resolver);
    }

    private sealed class StubMembershipResolver(
        MembershipRole? membershipRole,
        IReadOnlySet<Guid>? activeCloudAccountIds = null) : ITenantMembershipResolver
    {
        public MembershipRole? Role { get; } = membershipRole;

        public int ResolveMembershipCallCount { get; private set; }

        public Guid? CloudAccountTenantId { get; private set; }

        public Guid? CloudAccountId { get; private set; }

        public Task<bool> IsActiveTenantAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(tenantId == TenantId);

        public Task<TenantMembership?> ResolveActiveMembershipAsync(
            Guid tenantId,
            string issuer,
            string subject,
            CancellationToken cancellationToken = default)
        {
            ResolveMembershipCallCount++;
            TenantMembership? membership =
                Role is not null &&
                tenantId == TenantId &&
                issuer == Issuer &&
                subject == Subject
                    ? new TenantMembership(tenantId, issuer, subject, Role.Value)
                    : null;
            return Task.FromResult(membership);
        }

        public Task<bool> IsActiveCloudAccountAsync(
            Guid tenantId,
            Guid cloudAccountId,
            CancellationToken cancellationToken = default)
        {
            CloudAccountTenantId = tenantId;
            CloudAccountId = cloudAccountId;
            return Task.FromResult(
                tenantId == TenantId &&
                activeCloudAccountIds?.Contains(cloudAccountId) == true);
        }
    }
}
