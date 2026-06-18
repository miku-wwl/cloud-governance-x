using FinOps.Infrastructure.Azure;
using FinOps.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services.Any(service =>
            service.ServiceType == typeof(InfrastructureRegistrationMarker)))
        {
            return services;
        }

        services.TryAddSingleton(new InfrastructureRegistrationMarker());
        services.AddPostgreSql(configuration);
        services.AddAzureCloudServices(configuration);
        return services;
    }

    private sealed class InfrastructureRegistrationMarker;
}
