using System.Diagnostics;
using FinOps.Infrastructure;
using FinOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinOps.Migrator;

internal sealed class MigrationRunner(
    IDbContextFactory<FinOpsDbContext> dbContextFactory,
    IOptions<PostgreSqlHealthCheckOptions> databaseOptions,
    ILogger<MigrationRunner> logger)
{
    private const string AcquireLockSql = """
        SELECT pg_try_advisory_lock(
            hashtext('FinOps.Migrator'),
            hashtext(current_database()));
        """;

    private const string ReleaseLockSql = """
        SELECT pg_advisory_unlock(
            hashtext('FinOps.Migrator'),
            hashtext(current_database()));
        """;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var options = databaseOptions.Value;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation(
            "Starting database migration for {Username}@{Host}:{Port}/{Database}.",
            options.Username,
            options.Host,
            options.Port,
            options.Database);

        await using var dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        var lockAcquired = false;

        try
        {
            lockAcquired =
                await TryAcquireMigrationLockAsync(dbContext, cancellationToken);
            if (!lockAcquired)
            {
                throw new InvalidOperationException(
                    "Another FinOps database migration is already running for this database.");
            }

            logger.LogInformation("Acquired the database migration advisory lock.");

            var pendingMigrations = (
                await dbContext.Database.GetPendingMigrationsAsync(cancellationToken))
                .ToArray();

            logger.LogInformation(
                "Found {PendingMigrationCount} pending migration(s): {PendingMigrations}.",
                pendingMigrations.Length,
                pendingMigrations.Length == 0
                    ? "(none)"
                    : string.Join(", ", pendingMigrations));

            await dbContext.Database.MigrateAsync(cancellationToken);

            var remainingMigrations = (
                await dbContext.Database.GetPendingMigrationsAsync(cancellationToken))
                .ToHashSet(StringComparer.Ordinal);
            var appliedMigrations = pendingMigrations
                .Where(migration => !remainingMigrations.Contains(migration))
                .ToArray();

            stopwatch.Stop();
            logger.LogInformation(
                "Database migration completed. Applied {AppliedMigrationCount} migration(s): {AppliedMigrations}. Elapsed time: {ElapsedMilliseconds} ms.",
                appliedMigrations.Length,
                appliedMigrations.Length == 0
                    ? "(none)"
                    : string.Join(", ", appliedMigrations),
                stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            if (lockAcquired)
            {
                try
                {
                    await ReleaseMigrationLockAsync(dbContext);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(
                        exception,
                        "Could not explicitly release the database migration advisory lock. Closing the database connection will release it.");
                }
            }

            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private static async Task<bool> TryAcquireMigrationLockAsync(
        FinOpsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = AcquireLockSql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }

    private static async Task ReleaseMigrationLockAsync(FinOpsDbContext dbContext)
    {
        if (dbContext.Database.GetDbConnection().State !=
            System.Data.ConnectionState.Open)
        {
            return;
        }

        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = ReleaseLockSql;
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }
}
