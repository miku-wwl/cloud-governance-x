using System.Security.Claims;
using FinOps.Api.Tenancy;
using FinOps.Application.Tenancy;
using Microsoft.AspNetCore.Http;

namespace FinOps.Tests.Api;

public sealed class HttpTenantContextMiddlewareTests
{
    private static readonly Guid TenantId =
        Guid.Parse("20000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Unauthenticated_client_cannot_create_tenant_authority()
    {
        var context = CreateHttpContext();
        context.Request.Headers[HttpTenantContextMiddleware.TenantSelectionHeader] =
            TenantId.ToString();
        var tenantContext = new TenantContext();
        var middleware = CreateMiddleware(nextCalled: null);

        await middleware.InvokeAsync(
            context,
            new StubMembershipResolver(isMember: true),
            (ITenantContextInitializer)tenantContext);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Null(tenantContext.Current);
    }

    [Fact]
    public async Task Authenticated_subject_cannot_select_unowned_tenant()
    {
        var context = CreateHttpContext(authenticated: true);
        context.Request.Headers[HttpTenantContextMiddleware.TenantSelectionHeader] =
            TenantId.ToString();
        var tenantContext = new TenantContext();
        var middleware = CreateMiddleware(nextCalled: null);

        await middleware.InvokeAsync(
            context,
            new StubMembershipResolver(isMember: false),
            (ITenantContextInitializer)tenantContext);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Null(tenantContext.Current);
    }

    [Fact]
    public async Task Active_membership_establishes_trusted_http_context()
    {
        var nextCalled = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var context = CreateHttpContext(authenticated: true);
        context.Request.Headers[HttpTenantContextMiddleware.TenantSelectionHeader] =
            TenantId.ToString();
        var tenantContext = new TenantContext();
        var resolver = new StubMembershipResolver(isMember: true);
        var middleware = CreateMiddleware(nextCalled);

        await middleware.InvokeAsync(
            context,
            resolver,
            (ITenantContextInitializer)tenantContext);

        Assert.True(nextCalled.Task.IsCompletedSuccessfully);
        var current = tenantContext.RequireCurrent();
        Assert.Equal(TenantId, current.TenantId);
        Assert.Equal(TenantContextSource.HttpUser, current.Source);
        Assert.Equal("https://issuer.example", current.Issuer);
        Assert.Equal("subject-a", current.Subject);
        Assert.Equal(TenantId, resolver.RequestedTenantId);
    }

    [Fact]
    public async Task Query_string_tenant_does_not_create_authority()
    {
        var nextCalled = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var context = CreateHttpContext(authenticated: true);
        context.Request.QueryString = new QueryString($"?tenantId={TenantId}");
        var tenantContext = new TenantContext();
        var resolver = new StubMembershipResolver(isMember: true);
        var middleware = CreateMiddleware(nextCalled);

        await middleware.InvokeAsync(
            context,
            resolver,
            (ITenantContextInitializer)tenantContext);

        Assert.True(nextCalled.Task.IsCompletedSuccessfully);
        Assert.Null(tenantContext.Current);
        Assert.Null(resolver.RequestedTenantId);
    }

    private static HttpTenantContextMiddleware CreateMiddleware(
        TaskCompletionSource? nextCalled) =>
        new(_ =>
        {
            nextCalled?.TrySetResult();
            return Task.CompletedTask;
        });

    private static DefaultHttpContext CreateHttpContext(bool authenticated = false)
    {
        var context = new DefaultHttpContext();
        if (authenticated)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("iss", "https://issuer.example"),
                new Claim("sub", "subject-a")
            ],
            authenticationType: "test"));
        }

        return context;
    }

    private sealed class StubMembershipResolver(bool isMember) :
        ITenantMembershipResolver
    {
        public Guid? RequestedTenantId { get; private set; }

        public Task<bool> IsActiveTenantAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(isMember);

        public Task<bool> HasActiveMembershipAsync(
            Guid tenantId,
            string issuer,
            string subject,
            CancellationToken cancellationToken = default)
        {
            RequestedTenantId = tenantId;
            return Task.FromResult(isMember);
        }
    }
}
