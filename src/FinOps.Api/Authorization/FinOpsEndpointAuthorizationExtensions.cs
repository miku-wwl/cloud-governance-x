using System.Security.Claims;
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
                var auditSink = httpContext.RequestServices
                    .GetRequiredService<IFinOpsAuthorizationAuditSink>();
                var scope = FinOpsAuthorizationScope.TenantFromNullable(
                    tenantId: null);
                if (httpContext.User.Identity?.IsAuthenticated != true)
                {
                    await AppendAuditAsync(
                        auditSink,
                        httpContext,
                        permission,
                        scope,
                        isAllowed: false,
                        reason: "Authenticated user is required.",
                        statusCode: StatusCodes.Status401Unauthorized);
                    return Results.Unauthorized();
                }

                var tenantContext = httpContext.RequestServices
                    .GetRequiredService<ITenantContext>();
                var trustedContext = tenantContext.Current;
                if (trustedContext is null)
                {
                    await AppendAuditAsync(
                        auditSink,
                        httpContext,
                        permission,
                        scope,
                        isAllowed: false,
                        reason: "A trusted tenant context is required.",
                        statusCode: StatusCodes.Status403Forbidden);
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                scope = FinOpsAuthorizationScope.Tenant(trustedContext.TenantId);
                var authorizationService = httpContext.RequestServices
                    .GetRequiredService<IFinOpsAuthorizationService>();
                var decision = await authorizationService.AuthorizeAsync(
                    permission,
                    scope,
                    httpContext.RequestAborted);

                if (!decision.IsAllowed)
                {
                    await AppendAuditAsync(
                        auditSink,
                        httpContext,
                        permission,
                        scope,
                        isAllowed: false,
                        decision.Reason,
                        StatusCodes.Status403Forbidden);
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                await AppendAuditAsync(
                    auditSink,
                    httpContext,
                    permission,
                    scope,
                    isAllowed: true,
                    decision.Reason,
                    StatusCodes.Status200OK);
                return await next(context);
            });
    }

    private static async Task AppendAuditAsync(
        IFinOpsAuthorizationAuditSink auditSink,
        HttpContext httpContext,
        FinOpsPermission permission,
        FinOpsAuthorizationScope scope,
        bool isAllowed,
        string reason,
        int statusCode)
    {
        await auditSink.AppendAsync(
            new FinOpsAuthorizationAuditEntry(
                permission,
                scope,
                isAllowed,
                reason,
                httpContext.User.FindFirstValue("iss"),
                httpContext.User.FindFirstValue("sub") ??
                    httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                httpContext.Request.Method,
                httpContext.Request.Path.Value ?? "/",
                statusCode,
                httpContext.TraceIdentifier),
            httpContext.RequestAborted);
    }
}
