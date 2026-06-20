[CmdletBinding()]
param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$solution = Join-Path $repositoryRoot "FinOpsPlatform.slnx"
$migratorProject = Join-Path $repositoryRoot "src/FinOps.Migrator"
$apiProject = Join-Path $repositoryRoot "src/FinOps.Api"
$workerProject = Join-Path $repositoryRoot "src/FinOps.Worker"
$migratorAssembly = Join-Path $migratorProject "bin/Debug/net10.0/FinOps.Migrator.dll"
$apiAssembly = Join-Path $apiProject "bin/Debug/net10.0/FinOps.Api.dll"
$workerAssembly = Join-Path $workerProject "bin/Debug/net10.0/FinOps.Worker.dll"
$migrationsDirectory = Join-Path $repositoryRoot "src/FinOps.Infrastructure/Persistence/Migrations"
$migrationFiles = @(
    Get-ChildItem -LiteralPath $migrationsDirectory -Filter "*.cs" |
        Where-Object {
            $_.Name -notlike "*.Designer.cs" -and
            $_.Name -ne "FinOpsDbContextModelSnapshot.cs"
        } |
        Sort-Object Name
)
$expectedMigrationCount = $migrationFiles.Count
$previousMigration = $migrationFiles[-2].BaseName
$tenantFoundationMigration = $migrationFiles[-3].BaseName
$suffix = "$PID$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"
$database = "finops_migration_$suffix"
$secondDatabase = "finops_migration_alt_$suffix"
$runtimeRole = "finops_runtime_$suffix"
$runtimeCredential = "finops_runtime_test_$suffix"
$apiPort = 5099
$apiProcess = $null
$lockJob = $null
$backfillWriterJob = $null
$verificationError = $null
$tempDirectory = [IO.Path]::GetTempPath()
$apiStandardOutput = Join-Path $tempDirectory "finops-migration-api-$suffix.stdout.log"
$apiStandardError = Join-Path $tempDirectory "finops-migration-api-$suffix.stderr.log"
$previousEnvironment = @{
    Host = $env:PostgreSql__Host
    Port = $env:PostgreSql__Port
    Database = $env:PostgreSql__Database
    Username = $env:PostgreSql__Username
    Password = $env:PostgreSql__Password
    TimeoutSeconds = $env:PostgreSql__TimeoutSeconds
    ForceSampleData = $env:AzureCost__ForceSampleData
    Job = $env:Etl__Job
    TenantId = $env:Etl__TenantId
    TenantTestConnection = $env:FINOPS_TENANT_TEST_CONNECTION
    Urls = $env:ASPNETCORE_URLS
}

function Invoke-PostgreSql {
    param(
        [Parameter(Mandatory)][string]$TargetDatabase,
        [Parameter(Mandatory)][string]$Sql,
        [switch]$Scalar
    )

    $arguments = @(
        "exec",
        "finops-postgres",
        "psql",
        "-v",
        "ON_ERROR_STOP=1",
        "-U",
        "finops",
        "-d",
        $TargetDatabase
    )

    if ($Scalar) {
        $arguments += "-tAc"
    }
    else {
        $arguments += "-c"
    }

    $arguments += $Sql
    $output = & docker @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL command failed with code $LASTEXITCODE."
    }

    if ($Scalar) {
        return ([string]($output | Select-Object -Last 1)).Trim()
    }

    return $output
}

function Assert-PostgreSqlRejected {
    param(
        [Parameter(Mandatory)][string]$TargetDatabase,
        [Parameter(Mandatory)][string]$Sql,
        [Parameter(Mandatory)][string]$ExpectedError
    )

    $output = & docker exec finops-postgres psql `
        -v ON_ERROR_STOP=1 `
        -U finops `
        -d $TargetDatabase `
        -c $Sql 2>&1
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }

    if ($exitCode -eq 0) {
        throw "Expected PostgreSQL to reject the statement."
    }

    $message = $output -join [Environment]::NewLine
    if ($message -notmatch $ExpectedError) {
        throw (
            "PostgreSQL rejected the statement for an unexpected reason. " +
            "Expected error matching '$ExpectedError'."
        )
    }
}

function Invoke-Migrator {
    param(
        [Parameter(Mandatory)][string]$TargetDatabase,
        [switch]$ExpectFailure
    )

    $env:PostgreSql__Database = $TargetDatabase
    $arguments = @(
        "run",
        "--no-launch-profile",
        "--project",
        $migratorProject
    )
    if ($NoBuild) {
        $arguments += "--no-build"
    }

    $output = & dotnet @arguments 2>&1
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }

    if ($ExpectFailure) {
        if ($exitCode -ne 1) {
            throw "Expected Migrator exit code 1, got $exitCode."
        }
    }
    elseif ($exitCode -ne 0) {
        throw "Expected Migrator exit code 0, got $exitCode."
    }

    return ($output -join [Environment]::NewLine)
}

