namespace FinOps.Migrator;

public sealed class LegacyTenantBackfillOptions
{
    public const string SectionName = "LegacyTenantBackfill";

    public bool Enabled { get; init; }

    public bool Apply { get; init; }

    public bool LegacyWritersStopped { get; init; }

    public string DatabaseConfirmation { get; init; } = string.Empty;

    public long ExpectedResourceRows { get; init; } = -1;

    public long ExpectedCostRows { get; init; } = -1;

    public long ExpectedEtlRunRows { get; init; } = -1;

    public long MaximumLegacyRows { get; init; } = 100_000;

    public Guid OrganizationId { get; init; }

    public Guid TenantId { get; init; }

    public string OrganizationDisplayName { get; init; } =
        "FinOps Development Organization";

    public string TenantSlug { get; init; } = "legacy-development";

    public string TenantDisplayName { get; init; } =
        "Legacy Development Tenant";
}
