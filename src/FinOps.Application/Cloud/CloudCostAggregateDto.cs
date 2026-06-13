namespace FinOps.Application.Cloud;

public sealed record CloudCostAggregateDto(
    string Name,
    decimal Cost,
    string Currency);
