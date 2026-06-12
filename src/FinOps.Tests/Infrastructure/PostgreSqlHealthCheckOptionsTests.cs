using FinOps.Infrastructure;
using Npgsql;

namespace FinOps.Tests.Infrastructure;

public sealed class PostgreSqlHealthCheckOptionsTests
{
    [Fact]
    public void GetConnectionString_MapsConfiguredDatabaseSettings()
    {
        var options = new PostgreSqlHealthCheckOptions
        {
            Host = "database.internal",
            Port = 5544,
            Database = "governance",
            Username = "application",
            Password = "secret",
            TimeoutSeconds = 7
        };

        var result = new NpgsqlConnectionStringBuilder(options.GetConnectionString());

        Assert.Equal("database.internal", result.Host);
        Assert.Equal(5544, result.Port);
        Assert.Equal("governance", result.Database);
        Assert.Equal("application", result.Username);
        Assert.Equal("secret", result.Password);
        Assert.Equal(7, result.Timeout);
        Assert.Equal(7, result.CommandTimeout);
        Assert.False(result.Pooling);
    }
}
