namespace FinOps.Application.Etl;

public sealed record EtlJobRunDto(
    Guid Id,
    string JobName,
    string Provider,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    string Status,
    int RecordsProcessed,
    string? ErrorMessage);