function Invoke-LegacyTenantBackfill {
    param(
        [Parameter(Mandatory)][string]$TargetDatabase,
        [switch]$Apply,
        [bool]$AllowNonZeroExit = $false,
        [string]$EnvironmentName = "Development",
        [bool]$WritersStopped = $true,
        [long]$ExpectedResourceRows = 0,
        [long]$ExpectedCostRows = 0,
        [long]$ExpectedEtlRunRows = 0,
        [long]$MaximumLegacyRows = 100000,
        [string]$DatabaseConfirmation
    )

    $previousBackfillEnvironment = @{
        DotnetEnvironment = $env:DOTNET_ENVIRONMENT
        Database = $env:PostgreSql__Database
        Enabled = $env:LegacyTenantBackfill__Enabled
        Apply = $env:LegacyTenantBackfill__Apply
        WritersStopped = $env:LegacyTenantBackfill__LegacyWritersStopped
        DatabaseConfirmation = $env:LegacyTenantBackfill__DatabaseConfirmation
        ExpectedResourceRows = $env:LegacyTenantBackfill__ExpectedResourceRows
        ExpectedCostRows = $env:LegacyTenantBackfill__ExpectedCostRows
        ExpectedEtlRunRows = $env:LegacyTenantBackfill__ExpectedEtlRunRows
        MaximumLegacyRows = $env:LegacyTenantBackfill__MaximumLegacyRows
        OrganizationId = $env:LegacyTenantBackfill__OrganizationId
        TenantId = $env:LegacyTenantBackfill__TenantId
        OrganizationDisplayName =
            $env:LegacyTenantBackfill__OrganizationDisplayName
        TenantSlug = $env:LegacyTenantBackfill__TenantSlug
        TenantDisplayName = $env:LegacyTenantBackfill__TenantDisplayName
    }

    try {
        $env:DOTNET_ENVIRONMENT = $EnvironmentName
        $env:PostgreSql__Database = $TargetDatabase
        $env:LegacyTenantBackfill__Enabled = "true"
        $env:LegacyTenantBackfill__Apply = $Apply.IsPresent.ToString()
        $env:LegacyTenantBackfill__LegacyWritersStopped =
            $WritersStopped.ToString()
        $env:LegacyTenantBackfill__DatabaseConfirmation = if (
            [string]::IsNullOrWhiteSpace($DatabaseConfirmation)
        ) {
            $TargetDatabase
        }
        else {
            $DatabaseConfirmation
        }
        $env:LegacyTenantBackfill__ExpectedResourceRows =
            $ExpectedResourceRows.ToString()
        $env:LegacyTenantBackfill__ExpectedCostRows =
            $ExpectedCostRows.ToString()
        $env:LegacyTenantBackfill__ExpectedEtlRunRows =
            $ExpectedEtlRunRows.ToString()
        $env:LegacyTenantBackfill__MaximumLegacyRows =
            $MaximumLegacyRows.ToString()
        $env:LegacyTenantBackfill__OrganizationId =
            "11000000-0000-0000-0000-000000000024"
        $env:LegacyTenantBackfill__TenantId =
            "22000000-0000-0000-0000-000000000024"
        $env:LegacyTenantBackfill__OrganizationDisplayName =
            "Backfill Integration Organization"
        $env:LegacyTenantBackfill__TenantSlug = "legacy-integration"
        $env:LegacyTenantBackfill__TenantDisplayName =
            "Legacy Integration Tenant"

        $output = & dotnet $migratorAssembly `
            --Operation=backfill-development-tenant 2>&1
        $exitCode = $LASTEXITCODE
        $output | ForEach-Object { Write-Host $_ }

        if ($AllowNonZeroExit) {
            if ($exitCode -ne 1) {
                throw "Expected legacy Tenant backfill exit code 1, got $exitCode."
            }
        }
        elseif ($exitCode -ne 0) {
            throw "Legacy Tenant backfill exited with code $exitCode."
        }

        return ($output -join [Environment]::NewLine)
    }
    finally {
        $env:DOTNET_ENVIRONMENT =
            $previousBackfillEnvironment.DotnetEnvironment
        $env:PostgreSql__Database = $previousBackfillEnvironment.Database
        $env:LegacyTenantBackfill__Enabled =
            $previousBackfillEnvironment.Enabled
        $env:LegacyTenantBackfill__Apply = $previousBackfillEnvironment.Apply
        $env:LegacyTenantBackfill__LegacyWritersStopped =
            $previousBackfillEnvironment.WritersStopped
        $env:LegacyTenantBackfill__DatabaseConfirmation =
            $previousBackfillEnvironment.DatabaseConfirmation
        $env:LegacyTenantBackfill__ExpectedResourceRows =
            $previousBackfillEnvironment.ExpectedResourceRows
        $env:LegacyTenantBackfill__ExpectedCostRows =
            $previousBackfillEnvironment.ExpectedCostRows
        $env:LegacyTenantBackfill__ExpectedEtlRunRows =
            $previousBackfillEnvironment.ExpectedEtlRunRows
        $env:LegacyTenantBackfill__MaximumLegacyRows =
            $previousBackfillEnvironment.MaximumLegacyRows
        $env:LegacyTenantBackfill__OrganizationId =
            $previousBackfillEnvironment.OrganizationId
        $env:LegacyTenantBackfill__TenantId =
            $previousBackfillEnvironment.TenantId
        $env:LegacyTenantBackfill__OrganizationDisplayName =
            $previousBackfillEnvironment.OrganizationDisplayName
        $env:LegacyTenantBackfill__TenantSlug =
            $previousBackfillEnvironment.TenantSlug
        $env:LegacyTenantBackfill__TenantDisplayName =
            $previousBackfillEnvironment.TenantDisplayName
    }
}

function Invoke-EfDatabaseUpdate {
    param(
        [Parameter(Mandatory)][string]$TargetDatabase,
        [Parameter(Mandatory)][string]$TargetMigration,
        [switch]$ExpectFailure
    )

    $env:PostgreSql__Database = $TargetDatabase
    $output = & dotnet tool run dotnet-ef database update $TargetMigration `
        --project (Join-Path $repositoryRoot "src/FinOps.Infrastructure/FinOps.Infrastructure.csproj") `
        --startup-project (Join-Path $repositoryRoot "src/FinOps.Infrastructure/FinOps.Infrastructure.csproj") `
        --context FinOpsDbContext `
        --no-build 2>&1
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    if ($ExpectFailure) {
        if ($exitCode -eq 0) {
            throw "Expected EF database update to '$TargetMigration' to fail."
        }
    }
    elseif ($exitCode -ne 0) {
        throw "EF database update to '$TargetMigration' failed."
    }

    return ($output -join [Environment]::NewLine)
}

function Wait-ForApiReadiness {
    param([Parameter(Mandatory)][System.Diagnostics.Process]$Process)

    $lastError = $null
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ($Process.HasExited) {
            break
        }

        try {
            $response = Invoke-WebRequest `
                -Uri "http://127.0.0.1:$apiPort/health" `
                -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            $lastError = $_.Exception.Message
            Start-Sleep -Milliseconds 250
        }
    }

    $standardOutput = if (Test-Path $apiStandardOutput) {
        Get-Content -LiteralPath $apiStandardOutput -Raw
    }
    else {
        ""
    }
    $standardError = if (Test-Path $apiStandardError) {
        Get-Content -LiteralPath $apiStandardError -Raw
    }
    else {
        ""
    }

    throw "API readiness failed. Last error: $lastError`nstdout:`n$standardOutput`nstderr:`n$standardError"
}

try {
    & docker compose up -d
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL container could not be started."
    }

    if (-not $NoBuild) {
        & dotnet build $solution
        if ($LASTEXITCODE -ne 0) {
            throw "The solution build failed."
        }
    }

    if (-not (Test-Path -LiteralPath $apiAssembly -PathType Leaf)) {
        throw "The API assembly does not exist: $apiAssembly"
    }

    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "The repository-local .NET tools could not be restored."
    }
    if (-not (Test-Path -LiteralPath $migratorAssembly -PathType Leaf)) {
        throw "The Migrator assembly does not exist: $migratorAssembly"
    }
    if (-not (Test-Path -LiteralPath $workerAssembly -PathType Leaf)) {
        throw "The Worker assembly does not exist: $workerAssembly"
    }

    $env:PostgreSql__Host = "localhost"
    $env:PostgreSql__Port = "5432"
    $env:PostgreSql__Username = "finops"
    $env:PostgreSql__Password = "finops_dev_password"
    $env:PostgreSql__TimeoutSeconds = "3"

    Invoke-PostgreSql `
        -TargetDatabase "postgres" `
        -Sql "DROP DATABASE IF EXISTS $database WITH (FORCE);" | Out-Null
    Invoke-PostgreSql `
        -TargetDatabase "postgres" `
        -Sql "DROP DATABASE IF EXISTS $secondDatabase WITH (FORCE);" | Out-Null
    Invoke-PostgreSql `
        -TargetDatabase "postgres" `
        -Sql "DROP ROLE IF EXISTS $runtimeRole;" | Out-Null
    Invoke-PostgreSql `
        -TargetDatabase "postgres" `
        -Sql "CREATE DATABASE $database OWNER finops;" | Out-Null
    Invoke-PostgreSql `
        -TargetDatabase "postgres" `
        -Sql "CREATE DATABASE $secondDatabase OWNER finops;" | Out-Null

    Write-Host "==> Empty database migration"
    $firstRunOutput = Invoke-Migrator -TargetDatabase $database
    if ($firstRunOutput -notmatch "Applied $expectedMigrationCount migration\(s\)") {
        throw (
            "The empty database run did not report exactly " +
            "$expectedMigrationCount applied migrations."
        )
    }
    if ($firstRunOutput.Contains("finops_dev_password", [StringComparison]::Ordinal)) {
        throw "Migrator output exposed the database credential."
    }

    $migrationCount = [int](Invoke-PostgreSql `
        -TargetDatabase $database `
        -Sql 'SELECT count(*) FROM "__EFMigrationsHistory";' `
        -Scalar)
    if ($migrationCount -ne $expectedMigrationCount) {
        throw (
            "Expected $expectedMigrationCount migration history rows, " +
            "found $migrationCount."
        )
    }

    Write-Host "==> Idempotent migration rerun"
    $secondRunOutput = Invoke-Migrator -TargetDatabase $database
    if ($secondRunOutput -notmatch "Applied 0 migration\(s\)") {
        throw "The repeat run did not report 0 applied migrations."
    }

    Write-Host "==> Empty legacy backfill no-op"
    $emptyBackfillOutput = Invoke-LegacyTenantBackfill `
        -TargetDatabase $database `
        -Apply
    if ($emptyBackfillOutput -notmatch "resources 0, costs 0, ETL runs 0") {
        throw "Empty legacy Tenant backfill did not report a no-op."
    }
    $emptyBackfillTenantCount = [int](Invoke-PostgreSql `
        -TargetDatabase $database `
        -Sql @"
SELECT count(*)
FROM tenants
WHERE id = '22000000-0000-0000-0000-000000000024';
"@ `
        -Scalar)
    if ($emptyBackfillTenantCount -ne 0) {
        throw "Empty legacy Tenant backfill created an unnecessary Tenant."
    }
    $emptyBackfillGuardCount = [int](Invoke-PostgreSql `
        -TargetDatabase $database `
        -Sql @"
SELECT count(*)
FROM pg_constraint
WHERE conname IN (
    'ck_cloud_resources_tenant_backfilled',
    'ck_cloud_cost_daily_tenant_backfilled',
    'ck_etl_job_runs_tenant_backfilled'
);
"@ `
        -Scalar)
    if ($emptyBackfillGuardCount -ne 3) {
        throw "Empty apply did not install all post-backfill NULL guards."
    }

    $productionBackfillOutput = Invoke-LegacyTenantBackfill `
        -TargetDatabase $database `
        -Apply `
        -EnvironmentName "Production" `
        -AllowNonZeroExit:$true
    if ($productionBackfillOutput -notmatch "restricted to the Development") {
        throw "Production environment did not reject legacy Tenant backfill."
    }

    $writerAcknowledgementOutput = Invoke-LegacyTenantBackfill `
        -TargetDatabase $database `
        -Apply `
        -WritersStopped $false `
        -AllowNonZeroExit:$true
    if ($writerAcknowledgementOutput -notmatch "pre-Day24 API and Worker") {
        throw "Missing old-writer acknowledgement did not reject backfill."
    }

    Write-Host "==> Concurrent migration rejection"
    $lockJob = Start-Job -ScriptBlock {
        param($TargetDatabase)

        & docker exec finops-postgres psql `
            -v ON_ERROR_STOP=1 `
            -U finops `
            -d $TargetDatabase `
            -c "SELECT pg_advisory_lock(hashtext('FinOps.Migrator'), hashtext(current_database())); SELECT pg_sleep(30);"
    } -ArgumentList $database

    $lockObserved = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        $lockCount = [int](Invoke-PostgreSql `
            -TargetDatabase $database `
            -Sql "SELECT count(*) FROM pg_locks WHERE locktype = 'advisory' AND granted AND database = (SELECT oid FROM pg_database WHERE datname = current_database());" `
            -Scalar)
        if ($lockCount -gt 0) {
            $lockObserved = $true
            break
        }

        Start-Sleep -Milliseconds 250
    }
    if (-not $lockObserved) {
        throw "The advisory lock holder did not start."
    }

    $concurrentOutput = Invoke-Migrator `
        -TargetDatabase $database `
        -ExpectFailure
    if ($concurrentOutput -notmatch "Another FinOps database migration is already running") {
        throw "The concurrent Migrator did not report the lock conflict."
    }

    Write-Host "==> Different database migration isolation"
    $differentDatabaseOutput = Invoke-Migrator -TargetDatabase $secondDatabase
    if (
        $differentDatabaseOutput -notmatch
        "Applied $expectedMigrationCount migration\(s\)"
    ) {
        throw "A migration lock in one database incorrectly blocked another database."
    }

    Write-Host "==> Backfill control migration Down and reapply"
    Invoke-EfDatabaseUpdate `
        -TargetDatabase $secondDatabase `
        -TargetMigration $previousMigration

    $tenancyTableCount = [int](Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
SELECT count(*)
FROM (
    VALUES
        ('organizations'),
        ('tenants'),
        ('provider_connections'),
        ('cloud_accounts'),
        ('memberships')
) AS expected(name)
WHERE to_regclass('public.' || expected.name) IS NOT NULL;
"@ `
        -Scalar)
    if ($tenancyTableCount -ne 5) {
        throw "The latest migration Down path damaged the tenancy foundation tables."
    }

    $tenantColumnCount = [int](Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
SELECT count(*)
FROM information_schema.columns
WHERE table_schema = 'public'
  AND column_name = 'tenant_id'
  AND table_name IN ('cloud_resources', 'cloud_cost_daily', 'etl_job_runs');
"@ `
        -Scalar)
    if ($tenantColumnCount -ne 3) {
        throw "The control migration Down path damaged core tenant columns."
    }
    $controlTableExists = Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql "SELECT to_regclass('public.legacy_tenant_backfill_control') IS NOT NULL;" `
        -Scalar
    if ($controlTableExists -ne "f") {
        throw "The unapplied backfill control table survived its Down path."
    }

    $rolledBackMigrationCount = [int](Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql 'SELECT count(*) FROM "__EFMigrationsHistory";' `
        -Scalar)
    if ($rolledBackMigrationCount -ne ($expectedMigrationCount - 1)) {
        throw (
            "Expected $($expectedMigrationCount - 1) history rows after rollback, " +
            "found $rolledBackMigrationCount."
        )
    }

    $reapplyOutput = Invoke-Migrator -TargetDatabase $secondDatabase
    if ($reapplyOutput -notmatch "Applied 1 migration\(s\)") {
        throw "The rolled-back backfill control migration was not reapplied."
    }

    Write-Host "==> Tenant-aware core migration Down and reapply"
    Invoke-EfDatabaseUpdate `
        -TargetDatabase $secondDatabase `
        -TargetMigration $tenantFoundationMigration
    $tenantColumnCount = [int](Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
SELECT count(*)
FROM information_schema.columns
WHERE table_schema = 'public'
  AND column_name = 'tenant_id'
  AND table_name IN ('cloud_resources', 'cloud_cost_daily', 'etl_job_runs');
"@ `
        -Scalar)
    if ($tenantColumnCount -ne 0) {
        throw "The tenant-aware core Down path left tenant columns behind."
    }

    $rolledBackMigrationCount = [int](Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql 'SELECT count(*) FROM "__EFMigrationsHistory";' `
        -Scalar)
    if ($rolledBackMigrationCount -ne ($expectedMigrationCount - 2)) {
        throw (
            "Expected $($expectedMigrationCount - 2) history rows after core rollback, " +
            "found $rolledBackMigrationCount."
        )
    }

    $reapplyOutput = Invoke-Migrator -TargetDatabase $secondDatabase
    if ($reapplyOutput -notmatch "Applied 2 migration\(s\)") {
        throw "The core tenant and backfill control migrations were not reapplied."
    }

    Write-Host "==> Rolling-deployment legacy uniqueness"
    $legacyIndexCount = [int](Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
SELECT count(*)
FROM pg_indexes
WHERE schemaname = 'public'
  AND indexname IN (
      'ux_cloud_resources_legacy_provider_resource_id',
      'ux_cloud_cost_daily_legacy_identity'
  )
  AND indexdef LIKE '%WHERE (tenant_id IS NULL)%';
"@ `
        -Scalar)
    if ($legacyIndexCount -ne 2) {
        throw "The nullable-tenant compatibility indexes are missing or unfiltered."
    }

    $cloudAccountConstraintCount = [int](Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
SELECT count(*)
FROM pg_constraint
WHERE conname = 'ak_cloud_accounts_tenant_provider_external'
  AND contype = 'u';
"@ `
        -Scalar)
    $duplicateCloudAccountIndexCount = [int](Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
SELECT count(*)
FROM pg_indexes
WHERE schemaname = 'public'
  AND indexname = 'ux_cloud_accounts_tenant_provider_external';
"@ `
        -Scalar)
    if (
        $cloudAccountConstraintCount -ne 1 -or
        $duplicateCloudAccountIndexCount -ne 0
    ) {
        throw "Cloud account composite uniqueness is not represented by exactly one alternate key."
    }

    Assert-PostgreSqlRejected `
        -TargetDatabase $secondDatabase `
        -ExpectedError "duplicate key value violates unique constraint `"ux_cloud_resources_legacy_provider_resource_id`"" `
        -Sql @"
