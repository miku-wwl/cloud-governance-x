namespace FinOps.Application.Cloud;

public interface ICloudResourceInventoryProvider
{
    Task<IReadOnlyList<CloudResourceDto>> GetResourcesAsync(
        CancellationToken cancellationToken = default);
}
