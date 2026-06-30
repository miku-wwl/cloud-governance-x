using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using FinOps.Application.Cloud;
using FinOps.Application.Cloud.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinOps.Infrastructure.Azure;

public static class AzureServiceCollectionExtensions
{
    public static IServiceCollection AddAzureCloudServices(
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
        services.AddHttpClient<ICloudCostProvider, AzureCostProvider>(client =>
        {
            client.BaseAddress = new Uri("https://management.azure.com/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }
}