BEGIN;
INSERT INTO cloud_resources
    (id, provider, account_id, resource_id, resource_id_normalized, resource_name,
     resource_type, region, tags_json, first_seen_at, last_seen_at)
VALUES
    ('60000000-0000-0000-0000-000000000001', 'azure', 'legacy-account',
     '/legacy/resource', '/LEGACY/RESOURCE', 'Legacy resource', 'demo/type',
     'australiaeast', '{}', '2026-06-19T00:00:00Z', '2026-06-19T00:00:00Z'),
    ('60000000-0000-0000-0000-000000000002', 'azure', 'legacy-account',
     '/legacy/resource', '/LEGACY/RESOURCE', 'Legacy resource duplicate', 'demo/type',
     'australiaeast', '{}', '2026-06-19T00:00:00Z', '2026-06-19T00:00:00Z');
COMMIT;
"@

    Assert-PostgreSqlRejected `
        -TargetDatabase $secondDatabase `
        -ExpectedError "duplicate key value violates unique constraint `"ux_cloud_cost_daily_legacy_identity`"" `
        -Sql @"
BEGIN;
INSERT INTO cloud_cost_daily
    (id, provider, account_id, usage_date, service_name, resource_group,
     cost, currency, raw_json)
VALUES
    ('70000000-0000-0000-0000-000000000001', 'azure', 'legacy-account',
     '2026-06-19', 'Storage', 'legacy-rg', 1, 'NZD', '{}'),
    ('70000000-0000-0000-0000-000000000002', 'azure', 'legacy-account',
     '2026-06-19', 'Storage', 'legacy-rg', 2, 'NZD', '{}');
