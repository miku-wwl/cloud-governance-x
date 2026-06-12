namespace FinOps.Application.Cloud;

public interface ICloudResourceRepository
{
    Task<CloudResourceUpsertResult> UpsertAsync(
        IReadOnlyCollection<CloudResourceDto> resources,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default);
}
