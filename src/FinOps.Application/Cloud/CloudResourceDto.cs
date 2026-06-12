namespace FinOps.Application.Cloud;

public sealed record CloudResourceDto(
    string Provider,
    string AccountId,
    string ResourceId,
    string ResourceName,
    string ResourceType,
    string Region,
    string? ResourceGroup,
    IReadOnlyDictionary<string, string> Tags);
