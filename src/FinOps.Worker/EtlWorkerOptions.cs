namespace FinOps.Worker;

public sealed class EtlWorkerOptions
{
    public const string SectionName = "Etl";

    public string Job { get; init; } = "Resources";

    public int CostDays { get; init; } = 7;
}
