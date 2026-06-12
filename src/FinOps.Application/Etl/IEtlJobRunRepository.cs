namespace FinOps.Application.Etl;

public interface IEtlJobRunRepository
{
    Task<Guid> StartAsync(
        string jobName,
        string provider,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        Guid id,
        DateTimeOffset finishedAt,
        int recordsProcessed,
        CancellationToken cancellationToken = default);

    Task FailAsync(
        Guid id,
        DateTimeOffset finishedAt,
        int recordsProcessed,
        string errorMessage,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EtlJobRunDto>> GetRecentAsync(
        string? jobName,
        int take,
        CancellationToken cancellationToken = default);
}
