[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)]
    [int]$Port = 5108,
    [ValidatePattern("^[a-z][a-z0-9_]{2,62}$")]
    [string]$Database = "finops_day7"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$solution = Join-Path $repositoryRoot "FinOpsPlatform.slnx"
$apiProject = Join-Path $repositoryRoot "src/FinOps.Api"
$workerProject = Join-Path $repositoryRoot "src/FinOps.Worker"
$stdoutPath = Join-Path $env:TEMP "finops-day7-api.log"
$stderrPath = Join-Path $env:TEMP "finops-day7-api.err.log"
$apiProcess = $null
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

function Invoke-CostWorker {
    $previousDatabase = $env:PostgreSql__Database
    $previousJob = $env:Etl__Job
    $previousDays = $env:Etl__CostDays

    try {
        $env:PostgreSql__Database = $Database
        $env:Etl__Job = "Costs"
        $env:Etl__CostDays = "7"

        & dotnet run `
            --no-build `
            --no-launch-profile `
            --project $workerProject

        if ($LASTEXITCODE -ne 0) {
            throw "The Cost Worker exited with code $LASTEXITCODE."
        }
    }
    finally {
        $env:PostgreSql__Database = $previousDatabase
        $env:Etl__Job = $previousJob
        $env:Etl__CostDays = $previousDays
    }
}

function Start-Day7Api {
    $previousDatabase = $env:PostgreSql__Database

    try {
        $env:PostgreSql__Database = $Database

        return Start-Process dotnet `
            -ArgumentList @(
                "run",
                "--no-build",
                "--no-launch-profile",
                "--project",
                $apiProject,
                "--urls",
                "http://localhost:$Port"
            ) `
            -WorkingDirectory $repositoryRoot `
            -WindowStyle Hidden `
            -RedirectStandardOutput $stdoutPath `
            -RedirectStandardError $stderrPath `
            -PassThru
    }
    finally {
        $env:PostgreSql__Database = $previousDatabase
    }
}

function Wait-Day7Api {
    $deadline = (Get-Date).AddSeconds(60)
    do {
        Start-Sleep -Milliseconds 750

        if ($apiProcess.HasExited) {
            throw "The API exited with code $($apiProcess.ExitCode)."
        }

        try {
            $response = Invoke-WebRequest "http://localhost:$Port/health" -TimeoutSec 10
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

function Get-TotalsByCurrency {
    param([Parameter(Mandatory)]$Rows)

    $totals = @{}
    foreach ($row in $Rows) {
        $currency = [string]$row.currency
        if (-not $totals.ContainsKey($currency)) {
            $totals[$currency] = [decimal]0
        }

        $totals[$currency] += [decimal]$row.cost
    }

    return $totals
}

Remove-Item -LiteralPath $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue

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

    & (Join-Path $repositoryRoot "scripts/Invoke-DatabaseMigration.ps1") `
        -Database $Database `
        -NoBuild

    Invoke-CostWorker

    $workerRunCount = [int](Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT count(*) FROM etl_job_runs WHERE job_name = 'azure-cost-sync' AND status = 'Succeeded';" `
        -Scalar)
    if ($workerRunCount -ne 1) {
        throw "The Cost Worker did not persist exactly one successful ETL run."
    }

    $apiProcess = Start-Day7Api
    Wait-Day7Api

    $manualResult = Invoke-RestMethod `
        "http://localhost:$Port/api/admin/sync/azure/costs?days=7" `
        -Method Post `
        -TimeoutSec 180

    if ($manualResult.inserted -ne 0 -or $manualResult.updated -ne $manualResult.retrieved) {
        throw "The manual repeat sync was not idempotent."
    }

    $daily = Invoke-RestMethod `
        "http://localhost:$Port/api/costs/daily" `
        -TimeoutSec 30
    $byService = Invoke-RestMethod `
        "http://localhost:$Port/api/costs/by-service" `
        -TimeoutSec 30
    $byResourceGroup = Invoke-RestMethod `
        "http://localhost:$Port/api/costs/by-resource-group" `
        -TimeoutSec 30

    if (
        $null -eq $daily -or
        $null -eq $byService -or
        $null -eq $byResourceGroup
    ) {
        throw "One or more cost query APIs returned no data."
    }

    $dailyTotals = Get-TotalsByCurrency -Rows $daily
    $serviceTotals = Get-TotalsByCurrency -Rows $byService
    $resourceGroupTotals = Get-TotalsByCurrency -Rows $byResourceGroup

    foreach ($currency in $dailyTotals.Keys) {
        if (
            -not $serviceTotals.ContainsKey($currency) -or
            -not $resourceGroupTotals.ContainsKey($currency) -or
            [Math]::Abs($dailyTotals[$currency] - $serviceTotals[$currency]) -gt 0.000001 -or
            [Math]::Abs($dailyTotals[$currency] - $resourceGroupTotals[$currency]) -gt 0.000001
        ) {
            throw "Cost API totals are inconsistent for currency '$currency'."
        }
    }

    foreach ($currencyGroup in ($byService | Group-Object currency)) {
        $percentageTotal = [decimal](($currencyGroup.Group |
            Measure-Object -Property percentage -Sum).Sum)
        if ([Math]::Abs($percentageTotal - 100) -gt 0.05) {
            throw "Service percentages for '$($currencyGroup.Name)' total $percentageTotal instead of 100."
        }
    }

    $databaseRowCount = [int](Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT count(*) FROM cloud_cost_daily;" `
        -Scalar)
    $successfulRuns = [int](Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT count(*) FROM etl_job_runs WHERE job_name = 'azure-cost-sync' AND status = 'Succeeded';" `
        -Scalar)

    if ($databaseRowCount -ne $manualResult.retrieved -or $successfulRuns -ne 2) {
        throw "Database row count or ETL history does not match the Worker and API runs."
    }

    Write-Host (
        "Day 7 verified: Cost Worker and manual sync processed $databaseRowCount rows " +
        "without duplicates; daily, service, and resource-group APIs returned consistent totals."
    )
    $verified = $true
}
finally {
    if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        $apiProcess.WaitForExit()
    }

    if (docker compose ps --status running --services | Select-String -SimpleMatch "postgres") {
        Invoke-PostgreSql `
            -TargetDatabase "postgres" `
            -Sql "DROP DATABASE IF EXISTS $Database WITH (FORCE);"
    }

    foreach ($logPath in @($stdoutPath, $stderrPath)) {
        if (Test-Path -LiteralPath $logPath) {
            if (-not $verified) {
                Get-Content -LiteralPath $logPath
            }

            Remove-Item -LiteralPath $logPath -Force
        }
    }
}
