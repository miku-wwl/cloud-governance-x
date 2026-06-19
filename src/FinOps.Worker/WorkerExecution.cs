using FinOps.Application.Tenancy;
using FinOps.Worker.Jobs;

namespace FinOps.Worker;

internal interface IWorkerExecution
{
    Task ExecuteAsync(WorkerJobRequest request, CancellationToken cancellationToken);
}

internal sealed class WorkerExecution(
    IWorkerJobDispatcher dispatcher,
    ITenantContextInitializer tenantContextInitializer,
    ITenantMembershipResolver tenantResolver) : IWorkerExecution
{
    public async Task ExecuteAsync(
        WorkerJobRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfEqual(request.TenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.JobName);

        if (!await tenantResolver.IsActiveTenantAsync(
            request.TenantId,
            cancellationToken))
        {
            throw new InvalidOperationException(
                $"Worker tenant '{request.TenantId}' does not exist or is not active.");
        }

        tenantContextInitializer.Initialize(
            TrustedTenantContext.ForBackgroundJob(request.TenantId));

        await dispatcher.DispatchAsync(request.JobName, cancellationToken);
    }
}

internal sealed record WorkerJobRequest(string JobName, Guid TenantId);
