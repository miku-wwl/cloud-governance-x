using FinOps.Application.Etl;

namespace FinOps.Application.Cloud;

public sealed class CloudResourceSyncService(
    ICloudResourceInventoryProvider inventoryProvider,
    ICloudResourceRepository repository,
    IEtlJobRunRepository jobRunRepository,
    TimeProvider timeProvider) : ICloudResourceSyncService
{
    public const string JobName = "azure-resource-sync";
    public const string Provider = "Azure";

    public async Task<CloudResourceSyncResult> SyncAsync(
        CancellationToken cancellationToken = default)
    {
        var startedAt = timeProvider.GetUtcNow();
        var jobRunId = await jobRunRepository.StartAsync(
            JobName,
            Provider,
            startedAt,
            cancellationToken);
        var recordsProcessed = 0;

        try
        {
            var resources = await inventoryProvider.GetResourcesAsync(cancellationToken);
            recordsProcessed = resources.Count;
            var result = await repository.UpsertAsync(
                resources,
                timeProvider.GetUtcNow(),
                cancellationToken);

            await jobRunRepository.CompleteAsync(
                jobRunId,
                timeProvider.GetUtcNow(),
                recordsProcessed,
                cancellationToken);

            return new CloudResourceSyncResult(
                jobRunId,
                resources.Count,
                result.Inserted,
                result.Updated);
        }
        catch (Exception exception)
        {
            await jobRunRepository.FailAsync(
                jobRunId,
                timeProvider.GetUtcNow(),
                recordsProcessed,
                GetErrorSummary(exception),
                CancellationToken.None);
            throw;
        }
    }

    private static string GetErrorSummary(Exception exception)
    {
        return exception.Message
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?? exception.GetType().Name;
    }
}
