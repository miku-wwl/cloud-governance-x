using System.Security.Claims;
using System.Text.Json;
using FinOps.Api.Endpoints;
using FinOps.Application.Authorization;
using FinOps.Application.Cloud;
using FinOps.Application.Cloud.Azure;
using FinOps.Application.Etl;
using FinOps.Application.Tenancy;
using FinOps.Domain.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinOps.Tests.Api;

public sealed class EndpointRouteTests
{
    [Fact]
    public void Endpoint_modules_preserve_existing_route_surface()
    {
        var app = BuildRouteOnlyApplication();

        var routes = ((IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => new
            {
                Pattern = endpoint.RoutePattern.RawText,
                Methods = endpoint.Metadata
                    .OfType<HttpMethodMetadata>()
                    .SelectMany(metadata => metadata.HttpMethods)
                    .OrderBy(method => method)
                    .ToArray()
            })
            .OrderBy(route => route.Pattern)
            .ThenBy(route => string.Join(",", route.Methods))
            .ToArray();

        Assert.Collection(
            routes,
            route => AssertRoute(route.Pattern, route.Methods, "GET", "/"),
            route => AssertRoute(route.Pattern, route.Methods, "GET", "/api/admin/etl-runs"),
            route => AssertRoute(route.Pattern, route.Methods, "POST", "/api/admin/sync/azure/costs"),
            route => AssertRoute(route.Pattern, route.Methods, "POST", "/api/admin/sync/azure/resources"),
            route => AssertRoute(route.Pattern, route.Methods, "GET", "/api/cloud/azure/subscriptions"),
            route => AssertRoute(route.Pattern, route.Methods, "GET", "/api/costs/by-resource-group"),
            route => AssertRoute(route.Pattern, route.Methods, "GET", "/api/costs/by-service"),
            route => AssertRoute(route.Pattern, route.Methods, "GET", "/api/costs/daily"),
            route => AssertRoute(route.Pattern, route.Methods, null, "/health"),
            route => AssertRoute(route.Pattern, route.Methods, null, "/health/live"));
    }

    [Fact]
    public void Health_routes_are_explicitly_anonymous()
    {
        var app = BuildRouteOnlyApplication();
        var healthRoutes = ((IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText is
                "/" or "/health" or "/health/live")
            .ToArray();

        Assert.Equal(3, healthRoutes.Length);
        Assert.All(
            healthRoutes,
            endpoint => Assert.NotNull(endpoint.Metadata.GetMetadata<IAllowAnonymous>()));
    }

    [Fact]
    public async Task Cost_sync_endpoint_preserves_default_days_and_response_shape()
    {
        var app = BuildRouteOnlyApplication();
        var response = await InvokeEndpointAsync(
            app,
            "POST",
            "/api/admin/sync/azure/costs");
        var syncService = Assert.IsType<StubCloudCostSyncService>(
            app.Services.GetRequiredService<ICloudCostSyncService>());

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(7, syncService.LastDays);

        using var document = JsonDocument.Parse(response.Body);
        Assert.True(document.RootElement.TryGetProperty("jobRunId", out _));
        Assert.True(document.RootElement.TryGetProperty("retrieved", out _));
        Assert.True(document.RootElement.TryGetProperty("usedSampleData", out _));
    }

    [Fact]
    public async Task Etl_history_endpoint_preserves_default_take()
    {
        var app = BuildRouteOnlyApplication();
        var response = await InvokeEndpointAsync(app, "GET", "/api/admin/etl-runs");
        var repository = Assert.IsType<StubEtlJobRunRepository>(
            app.Services.GetRequiredService<IEtlJobRunRepository>());

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(20, repository.LastTake);
    }

    [Fact]
    public async Task Cost_sync_endpoint_rejects_invalid_days_binding()
    {
        var app = BuildRouteOnlyApplication();
        var response = await InvokeEndpointAsync(
            app,
            "POST",
            "/api/admin/sync/azure/costs",
            "?days=invalid");

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
    }

    private static WebApplication BuildRouteOnlyApplication()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddSingleton<IAzureSubscriptionReader, StubAzureSubscriptionReader>();
        builder.Services.AddSingleton<ICloudResourceSyncService, StubCloudResourceSyncService>();
        builder.Services.AddSingleton<ICloudCostSyncService, StubCloudCostSyncService>();
        builder.Services.AddSingleton<ICloudCostQueryService, StubCloudCostQueryService>();
        builder.Services.AddSingleton<IEtlJobRunRepository, StubEtlJobRunRepository>();
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<TenantContext>();
        builder.Services.AddScoped<ITenantContext>(
            provider => provider.GetRequiredService<TenantContext>());
        builder.Services.AddScoped<ITenantContextInitializer>(
            provider => provider.GetRequiredService<TenantContext>());
        builder.Services.AddScoped<IFinOpsAuthorizationService, FinOpsAuthorizationService>();
        builder.Services.AddScoped<IFinOpsAuthorizationAuditSink, NoOpFinOpsAuthorizationAuditSink>();
        builder.Services.AddScoped<ITenantMembershipResolver, StubTenantMembershipResolver>();
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);

        var app = builder.Build();
        app.MapFinOpsEndpoints();
        return app;
    }