COMMIT;
"@

    Write-Host "==> Repeatable legacy Tenant backfill"
    Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
INSERT INTO cloud_resources
    (id, provider, account_id, resource_id, resource_id_normalized,
     resource_name, resource_type, region, tags_json, first_seen_at, last_seen_at)
VALUES
    ('61000000-0000-0000-0000-000000000001', 'Azure', 'legacy-subscription',
     '/legacy/resource', '/LEGACY/RESOURCE', 'Legacy resource', 'demo/type',
     'australiaeast', '{}', '2026-06-19T00:00:00Z', '2026-06-19T00:00:00Z');

INSERT INTO cloud_cost_daily
    (id, provider, account_id, usage_date, service_name, resource_group,
     cost, currency, raw_json)
VALUES
    ('71000000-0000-0000-0000-000000000001', 'Azure',
     'legacy-subscription', '2026-06-19', 'Storage', 'legacy-rg',
     12.5, 'NZD', '{}');

INSERT INTO etl_job_runs
    (id, job_name, provider, started_at, status, records_processed)
VALUES
    ('81000000-0000-0000-0000-000000000001', 'legacy-sync', 'Azure',
     '2026-06-19T00:00:00Z', 'Succeeded', 2);
"@ | Out-Null

    Write-Host "==> Active writer blocks legacy Tenant backfill"
    $backfillWriterJob = Start-Job -ScriptBlock {
        param($TargetDatabase)

        & docker exec finops-postgres psql `
            -v ON_ERROR_STOP=1 `
            -U finops `
            -d $TargetDatabase `
            -c "BEGIN; LOCK TABLE cloud_resources IN ROW EXCLUSIVE MODE; SELECT pg_sleep(30); COMMIT;"
    } -ArgumentList $secondDatabase

    $writerLockObserved = $false
    $writerBackendPid = 0
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        $writerBackendPid = [int](Invoke-PostgreSql `
            -TargetDatabase $secondDatabase `
            -Sql @"
SELECT COALESCE(max(lock.pid), 0)
FROM pg_locks AS lock
JOIN pg_class AS relation ON relation.oid = lock.relation
WHERE relation.relname = 'cloud_resources'
  AND lock.mode = 'RowExclusiveLock'
  AND lock.granted;
"@ `
            -Scalar)
        if ($writerBackendPid -gt 0) {
            $writerLockObserved = $true
            break
        }

        Start-Sleep -Milliseconds 250
    }
    if (-not $writerLockObserved) {
        throw "The simulated legacy writer did not acquire its table lock."
    }

    $activeWriterOutput = Invoke-LegacyTenantBackfill `
        -TargetDatabase $secondDatabase `
        -AllowNonZeroExit:$true
    if ($activeWriterOutput -notmatch "could not obtain lock on relation") {
        throw "An active legacy writer did not fail the backfill immediately."
    }
    Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql "SELECT pg_terminate_backend($writerBackendPid);" | Out-Null
    Stop-Job $backfillWriterJob -ErrorAction SilentlyContinue
    Receive-Job $backfillWriterJob -ErrorAction SilentlyContinue | Write-Host
    Remove-Job $backfillWriterJob -Force -ErrorAction SilentlyContinue
    $backfillWriterJob = $null

    Invoke-LegacyTenantBackfill -TargetDatabase $secondDatabase | Out-Null
    $dryRunState = Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
