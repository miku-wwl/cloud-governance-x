using FinOps.Worker.Jobs;

namespace FinOps.Worker;

internal interface IWorkerExecution
{
    Task ExecuteAsync(string jobName, CancellationToken cancellationToken);
}

internal sealed class WorkerExecution(
    IWorkerJobDispatcher dispatcher) : IWorkerExecution
{
    public Task ExecuteAsync(string jobName, CancellationToken cancellationToken) =>
        dispatcher.DispatchAsync(jobName, cancellationToken);
}
