using FinOps.Api.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FinOps.Tests.Api;

public sealed class E2eTestIdentityMiddlewareTests
{
    [Fact]
    public async Task Disabled_identity_does_not_authenticate_request()
    {
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(new Dictionary<string, string?>());

        await middleware.InvokeAsync(context);

        Assert.NotEqual(true, context.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task Enabled_identity_uses_explicit_issuer_and_subject()
    {
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(new Dictionary<string, string?>
        {
            [E2eTestIdentityMiddleware.EnabledKey] = "true",
            [E2eTestIdentityMiddleware.IssuerKey] = "https://e2e.finops.local",
            [E2eTestIdentityMiddleware.SubjectKey] = "e2e-operator"
        });

        await middleware.InvokeAsync(context);

        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.Equal(
            "https://e2e.finops.local",
            context.User.FindFirst("iss")?.Value);
        Assert.Equal("e2e-operator", context.User.FindFirst("sub")?.Value);
    }

    private static E2eTestIdentityMiddleware CreateMiddleware(
        IReadOnlyDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        return new E2eTestIdentityMiddleware(
            _ => Task.CompletedTask,
            configuration);
    }
}
