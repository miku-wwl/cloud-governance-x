namespace FinOps.Application.Cloud;

public interface ICloudCostProvider
{
    Task<IReadOnlyList<CloudCostDailyDto>> GetDailyCostsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}
