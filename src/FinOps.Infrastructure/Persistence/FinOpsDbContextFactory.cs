using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinOps.Infrastructure.Persistence;

public sealed class FinOpsDbContextFactory : IDesignTimeDbContextFactory<FinOpsDbContext>
{
    public FinOpsDbContext CreateDbContext(string[] args)
    {
        var options = new PostgreSqlHealthCheckOptions
        {
            Host = Environment.GetEnvironmentVariable("PostgreSql__Host") ?? "localhost",
            Port = ParseInt(Environment.GetEnvironmentVariable("PostgreSql__Port"), 5432),
            Database = Environment.GetEnvironmentVariable("PostgreSql__Database") ?? "finops",
            Username = Environment.GetEnvironmentVariable("PostgreSql__Username") ?? "finops",
            Password = Environment.GetEnvironmentVariable("PostgreSql__Password") ?? "finops_dev_password"
        };

        var builder = new DbContextOptionsBuilder<FinOpsDbContext>();
        builder.UseNpgsql(options.GetConnectionString());

        return new FinOpsDbContext(builder.Options);
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