    [Fact]
    public void Business_routes_require_authorization_metadata()
    {
        var app = BuildRouteOnlyApplication();
        var businessRoutes = ((IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith(
                "/api/",
                StringComparison.Ordinal) == true)
            .ToArray();

        Assert.Equal(7, businessRoutes.Length);
        Assert.All(
            businessRoutes,
            endpoint => Assert.NotNull(
                endpoint.Metadata.GetMetadata<IAuthorizeData>()));
    }

    private static void AssertRoute(
        string? actualPattern,
        string[] actualMethods,
        string? expectedMethod,
        string expectedPattern)
    {
        Assert.Equal(expectedPattern, actualPattern);

        if (expectedMethod is null)
        {
            Assert.Empty(actualMethods);
            return;
        }

        Assert.Equal([expectedMethod], actualMethods);
    }

    private static async Task<(int StatusCode, string Body)> InvokeEndpointAsync(
        WebApplication app,
        string method,
        string path,
        string queryString = "",
        bool authenticated = true,
        bool initializeTenant = true,
        MembershipRole role = MembershipRole.Operator)
    {
        var endpoint = ((IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(route => route.RoutePattern.RawText == path);
        await using var scope = app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(queryString);
        if (authenticated)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("iss", StubTenantMembershipResolver.Issuer),
                new Claim("sub", StubTenantMembershipResolver.Subject)
            ],
            authenticationType: "test"));
        }

        var resolver = scope.ServiceProvider
            .GetRequiredService<ITenantMembershipResolver>();
        Assert.IsType<StubTenantMembershipResolver>(resolver).Role = role;

        if (initializeTenant)
        {
            var initializer = scope.ServiceProvider
                .GetRequiredService<ITenantContextInitializer>();
            initializer.Initialize(TrustedTenantContext.ForHttpUser(
                StubTenantMembershipResolver.TenantId,
                StubTenantMembershipResolver.Issuer,
                StubTenantMembershipResolver.Subject,
                role));
        }

        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await endpoint.RequestDelegate!(context);
        responseBody.Position = 0;
        using var reader = new StreamReader(responseBody);
        var body = await reader.ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task Business_endpoint_rejects_anonymous_request()
    {
        var app = BuildRouteOnlyApplication();

        var response = await InvokeEndpointAsync(
            app,
            "GET",
            "/api/costs/daily",
            authenticated: false,
            initializeTenant: false);

        Assert.Equal(StatusCodes.Status401Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Business_endpoint_rejects_authenticated_request_without_tenant_context()
    {
        var app = BuildRouteOnlyApplication();

        var response = await InvokeEndpointAsync(
            app,
            "GET",
            "/api/costs/daily",
            initializeTenant: false);

        Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Business_endpoint_rejects_role_without_permission()
    {
        var app = BuildRouteOnlyApplication();

        var response = await InvokeEndpointAsync(
            app,
            "POST",
            "/api/admin/sync/azure/resources",
            role: MembershipRole.Auditor);

        Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);
    }

    private sealed class StubAzureSubscriptionReader : IAzureSubscriptionReader
    {
        public Task<IReadOnlyList<AzureSubscriptionDto>> GetSubscriptionsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AzureSubscriptionDto>>([]);
    }

    private sealed class StubCloudResourceSyncService : ICloudResourceSyncService
    {
        public Task<CloudResourceSyncResult> SyncAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CloudResourceSyncResult(Guid.Empty, 0, 0, 0));
    }

    private sealed class StubCloudCostSyncService : ICloudCostSyncService
    {
        public int? LastDays { get; private set; }

        public Task<CloudCostSyncResult> SyncRecentAsync(
            int days = 7,
            CancellationToken cancellationToken = default)
        {
            LastDays = days;
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
        public int? LastTake { get; private set; }

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
            CancellationToken cancellationToken = default)
        {
            LastTake = take;
            return Task.FromResult<IReadOnlyList<EtlJobRunDto>>([]);
        }
    }

    private sealed class StubTenantMembershipResolver : ITenantMembershipResolver
    {
        public const string Issuer = "https://issuer.example";
        public const string Subject = "subject-a";
        public static readonly Guid TenantId =
            Guid.Parse("50000000-0000-0000-0000-000000000028");

        public MembershipRole Role { get; set; } = MembershipRole.Operator;

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
                    ? new TenantMembership(tenantId, issuer, subject, Role)
                    : null;
            return Task.FromResult(membership);
        }

        public Task<bool> IsActiveCloudAccountAsync(
            Guid tenantId,
            Guid cloudAccountId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(tenantId == TenantId);
    }
}