SELECT
    (SELECT count(*) FROM cloud_resources WHERE tenant_id IS NULL),
    (SELECT count(*) FROM cloud_cost_daily WHERE tenant_id IS NULL),
    (SELECT count(*) FROM etl_job_runs WHERE tenant_id IS NULL),
    (SELECT count(*) FROM tenants
     WHERE id = '22000000-0000-0000-0000-000000000024');
"@ `
        -Scalar
    if ($dryRunState -ne "1|1|1|0") {
        throw "Legacy Tenant backfill dry-run changed persisted data: $dryRunState"
    }

    $staleCountOutput = Invoke-LegacyTenantBackfill `
        -TargetDatabase $secondDatabase `
        -Apply `
        -ExpectedResourceRows 0 `
        -ExpectedCostRows 1 `
        -ExpectedEtlRunRows 1 `
        -AllowNonZeroExit:$true
    if ($staleCountOutput -notmatch "row counts changed after dry-run") {
        throw "Stale dry-run counts did not reject the apply operation."
    }

    $maximumRowsOutput = Invoke-LegacyTenantBackfill `
        -TargetDatabase $secondDatabase `
        -Apply `
        -ExpectedResourceRows 1 `
        -ExpectedCostRows 1 `
        -ExpectedEtlRunRows 1 `
        -MaximumLegacyRows 2 `
        -AllowNonZeroExit:$true
    if ($maximumRowsOutput -notmatch "exceeds the approved maximum") {
        throw "Legacy row maximum did not reject an oversized apply operation."
    }

    $wrongDatabaseOutput = Invoke-LegacyTenantBackfill `
        -TargetDatabase $secondDatabase `
        -Apply `
        -ExpectedResourceRows 1 `
        -ExpectedCostRows 1 `
        -ExpectedEtlRunRows 1 `
        -DatabaseConfirmation "wrong_database" `
        -AllowNonZeroExit:$true
    if ($wrongDatabaseOutput -notmatch "database confirmation does not match") {
        throw "Wrong target database confirmation did not reject apply."
    }

    Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
INSERT INTO cloud_resources
    (id, provider, account_id, resource_id, resource_id_normalized,
     resource_name, resource_type, region, tags_json, first_seen_at, last_seen_at)
VALUES
    ('61000000-0000-0000-0000-000000000002', 'Azure',
     'collision-account', '/collision/a', '/COLLISION',
     'Collision A', 'demo/type', 'australiaeast', '{}',
     '2026-06-19T00:00:00Z', '2026-06-19T00:00:00Z'),
    ('61000000-0000-0000-0000-000000000003', 'azure',
     'collision-account', '/collision/b', '/COLLISION',
     'Collision B', 'demo/type', 'australiaeast', '{}',
     '2026-06-19T00:00:00Z', '2026-06-19T00:00:00Z');
"@ | Out-Null
    $collisionOutput = Invoke-LegacyTenantBackfill `
        -TargetDatabase $secondDatabase `
        -Apply `
        -ExpectedResourceRows 3 `
        -ExpectedCostRows 1 `
        -ExpectedEtlRunRows 1 `
        -AllowNonZeroExit:$true
    if ($collisionOutput -notmatch "collide after Provider normalization") {
        throw "Provider normalization collision was not reported."
    }

    $collisionRows = [int](Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
SELECT count(*)
FROM cloud_resources
WHERE tenant_id IS NULL
  AND resource_id_normalized = '/COLLISION';
"@ `
        -Scalar)
    if ($collisionRows -ne 2) {
        throw "Failed backfill did not preserve all collision rows."
    }
    Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
DELETE FROM cloud_resources
WHERE tenant_id IS NULL
  AND resource_id_normalized = '/COLLISION';
