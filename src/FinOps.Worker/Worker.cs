using FinOps.Application.Cloud;
using FinOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FinOps.Worker;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime applicationLifetime,
    IOptions<EtlWorkerOptions> options,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FinOpsDbContext>();
            await dbContext.Database.MigrateAsync(stoppingToken);

            if (string.Equals(options.Value.Job, "Costs", StringComparison.OrdinalIgnoreCase))
            {
                var costSyncService =
                    scope.ServiceProvider.GetRequiredService<ICloudCostSyncService>();
                var result = await costSyncService.SyncRecentAsync(
                    options.Value.CostDays,
                    stoppingToken);

                logger.LogInformation(
                    "Azure cost sync completed. Job run: {JobRunId}, retrieved: {Retrieved}, inserted: {Inserted}, updated: {Updated}, sample data: {UsedSampleData}.",
                    result.JobRunId,
                    result.Retrieved,
                    result.Inserted,
                    result.Updated,
                    result.UsedSampleData);
            }
            else if (string.Equals(
                options.Value.Job,
                "Resources",
                StringComparison.OrdinalIgnoreCase))
            {
                var resourceSyncService =
                    scope.ServiceProvider.GetRequiredService<ICloudResourceSyncService>();
                var result = await resourceSyncService.SyncAsync(stoppingToken);

                logger.LogInformation(
                    "Azure resource sync completed. Job run: {JobRunId}, retrieved: {Retrieved}, inserted: {Inserted}, updated: {Updated}.",
                    result.JobRunId,
                    result.Retrieved,
                    result.Inserted,
                    result.Updated);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported ETL job '{options.Value.Job}'. Use 'Resources' or 'Costs'.");
            }
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "Azure ETL job {Job} failed.",
                options.Value.Job);
            Environment.ExitCode = 1;
        }
        finally
        {
            applicationLifetime.StopApplication();
        }
    }
}
