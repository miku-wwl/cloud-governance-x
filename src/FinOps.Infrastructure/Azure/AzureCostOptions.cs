namespace FinOps.Infrastructure.Azure;

internal sealed class AzureCostOptions
{
    public const string SectionName = "AzureCost";

    public bool UseSampleDataWhenUnavailable { get; init; } = true;

    public bool ForceSampleData { get; init; }
}
