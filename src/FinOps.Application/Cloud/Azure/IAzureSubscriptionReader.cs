namespace FinOps.Application.Cloud.Azure;

public interface IAzureSubscriptionReader
{
    Task<IReadOnlyList<AzureSubscriptionDto>> GetSubscriptionsAsync(
        CancellationToken cancellationToken = default);
}
