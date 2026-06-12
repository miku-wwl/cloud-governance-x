using FinOps.Application.Cloud;
using FinOps.Application.Etl;

namespace FinOps.Tests.Application;

public sealed class CloudResourceSyncServiceTests
{
    [Fact]
    public async Task SyncAsync_ReturnsProviderAndRepositoryCounts()
    {
        var resources = new[]
        {
            CreateResource("/subscriptions/1/resources/a"),
            CreateResource("/subscriptions/1/resources/b")
        };
        var provider = new StubInventoryProvider(resources);
        var repository = new StubRepository(new CloudResourceUpsertResult(1, 1));
        var jobRuns = new StubJobRunRepository();
        var service = new CloudResourceSyncService(
            provider,
            repository,
            jobRuns,
            new FixedTimeProvider());

        var result = await service.SyncAsync();

        Assert.Equal(jobRuns.JobRunId, result.JobRunId);
        Assert.Equal(2, result.Retrieved);
        Assert.Equal(1, result.Inserted);
        Assert.Equal(1, result.Updated);
        Assert.Equal(resources, repository.Resources);
        Assert.Equal(2, jobRuns.CompletedRecords);
        Assert.Null(jobRuns.FailureMessage);
    }

    [Fact]
    public async Task SyncAsync_RecordsFailureAndRethrows()
    {
        var expected = new InvalidOperationException("Resource Graph unavailable.");
        var jobRuns = new StubJobRunRepository();
        var service = new CloudResourceSyncService(
            new FailingInventoryProvider(expected),
            new StubRepository(new CloudResourceUpsertResult(0, 0)),
            jobRuns,
            new FixedTimeProvider());

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SyncAsync());

        Assert.Same(expected, actual);
        Assert.Contains(expected.Message, jobRuns.FailureMessage, StringComparison.Ordinal);
        Assert.Equal(0, jobRuns.FailedRecords);
    }

    private static CloudResourceDto CreateResource(string id)
    {
        return new CloudResourceDto(
            "Azure",
            "subscription",
            id,
            "resource",
            "type",
            "australiaeast",
            "rg-demo",
            new Dictionary<string, string>());
    }

    private sealed class StubInventoryProvider(IReadOnlyList<CloudResourceDto> resources)
        : ICloudResourceInventoryProvider
    {
        public Task<IReadOnlyList<CloudResourceDto>> GetResourcesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(resources);
        }
    }

    private sealed class FailingInventoryProvider(Exception exception)
        : ICloudResourceInventoryProvider
    {
        public Task<IReadOnlyList<CloudResourceDto>> GetResourcesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<IReadOnlyList<CloudResourceDto>>(exception);
        }
    }

    private sealed class StubRepository(CloudResourceUpsertResult result)
        : ICloudResourceRepository
    {
        public IReadOnlyCollection<CloudResourceDto>? Resources { get; private set; }

        public Task<CloudResourceUpsertResult> UpsertAsync(
            IReadOnlyCollection<CloudResourceDto> resources,
            DateTimeOffset observedAt,
            CancellationToken cancellationToken = default)
        {
            Resources = resources;
            return Task.FromResult(result);
        }
    }

    private sealed class StubJobRunRepository : IEtlJobRunRepository
    {
        public Guid JobRunId { get; } = Guid.NewGuid();

        public int? CompletedRecords { get; private set; }

        public int? FailedRecords { get; private set; }

        public string? FailureMessage { get; private set; }

        public Task<Guid> StartAsync(
            string jobName,
            string provider,
            DateTimeOffset startedAt,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(CloudResourceSyncService.JobName, jobName);
            Assert.Equal(CloudResourceSyncService.Provider, provider);
            return Task.FromResult(JobRunId);
        }

        public Task CompleteAsync(
            Guid id,
            DateTimeOffset finishedAt,
            int recordsProcessed,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(JobRunId, id);
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
            Assert.Equal(JobRunId, id);
            FailedRecords = recordsProcessed;
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
            return new DateTimeOffset(2026, 6, 13, 0, 0, 0, TimeSpan.Zero);
        }
    }
}
