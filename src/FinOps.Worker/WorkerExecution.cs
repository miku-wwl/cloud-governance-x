using FinOps.Infrastructure.Persistence;
using FinOps.Worker.Jobs;
using Microsoft.EntityFrameworkCore;

namespace FinOps.Worker;

internal interface IWorkerExecution
{
    Task ExecuteAsync(string jobName, CancellationToken cancellationToken);
}

internal sealed class WorkerExecution(
    FinOpsDbContext dbContext,
    IWorkerJobDispatcher dispatcher) : IWorkerExecution
{
    public async Task ExecuteAsync(string jobName, CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
        await dispatcher.DispatchAsync(jobName, cancellationToken);
    }
}
