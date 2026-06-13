using FinOps.Application.Cloud;
using FinOps.Application.Cloud.Azure;
using FinOps.Application.Etl;
using FinOps.Infrastructure;
using FinOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ICloudResourceSyncService, CloudResourceSyncService>();
builder.Services.AddScoped<ICloudCostSyncService, CloudCostSyncService>();
builder.Services.AddScoped<ICloudCostQueryService, CloudCostQueryService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddPostgreSqlHealthCheck(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FinOpsDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "FinOps.Api",
    status = "running"
}));

app.MapGet(
    "/api/cloud/azure/subscriptions",
    async (IAzureSubscriptionReader subscriptionReader, CancellationToken cancellationToken) =>
    {
        var subscriptions = await subscriptionReader.GetSubscriptionsAsync(cancellationToken);
        return Results.Ok(subscriptions);
    });

app.MapPost(
    "/api/admin/sync/azure/resources",
    async (ICloudResourceSyncService syncService, CancellationToken cancellationToken) =>
    {
        var result = await syncService.SyncAsync(cancellationToken);
        return Results.Ok(result);
    });

app.MapGet(
    "/api/costs/daily",
    (
        ICloudCostQueryService queryService,
        string? provider,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken) =>
        queryService.GetDailyAsync(provider, from, to, cancellationToken));

app.MapGet(
    "/api/costs/by-service",
    (
        ICloudCostQueryService queryService,
        string? provider,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken) =>
        queryService.GetByServiceAsync(provider, from, to, cancellationToken));

app.MapGet(
    "/api/costs/by-resource-group",
    (
        ICloudCostQueryService queryService,
        string? provider,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken) =>
        queryService.GetByResourceGroupAsync(provider, from, to, cancellationToken));

app.MapPost(
    "/api/admin/sync/azure/costs",
    async (
        ICloudCostSyncService syncService,
        int? days,
        CancellationToken cancellationToken) =>
    {
        var result = await syncService.SyncRecentAsync(days ?? 7, cancellationToken);
        return Results.Ok(result);
    });

app.MapGet(
    "/api/admin/etl-runs",
    async (
        IEtlJobRunRepository jobRunRepository,
        string? jobName,
        int? take,
        CancellationToken cancellationToken) =>
    {
        var runs = await jobRunRepository.GetRecentAsync(
            jobName,
            take ?? 20,
            cancellationToken);
        return Results.Ok(runs);
    });

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.Run();

public partial class Program;
