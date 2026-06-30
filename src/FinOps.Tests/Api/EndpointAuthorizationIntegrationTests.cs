using System.Net;
using FinOps.Api.Authorization;
using FinOps.Api.Endpoints;
using FinOps.Api.Tenancy;
using FinOps.Application.Authorization;
using FinOps.Application.Cloud;
using FinOps.Application.Cloud.Azure;
using FinOps.Application.Etl;
using FinOps.Application.Tenancy;
using FinOps.Domain.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinOps.Tests.Api;

public sealed class EndpointAuthorizationIntegrationTests
{
    private static readonly Guid TenantId =
        Guid.Parse("50000000-0000-0000-0000-000000000128");
    private static readonly Guid OtherTenantId =
        Guid.Parse("50000000-0000-0000-0000-000000000129");

    [Fact]
    public async Task E2e_identity_and_active_role_can_call_authorized_endpoint()
    {
        await using var app = await CreateApplicationAsync(MembershipRole.Operator);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/admin/sync/azure/costs");
        request.Headers.Add(
            HttpTenantContextMiddleware.TenantSelectionHeader,
            TenantId.ToString());

        using var response = await app.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var audit = app.Services.GetRequiredService<CapturingAuthorizationAuditSink>();
        var entry = Assert.Single(audit.Entries);
        Assert.True(entry.IsAllowed);
        Assert.Equal(FinOpsPermission.CostSync, entry.Permission);
        Assert.Equal(TenantId, entry.Scope.TenantId);
        Assert.Equal("/api/admin/sync/azure/costs", entry.Path);
        Assert.Equal(200, entry.StatusCode);
    }

    [Fact]
    public async Task Authenticated_request_without_tenant_context_is_forbidden()
    {
        await using var app = await CreateApplicationAsync(MembershipRole.Operator);

        using var response = await app.GetTestClient()
            .GetAsync("/api/costs/daily");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var audit = app.Services.GetRequiredService<CapturingAuthorizationAuditSink>();
        var entry = Assert.Single(audit.Entries);
        Assert.False(entry.IsAllowed);
        Assert.Equal(FinOpsPermission.CostRead, entry.Permission);
        Assert.Null(entry.Scope.TenantId);
        Assert.Equal(403, entry.StatusCode);
    }

    [Fact]
    public async Task Tenant_escape_header_for_unowned_tenant_is_forbidden_before_endpoint()
    {
        await using var app = await CreateApplicationAsync(MembershipRole.Operator);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/admin/sync/azure/costs");
        request.Headers.Add(
            HttpTenantContextMiddleware.TenantSelectionHeader,
            OtherTenantId.ToString());

        using var response = await app.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var audit = app.Services.GetRequiredService<CapturingAuthorizationAuditSink>();
        Assert.Empty(audit.Entries);
        var syncService = app.Services.GetRequiredService<StubCloudCostSyncService>();
        Assert.Equal(0, syncService.CallCount);
    }

