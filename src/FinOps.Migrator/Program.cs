using FinOps.Infrastructure;
using FinOps.Infrastructure.Persistence;
using FinOps.Migrator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

return await RunAsync(args);

static async Task<int> RunAsync(string[] args)
{
    IHost? host = null;

    try
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Services.Configure<PostgreSqlHealthCheckOptions>(
            builder.Configuration.GetSection(PostgreSqlHealthCheckOptions.SectionName));
        builder.Services.AddPostgreSql(builder.Configuration);
        builder.Services.AddScoped<MigrationRunner>();

        host = builder.Build();

        await using var scope = host.Services.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();
        await runner.RunAsync(CancellationToken.None);
        return 0;
    }
    catch (Exception exception)
    {
        if (host is null)
        {
            Console.Error.WriteLine($"Database migration failed: {exception}");
        }
        else
        {
            var logger = host.Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("FinOps.Migrator");
            logger.LogCritical(exception, "Database migration failed.");
        }

        return 1;
    }
    finally
    {
        host?.Dispose();
    }
}
