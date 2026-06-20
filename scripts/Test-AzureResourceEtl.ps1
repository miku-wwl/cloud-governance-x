[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)]
    [int]$Port = 5105,
    [ValidateRange(1024, 65535)]
    [int]$FailurePort = 5106,
    [ValidatePattern("^[a-z][a-z0-9_]{2,62}$")]
    [string]$Database = "finops_day5"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$apiProject = Join-Path $repositoryRoot "src/FinOps.Api"
$workerProject = Join-Path $repositoryRoot "src/FinOps.Worker"
$apiAssembly = Join-Path $apiProject "bin/Debug/net10.0/FinOps.Api.dll"
$workerAssembly = Join-Path $workerProject "bin/Debug/net10.0/FinOps.Worker.dll"
$solution = Join-Path $repositoryRoot "FinOpsPlatform.slnx"
$successLog = Join-Path $env:TEMP "finops-day5-success.log"
$failureLog = Join-Path $env:TEMP "finops-day5-failure.log"
$successErrorLog = Join-Path $env:TEMP "finops-day5-success.err.log"
$failureErrorLog = Join-Path $env:TEMP "finops-day5-failure.err.log"
$apiProcesses = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()
$tenantHeaders = @{
    "X-FinOps-Tenant-Id" = "20000000-0000-0000-0000-000000000001"
}
$verified = $false

function Invoke-PostgreSql {
    param(
        [Parameter(Mandatory)][string]$TargetDatabase,
        [Parameter(Mandatory)][string]$Sql,
        [switch]$Scalar
    )

    $arguments = @(
        "compose", "exec", "-T", "postgres",
        "psql", "-v", "ON_ERROR_STOP=1",
        "-U", "finops",
        "-d", $TargetDatabase
    )

    if ($Scalar) {
        $arguments += @("-t", "-A")
    }

    $arguments += @("-c", $Sql)
    $output = & docker @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL command failed."
    }

    return ($output | Out-String).Trim()
}

function Start-Day5Api {
    param(
        [Parameter(Mandatory)][int]$ApiPort,
        [Parameter(Mandatory)][string]$StandardOutputPath,
        [Parameter(Mandatory)][string]$StandardErrorPath,
        [string]$TenantId
    )

    $previousDatabase = $env:PostgreSql__Database
    $previousTenant = $env:Azure__TenantId
    $previousCredential = $env:AZURE_TOKEN_CREDENTIALS
    $previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
    $previousIdentityEnabled = $env:E2EIdentity__Enabled
    $previousIdentityIssuer = $env:E2EIdentity__Issuer
    $previousIdentitySubject = $env:E2EIdentity__Subject

    try {
        $env:PostgreSql__Database = $Database
        $env:Azure__TenantId = $TenantId
        $env:ASPNETCORE_ENVIRONMENT = "E2E"
        $env:E2EIdentity__Enabled = "true"
        $env:E2EIdentity__Issuer = "https://e2e.finops.local"
        $env:E2EIdentity__Subject = "e2e-operator"
        $env:AZURE_TOKEN_CREDENTIALS = if ($TenantId) {
            "AzureCliCredential"
        }
        else {
            $null
        }

        $startProcessArguments = @{
            FilePath = "dotnet"
            ArgumentList = @($apiAssembly, "--urls", "http://localhost:$ApiPort")
            WorkingDirectory = $apiProject
            RedirectStandardOutput = $StandardOutputPath
            RedirectStandardError = $StandardErrorPath
            PassThru = $true
        }
        if ($IsWindows) {
            $startProcessArguments["WindowStyle"] = "Hidden"
        }

        $process = Start-Process @startProcessArguments
        $apiProcesses.Add($process)
        return $process
    }
    finally {
        $env:PostgreSql__Database = $previousDatabase
        $env:Azure__TenantId = $previousTenant
        $env:AZURE_TOKEN_CREDENTIALS = $previousCredential
        $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
        $env:E2EIdentity__Enabled = $previousIdentityEnabled
        $env:E2EIdentity__Issuer = $previousIdentityIssuer
        $env:E2EIdentity__Subject = $previousIdentitySubject
    }
}

function Wait-Day5Api {
    param(
        [Parameter(Mandatory)][System.Diagnostics.Process]$Process,
        [Parameter(Mandatory)][string]$BaseUri
    )

    $deadline = (Get-Date).AddSeconds(60)

    do {
        Start-Sleep -Milliseconds 750

        if ($Process.HasExited) {
            throw "The API exited with code $($Process.ExitCode)."
        }

        try {
            $response = Invoke-WebRequest "$BaseUri/health" -TimeoutSec 10
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            $lastError = $_
        }
    }
    while ((Get-Date) -lt $deadline)

    throw "The API did not become ready: $lastError"
}

function Stop-Day5Api {
    param([System.Diagnostics.Process]$Process)

    if ($null -ne $Process -and -not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force
        $Process.WaitForExit()
    }
}

Remove-Item -LiteralPath @(
    $successLog,
    $failureLog,
    $successErrorLog,
    $failureErrorLog
) -Force -ErrorAction SilentlyContinue

