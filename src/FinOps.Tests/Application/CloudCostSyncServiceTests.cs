using FinOps.Application.Cloud;
using FinOps.Application.Etl;

namespace FinOps.Tests.Application;

public sealed class CloudCostSyncServiceTests
{
    [Fact]
    public async Task SyncRecentAsync_UpsertsCostsAndCompletesJob()
    {
        var costs = new[]
        {
            CreateCost(new DateOnly(2026, 6, 12), "azure-cost-management"),
            CreateCost(new DateOnly(2026, 6, 11), "azure-cost-management")
        };
        var repository = new StubCostRepository(new CloudCostUpsertResult(1, 1));
        var jobRuns = new StubJobRunRepository();
        var service = new CloudCostSyncService(
            new StubCostProvider(costs),
            repository,
            jobRuns,
            new FixedTimeProvider());

        var result = await service.SyncRecentAsync();

        Assert.Equal(new DateOnly(2026, 6, 6), result.From);
        Assert.Equal(new DateOnly(2026, 6, 12), result.To);
        Assert.Equal(2, result.Retrieved);
        Assert.Equal(1, result.Inserted);
        Assert.Equal(1, result.Updated);
        Assert.Equal(2, jobRuns.CompletedRecords);
        Assert.Equal(costs, repository.Costs);
    }

    [Fact]
    public async Task SyncRecentAsync_RecordsProviderFailure()
    {
        var expected = new InvalidOperationException("Cost API unavailable.");
        var jobRuns = new StubJobRunRepository();
        var service = new CloudCostSyncService(
            new FailingCostProvider(expected),
            new StubCostRepository(new CloudCostUpsertResult(0, 0)),
            jobRuns,
            new FixedTimeProvider());

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SyncRecentAsync());

        Assert.Same(expected, actual);
        Assert.Equal(expected.Message, jobRuns.FailureMessage);
    }

    private static CloudCostDailyDto CreateCost(DateOnly usageDate, string source)
    {
        return new CloudCostDailyDto(
            "Azure",
            "subscription-1",
            usageDate,
            "Storage",
            "rg-demo",
            1.25m,
            "USD",
            $$"""{"source":"{{source}}"}""");
    }

    private sealed class StubCostProvider(IReadOnlyList<CloudCostDailyDto> costs)
        : ICloudCostProvider
    {
        public Task<IReadOnlyList<CloudCostDailyDto>> GetDailyCostsAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(costs);
        }
    }

    private sealed class FailingCostProvider(Exception exception) : ICloudCostProvider
    {
        public Task<IReadOnlyList<CloudCostDailyDto>> GetDailyCostsAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<IReadOnlyList<CloudCostDailyDto>>(exception);
        }
    }

    private sealed class StubCostRepository(CloudCostUpsertResult result) : ICloudCostRepository
    {
        public IReadOnlyCollection<CloudCostDailyDto>? Costs { get; private set; }

        public Task<CloudCostUpsertResult> UpsertAsync(
            IReadOnlyCollection<CloudCostDailyDto> costs,
            CancellationToken cancellationToken = default)
        {
            Costs = costs;
            return Task.FromResult(result);
        }
    }

    private sealed class StubJobRunRepository : IEtlJobRunRepository
    {
        public Guid Id { get; } = Guid.NewGuid();

        public int? CompletedRecords { get; private set; }

        public string? FailureMessage { get; private set; }

        public Task<Guid> StartAsync(
            string jobName,
            string provider,
            DateTimeOffset startedAt,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(CloudCostSyncService.JobName, jobName);
            Assert.Equal(CloudCostSyncService.Provider, provider);
            return Task.FromResult(Id);
        }

        public Task CompleteAsync(
            Guid id,
            DateTimeOffset finishedAt,
            int recordsProcessed,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(Id, id);
            CompletedRecords = recordsProcessed;
            return Task.CompletedTask;
        }

        public Task FailAsync(
            Guid id,
            DateTimeOffset finishedAt,
            int recordsProcessed,
            string errorMessage,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(Id, id);
            FailureMessage = errorMessage;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<EtlJobRunDto>> GetRecentAsync(
            string? jobName,
            int take,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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
