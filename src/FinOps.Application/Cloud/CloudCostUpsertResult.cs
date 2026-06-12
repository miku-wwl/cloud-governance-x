namespace FinOps.Application.Cloud;

public sealed record CloudCostUpsertResult(
    int Inserted,
    int Updated);
