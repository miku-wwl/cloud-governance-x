using FinOps.Application.Authorization;
using FinOps.Application.Tenancy;

namespace FinOps.Api.Authorization;

internal static class FinOpsEndpointAuthorizationExtensions
{
    public static RouteHandlerBuilder RequireFinOpsPermission(
        this RouteHandlerBuilder builder,
        FinOpsPermission permission)
    {
        return builder
            .RequireAuthorization()
            .AddEndpointFilter(async (context, next) =>
            {
                var httpContext = context.HttpContext;
                if (httpContext.User.Identity?.IsAuthenticated != true)
                {
                    return Results.Unauthorized();
                }

                var tenantContext = httpContext.RequestServices
                    .GetRequiredService<ITenantContext>();
                var trustedContext = tenantContext.Current;
                if (trustedContext is null)
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                var authorizationService = httpContext.RequestServices
                    .GetRequiredService<IFinOpsAuthorizationService>();
                var decision = await authorizationService.AuthorizeAsync(
                    permission,
                    FinOpsAuthorizationScope.Tenant(trustedContext.TenantId),
                    httpContext.RequestAborted);

                return decision.IsAllowed
                    ? await next(context)
                    : Results.StatusCode(StatusCodes.Status403Forbidden);
            });
    }
}
