using FinOps.Application.Cloud.Azure;

namespace FinOps.Infrastructure.Azure;

internal static class AzureSubscriptionMapper
{
    public static AzureSubscriptionDto Map(
        string? subscriptionId,
        string? displayName,
        string? tenantId,
        string? state)
    {
        return new AzureSubscriptionDto(
            subscriptionId ?? string.Empty,
            displayName ?? string.Empty,
            tenantId ?? string.Empty,
            state ?? "Unknown");
    }
}
