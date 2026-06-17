namespace FinOps.Api.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFinOpsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthEndpoints();
        endpoints.MapCloudEndpoints();
        endpoints.MapResourceEndpoints();
        endpoints.MapCostEndpoints();
        endpoints.MapEtlEndpoints();

        return endpoints;
    }
}
