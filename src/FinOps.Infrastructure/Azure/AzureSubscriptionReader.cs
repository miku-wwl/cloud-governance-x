using Azure.ResourceManager;
using FinOps.Application.Cloud.Azure;

namespace FinOps.Infrastructure.Azure;

internal sealed class AzureSubscriptionReader(ArmClient armClient) : IAzureSubscriptionReader
{
    public async Task<IReadOnlyList<AzureSubscriptionDto>> GetSubscriptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var subscriptions = new List<AzureSubscriptionDto>();

        await foreach (var subscription in armClient
            .GetSubscriptions()
            .GetAllAsync(cancellationToken))
        {
            subscriptions.Add(AzureSubscriptionMapper.Map(
                subscription.Data.SubscriptionId,
                subscription.Data.DisplayName,
                subscription.Data.TenantId?.ToString(),
                subscription.Data.State?.ToString()));
        }

        return subscriptions;
    }
}
