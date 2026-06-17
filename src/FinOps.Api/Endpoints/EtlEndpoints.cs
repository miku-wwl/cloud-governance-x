using FinOps.Application.Etl;

namespace FinOps.Api.Endpoints;

internal static class EtlEndpoints
{
    public static IEndpointRouteBuilder MapEtlEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
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

        return endpoints;
    }
}
