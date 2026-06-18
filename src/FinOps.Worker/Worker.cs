using Microsoft.Extensions.Options;

namespace FinOps.Worker;

internal sealed class Worker(
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime applicationLifetime,
    IOptions<EtlWorkerOptions> options,
    IProcessExitCode processExitCode,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var execution = scope.ServiceProvider.GetRequiredService<IWorkerExecution>();
            await execution.ExecuteAsync(options.Value.Job, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Azure ETL job {Job} was cancelled.",
                options.Value.Job);
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "Azure ETL job {Job} failed.",
                options.Value.Job);
            processExitCode.Value = 1;
        }
        finally
        {
            applicationLifetime.StopApplication();
        }
    }
}
