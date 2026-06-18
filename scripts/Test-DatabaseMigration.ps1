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
$suffix = "$PID$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"
$database = "finops_migration_$suffix"
$runtimeRole = "finops_runtime_$suffix"
$runtimeCredential = "finops_runtime_test_$suffix"
$apiPort = 5099
$apiProcess = $null
$lockJob = $null
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
        -Sql "DROP ROLE IF EXISTS $runtimeRole;" | Out-Null
    Invoke-PostgreSql `
        -TargetDatabase "postgres" `
        -Sql "CREATE DATABASE $database OWNER finops;" | Out-Null

    Write-Host "==> Empty database migration"
    $firstRunOutput = Invoke-Migrator -TargetDatabase $database
    if ($firstRunOutput -notmatch "Applied 3 migration\(s\)") {
        throw "The empty database run did not report exactly 3 applied migrations."
    }
    if ($firstRunOutput.Contains("finops_dev_password", [StringComparison]::Ordinal)) {
        throw "Migrator output exposed the database credential."
    }

    $migrationCount = [int](Invoke-PostgreSql `
        -TargetDatabase $database `
        -Sql 'SELECT count(*) FROM "__EFMigrationsHistory";' `
        -Scalar)
    if ($migrationCount -ne 3) {
        throw "Expected 3 migration history rows, found $migrationCount."
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
        ArgumentList = @(
            "run",
            "--no-launch-profile",
            "--no-build",
            "--project",
            $apiProject
        )
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
    & dotnet run `
        --no-launch-profile `
        --no-build `
        --project $workerProject
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

    Write-Host "Database migration verification passed." -ForegroundColor Green
}
finally {
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
            -Sql "DROP ROLE IF EXISTS $runtimeRole;" | Out-Null
    }
    catch {
        Write-Warning "Database migration test cleanup failed: $($_.Exception.Message)"
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
}
