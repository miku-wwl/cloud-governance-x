namespace FinOps.Api.Tenancy;

public static class HttpTenantContextApplicationBuilderExtensions
{
    public static IApplicationBuilder UseHttpTenantContext(
        this IApplicationBuilder application) =>
        application.UseMiddleware<HttpTenantContextMiddleware>();
}
