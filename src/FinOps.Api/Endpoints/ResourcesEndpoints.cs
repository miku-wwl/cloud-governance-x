using FinOps.Application.Cloud;

namespace FinOps.Api.Endpoints;

internal static class ResourcesEndpoints
{
    public static IEndpointRouteBuilder MapResourceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/admin/sync/azure/resources",
            async (
                ICloudResourceSyncService syncService,
                CancellationToken cancellationToken) =>
            {
                var result = await syncService.SyncAsync(cancellationToken);
                return Results.Ok(result);
            });

        return endpoints;
    }
}
