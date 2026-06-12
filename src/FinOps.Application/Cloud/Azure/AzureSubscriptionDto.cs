namespace FinOps.Application.Cloud.Azure;

public sealed record AzureSubscriptionDto(
    string SubscriptionId,
    string DisplayName,
    string TenantId,
    string State);
