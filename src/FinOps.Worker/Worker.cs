using FinOps.Application.Cloud;
using FinOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinOps.Worker;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime applicationLifetime,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FinOpsDbContext>();
            await dbContext.Database.MigrateAsync(stoppingToken);

            var syncService = scope.ServiceProvider.GetRequiredService<ICloudResourceSyncService>();
            var result = await syncService.SyncAsync(stoppingToken);

            logger.LogInformation(
                "Azure resource sync completed. Retrieved: {Retrieved}, inserted: {Inserted}, updated: {Updated}.",
                result.Retrieved,
                result.Inserted,
                result.Updated);
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Azure resource sync failed.");
            Environment.ExitCode = 1;
        }
        finally
        {
            applicationLifetime.StopApplication();
        }
    }
}
