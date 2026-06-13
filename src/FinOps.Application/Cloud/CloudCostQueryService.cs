namespace FinOps.Application.Cloud;

public sealed class CloudCostQueryService(
    ICloudCostQueryRepository repository,
    TimeProvider timeProvider) : ICloudCostQueryService
{
    private const string DefaultProvider = "Azure";

    public Task<IReadOnlyList<CloudCostDailyPointDto>> GetDailyAsync(
        string? provider,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var range = ResolveRange(from, to);
        return repository.GetDailyAsync(
            ResolveProvider(provider),
            range.From,
            range.To,
            cancellationToken);
    }

    public async Task<IReadOnlyList<CloudCostBreakdownDto>> GetByServiceAsync(
        string? provider,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var range = ResolveRange(from, to);
        var aggregates = await repository.GetByServiceAsync(
            ResolveProvider(provider),
            range.From,
            range.To,
            cancellationToken);
        return AddPercentages(aggregates);
    }

    public async Task<IReadOnlyList<CloudCostBreakdownDto>> GetByResourceGroupAsync(
        string? provider,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var range = ResolveRange(from, to);
        var aggregates = await repository.GetByResourceGroupAsync(
            ResolveProvider(provider),
            range.From,
            range.To,
            cancellationToken);
        return AddPercentages(aggregates);
    }

    private (DateOnly From, DateOnly To) ResolveRange(DateOnly? from, DateOnly? to)
    {
        var resolvedTo = to ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var resolvedFrom = from ?? resolvedTo.AddDays(-6);

        if (resolvedTo < resolvedFrom)
        {
            throw new ArgumentException("The cost query end date must not precede its start date.");
        }

        if (resolvedTo.DayNumber - resolvedFrom.DayNumber > 366)
        {
            throw new ArgumentOutOfRangeException(
                nameof(from),
                "The cost query range cannot exceed 367 days.");
        }

        return (resolvedFrom, resolvedTo);
    }

    private static string ResolveProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return DefaultProvider;
        }

        var trimmed = provider.Trim();
        return trimmed.ToUpperInvariant() switch
        {
            "AZURE" => "Azure",
            "AWS" => "AWS",
            _ => trimmed
        };
    }

    private static IReadOnlyList<CloudCostBreakdownDto> AddPercentages(
        IReadOnlyList<CloudCostAggregateDto> aggregates)
    {
        var totalsByCurrency = aggregates
            .GroupBy(aggregate => aggregate.Currency, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(aggregate => aggregate.Cost),
                StringComparer.OrdinalIgnoreCase);

        return aggregates
            .Select(aggregate =>
            {
                var total = totalsByCurrency[aggregate.Currency];
                var percentage = total == 0
                    ? 0
                    : decimal.Round(aggregate.Cost / total * 100, 2);

                return new CloudCostBreakdownDto(
                    aggregate.Name,
                    aggregate.Cost,
                    aggregate.Currency,
                    percentage);
            })
            .ToArray();
    }
}
