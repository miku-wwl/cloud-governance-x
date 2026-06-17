using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinOps.Infrastructure;

public static class PostgreSqlHealthCheckExtensions
{
    public static IHealthChecksBuilder AddPostgreSqlHealthCheck(
        this IHealthChecksBuilder healthChecks,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(PostgreSqlHealthCheckOptions.SectionName)
            .Get<PostgreSqlHealthCheckOptions>()
            ?? new PostgreSqlHealthCheckOptions();

        return healthChecks.AddCheck(
            "postgresql",
            new PostgreSqlHealthCheck(options),
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"]);
    }
}
