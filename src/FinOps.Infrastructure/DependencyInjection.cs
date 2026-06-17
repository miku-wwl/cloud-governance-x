using FinOps.Infrastructure.Azure;
using FinOps.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
}
