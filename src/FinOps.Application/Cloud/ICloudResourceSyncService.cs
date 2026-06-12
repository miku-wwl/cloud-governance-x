namespace FinOps.Application.Cloud;

public interface ICloudResourceSyncService
{
    Task<CloudResourceSyncResult> SyncAsync(
        CancellationToken cancellationToken = default);
}
