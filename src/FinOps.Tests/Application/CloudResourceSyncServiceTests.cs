using FinOps.Application.Cloud;

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
        var service = new CloudResourceSyncService(provider, repository);

        var result = await service.SyncAsync();

        Assert.Equal(2, result.Retrieved);
        Assert.Equal(1, result.Inserted);
        Assert.Equal(1, result.Updated);
        Assert.Equal(resources, repository.Resources);
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
}
