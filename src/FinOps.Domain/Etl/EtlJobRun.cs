namespace FinOps.Domain.Etl;

public sealed class EtlJobRun
{
    public const string RunningStatus = "Running";
    public const string SucceededStatus = "Succeeded";
    public const string FailedStatus = "Failed";

    private EtlJobRun()
    {
    }

    private EtlJobRun(
        Guid tenantId,
        string jobName,
        string provider,
        DateTimeOffset startedAt)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        JobName = jobName;
        Provider = provider;
        StartedAt = startedAt;
        Status = RunningStatus;
    }

    public Guid Id { get; private set; }

    public Guid? TenantId { get; private set; }

    public string JobName { get; private set; } = string.Empty;

    public string Provider { get; private set; } = string.Empty;

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public string Status { get; private set; } = RunningStatus;

    public int RecordsProcessed { get; private set; }

    public string? ErrorMessage { get; private set; }

    public static EtlJobRun Start(
        Guid tenantId,
        string jobName,
        string provider,
        DateTimeOffset startedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        return new EtlJobRun(tenantId, jobName, provider, startedAt);
    }

    public void Complete(DateTimeOffset finishedAt, int recordsProcessed)
    {
        EnsureRunning();
        ArgumentOutOfRangeException.ThrowIfNegative(recordsProcessed);

        FinishedAt = finishedAt;
        RecordsProcessed = recordsProcessed;
        Status = SucceededStatus;
        ErrorMessage = null;
    }

    public void Fail(
        DateTimeOffset finishedAt,
        int recordsProcessed,
        string errorMessage)
    {
        EnsureRunning();
        ArgumentOutOfRangeException.ThrowIfNegative(recordsProcessed);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        FinishedAt = finishedAt;
        RecordsProcessed = recordsProcessed;
        Status = FailedStatus;
        ErrorMessage = errorMessage.Length <= 4000
            ? errorMessage
            : errorMessage[..4000];
    }

    private void EnsureRunning()
    {
        if (!string.Equals(Status, RunningStatus, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"ETL job run '{Id}' is already in terminal status '{Status}'.");
        }
    }
}