"@ | Out-Null

    Invoke-LegacyTenantBackfill `
        -TargetDatabase $secondDatabase `
        -Apply `
        -ExpectedResourceRows 1 `
        -ExpectedCostRows 1 `
        -ExpectedEtlRunRows 1 | Out-Null
    $appliedState = Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
SELECT
    (SELECT count(*) FROM cloud_resources),
    (SELECT count(*) FROM cloud_cost_daily),
    (SELECT count(*) FROM etl_job_runs),
    (SELECT count(*) FROM cloud_resources WHERE tenant_id IS NULL),
    (SELECT count(*) FROM cloud_cost_daily WHERE tenant_id IS NULL),
    (SELECT count(*) FROM etl_job_runs WHERE tenant_id IS NULL),
    (SELECT count(*) FROM provider_connections
     WHERE tenant_id = '22000000-0000-0000-0000-000000000024'),
    (SELECT count(*) FROM cloud_accounts
     WHERE tenant_id = '22000000-0000-0000-0000-000000000024'),
    (SELECT provider FROM cloud_resources
     WHERE id = '61000000-0000-0000-0000-000000000001');
"@ `
        -Scalar
    if ($appliedState -ne "1|1|1|0|0|0|1|1|azure") {
        throw "Legacy Tenant backfill result was unexpected: $appliedState"
    }
    $backfillGuardCount = [int](Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
SELECT count(*)
FROM pg_constraint
WHERE conname IN (
    'ck_cloud_resources_tenant_backfilled',
    'ck_cloud_cost_daily_tenant_backfilled',
    'ck_etl_job_runs_tenant_backfilled'
);
"@ `
        -Scalar)
    if ($backfillGuardCount -ne 3) {
        throw "Applied backfill did not install all NULL Tenant guards."
    }
    $backfillMarkerState = Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
SELECT tenant_id::text || '|' ||
       resource_rows::text || '|' ||
       cost_rows::text || '|' ||
       etl_run_rows::text
FROM legacy_tenant_backfill_control
WHERE operation_name = 'day24-development-tenant-backfill';
"@ `
        -Scalar
    if (
        $backfillMarkerState -ne
        "22000000-0000-0000-0000-000000000024|1|1|1"
    ) {
        throw "Persistent Day24 backfill completion marker is missing or invalid."
    }

    $blockedDownOutput = Invoke-EfDatabaseUpdate `
        -TargetDatabase $secondDatabase `
        -TargetMigration $previousMigration `
        -ExpectFailure
    if ($blockedDownOutput -notmatch "Cannot roll back tenant-aware schema") {
        throw "Backfill completion did not report the controlled restore requirement."
    }
    $postDownAttemptState = Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
SELECT
    (SELECT count(*) FROM information_schema.columns
     WHERE table_name IN ('cloud_resources', 'cloud_cost_daily', 'etl_job_runs')
       AND column_name = 'tenant_id'),
    (SELECT count(*) FROM legacy_tenant_backfill_control
     WHERE operation_name = 'day24-development-tenant-backfill'),
    (SELECT count(*) FROM "__EFMigrationsHistory");
"@ `
        -Scalar
    if ($postDownAttemptState -ne "3|1|$expectedMigrationCount") {
        throw "Failed schema Down changed tenant columns, marker, or migration history."
    }

    Assert-PostgreSqlRejected `
        -TargetDatabase $secondDatabase `
        -ExpectedError "ck_etl_job_runs_tenant_backfilled" `
        -Sql @"
INSERT INTO etl_job_runs
    (id, job_name, provider, started_at, status, records_processed)
VALUES
    ('81000000-0000-0000-0000-000000000002', 'legacy-writer',
     'azure', '2026-06-19T00:00:00Z', 'Succeeded', 0);
"@

    $repeatBackfillOutput = Invoke-LegacyTenantBackfill `
        -TargetDatabase $secondDatabase `
        -Apply
    if (
        $repeatBackfillOutput -notmatch
        "resources 0, costs 0, ETL runs 0"
    ) {
        throw "Repeat legacy Tenant backfill was not a no-op."
    }

    Write-Host "==> Tenancy PostgreSQL constraint integration"
    $organizationId = "10000000-0000-0000-0000-000000000001"
    $tenantAId = "20000000-0000-0000-0000-000000000001"
    $tenantBId = "20000000-0000-0000-0000-000000000002"
    $connectionAId = "30000000-0000-0000-0000-000000000001"
    $connectionBId = "30000000-0000-0000-0000-000000000002"
    $accountAId = "40000000-0000-0000-0000-000000000001"
    $membershipAId = "50000000-0000-0000-0000-000000000001"
    $timestamp = "2026-06-19T00:00:00Z"

    Invoke-PostgreSql `
        -TargetDatabase $secondDatabase `
        -Sql @"
INSERT INTO organizations
    (id, display_name, status, created_at, updated_at)
VALUES
    ('$organizationId', 'Integration Organization', 'Active', '$timestamp', '$timestamp');

INSERT INTO tenants
    (id, organization_id, slug, display_name, status, created_at, updated_at)
VALUES
    ('$tenantAId', '$organizationId', 'tenant-a', 'Tenant A', 'Active', '$timestamp', '$timestamp'),
    ('$tenantBId', '$organizationId', 'tenant-b', 'Tenant B', 'Active', '$timestamp', '$timestamp');

INSERT INTO provider_connections
    (id, tenant_id, provider, display_name, credential_reference, status, created_at, updated_at)
VALUES
    ('$connectionAId', '$tenantAId', 'azure', 'Azure Primary', 'workload://tenant-a', 'Pending', '$timestamp', '$timestamp'),
    ('$connectionBId', '$tenantBId', 'azure', 'Azure Primary', 'workload://tenant-b', 'Pending', '$timestamp', '$timestamp');

INSERT INTO cloud_accounts
    (id, tenant_id, provider_connection_id, provider, external_account_id, display_name, status, created_at, updated_at)
VALUES
    ('$accountAId', '$tenantAId', '$connectionAId', 'azure', 'subscription-a', 'Subscription A', 'Pending', '$timestamp', '$timestamp');

INSERT INTO memberships
    (id, tenant_id, issuer, subject, subject_type, display_name, status, created_at, updated_at)
VALUES
    ('$membershipAId', '$tenantAId', 'https://issuer.example', 'subject-a', 'Human', 'Subject A', 'Invited', '$timestamp', '$timestamp'),
    ('50000000-0000-0000-0000-000000000003', '$tenantBId', 'https://issuer.example', 'subject-a', 'Human', 'Subject A in Tenant B', 'Invited', '$timestamp', '$timestamp');
"@ | Out-Null

    Assert-PostgreSqlRejected `
        -TargetDatabase $secondDatabase `
        -Sql @"
INSERT INTO cloud_accounts
    (id, tenant_id, provider_connection_id, provider, external_account_id, display_name, status, created_at, updated_at)
VALUES
    ('40000000-0000-0000-0000-000000000002', '$tenantAId', '$connectionBId', 'azure', 'subscription-b', 'Cross Tenant', 'Pending', '$timestamp', '$timestamp');
"@ `
        -ExpectedError "fk_cloud_accounts_provider_connection_scope"

    Assert-PostgreSqlRejected `
        -TargetDatabase $secondDatabase `
        -Sql @"
