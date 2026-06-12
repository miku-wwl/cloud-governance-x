using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using FinOps.Application.Cloud.Azure;
using FinOps.Application.Cloud;
using FinOps.Application.Etl;
using FinOps.Infrastructure.Azure;
using FinOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
        services.AddPostgreSql(configuration);
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
        services.AddScoped<ICloudResourceInventoryProvider, AzureResourceInventoryProvider>();
        services.Configure<AzureCostOptions>(
            configuration.GetSection(AzureCostOptions.SectionName));
        services.AddHttpClient<ICloudCostProvider, AzureCostProvider>(client =>
        {
            client.BaseAddress = new Uri("https://management.azure.com/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
    }

    private static void AddPostgreSql(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(PostgreSqlHealthCheckOptions.SectionName)
            .Get<PostgreSqlHealthCheckOptions>()
            ?? new PostgreSqlHealthCheckOptions();

        services.AddDbContextFactory<FinOpsDbContext>(dbOptions =>
            dbOptions.UseNpgsql(options.GetConnectionString()));
        services.AddScoped<ICloudResourceRepository, CloudResourceRepository>();
        services.AddScoped<ICloudCostRepository, CloudCostRepository>();
        services.AddScoped<IEtlJobRunRepository, EtlJobRunRepository>();
    }
}
