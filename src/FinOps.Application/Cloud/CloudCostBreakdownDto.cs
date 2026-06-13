namespace FinOps.Application.Cloud;

public sealed record CloudCostBreakdownDto(
    string Name,
    decimal Cost,
    string Currency,
    decimal Percentage);
