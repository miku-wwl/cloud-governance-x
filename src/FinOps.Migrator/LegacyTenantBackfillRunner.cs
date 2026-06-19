using System.Data.Common;
using System.Diagnostics;
using FinOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinOps.Migrator;

internal sealed class LegacyTenantBackfillRunner(
    IDbContextFactory<FinOpsDbContext> dbContextFactory,
    IOptions<LegacyTenantBackfillOptions> backfillOptions,
    IHostEnvironment hostEnvironment,
    ILogger<LegacyTenantBackfillRunner> logger)
{
    private const string AcquireLockSql = """
        SELECT pg_try_advisory_xact_lock(
            hashtext('FinOps.LegacyTenantBackfill'),
            hashtext(current_database()));
        """;

    private const string LockCoreTablesSql = """
        LOCK TABLE
            cloud_resources,
            cloud_cost_daily,
            etl_job_runs,
            organizations,
            tenants,
            provider_connections,
            cloud_accounts
        IN SHARE ROW EXCLUSIVE MODE NOWAIT;
        """;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var options = backfillOptions.Value;
        ValidateOptions(options);

        if (!hostEnvironment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Legacy tenant backfill is restricted to the Development environment.");
        }

        var stopwatch = Stopwatch.StartNew();
        await using var dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var pendingMigrations = (
            await dbContext.Database.GetPendingMigrationsAsync(cancellationToken))
            .ToArray();
        if (pendingMigrations.Length > 0)
        {
            throw new InvalidOperationException(
                "Apply all schema migrations before running legacy tenant backfill.");
        }

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var lockAcquired = await ExecuteScalarAsync<bool>(
                dbContext,
                AcquireLockSql,
                cancellationToken);
            if (!lockAcquired)
            {
                throw new InvalidOperationException(
                    "Another legacy tenant backfill is already running for this database.");
            }

            await ExecuteNonQueryAsync(
                dbContext,
                LockCoreTablesSql,
                cancellationToken);
            var before = await ReadCountsAsync(dbContext, cancellationToken);
            await ValidateTargetAndCountsAsync(
                dbContext,
                options,
                before,
                cancellationToken);
            if (before.LegacyResources == 0 &&
                before.LegacyCosts == 0 &&
                before.LegacyRuns == 0)
            {
                await InstallCompletedBackfillGuardsAsync(
                    dbContext,
                    cancellationToken);
                await RecordCompletedBackfillAsync(
                    dbContext,
                    options,
                    before,
                    cancellationToken);
                if (options.Apply)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                stopwatch.Stop();
                logger.LogInformation(
                    "Legacy tenant backfill {Mode}. Tenant {TenantId}; resources 0, costs 0, ETL runs 0. Elapsed time: {ElapsedMilliseconds} ms.",
                    options.Apply ? "applied" : "dry-run completed",
                    options.TenantId,
                    stopwatch.ElapsedMilliseconds);
                return;
            }

            await AssertNoNormalizationCollisionsAsync(
                dbContext,
                options,
                cancellationToken);
            await EnsureDevelopmentOwnershipAsync(
                dbContext,
                options,
                cancellationToken);

            var resourceUpdates = await ExecuteNonQueryAsync(
                dbContext,
                """
                UPDATE cloud_resources
                SET tenant_id = @tenant_id,
                    provider = lower(btrim(provider))
                WHERE tenant_id IS NULL;
                """,
                cancellationToken,
                ("tenant_id", options.TenantId));
            var costUpdates = await ExecuteNonQueryAsync(
                dbContext,
                """
                UPDATE cloud_cost_daily
                SET tenant_id = @tenant_id,
                    provider = lower(btrim(provider))
                WHERE tenant_id IS NULL;
                """,
                cancellationToken,
                ("tenant_id", options.TenantId));
            var runUpdates = await ExecuteNonQueryAsync(
                dbContext,
                """
                UPDATE etl_job_runs
                SET tenant_id = @tenant_id,
                    provider = lower(btrim(provider))
                WHERE tenant_id IS NULL;
                """,
                cancellationToken,
                ("tenant_id", options.TenantId));

            var after = await ReadCountsAsync(dbContext, cancellationToken);
            if (before.TotalResources != after.TotalResources ||
                before.TotalCosts != after.TotalCosts ||
                before.TotalRuns != after.TotalRuns)
            {
                throw new InvalidOperationException(
                    "Backfill changed core table row counts.");
            }

            if (after.LegacyResources != 0 ||
                after.LegacyCosts != 0 ||
                after.LegacyRuns != 0)
            {
                throw new InvalidOperationException(
                    "Backfill left one or more legacy rows without a tenant.");
            }

            await InstallCompletedBackfillGuardsAsync(
                dbContext,
                cancellationToken);
            await RecordCompletedBackfillAsync(
                dbContext,
                options,
                before,
                cancellationToken);

            if (options.Apply)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            stopwatch.Stop();
            logger.LogInformation(
                "Legacy tenant backfill {Mode}. Tenant {TenantId}; resources {ResourceUpdates}, costs {CostUpdates}, ETL runs {RunUpdates}. Elapsed time: {ElapsedMilliseconds} ms.",
                options.Apply ? "applied" : "dry-run completed",
                options.TenantId,
                resourceUpdates,
                costUpdates,
                runUpdates,
                stopwatch.ElapsedMilliseconds);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private static void ValidateOptions(LegacyTenantBackfillOptions options)
    {
        if (!options.Enabled)
        {
            throw new InvalidOperationException(
                "LegacyTenantBackfill:Enabled must be explicitly set to true.");
        }

        if (!options.LegacyWritersStopped)
        {
            throw new InvalidOperationException(
                "Confirm that all pre-Day24 API and Worker writers are stopped.");
        }

        if (options.MaximumLegacyRows is < 1 or > 10_000_000)
        {
            throw new InvalidOperationException(
                "MaximumLegacyRows must be between 1 and 10,000,000.");
        }

        if (options.Apply &&
            (string.IsNullOrWhiteSpace(options.DatabaseConfirmation) ||
             options.ExpectedResourceRows < 0 ||
             options.ExpectedCostRows < 0 ||
             options.ExpectedEtlRunRows < 0))
        {
            throw new InvalidOperationException(
                "Apply requires the target database name and all dry-run row counts.");
        }

        ArgumentOutOfRangeException.ThrowIfEqual(
            options.OrganizationId,
            Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(options.TenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            options.OrganizationDisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.TenantSlug);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.TenantDisplayName);
        if (options.OrganizationDisplayName.Trim().Length > 256 ||
            options.TenantDisplayName.Trim().Length > 256)
        {
            throw new InvalidOperationException(
                "Legacy backfill display names cannot exceed 256 characters.");
        }

        var slug = options.TenantSlug;
        if (slug.Length is < 3 or > 63 ||
            slug[0] == '-' ||
            slug[^1] == '-' ||
            !slug.All(character =>
                char.IsAsciiLetterOrDigit(character) || character == '-') ||
            !string.Equals(slug, slug.ToLowerInvariant(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Legacy development Tenant slug is invalid.");
        }
    }

    private static async Task ValidateTargetAndCountsAsync(
        FinOpsDbContext dbContext,
        LegacyTenantBackfillOptions options,
        BackfillCounts counts,
        CancellationToken cancellationToken)
    {
        var totalLegacyRows =
            counts.LegacyResources + counts.LegacyCosts + counts.LegacyRuns;
        if (totalLegacyRows > options.MaximumLegacyRows)
        {
            throw new InvalidOperationException(
                $"Legacy row count {totalLegacyRows} exceeds the approved maximum {options.MaximumLegacyRows}.");
        }

        if (!options.Apply)
        {
            return;
        }

        var databaseName = await ExecuteScalarAsync<string>(
            dbContext,
            "SELECT current_database();",
            cancellationToken);
        if (!string.Equals(
            databaseName,
            options.DatabaseConfirmation,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Target database confirmation does not match the connected database.");
        }

        if (counts.LegacyResources != options.ExpectedResourceRows ||
            counts.LegacyCosts != options.ExpectedCostRows ||
            counts.LegacyRuns != options.ExpectedEtlRunRows)
        {
            throw new InvalidOperationException(
                "Legacy row counts changed after dry-run; review the new data before applying.");
        }
    }

    private static async Task AssertNoNormalizationCollisionsAsync(
        FinOpsDbContext dbContext,
        LegacyTenantBackfillOptions options,
        CancellationToken cancellationToken)
    {
        var resourceCollision = await ExecuteScalarAsync<bool>(
            dbContext,
            """
            SELECT EXISTS (
                SELECT 1
                FROM cloud_resources
                WHERE tenant_id IS NULL
                GROUP BY lower(btrim(provider)), resource_id_normalized
                HAVING count(*) > 1);
            """,
            cancellationToken);
        if (resourceCollision)
        {
            throw new InvalidOperationException(
                "Legacy resource rows collide after Provider normalization.");
        }

        var invalidProvider = await ExecuteScalarAsync<bool>(
            dbContext,
            """
            SELECT EXISTS (
                SELECT 1
                FROM (
                    SELECT provider FROM cloud_resources WHERE tenant_id IS NULL
                    UNION ALL
                    SELECT provider FROM cloud_cost_daily WHERE tenant_id IS NULL
                    UNION ALL
                    SELECT provider FROM etl_job_runs WHERE tenant_id IS NULL
                ) AS legacy
                WHERE length(btrim(provider)) NOT BETWEEN 1 AND 32);
            """,
            cancellationToken);
        if (invalidProvider)
        {
            throw new InvalidOperationException(
                "A legacy Provider is empty or exceeds the supported length.");
        }

        var costCollision = await ExecuteScalarAsync<bool>(
            dbContext,
            """
            SELECT EXISTS (
                SELECT 1
                FROM cloud_cost_daily
                WHERE tenant_id IS NULL
                GROUP BY
                    lower(btrim(provider)),
                    account_id,
                    usage_date,
                    service_name,
                    resource_group,
                    currency
                HAVING count(*) > 1);
            """,
            cancellationToken);
        if (costCollision)
        {
            throw new InvalidOperationException(
                "Legacy cost rows collide after Provider normalization.");
        }

        var ownershipConflict = await ExecuteScalarAsync<bool>(
            dbContext,
            """
            WITH legacy_accounts AS (
                SELECT DISTINCT lower(btrim(provider)) AS provider, account_id
                FROM cloud_resources
                WHERE tenant_id IS NULL
                UNION
                SELECT DISTINCT lower(btrim(provider)) AS provider, account_id
                FROM cloud_cost_daily
                WHERE tenant_id IS NULL
            )
            SELECT EXISTS (
                SELECT 1
                FROM legacy_accounts AS legacy
                JOIN cloud_accounts AS account
                  ON account.provider = legacy.provider
                 AND account.external_account_id = legacy.account_id
                WHERE account.tenant_id <> @tenant_id);
            """,
            cancellationToken,
            ("tenant_id", options.TenantId));
        if (ownershipConflict)
        {
            throw new InvalidOperationException(
                "A legacy Provider account is already owned by another Tenant.");
        }
    }

    private static async Task EnsureDevelopmentOwnershipAsync(
        FinOpsDbContext dbContext,
        LegacyTenantBackfillOptions options,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await ExecuteNonQueryAsync(
            dbContext,
            """
            INSERT INTO organizations
                (id, display_name, status, created_at, updated_at)
            VALUES
                (@organization_id, @organization_name, 'Active', @now, @now)
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO tenants
                (id, organization_id, slug, display_name, status, created_at, updated_at)
            VALUES
                (@tenant_id, @organization_id, @tenant_slug, @tenant_name,
                 'Active', @now, @now)
            ON CONFLICT (id) DO NOTHING;
            """,
            cancellationToken,
            ("organization_id", options.OrganizationId),
            ("organization_name", options.OrganizationDisplayName.Trim()),
            ("tenant_id", options.TenantId),
            ("tenant_slug", options.TenantSlug),
            ("tenant_name", options.TenantDisplayName.Trim()),
            ("now", now));

        var tenantMatches = await ExecuteScalarAsync<bool>(
            dbContext,
            """
            SELECT EXISTS (
                SELECT 1
                FROM tenants AS tenant
                JOIN organizations AS organization
                  ON organization.id = tenant.organization_id
                WHERE tenant.id = @tenant_id
                  AND tenant.organization_id = @organization_id
                  AND tenant.slug = @tenant_slug
                  AND tenant.display_name = @tenant_name
                  AND tenant.status = 'Active'
                  AND organization.display_name = @organization_name
                  AND organization.status = 'Active');
            """,
            cancellationToken,
            ("tenant_id", options.TenantId),
            ("organization_id", options.OrganizationId),
            ("tenant_slug", options.TenantSlug),
            ("tenant_name", options.TenantDisplayName.Trim()),
            ("organization_name", options.OrganizationDisplayName.Trim()));
        if (!tenantMatches)
        {
            throw new InvalidOperationException(
                "The requested development Tenant ID conflicts with existing data.");
        }

        await ExecuteNonQueryAsync(
            dbContext,
            """
            WITH legacy_providers AS (
                SELECT DISTINCT lower(btrim(provider)) AS provider
                FROM cloud_resources
                WHERE tenant_id IS NULL
                UNION
                SELECT DISTINCT lower(btrim(provider)) AS provider
                FROM cloud_cost_daily
                WHERE tenant_id IS NULL
                UNION
                SELECT DISTINCT lower(btrim(provider)) AS provider
                FROM etl_job_runs
                WHERE tenant_id IS NULL
            )
            INSERT INTO provider_connections
                (id, tenant_id, provider, display_name, credential_reference,
                 status, created_at, updated_at)
            SELECT
                gen_random_uuid(),
                @tenant_id,
                provider,
                'Legacy ' || provider || ' backfill',
                'development-backfill://' || provider,
                'Active',
                @now,
                @now
            FROM legacy_providers
            ON CONFLICT (tenant_id, provider, credential_reference) DO NOTHING;

            WITH legacy_accounts AS (
                SELECT DISTINCT lower(btrim(provider)) AS provider, account_id
                FROM cloud_resources
                WHERE tenant_id IS NULL
                UNION
                SELECT DISTINCT lower(btrim(provider)) AS provider, account_id
                FROM cloud_cost_daily
                WHERE tenant_id IS NULL
            )
            INSERT INTO cloud_accounts
                (id, tenant_id, provider_connection_id, provider,
                 external_account_id, display_name, environment, status,
                 created_at, updated_at)
            SELECT
                gen_random_uuid(),
                @tenant_id,
                connection.id,
                legacy.provider,
                legacy.account_id,
                'Legacy account ' || legacy.account_id,
                'development',
                'Active',
                @now,
                @now
            FROM legacy_accounts AS legacy
            JOIN provider_connections AS connection
              ON connection.tenant_id = @tenant_id
             AND connection.provider = legacy.provider
             AND connection.credential_reference =
                 'development-backfill://' || legacy.provider
            ON CONFLICT (provider, external_account_id) DO NOTHING;
            """,
            cancellationToken,
            ("tenant_id", options.TenantId),
            ("now", now));

        var allAccountsOwned = await ExecuteScalarAsync<bool>(
            dbContext,
            """
            WITH legacy_accounts AS (
                SELECT DISTINCT lower(btrim(provider)) AS provider, account_id
                FROM cloud_resources
                WHERE tenant_id IS NULL
                UNION
                SELECT DISTINCT lower(btrim(provider)) AS provider, account_id
                FROM cloud_cost_daily
                WHERE tenant_id IS NULL
            )
            SELECT NOT EXISTS (
                SELECT 1
                FROM legacy_accounts AS legacy
                LEFT JOIN cloud_accounts AS account
                  ON account.tenant_id = @tenant_id
                 AND account.provider = legacy.provider
                 AND account.external_account_id = legacy.account_id
                WHERE account.id IS NULL);
            """,
            cancellationToken,
            ("tenant_id", options.TenantId));
        if (!allAccountsOwned)
        {
            throw new InvalidOperationException(
                "One or more legacy Provider accounts could not be onboarded.");
        }
    }

    private static Task<int> InstallCompletedBackfillGuardsAsync(
        FinOpsDbContext dbContext,
        CancellationToken cancellationToken) =>
        ExecuteNonQueryAsync(
            dbContext,
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'ck_cloud_resources_tenant_backfilled'
                ) THEN
                    ALTER TABLE cloud_resources
                    ADD CONSTRAINT ck_cloud_resources_tenant_backfilled
                    CHECK (tenant_id IS NOT NULL);
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'ck_cloud_cost_daily_tenant_backfilled'
                ) THEN
                    ALTER TABLE cloud_cost_daily
                    ADD CONSTRAINT ck_cloud_cost_daily_tenant_backfilled
                    CHECK (tenant_id IS NOT NULL);
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'ck_etl_job_runs_tenant_backfilled'
                ) THEN
                    ALTER TABLE etl_job_runs
                    ADD CONSTRAINT ck_etl_job_runs_tenant_backfilled
                    CHECK (tenant_id IS NOT NULL);
                END IF;
            END
            $$;
            """,
            cancellationToken);

    private static Task<int> RecordCompletedBackfillAsync(
        FinOpsDbContext dbContext,
        LegacyTenantBackfillOptions options,
        BackfillCounts counts,
        CancellationToken cancellationToken) =>
        ExecuteNonQueryAsync(
            dbContext,
            """
            INSERT INTO legacy_tenant_backfill_control
                (operation_name, tenant_id, completed_at,
                 resource_rows, cost_rows, etl_run_rows)
            VALUES
                ('day24-development-tenant-backfill', @tenant_id, @completed_at,
                 @resource_rows, @cost_rows, @etl_run_rows)
            ON CONFLICT (operation_name) DO UPDATE
            SET tenant_id = EXCLUDED.tenant_id,
                completed_at = EXCLUDED.completed_at,
                resource_rows = EXCLUDED.resource_rows,
                cost_rows = EXCLUDED.cost_rows,
                etl_run_rows = EXCLUDED.etl_run_rows;
            """,
            cancellationToken,
            ("tenant_id", options.TenantId),
            ("completed_at", DateTimeOffset.UtcNow),
            ("resource_rows", counts.LegacyResources),
            ("cost_rows", counts.LegacyCosts),
            ("etl_run_rows", counts.LegacyRuns));

    private static async Task<BackfillCounts> ReadCountsAsync(
        FinOpsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            dbContext,
            """
            SELECT
                (SELECT count(*) FROM cloud_resources),
                (SELECT count(*) FROM cloud_resources WHERE tenant_id IS NULL),
                (SELECT count(*) FROM cloud_cost_daily),
                (SELECT count(*) FROM cloud_cost_daily WHERE tenant_id IS NULL),
                (SELECT count(*) FROM etl_job_runs),
                (SELECT count(*) FROM etl_job_runs WHERE tenant_id IS NULL);
            """);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "Could not read legacy backfill row counts.");
        }

        return new BackfillCounts(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            reader.GetInt64(3),
            reader.GetInt64(4),
            reader.GetInt64(5));
    }

    private static async Task<T> ExecuteScalarAsync<T>(
        FinOpsDbContext dbContext,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = CreateCommand(
            dbContext,
            sql,
            parameters);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is not T typed)
        {
            throw new InvalidOperationException(
                $"Backfill query returned an unexpected {typeof(T).Name} result.");
        }

        return typed;
    }

    private static async Task<int> ExecuteNonQueryAsync(
        FinOpsDbContext dbContext,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = CreateCommand(
            dbContext,
            sql,
            parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DbCommand CreateCommand(
        FinOpsDbContext dbContext,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction =
            dbContext.Database.CurrentTransaction?.GetDbTransaction();
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        return command;
    }

    private sealed record BackfillCounts(
        long TotalResources,
        long LegacyResources,
        long TotalCosts,
        long LegacyCosts,
        long TotalRuns,
        long LegacyRuns);
}
