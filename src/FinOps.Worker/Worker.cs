namespace FinOps.Worker;

public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FinOps Worker started at {StartedAt}.", DateTimeOffset.UtcNow);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            logger.LogInformation("FinOps Worker heartbeat at {HeartbeatAt}.", DateTimeOffset.UtcNow);
        }
    }
}
