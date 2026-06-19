using FinOps.Application.Tenancy;

namespace FinOps.Api.Tenancy;

public sealed class HttpTenantContextMiddleware(RequestDelegate next)
{
    public const string TenantSelectionHeader = "X-FinOps-Tenant-Id";

    public async Task InvokeAsync(
        HttpContext httpContext,
        ITenantMembershipResolver membershipResolver,
        ITenantContextInitializer tenantContextInitializer)
    {
        if (!httpContext.Request.Headers.TryGetValue(
            TenantSelectionHeader,
            out var tenantValues))
        {
            await next(httpContext);
            return;
        }

        if (tenantValues.Count != 1 || !Guid.TryParse(tenantValues[0], out var tenantId))
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (tenantId == Guid.Empty || httpContext.User.Identity?.IsAuthenticated != true)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var issuer = httpContext.User.FindFirst("iss")?.Value;
        var subject = httpContext.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject))
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var isMember = await membershipResolver.HasActiveMembershipAsync(
            tenantId,
            issuer,
            subject,
            httpContext.RequestAborted);
        if (!isMember)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        tenantContextInitializer.Initialize(TrustedTenantContext.ForHttpUser(
            tenantId,
            issuer,
            subject));

        await next(httpContext);
    }
}
