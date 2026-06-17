using FinOps.Application.Cloud;

namespace FinOps.Api.Endpoints;

internal static class CostsEndpoints
{
    public static IEndpointRouteBuilder MapCostEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/api/costs/daily",
            (
                ICloudCostQueryService queryService,
                string? provider,
                DateOnly? from,
                DateOnly? to,
                CancellationToken cancellationToken) =>
                queryService.GetDailyAsync(provider, from, to, cancellationToken));

        endpoints.MapGet(
            "/api/costs/by-service",
            (
                ICloudCostQueryService queryService,
                string? provider,
                DateOnly? from,
                DateOnly? to,
                CancellationToken cancellationToken) =>
                queryService.GetByServiceAsync(provider, from, to, cancellationToken));

        endpoints.MapGet(
            "/api/costs/by-resource-group",
            (
                ICloudCostQueryService queryService,
                string? provider,
                DateOnly? from,
                DateOnly? to,
                CancellationToken cancellationToken) =>
                queryService.GetByResourceGroupAsync(provider, from, to, cancellationToken));

        endpoints.MapPost(
            "/api/admin/sync/azure/costs",
            async (
                ICloudCostSyncService syncService,
                int? days,
                CancellationToken cancellationToken) =>
            {
                var result = await syncService.SyncRecentAsync(days ?? 7, cancellationToken);
                return Results.Ok(result);
            });

        return endpoints;
    }
}
