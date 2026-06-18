using System.Text.Json;
using FinOps.Api.Endpoints;
using FinOps.Application.Cloud;
using FinOps.Application.Cloud.Azure;
using FinOps.Application.Etl;
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
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);

        var app = builder.Build();
        app.MapFinOpsEndpoints();
        return app;
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
        string queryString = "")
    {
        var endpoint = ((IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(route => route.RoutePattern.RawText == path);
        var context = new DefaultHttpContext
        {
            RequestServices = app.Services
        };
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(queryString);
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await endpoint.RequestDelegate!(context);
        responseBody.Position = 0;
        using var reader = new StreamReader(responseBody);
        var body = await reader.ReadToEndAsync();
        return (context.Response.StatusCode, body);
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
}
