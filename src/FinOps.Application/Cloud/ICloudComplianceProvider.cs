namespace FinOps.Application.Cloud;

public interface ICloudComplianceProvider
{
    Task<IReadOnlyList<ComplianceFindingDto>> GetComplianceFindingsAsync(
        CancellationToken cancellationToken = default);
}
