namespace FinOps.Application.Cloud;

public interface ICloudCostRepository
{
    Task<CloudCostUpsertResult> UpsertAsync(
        IReadOnlyCollection<CloudCostDailyDto> costs,
        CancellationToken cancellationToken = default);
}
