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
$suffix = "$PID$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"
$database = "finops_migration_$suffix"
$secondDatabase = "finops_migration_alt_$suffix"
$runtimeRole = "finops_runtime_$suffix"
$runtimeCredential = "finops_runtime_test_$suffix"
$apiPort = 5099
$apiProcess = $null
$lockJob = $null
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

function Invoke-EfDatabaseUpdate {
    param(
        [Parameter(Mandatory)][string]$TargetDatabase,
        [Parameter(Mandatory)][string]$TargetMigration
    )

    $env:PostgreSql__Database = $TargetDatabase
    & dotnet tool run dotnet-ef database update $TargetMigration `
        --project (Join-Path $repositoryRoot "src/FinOps.Infrastructure/FinOps.Infrastructure.csproj") `
        --startup-project (Join-Path $repositoryRoot "src/FinOps.Infrastructure/FinOps.Infrastructure.csproj") `
        --context FinOpsDbContext `
        --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "EF database update to '$TargetMigration' failed."
    }
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

    Write-Host "==> Latest migration Down and reapply"
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
    if ($tenancyTableCount -ne 0) {
        throw "The latest migration Down path left tenancy tables behind."
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
        throw "The rolled-back tenancy migration was not reapplied."
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
