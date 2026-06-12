namespace FinOps.Application.Cloud;

public sealed class CloudResourceSyncService(
    ICloudResourceInventoryProvider inventoryProvider,
    ICloudResourceRepository repository) : ICloudResourceSyncService
{
    public async Task<CloudResourceSyncResult> SyncAsync(
        CancellationToken cancellationToken = default)
    {
        var resources = await inventoryProvider.GetResourcesAsync(cancellationToken);
        var result = await repository.UpsertAsync(
            resources,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return new CloudResourceSyncResult(
            resources.Count,
            result.Inserted,
            result.Updated);
    }
}
