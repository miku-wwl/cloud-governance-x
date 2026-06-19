using FinOps.Application.Tenancy;
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
        builder.Services.AddScoped<TenantContext>();
        builder.Services.AddScoped<ITenantContext>(serviceProvider =>
            serviceProvider.GetRequiredService<TenantContext>());
        builder.Services.AddScoped<ITenantContextInitializer>(serviceProvider =>
            serviceProvider.GetRequiredService<TenantContext>());
        builder.Services.AddScoped<MigrationRunner>();
        builder.Services.Configure<LegacyTenantBackfillOptions>(
            builder.Configuration.GetSection(
                LegacyTenantBackfillOptions.SectionName));
        builder.Services.AddScoped<LegacyTenantBackfillRunner>();

        host = builder.Build();

        await using var scope = host.Services.CreateAsyncScope();
        var operation = builder.Configuration["Operation"] ?? "migrate";
        if (string.Equals(
            operation,
            "backfill-development-tenant",
            StringComparison.OrdinalIgnoreCase))
        {
            var runner = scope.ServiceProvider
                .GetRequiredService<LegacyTenantBackfillRunner>();
            await runner.RunAsync(CancellationToken.None);
        }
        else if (string.Equals(
            operation,
            "migrate",
            StringComparison.OrdinalIgnoreCase))
        {
            var runner = scope.ServiceProvider
                .GetRequiredService<MigrationRunner>();
            await runner.RunAsync(CancellationToken.None);
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported Migrator operation '{operation}'.");
        }

        return 0;
    }
    catch (Exception exception)
    {
        if (host is null)
        {
            Console.Error.WriteLine($"Database operation failed: {exception}");
        }
        else
        {
            var logger = host.Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("FinOps.Migrator");
            logger.LogCritical(exception, "Database operation failed.");
        }

        return 1;
    }
    finally
    {
        host?.Dispose();
    }
}
