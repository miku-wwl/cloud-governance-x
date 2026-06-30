using FinOps.Application.Cloud;
using Microsoft.Extensions.Options;

namespace FinOps.Worker.Jobs;

internal sealed class CostSyncJobHandler(
    ICloudCostSyncService costSyncService,
    IOptions<EtlWorkerOptions> options,
    ILogger<Worker> logger) : IWorkerJobHandler
{
    public string Name => "Costs";

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var result = await costSyncService.SyncRecentAsync(
            options.Value.CostDays,
            cancellationToken);

        logger.LogInformation(
            "Azure cost sync completed. Job run: {JobRunId}, retrieved: {Retrieved}, inserted: {Inserted}, updated: {Updated}.",
            result.JobRunId,
            result.Retrieved,
            result.Inserted,
            result.Updated);
    }
}
