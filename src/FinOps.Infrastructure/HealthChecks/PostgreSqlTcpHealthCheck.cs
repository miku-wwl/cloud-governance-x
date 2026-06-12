using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinOps.Infrastructure;

public sealed class PostgreSqlTcpHealthCheck(PostgreSqlHealthCheckOptions options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(options.Host, options.Port, timeout.Token);

            return HealthCheckResult.Healthy(
                $"PostgreSQL is reachable at {options.Host}:{options.Port}.");
        }
        catch (Exception exception) when (
            exception is SocketException or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                $"PostgreSQL is not reachable at {options.Host}:{options.Port}.",
                exception);
        }
    }
}
