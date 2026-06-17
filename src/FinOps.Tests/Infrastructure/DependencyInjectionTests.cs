using Azure.Core;
using Azure.ResourceManager;
using FinOps.Application.Cloud;
using FinOps.Application.Cloud.Azure;
using FinOps.Application.Etl;
using FinOps.Infrastructure;
using FinOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinOps.Tests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void Application_use_cases_are_registered_once_with_expected_lifetimes()
    {
        var services = new ServiceCollection();

        services.AddApplicationUseCases();

        AssertService<ICloudResourceSyncService, CloudResourceSyncService>(
            services,
            ServiceLifetime.Scoped);
        AssertService<ICloudCostSyncService, CloudCostSyncService>(
            services,
            ServiceLifetime.Scoped);
        AssertService<ICloudCostQueryService, CloudCostQueryService>(
            services,
            ServiceLifetime.Scoped);
        Assert.Single(services, service => service.ServiceType == typeof(TimeProvider));
        Assert.Equal(
            ServiceLifetime.Singleton,
            services.Single(service => service.ServiceType == typeof(TimeProvider)).Lifetime);
    }

    [Fact]
    public void Infrastructure_services_are_registered_with_expected_lifetimes()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddApplicationUseCases();
        services.AddInfrastructure(configuration);

        AssertService<IAzureSubscriptionReader>(services, ServiceLifetime.Singleton);
        AssertService<ICloudResourceInventoryProvider>(services, ServiceLifetime.Scoped);
        AssertService<ICloudCostProvider>(services, ServiceLifetime.Transient);
        AssertService<ICloudResourceRepository>(services, ServiceLifetime.Scoped);
        AssertService<ICloudCostRepository>(services, ServiceLifetime.Scoped);
        AssertService<ICloudCostQueryRepository>(services, ServiceLifetime.Scoped);
        AssertService<IEtlJobRunRepository>(services, ServiceLifetime.Scoped);
        AssertService<TokenCredential>(services, ServiceLifetime.Singleton);
        AssertService<ArmClient>(services, ServiceLifetime.Singleton);
        Assert.Contains(services, service =>
            service.ServiceType == typeof(IDbContextFactory<FinOpsDbContext>));
    }

    [Fact]
    public void Api_and_worker_registration_graph_can_be_validated_without_starting_dependencies()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddLogging();
        services.AddOptions();
        services.AddHttpClient();
        services.AddApplicationUseCases();
        services.AddInfrastructure(configuration);
        services
            .AddHealthChecks()
            .AddPostgreSqlHealthCheck(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ICloudResourceSyncService>();
        scope.ServiceProvider.GetRequiredService<ICloudCostSyncService>();
        scope.ServiceProvider.GetRequiredService<ICloudCostQueryService>();
        scope.ServiceProvider.GetRequiredService<IAzureSubscriptionReader>();
        scope.ServiceProvider.GetRequiredService<IDbContextFactory<FinOpsDbContext>>();
    }

    private static IConfiguration CreateConfiguration()
    {
        Dictionary<string, string?> values = new()
        {
            ["PostgreSql:Host"] = "localhost",
            ["PostgreSql:Port"] = "5432",
            ["PostgreSql:Database"] = "finops_di_test",
            ["PostgreSql:Username"] = "finops",
            ["PostgreSql:Password"] = "finops_dev_password",
            ["PostgreSql:TimeoutSeconds"] = "3",
            ["Azure:TenantId"] = "",
            ["AzureCost:UseSampleDataWhenUnavailable"] = "true",
            ["AzureCost:ForceSampleData"] = "false"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static void AssertService<TService>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(TService));

        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }

    private static void AssertService<TService, TImplementation>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(TService));

        Assert.Equal(expectedLifetime, descriptor.Lifetime);
        Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
    }
}
