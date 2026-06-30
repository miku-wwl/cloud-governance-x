namespace FinOps.Application.Authorization;

public sealed record FinOpsAuthorizationAuditEntry(
    FinOpsPermission Permission,
    FinOpsAuthorizationScope Scope,
    bool IsAllowed,
    string Reason,
    string? ActorIssuer,
    string? ActorSubject,
    string HttpMethod,
    string Path,
    int StatusCode,
    string CorrelationId);

public interface IFinOpsAuthorizationAuditSink
{
    Task AppendAsync(
        FinOpsAuthorizationAuditEntry entry,
        CancellationToken cancellationToken = default);
}

public sealed class NoOpFinOpsAuthorizationAuditSink : IFinOpsAuthorizationAuditSink
{
    public Task AppendAsync(
        FinOpsAuthorizationAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return Task.CompletedTask;
    }
}
