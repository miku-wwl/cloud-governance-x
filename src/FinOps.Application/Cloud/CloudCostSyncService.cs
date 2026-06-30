using FinOps.Application.Etl;

namespace FinOps.Application.Cloud;

public sealed class CloudCostSyncService(
    ICloudCostProvider costProvider,
    ICloudCostRepository repository,
    IEtlJobRunRepository jobRunRepository,
    TimeProvider timeProvider) : ICloudCostSyncService
{
    public const string JobName = "azure-cost-sync";
    public const string Provider = "Azure";

    public async Task<CloudCostSyncResult> SyncRecentAsync(
        int days = 7,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(days, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(days, 31);

        var to = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var from = to.AddDays(-(days - 1));
        var jobRunId = await jobRunRepository.StartAsync(
            JobName,
            Provider,
            timeProvider.GetUtcNow(),
            cancellationToken);
        var recordsProcessed = 0;

        try
        {
            var costs = await costProvider.GetDailyCostsAsync(from, to, cancellationToken);
            recordsProcessed = costs.Count;
            var result = await repository.UpsertAsync(costs, cancellationToken);

            await jobRunRepository.CompleteAsync(
                jobRunId,
                timeProvider.GetUtcNow(),
                recordsProcessed,
                cancellationToken);

            return new CloudCostSyncResult(
                jobRunId,
                from,
                to,
                costs.Count,
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
