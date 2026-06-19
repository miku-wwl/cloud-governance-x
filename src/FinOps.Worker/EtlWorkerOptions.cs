namespace FinOps.Worker;

public sealed class EtlWorkerOptions
{
    public const string SectionName = "Etl";

    public string Job { get; init; } = "Resources";

    public Guid TenantId { get; init; }

    public int CostDays { get; init; } = 7;
}
