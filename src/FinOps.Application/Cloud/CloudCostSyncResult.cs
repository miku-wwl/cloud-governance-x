namespace FinOps.Application.Cloud;

public sealed record CloudCostSyncResult(
    Guid JobRunId,
    DateOnly From,
    DateOnly To,
    int Retrieved,
    int Inserted,
    int Updated,
    bool UsedSampleData);
