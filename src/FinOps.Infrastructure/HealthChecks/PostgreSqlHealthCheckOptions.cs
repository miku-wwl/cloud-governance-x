using Npgsql;

namespace FinOps.Infrastructure;

public sealed class PostgreSqlHealthCheckOptions
{
    public const string SectionName = "PostgreSql";

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5432;

    public string Database { get; init; } = "finops";

    public string Username { get; init; } = "finops";

    public string Password { get; init; } = "finops_dev_password";

    public int TimeoutSeconds { get; init; } = 3;

    public string GetConnectionString()
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Database = Database,
            Username = Username,
            Password = Password,
            Timeout = TimeoutSeconds,
            CommandTimeout = TimeoutSeconds,
            Pooling = false
        }.ConnectionString;
    }
}
