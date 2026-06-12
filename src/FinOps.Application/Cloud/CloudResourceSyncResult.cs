namespace FinOps.Application.Cloud;

public sealed record CloudResourceSyncResult(
    Guid JobRunId,
    int Retrieved,
    int Inserted,
    int Updated);
