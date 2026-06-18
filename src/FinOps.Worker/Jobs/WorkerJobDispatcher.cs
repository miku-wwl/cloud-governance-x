namespace FinOps.Worker.Jobs;

internal sealed class WorkerJobDispatcher : IWorkerJobDispatcher
{
    private readonly IReadOnlyDictionary<string, IWorkerJobHandler> handlers;

    public WorkerJobDispatcher(IEnumerable<IWorkerJobHandler> handlers)
    {
        var handlersByName = new Dictionary<string, IWorkerJobHandler>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var handler in handlers)
        {
            if (!handlersByName.TryAdd(handler.Name, handler))
            {
                throw new InvalidOperationException(
                    $"Multiple Worker job handlers are registered for '{handler.Name}'.");
            }
        }

        this.handlers = handlersByName;
    }

    public Task DispatchAsync(string jobName, CancellationToken cancellationToken)
    {
        if (!handlers.TryGetValue(jobName, out var handler))
        {
            var supportedJobs = string.Join(
                ", ",
                handlers.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));

            throw new InvalidOperationException(
                $"Unsupported ETL job '{jobName}'. Use one of: {supportedJobs}.");
        }

        return handler.ExecuteAsync(cancellationToken);
    }
}
