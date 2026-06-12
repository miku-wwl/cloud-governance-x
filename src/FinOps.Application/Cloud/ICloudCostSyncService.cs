namespace FinOps.Application.Cloud;

public interface ICloudCostSyncService
{
    Task<CloudCostSyncResult> SyncRecentAsync(
        int days = 7,
        CancellationToken cancellationToken = default);
}
