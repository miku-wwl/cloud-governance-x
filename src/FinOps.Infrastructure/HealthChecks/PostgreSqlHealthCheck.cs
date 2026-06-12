using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace FinOps.Infrastructure;

public sealed class PostgreSqlHealthCheck(PostgreSqlHealthCheckOptions options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

        try
        {
            await using var connection = new NpgsqlConnection(options.GetConnectionString());
            await connection.OpenAsync(timeout.Token);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(timeout.Token);

            return HealthCheckResult.Healthy(
                $"PostgreSQL database '{options.Database}' is ready at {options.Host}:{options.Port}.");
        }
        catch (Exception exception) when (
            exception is NpgsqlException or TimeoutException or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                $"PostgreSQL database '{options.Database}' is not ready at {options.Host}:{options.Port}.",
                exception);
        }
    }
}