try {
    & az account show --output none
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI is not authenticated. Run 'az login' first."
    }

    & docker compose up -d
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL container could not be started."
    }

    Invoke-PostgreSql `
        -TargetDatabase "postgres" `
        -Sql "DROP DATABASE IF EXISTS $Database WITH (FORCE);"
    Invoke-PostgreSql `
        -TargetDatabase "postgres" `
        -Sql "CREATE DATABASE $Database OWNER finops;"

    & dotnet build $solution
    if ($LASTEXITCODE -ne 0) {
        throw "The solution build failed."
    }

    foreach ($assembly in @($apiAssembly, $workerAssembly)) {
        if (-not (Test-Path -LiteralPath $assembly -PathType Leaf)) {
            throw "The required assembly does not exist: $assembly"
        }
    }

    & (Join-Path $repositoryRoot "scripts/Invoke-DatabaseMigration.ps1") `
        -Database $Database `
        -NoBuild

    & (Join-Path $repositoryRoot "scripts/Initialize-TestTenant.ps1") `
        -Database $Database

    $previousDatabase = $env:PostgreSql__Database
    $previousTenantId = $env:Etl__TenantId
    try {
        $env:PostgreSql__Database = $Database
        $env:Etl__TenantId = "20000000-0000-0000-0000-000000000001"
        Push-Location $workerProject
        try {
            & dotnet $workerAssembly
        }
        finally {
            Pop-Location
        }

        if ($LASTEXITCODE -ne 0) {
            throw "The Worker ETL run failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        $env:PostgreSql__Database = $previousDatabase
        $env:Etl__TenantId = $previousTenantId
    }

    $workerRunCount = [int](Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT count(*) FROM etl_job_runs WHERE job_name = 'azure-resource-sync' AND status = 'Succeeded';" `
        -Scalar)

    if ($workerRunCount -ne 1) {
        throw "The Worker did not persist exactly one successful ETL run."
    }

    $successBaseUri = "http://localhost:$Port"
    $successApi = Start-Day5Api `
        -ApiPort $Port `
        -StandardOutputPath $successLog `
        -StandardErrorPath $successErrorLog
    Wait-Day5Api -Process $successApi -BaseUri $successBaseUri

    $syncResult = Invoke-RestMethod `
        "$successBaseUri/api/admin/sync/azure/resources" `
        -Method Post `
        -Headers $tenantHeaders `
        -TimeoutSec 120

    $successfulRuns = Invoke-RestMethod `
        "$successBaseUri/api/admin/etl-runs?jobName=azure-resource-sync&take=10" `
        -Headers $tenantHeaders `
        -TimeoutSec 30
    $successfulRun = $null
    foreach ($run in $successfulRuns) {
        if ([string]$run.id -eq [string]$syncResult.jobRunId) {
            $successfulRun = $run
            break
        }
    }

    if ($null -eq $successfulRun) {
        throw "The successful ETL run was not returned by the history API."
    }

    if (
        [string]$successfulRun.status -ne "Succeeded" -or
        [int]$successfulRun.recordsProcessed -ne [int]$syncResult.retrieved -or
        -not [string]::IsNullOrEmpty([string]$successfulRun.errorMessage)
    ) {
        throw (
            "The successful ETL run history is inconsistent. " +
            "Status='$($successfulRun.status)', " +
            "recordsProcessed='$($successfulRun.recordsProcessed)', " +
            "retrieved='$($syncResult.retrieved)', " +
            "errorMessage='$($successfulRun.errorMessage)'."
        )
    }

    Stop-Day5Api -Process $successApi

    $failureBaseUri = "http://localhost:$FailurePort"
    $failureApi = Start-Day5Api `
        -ApiPort $FailurePort `
        -StandardOutputPath $failureLog `
        -StandardErrorPath $failureErrorLog `
        -TenantId "00000000-0000-0000-0000-000000000001"
    Wait-Day5Api -Process $failureApi -BaseUri $failureBaseUri

    $failureResponse = Invoke-WebRequest `
        "$failureBaseUri/api/admin/sync/azure/resources" `
        -Method Post `
        -Headers $tenantHeaders `
        -SkipHttpErrorCheck `
        -TimeoutSec 120

    if ($failureResponse.StatusCode -ne 500) {
        throw "The forced Azure authentication failure returned HTTP $($failureResponse.StatusCode), expected 500."
    }

    $runsAfterFailure = Invoke-RestMethod `
        "$failureBaseUri/api/admin/etl-runs?jobName=azure-resource-sync&take=10" `
        -Headers $tenantHeaders `
        -TimeoutSec 30
    $failedRun = $null
    foreach ($run in $runsAfterFailure) {
        if ([string]$run.status -eq "Failed") {
            $failedRun = $run
            break
        }
    }

    if (
        $null -eq $failedRun -or
        $null -eq $failedRun.finishedAt -or
        [string]::IsNullOrWhiteSpace($failedRun.errorMessage)
    ) {
        throw "The failed ETL run was not recorded with completion time and error details."
    }

    $databaseRunCount = [int](Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT count(*) FROM etl_job_runs WHERE job_name = 'azure-resource-sync' AND status IN ('Succeeded', 'Failed');" `
        -Scalar)

    if ($databaseRunCount -lt 3) {
        throw "Expected Worker success, API success, and API failure records in PostgreSQL."
    }

    Write-Host "Day 5 verified: Worker and manual API syncs succeeded with ETL history, and a forced API failure was persisted."
    $verified = $true
}
finally {
    foreach ($apiProcess in $apiProcesses) {
        Stop-Day5Api -Process $apiProcess
    }

    if (docker compose ps --status running --services | Select-String -SimpleMatch "postgres") {
        Invoke-PostgreSql `
            -TargetDatabase "postgres" `
            -Sql "DROP DATABASE IF EXISTS $Database WITH (FORCE);"
    }

    foreach ($logPath in @(
        $successLog,
        $failureLog,
        $successErrorLog,
        $failureErrorLog
    )) {
        if (Test-Path -LiteralPath $logPath) {
            if (-not $verified) {
                Get-Content -LiteralPath $logPath
            }

            Remove-Item -LiteralPath $logPath -Force
        }
    }
}
