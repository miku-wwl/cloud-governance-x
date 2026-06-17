using FinOps.Application.Cloud.Azure;

namespace FinOps.Api.Endpoints;

internal static class CloudEndpoints
{
    public static IEndpointRouteBuilder MapCloudEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/api/cloud/azure/subscriptions",
            async (
                IAzureSubscriptionReader subscriptionReader,
                CancellationToken cancellationToken) =>
            {
                var subscriptions =
                    await subscriptionReader.GetSubscriptionsAsync(cancellationToken);
                return Results.Ok(subscriptions);
            });

        return endpoints;
    }
}