    [Fact]
    public async Task Query_string_tenant_does_not_create_authority_for_business_endpoint()
    {
        await using var app = await CreateApplicationAsync(MembershipRole.Operator);

        using var response = await app.GetTestClient()
            .GetAsync($"/api/costs/daily?tenantId={TenantId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var audit = app.Services.GetRequiredService<CapturingAuthorizationAuditSink>();
        var entry = Assert.Single(audit.Entries);
        Assert.False(entry.IsAllowed);
        Assert.Equal(FinOpsPermission.CostRead, entry.Permission);
        Assert.Null(entry.Scope.TenantId);
    }

    [Fact]
    public async Task Authenticated_role_without_permission_is_forbidden()
    {
        await using var app = await CreateApplicationAsync(MembershipRole.Auditor);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/admin/sync/azure/resources");
        request.Headers.Add(
            HttpTenantContextMiddleware.TenantSelectionHeader,
            TenantId.ToString());

        using var response = await app.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var audit = app.Services.GetRequiredService<CapturingAuthorizationAuditSink>();
        var entry = Assert.Single(audit.Entries);
        Assert.False(entry.IsAllowed);
        Assert.Equal(FinOpsPermission.ResourceSync, entry.Permission);
        Assert.Equal(TenantId, entry.Scope.TenantId);
        Assert.Equal(403, entry.StatusCode);
        var syncService = app.Services.GetRequiredService<StubCloudResourceSyncService>();
        Assert.Equal(0, syncService.CallCount);
    }

    private static async Task<WebApplication> CreateApplicationAsync(
        MembershipRole role)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "E2E"
        });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [E2eTestIdentityMiddleware.EnabledKey] = "true",
            [E2eTestIdentityMiddleware.IssuerKey] = StubTenantMembershipResolver.Issuer,
            [E2eTestIdentityMiddleware.SubjectKey] = StubTenantMembershipResolver.Subject
        });

        builder.Services.AddAuthorization();
        builder.Services.AddScoped<TenantContext>();
        builder.Services.AddScoped<ITenantContext>(
            provider => provider.GetRequiredService<TenantContext>());
        builder.Services.AddScoped<ITenantContextInitializer>(
            provider => provider.GetRequiredService<TenantContext>());
        builder.Services.AddScoped<IFinOpsAuthorizationService, FinOpsAuthorizationService>();
        builder.Services.AddSingleton<CapturingAuthorizationAuditSink>();
        builder.Services.AddSingleton<IFinOpsAuthorizationAuditSink>(
            provider => provider.GetRequiredService<CapturingAuthorizationAuditSink>());
        builder.Services.AddScoped<ITenantMembershipResolver>(_ =>
            new StubTenantMembershipResolver(role));
        builder.Services.AddSingleton<IAzureSubscriptionReader, StubAzureSubscriptionReader>();
        builder.Services.AddSingleton<StubCloudResourceSyncService>();
        builder.Services.AddSingleton<ICloudResourceSyncService>(
            provider => provider.GetRequiredService<StubCloudResourceSyncService>());
        builder.Services.AddSingleton<StubCloudCostSyncService>();
        builder.Services.AddSingleton<ICloudCostSyncService>(
            provider => provider.GetRequiredService<StubCloudCostSyncService>());
        builder.Services.AddSingleton<ICloudCostQueryService, StubCloudCostQueryService>();
        builder.Services.AddSingleton<IEtlJobRunRepository, StubEtlJobRunRepository>();
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);

        var app = builder.Build();
        app.UseE2eTestIdentity(app.Environment, builder.Configuration);
        app.UseHttpTenantContext();
        app.UseAuthorization();
        app.MapFinOpsEndpoints();
        await app.StartAsync();
        return app;
    }

    private sealed class CapturingAuthorizationAuditSink : IFinOpsAuthorizationAuditSink
    {
        private readonly List<FinOpsAuthorizationAuditEntry> entries = [];

        public IReadOnlyList<FinOpsAuthorizationAuditEntry> Entries => entries;

        public Task AppendAsync(
            FinOpsAuthorizationAuditEntry entry,
            CancellationToken cancellationToken = default)
        {
            entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    private sealed class StubTenantMembershipResolver(MembershipRole role) :
        ITenantMembershipResolver
    {
        public const string Issuer = "https://issuer.example";
        public const string Subject = "subject-a";

        public Task<bool> IsActiveTenantAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(tenantId == TenantId);

        public Task<bool> HasActiveMembershipAsync(
            Guid tenantId,
            string issuer,
            string subject,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                tenantId == TenantId &&
                issuer == Issuer &&
                subject == Subject);

        public Task<TenantMembership?> ResolveActiveMembershipAsync(
            Guid tenantId,
            string issuer,
            string subject,
            CancellationToken cancellationToken = default)
        {
            TenantMembership? membership =
                tenantId == TenantId &&
                issuer == Issuer &&
                subject == Subject
                    ? new TenantMembership(tenantId, issuer, subject, role)
                    : null;
            return Task.FromResult(membership);
        }

        public Task<bool> IsActiveCloudAccountAsync(
            Guid tenantId,
            Guid cloudAccountId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(tenantId == TenantId);
    }

    private sealed class StubAzureSubscriptionReader : IAzureSubscriptionReader
    {
        public Task<IReadOnlyList<AzureSubscriptionDto>> GetSubscriptionsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AzureSubscriptionDto>>([]);
    }

    private sealed class StubCloudResourceSyncService : ICloudResourceSyncService
    {
        public int CallCount { get; private set; }

        public Task<CloudResourceSyncResult> SyncAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new CloudResourceSyncResult(Guid.Empty, 0, 0, 0));
        }
    }

    private sealed class StubCloudCostSyncService : ICloudCostSyncService
    {
        public int CallCount { get; private set; }

        public Task<CloudCostSyncResult> SyncRecentAsync(
            int days = 7,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new CloudCostSyncResult(
                Guid.Empty,
                DateOnly.MinValue,
                DateOnly.MinValue,
                0,
                0,
                0,
                UsedSampleData: false));
        }
    }

    private sealed class StubCloudCostQueryService : ICloudCostQueryService
    {
        public Task<IReadOnlyList<CloudCostDailyPointDto>> GetDailyAsync(
            string? provider,
            DateOnly? from,
            DateOnly? to,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CloudCostDailyPointDto>>([]);

        public Task<IReadOnlyList<CloudCostBreakdownDto>> GetByServiceAsync(
            string? provider,
            DateOnly? from,
            DateOnly? to,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CloudCostBreakdownDto>>([]);

        public Task<IReadOnlyList<CloudCostBreakdownDto>> GetByResourceGroupAsync(
            string? provider,
            DateOnly? from,
            DateOnly? to,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CloudCostBreakdownDto>>([]);
    }

    private sealed class StubEtlJobRunRepository : IEtlJobRunRepository
    {
        public Task<Guid> StartAsync(
            string jobName,
            string provider,
            DateTimeOffset startedAt,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Guid.Empty);

        public Task CompleteAsync(
            Guid id,
            DateTimeOffset finishedAt,
            int recordsProcessed,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task FailAsync(
            Guid id,
            DateTimeOffset finishedAt,
            int recordsProcessed,
            string errorMessage,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<EtlJobRunDto>> GetRecentAsync(
            string? jobName,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EtlJobRunDto>>([]);
    }
}
