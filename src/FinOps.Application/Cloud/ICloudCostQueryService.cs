namespace FinOps.Application.Cloud;

public interface ICloudCostQueryService
{
    Task<IReadOnlyList<CloudCostDailyPointDto>> GetDailyAsync(
        string? provider,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudCostBreakdownDto>> GetByServiceAsync(
        string? provider,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudCostBreakdownDto>> GetByResourceGroupAsync(
        string? provider,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);
}
