namespace FinOps.Application.Cloud;

public sealed record ComplianceFindingDto(
    string Provider,
    string ResourceId,
    string RuleId,
    string Severity,
    string Status,
    string Recommendation);
