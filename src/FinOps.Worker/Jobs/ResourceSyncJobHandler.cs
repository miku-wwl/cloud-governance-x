using FinOps.Application.Cloud;

namespace FinOps.Worker.Jobs;

internal sealed class ResourceSyncJobHandler(
    ICloudResourceSyncService resourceSyncService,
    ILogger<Worker> logger) : IWorkerJobHandler
{
    public string Name => "Resources";

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var result = await resourceSyncService.SyncAsync(cancellationToken);

        logger.LogInformation(
            "Azure resource sync completed. Job run: {JobRunId}, retrieved: {Retrieved}, inserted: {Inserted}, updated: {Updated}.",
            result.JobRunId,
            result.Retrieved,
            result.Inserted,
            result.Updated);
    }
}
