using FinOps.Application.Cloud;

namespace FinOps.Tests.Application;

public sealed class CloudCostQueryServiceTests
{
    [Fact]
    public async Task GetDailyAsync_NormalizesAzureAndUsesSevenDayRange()
    {
        var repository = new StubQueryRepository();
        var service = new CloudCostQueryService(repository, new FixedTimeProvider());

        await service.GetDailyAsync("azure", null, null);

        Assert.Equal("Azure", repository.Provider);
        Assert.Equal(new DateOnly(2026, 6, 6), repository.From);
        Assert.Equal(new DateOnly(2026, 6, 12), repository.To);
    }

    [Fact]
    public async Task GetByServiceAsync_CalculatesPercentagesPerCurrency()
    {
        var repository = new StubQueryRepository
        {
            ServiceAggregates =
            [
                new CloudCostAggregateDto("Storage", 75m, "USD"),
                new CloudCostAggregateDto("Compute", 25m, "USD"),
                new CloudCostAggregateDto("Storage", 30m, "NZD"),
                new CloudCostAggregateDto("Compute", 30m, "NZD")
            ]
        };
        var service = new CloudCostQueryService(repository, new FixedTimeProvider());

        var result = await service.GetByServiceAsync(
            "Azure",
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 12));

        Assert.Equal(75m, result.Single(item =>
            item.Name == "Storage" && item.Currency == "USD").Percentage);
        Assert.Equal(50m, result.Single(item =>
            item.Name == "Storage" && item.Currency == "NZD").Percentage);
        Assert.All(
            result.GroupBy(item => item.Currency),
            group => Assert.Equal(100m, group.Sum(item => item.Percentage)));
    }

    private sealed class StubQueryRepository : ICloudCostQueryRepository
    {
        public string? Provider { get; private set; }

        public DateOnly From { get; private set; }

        public DateOnly To { get; private set; }

        public IReadOnlyList<CloudCostAggregateDto> ServiceAggregates { get; init; } = [];

        public Task<IReadOnlyList<CloudCostDailyPointDto>> GetDailyAsync(
            string provider,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
        {
            Capture(provider, from, to);
            return Task.FromResult<IReadOnlyList<CloudCostDailyPointDto>>([]);
        }

        public Task<IReadOnlyList<CloudCostAggregateDto>> GetByServiceAsync(
            string provider,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
        {
            Capture(provider, from, to);
            return Task.FromResult(ServiceAggregates);
        }

        public Task<IReadOnlyList<CloudCostAggregateDto>> GetByResourceGroupAsync(
            string provider,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
        {
            Capture(provider, from, to);
            return Task.FromResult<IReadOnlyList<CloudCostAggregateDto>>([]);
        }

        private void Capture(string provider, DateOnly from, DateOnly to)
        {
            Provider = provider;
            From = from;
            To = to;
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(2026, 6, 12, 12, 0, 0, TimeSpan.Zero);
        }
    }
}
