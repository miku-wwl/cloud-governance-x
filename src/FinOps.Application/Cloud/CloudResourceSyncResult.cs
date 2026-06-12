namespace FinOps.Application.Cloud;

public sealed record CloudResourceSyncResult(
    int Retrieved,
    int Inserted,
    int Updated);
