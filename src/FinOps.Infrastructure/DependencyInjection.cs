using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinOps.Infrastructure;

public static class DependencyInjection
{
    public static IHealthChecksBuilder AddPostgreSqlTcpCheck(
        this IHealthChecksBuilder healthChecks,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(PostgreSqlHealthCheckOptions.SectionName)
            .Get<PostgreSqlHealthCheckOptions>()
            ?? new PostgreSqlHealthCheckOptions();

        return healthChecks.AddCheck(
            "postgresql",
            new PostgreSqlTcpHealthCheck(options),
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"]);
    }
}
