using System.Security.Claims;

namespace FinOps.Api.Tenancy;

public sealed class E2eTestIdentityMiddleware(
    RequestDelegate next,
    IConfiguration configuration)
{
    public const string EnabledKey = "E2EIdentity:Enabled";
    public const string IssuerKey = "E2EIdentity:Issuer";
    public const string SubjectKey = "E2EIdentity:Subject";

    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (!configuration.GetValue<bool>(EnabledKey))
        {
            await next(httpContext);
            return;
        }

        var issuer = configuration[IssuerKey];
        var subject = configuration[SubjectKey];
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException(
                "E2E identity requires explicit issuer and subject configuration.");
        }

        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("iss", issuer),
            new Claim("sub", subject)
        ],
        authenticationType: "E2E"));

        await next(httpContext);
    }
}
