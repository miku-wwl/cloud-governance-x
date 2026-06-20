namespace FinOps.Api.Tenancy;

public static class E2eTestIdentityApplicationBuilderExtensions
{
    public static IApplicationBuilder UseE2eTestIdentity(
        this IApplicationBuilder application,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>(E2eTestIdentityMiddleware.EnabledKey))
        {
            return application;
        }

        if (!environment.IsEnvironment("E2E"))
        {
            throw new InvalidOperationException(
                "The synthetic E2E identity can only be enabled in the E2E environment.");
        }

        return application.UseMiddleware<E2eTestIdentityMiddleware>();
    }
}
