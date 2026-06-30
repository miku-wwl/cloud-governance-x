using FinOps.Application.Authorization;
using FinOps.Application.Cloud;
using FinOps.Application.Tenancy;
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
        services.TryAddScoped<IFinOpsAuthorizationService, FinOpsAuthorizationService>();
        services.TryAddScoped<IFinOpsAuthorizationAuditSink, NoOpFinOpsAuthorizationAuditSink>();
        services.TryAddScoped<TenantContext>();
        services.TryAddScoped<ITenantContext>(serviceProvider =>
            serviceProvider.GetRequiredService<TenantContext>());
        services.TryAddScoped<ITenantContextInitializer>(serviceProvider =>
            serviceProvider.GetRequiredService<TenantContext>());

        return services;
    }
}
