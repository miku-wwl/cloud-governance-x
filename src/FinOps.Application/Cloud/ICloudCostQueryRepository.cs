namespace FinOps.Application.Cloud;

public interface ICloudCostQueryRepository
{
    Task<IReadOnlyList<CloudCostDailyPointDto>> GetDailyAsync(
        string provider,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudCostAggregateDto>> GetByServiceAsync(
        string provider,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudCostAggregateDto>> GetByResourceGroupAsync(
        string provider,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}
