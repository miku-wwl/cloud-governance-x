using FinOps.Application.Authorization;
using FinOps.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace FinOps.Infrastructure.Persistence;

internal sealed class AuthorizationAuditSink(
    IDbContextFactory<FinOpsDbContext> contextFactory,
    TimeProvider timeProvider) : IFinOpsAuthorizationAuditSink
{
    public async Task AppendAsync(
        FinOpsAuthorizationAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await using var context =
            await contextFactory.CreateDbContextAsync(cancellationToken);
        context.AuthorizationAuditEvents.Add(AuthorizationAuditEvent.Record(
            timeProvider.GetUtcNow(),
            entry.Permission.ToString(),
            entry.Scope.Kind.ToString(),
            entry.Scope.TenantId,
            entry.Scope.CloudAccountId,
            entry.IsAllowed,
            IsHighPrivilege(entry.Permission),
            entry.Reason,
            entry.ActorIssuer,
            entry.ActorSubject,
            entry.HttpMethod,
            entry.Path,
            entry.StatusCode,
            entry.CorrelationId));
        await context.SaveChangesAsync(cancellationToken);
    }

    private static bool IsHighPrivilege(FinOpsPermission permission) =>
        permission is
            FinOpsPermission.TenantManage or
            FinOpsPermission.ResourceSync or
            FinOpsPermission.CostSync or
            FinOpsPermission.PlatformOperate;
}
