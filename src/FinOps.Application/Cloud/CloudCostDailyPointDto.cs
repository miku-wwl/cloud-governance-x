namespace FinOps.Application.Cloud;

public sealed record CloudCostDailyPointDto(
    DateOnly UsageDate,
    decimal Cost,
    string Currency);
