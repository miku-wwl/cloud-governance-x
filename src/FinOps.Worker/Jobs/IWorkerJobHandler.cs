namespace FinOps.Worker.Jobs;

public interface IWorkerJobHandler
{
    string Name { get; }

    Task ExecuteAsync(CancellationToken cancellationToken);
}
