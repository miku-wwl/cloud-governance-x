namespace FinOps.Worker.Jobs;

public interface IWorkerJobDispatcher
{
    Task DispatchAsync(string jobName, CancellationToken cancellationToken);
}
