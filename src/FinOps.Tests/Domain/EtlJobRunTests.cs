using FinOps.Domain.Etl;

namespace FinOps.Tests.Domain;

public sealed class EtlJobRunTests
{
    [Fact]
    public void Complete_RecordsTerminalSuccess()
    {
        var startedAt = new DateTimeOffset(2026, 6, 13, 0, 0, 0, TimeSpan.Zero);
        var finishedAt = startedAt.AddMinutes(1);
        var run = EtlJobRun.Start(
            Guid.NewGuid(),
            "azure-resource-sync",
            "Azure",
            startedAt);

        run.Complete(finishedAt, 42);

        Assert.Equal(EtlJobRun.SucceededStatus, run.Status);
        Assert.Equal(finishedAt, run.FinishedAt);
        Assert.Equal(42, run.RecordsProcessed);
        Assert.Null(run.ErrorMessage);
    }

    [Fact]
    public void Fail_TruncatesOversizedErrorMessage()
    {
        var run = EtlJobRun.Start(
            Guid.NewGuid(),
            "azure-resource-sync",
            "Azure",
            DateTimeOffset.UtcNow);

        run.Fail(DateTimeOffset.UtcNow, 3, new string('x', 5000));

        Assert.Equal(EtlJobRun.FailedStatus, run.Status);
        Assert.Equal(3, run.RecordsProcessed);
        Assert.Equal(4000, run.ErrorMessage?.Length);
    }

    [Fact]
    public void Start_requires_tenant()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EtlJobRun.Start(
                Guid.Empty,
                "azure-resource-sync",
                "Azure",
                DateTimeOffset.UtcNow));
    }
}