INSERT INTO cloud_accounts
    (id, tenant_id, provider_connection_id, provider, external_account_id, display_name, status, created_at, updated_at)
VALUES
    ('40000000-0000-0000-0000-000000000003', '$tenantAId', '$connectionAId', 'aws', 'account-a', 'Provider Mismatch', 'Pending', '$timestamp', '$timestamp');
"@ `
        -ExpectedError "fk_cloud_accounts_provider_connection_scope"

    Assert-PostgreSqlRejected `
        -TargetDatabase $secondDatabase `
        -Sql @"
INSERT INTO memberships
    (id, tenant_id, issuer, subject, subject_type, display_name, status, created_at, updated_at)
VALUES
    ('50000000-0000-0000-0000-000000000002', '$tenantAId', 'https://issuer.example', 'subject-a', 'Human', 'Duplicate Subject', 'Invited', '$timestamp', '$timestamp');
"@ `
        -ExpectedError "ux_memberships_tenant_issuer_subject"

    Assert-PostgreSqlRejected `
        -TargetDatabase $secondDatabase `
        -Sql @"
INSERT INTO cloud_accounts
    (id, tenant_id, provider_connection_id, provider, external_account_id, display_name, status, created_at, updated_at)
VALUES
    ('40000000-0000-0000-0000-000000000004', '$tenantBId', '$connectionBId', 'azure', 'subscription-a', 'Duplicate Provider Account', 'Pending', '$timestamp', '$timestamp');
