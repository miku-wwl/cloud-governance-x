namespace FinOps.Application.Cloud;

public sealed record CloudCostDailyDto(
    string Provider,
    string AccountId,
    DateOnly UsageDate,
    string ServiceName,
    string? ResourceGroup,
    decimal Cost,
    string Currency,
    string RawJson);
