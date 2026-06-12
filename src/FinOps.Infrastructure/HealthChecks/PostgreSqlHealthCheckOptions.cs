namespace FinOps.Infrastructure;

public sealed class PostgreSqlHealthCheckOptions
{
    public const string SectionName = "PostgreSql";

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5432;

    public int TimeoutSeconds { get; init; } = 3;
}