"@ `
        -ExpectedError "ux_cloud_accounts_provider_external"

    Assert-PostgreSqlRejected `
        -TargetDatabase $secondDatabase `
        -Sql "DELETE FROM tenants WHERE id = '$tenantAId';" `
        -ExpectedError "FK_memberships_tenants_tenant_id"

    Stop-Job $lockJob -ErrorAction SilentlyContinue
    Receive-Job $lockJob -ErrorAction SilentlyContinue | Write-Host
    Remove-Job $lockJob -Force -ErrorAction SilentlyContinue
    $lockJob = $null

    Write-Host "==> Connection failure exit code"
    $env:PostgreSql__Port = "1"
    $env:PostgreSql__TimeoutSeconds = "1"
    Invoke-Migrator -TargetDatabase $database -ExpectFailure | Out-Null
    $env:PostgreSql__Port = "5432"
    $env:PostgreSql__TimeoutSeconds = "3"

    Write-Host "==> Explicit active Worker tenant"
    Invoke-PostgreSql `
        -TargetDatabase $database `
        -Sql @"
INSERT INTO organizations
    (id, display_name, status, created_at, updated_at)
VALUES
    ('10000000-0000-0000-0000-000000000001', 'Worker Organization', 'Active', '2026-06-19T00:00:00Z', '2026-06-19T00:00:00Z');

INSERT INTO tenants
    (id, organization_id, slug, display_name, status, created_at, updated_at)
VALUES
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'worker-tenant', 'Worker Tenant', 'Active', '2026-06-19T00:00:00Z', '2026-06-19T00:00:00Z');

INSERT INTO provider_connections
    (id, tenant_id, provider, display_name, credential_reference, status, created_at, updated_at)
VALUES
    ('30000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'azure', 'Worker Azure', 'development://worker', 'Active', '2026-06-19T00:00:00Z', '2026-06-19T00:00:00Z');

INSERT INTO cloud_accounts
    (id, tenant_id, provider_connection_id, provider, external_account_id, display_name, status, created_at, updated_at)
VALUES
    ('40000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'azure', 'sample-subscription', 'Worker Sample Subscription', 'Active', '2026-06-19T00:00:00Z', '2026-06-19T00:00:00Z');
"@ | Out-Null

    Write-Host "==> Tenant A/B Repository integration"
    $env:FINOPS_TENANT_TEST_CONNECTION = (
        "Host=localhost;Port=5432;Database=$database;" +
        "Username=finops;Password=finops_dev_password;Timeout=3"
    )
    & dotnet test `
        (Join-Path $repositoryRoot "src/FinOps.Tests/FinOps.Tests.csproj") `
        --no-build `
        --filter "FullyQualifiedName~TenantRepositoryIntegrationTests.Repositories_isolate"
    if ($LASTEXITCODE -ne 0) {
        throw "Tenant A/B Repository integration test failed."
    }
    $env:FINOPS_TENANT_TEST_CONNECTION = $null

    Write-Host "==> Restricted runtime role"
    Invoke-PostgreSql `
        -TargetDatabase "postgres" `
        -Sql "CREATE ROLE $runtimeRole LOGIN PASSWORD '$runtimeCredential';" | Out-Null
    Invoke-PostgreSql `
        -TargetDatabase "postgres" `
        -Sql "GRANT CONNECT ON DATABASE $database TO $runtimeRole;" | Out-Null
    Invoke-PostgreSql `
        -TargetDatabase $database `
        -Sql "GRANT USAGE ON SCHEMA public TO $runtimeRole; GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO $runtimeRole; REVOKE CREATE ON SCHEMA public FROM $runtimeRole;" | Out-Null

    $hasCreate = Invoke-PostgreSql `
        -TargetDatabase $database `
        -Sql "SELECT has_schema_privilege('$runtimeRole', 'public', 'CREATE');" `
        -Scalar
    if ($hasCreate -ne "f") {
        throw "The runtime role unexpectedly has schema CREATE permission."
    }

    $env:PostgreSql__Database = $database
    $env:PostgreSql__Username = $runtimeRole
    $env:PostgreSql__Password = $runtimeCredential
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$apiPort"
    Remove-Item -LiteralPath $apiStandardOutput, $apiStandardError `
        -Force `
        -ErrorAction SilentlyContinue

    $startProcessArguments = @{
        FilePath = "dotnet"
        ArgumentList = @($apiAssembly)
        WorkingDirectory = $repositoryRoot
        RedirectStandardOutput = $apiStandardOutput
        RedirectStandardError = $apiStandardError
        PassThru = $true
    }
    if ($IsWindows) {
        $startProcessArguments["WindowStyle"] = "Hidden"
    }

    $apiProcess = Start-Process @startProcessArguments
    Wait-ForApiReadiness -Process $apiProcess
    Write-Host "API readiness with restricted runtime role passed."

    if (-not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        $apiProcess.WaitForExit()
    }
    $apiProcess = $null

    $env:AzureCost__ForceSampleData = "true"
    $env:Etl__Job = "Costs"
    $env:Etl__TenantId = "20000000-0000-0000-0000-000000000001"
    & dotnet $workerAssembly
    if ($LASTEXITCODE -ne 0) {
        throw "Worker Costs failed with restricted runtime role."
    }

    $costRowCount = [int](Invoke-PostgreSql `
        -TargetDatabase $database `
        -Sql "SELECT count(*) FROM cloud_cost_daily;" `
        -Scalar)
    if ($costRowCount -le 0) {
        throw "Worker Costs did not write any rows."
    }

    Write-Host "==> Missing Worker tenant exit code"
    $env:Etl__TenantId = "00000000-0000-0000-0000-000000000000"
    $missingTenantOutput = & dotnet $workerAssembly 2>&1
    $missingTenantExitCode = $LASTEXITCODE
    $missingTenantOutput | ForEach-Object { Write-Host $_ }
    if ($missingTenantExitCode -ne 1) {
        throw "Expected missing-tenant Worker exit code 1, got $missingTenantExitCode."
    }
    if (
        ($missingTenantOutput -join [Environment]::NewLine) -notmatch
        "TenantId"
    ) {
        throw "The missing-tenant Worker failure did not identify TenantId."
    }
    $env:Etl__TenantId = "20000000-0000-0000-0000-000000000001"

    Write-Host "==> Unknown Worker job exit code"
    $env:Etl__Job = "Unknown"
    $unknownJobOutput = & dotnet $workerAssembly 2>&1
    $unknownJobExitCode = $LASTEXITCODE
    $unknownJobOutput | ForEach-Object { Write-Host $_ }
    if ($unknownJobExitCode -ne 1) {
        throw "Expected unknown Worker job exit code 1, got $unknownJobExitCode."
    }
    if (($unknownJobOutput -join [Environment]::NewLine) -notmatch "Unsupported ETL job") {
        throw "The unknown Worker job did not report the supported-job contract."
    }

    Write-Host "==> Worker handler failure exit code"
    $env:Etl__Job = "Costs"
    $env:PostgreSql__Port = "1"
    $env:PostgreSql__TimeoutSeconds = "1"
    $handlerFailureOutput = & dotnet $workerAssembly 2>&1
    $handlerFailureExitCode = $LASTEXITCODE
    $handlerFailureOutput | ForEach-Object { Write-Host $_ }
    if ($handlerFailureExitCode -ne 1) {
        throw "Expected Worker handler failure exit code 1, got $handlerFailureExitCode."
    }
    if (
        ($handlerFailureOutput -join [Environment]::NewLine) -notmatch
        "Azure ETL job Costs failed"
    ) {
        throw "The Worker handler failure did not report the job failure contract."
    }

    $env:PostgreSql__Port = "5432"
    $env:PostgreSql__TimeoutSeconds = "3"

    Write-Host "Database migration verification passed." -ForegroundColor Green
}
catch {
    $verificationError = $_.Exception
    throw
}
finally {
    $cleanupError = $null

    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
        $apiProcess.WaitForExit()
    }

    if ($lockJob) {
        Stop-Job $lockJob -ErrorAction SilentlyContinue
        Receive-Job $lockJob -ErrorAction SilentlyContinue | Write-Host
        Remove-Job $lockJob -Force -ErrorAction SilentlyContinue
    }
    if ($backfillWriterJob) {
        Stop-Job $backfillWriterJob -ErrorAction SilentlyContinue
        Receive-Job $backfillWriterJob -ErrorAction SilentlyContinue | Write-Host
        Remove-Job $backfillWriterJob -Force -ErrorAction SilentlyContinue
    }

    $env:PostgreSql__Host = "localhost"
    $env:PostgreSql__Port = "5432"
    $env:PostgreSql__Username = "finops"
    $env:PostgreSql__Password = "finops_dev_password"
    $env:PostgreSql__TimeoutSeconds = "3"
    try {
        Invoke-PostgreSql `
            -TargetDatabase "postgres" `
            -Sql "DROP DATABASE IF EXISTS $database WITH (FORCE);" | Out-Null
        Invoke-PostgreSql `
            -TargetDatabase "postgres" `
            -Sql "DROP DATABASE IF EXISTS $secondDatabase WITH (FORCE);" | Out-Null
        Invoke-PostgreSql `
            -TargetDatabase "postgres" `
            -Sql "DROP ROLE IF EXISTS $runtimeRole;" | Out-Null
    }
    catch {
        $cleanupError = $_.Exception
        Write-Warning "Database migration test cleanup failed: $($cleanupError.Message)"
    }

    $env:PostgreSql__Host = $previousEnvironment.Host
    $env:PostgreSql__Port = $previousEnvironment.Port
    $env:PostgreSql__Database = $previousEnvironment.Database
    $env:PostgreSql__Username = $previousEnvironment.Username
    $env:PostgreSql__Password = $previousEnvironment.Password
    $env:PostgreSql__TimeoutSeconds = $previousEnvironment.TimeoutSeconds
    $env:AzureCost__ForceSampleData = $previousEnvironment.ForceSampleData
    $env:Etl__Job = $previousEnvironment.Job
    $env:Etl__TenantId = $previousEnvironment.TenantId
    $env:FINOPS_TENANT_TEST_CONNECTION = $previousEnvironment.TenantTestConnection
    $env:ASPNETCORE_URLS = $previousEnvironment.Urls
    Remove-Item -LiteralPath $apiStandardOutput, $apiStandardError `
        -Force `
        -ErrorAction SilentlyContinue

    if ($cleanupError -and $verificationError) {
        Write-Warning (
            "Cleanup also failed after the primary verification failure: " +
            $cleanupError.Message
        )
    }
    elseif ($cleanupError) {
        throw "Database migration verification cleanup failed."
    }
}
