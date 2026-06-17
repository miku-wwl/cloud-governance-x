using FinOps.Application.Cloud;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinOps.Infrastructure;

public static class ApplicationUseCaseServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationUseCases(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICloudResourceSyncService, CloudResourceSyncService>();
        services.AddScoped<ICloudCostSyncService, CloudCostSyncService>();
        services.AddScoped<ICloudCostQueryService, CloudCostQueryService>();

        return services;
    }
}
