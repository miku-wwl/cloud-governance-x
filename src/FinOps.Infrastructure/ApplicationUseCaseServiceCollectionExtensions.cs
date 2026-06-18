using FinOps.Application.Cloud;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinOps.Infrastructure;

public static class ApplicationUseCaseServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationUseCases(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ICloudResourceSyncService, CloudResourceSyncService>();
        services.TryAddScoped<ICloudCostSyncService, CloudCostSyncService>();
        services.TryAddScoped<ICloudCostQueryService, CloudCostQueryService>();

        return services;
    }
}
