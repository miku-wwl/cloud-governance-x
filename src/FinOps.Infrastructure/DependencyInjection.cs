using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using FinOps.Application.Cloud.Azure;
using FinOps.Infrastructure.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAzureCloudServices(configuration);
        return services;
    }

    public static IHealthChecksBuilder AddPostgreSqlHealthCheck(
        this IHealthChecksBuilder healthChecks,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(PostgreSqlHealthCheckOptions.SectionName)
            .Get<PostgreSqlHealthCheckOptions>()
            ?? new PostgreSqlHealthCheckOptions();

        return healthChecks.AddCheck(
            "postgresql",
            new PostgreSqlHealthCheck(options),
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"]);
    }

    private static void AddAzureCloudServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var tenantId = configuration["Azure:TenantId"];
        var credentialOptions = new DefaultAzureCredentialOptions();

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            credentialOptions.TenantId = tenantId;
        }

        services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential(credentialOptions));
        services.AddSingleton(serviceProvider =>
            new ArmClient(serviceProvider.GetRequiredService<TokenCredential>()));
        services.AddSingleton<IAzureSubscriptionReader, AzureSubscriptionReader>();
    }
}
