using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace FinOps.Api.Endpoints;

internal static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () => Results.Ok(new
        {
            service = "FinOps.Api",
            status = "running"
        })).AllowAnonymous();

        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        }).AllowAnonymous();

        return endpoints;
    }
}
