using FinOps.Application.Tenancy;

namespace FinOps.Tests.Application;

public sealed class TenantContextTests
{
    [Fact]
    public void Missing_context_fails_closed()
    {
        ITenantContext context = new TenantContext();

        var exception = Assert.Throws<InvalidOperationException>(
            context.RequireCurrent);

        Assert.Contains("trusted tenant context", exception.Message);
    }

    [Fact]
    public void Context_can_only_be_initialized_once_per_scope()
    {
        var context = new TenantContext();
        var initializer = (ITenantContextInitializer)context;
        initializer.Initialize(
            TrustedTenantContext.ForBackgroundJob(Guid.NewGuid()));

        Assert.Throws<InvalidOperationException>(() =>
            initializer.Initialize(
                TrustedTenantContext.ForBackgroundJob(Guid.NewGuid())));
    }
}
