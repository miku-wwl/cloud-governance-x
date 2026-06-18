using FinOps.Worker.Jobs;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinOps.Worker;

public static class WorkerServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerJobs(this IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IWorkerJobHandler, ResourceSyncJobHandler>());
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IWorkerJobHandler, CostSyncJobHandler>());
        services.TryAddScoped<IWorkerJobDispatcher, WorkerJobDispatcher>();
        services.TryAddScoped<IWorkerExecution, WorkerExecution>();
        services.TryAddSingleton<IProcessExitCode, ProcessExitCode>();

        return services;
    }
}
