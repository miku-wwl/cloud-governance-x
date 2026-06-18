[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)]
    [int]$Port = 5107,
    [ValidatePattern("^[a-z][a-z0-9_]{2,62}$")]
    [string]$Database = "finops_day6"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$apiProject = Join-Path $repositoryRoot "src/FinOps.Api"
$apiAssembly = Join-Path $apiProject "bin/Debug/net10.0/FinOps.Api.dll"
$solution = Join-Path $repositoryRoot "FinOpsPlatform.slnx"
$stdoutPath = Join-Path $env:TEMP "finops-day6-api.log"
$stderrPath = Join-Path $env:TEMP "finops-day6-api.err.log"
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

function Start-Day6Api {
    param([bool]$ForceSampleData)

    $previousDatabase = $env:PostgreSql__Database
    $previousForceSample = $env:AzureCost__ForceSampleData

    try {
        $env:PostgreSql__Database = $Database
        $env:AzureCost__ForceSampleData = $ForceSampleData.ToString().ToLowerInvariant()

        $startProcessArguments = @{
            FilePath = "dotnet"
            ArgumentList = @($apiAssembly, "--urls", "http://localhost:$Port")
            WorkingDirectory = $apiProject
            RedirectStandardOutput = $stdoutPath
            RedirectStandardError = $stderrPath
            PassThru = $true
        }
        if ($IsWindows) {
            $startProcessArguments["WindowStyle"] = "Hidden"
        }

        return Start-Process @startProcessArguments
    }
    finally {
        $env:PostgreSql__Database = $previousDatabase
        $env:AzureCost__ForceSampleData = $previousForceSample
    }
}

function Wait-Day6Api {
    param([Parameter(Mandatory)][System.Diagnostics.Process]$Process)

    $deadline = (Get-Date).AddSeconds(60)
    do {
        Start-Sleep -Milliseconds 750

        if ($Process.HasExited) {
            throw "The API exited with code $($Process.ExitCode)."
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

function Stop-Day6Api {
    if ($null -ne $script:apiProcess -and -not $script:apiProcess.HasExited) {
        Stop-Process -Id $script:apiProcess.Id -Force
        $script:apiProcess.WaitForExit()
    }

    $script:apiProcess = $null
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

    if (-not (Test-Path -LiteralPath $apiAssembly -PathType Leaf)) {
        throw "The API assembly does not exist: $apiAssembly"
    }

    & (Join-Path $repositoryRoot "scripts/Invoke-DatabaseMigration.ps1") `
        -Database $Database `
        -NoBuild

    $apiProcess = Start-Day6Api -ForceSampleData $false
    Wait-Day6Api -Process $apiProcess

    $actualResult = Invoke-RestMethod `
        "http://localhost:$Port/api/admin/sync/azure/costs?days=7" `
        -Method Post `
        -TimeoutSec 180

    if ($actualResult.retrieved -le 0) {
        throw "The real-or-fallback Azure cost query returned no rows."
    }

    Stop-Day6Api
    Remove-Item -LiteralPath $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue

    $apiProcess = Start-Day6Api -ForceSampleData $true
    Wait-Day6Api -Process $apiProcess

    $firstSampleResult = Invoke-RestMethod `
        "http://localhost:$Port/api/admin/sync/azure/costs?days=7" `
        -Method Post `
        -TimeoutSec 120
    $secondSampleResult = Invoke-RestMethod `
        "http://localhost:$Port/api/admin/sync/azure/costs?days=7" `
        -Method Post `
        -TimeoutSec 120

    if (
        -not $firstSampleResult.usedSampleData -or
        -not $secondSampleResult.usedSampleData
    ) {
        throw "Forced sample mode was not reported by the cost sync result."
    }

    if (
        $secondSampleResult.inserted -ne 0 -or
        $secondSampleResult.updated -ne $secondSampleResult.retrieved
    ) {
        throw "Repeated sample cost sync was not idempotent."
    }

    $sampleCount = [int](Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT count(*) FROM cloud_cost_daily WHERE raw_json->>'source' = 'sample';" `
        -Scalar)
    $sampleDateCount = [int](Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT count(DISTINCT usage_date) FROM cloud_cost_daily WHERE raw_json->>'source' = 'sample';" `
        -Scalar)
    $sampleServiceCount = [int](Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT count(DISTINCT service_name) FROM cloud_cost_daily WHERE raw_json->>'source' = 'sample';" `
        -Scalar)
    $sampleResourceGroupCount = [int](Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT count(DISTINCT resource_group) FROM cloud_cost_daily WHERE raw_json->>'source' = 'sample';" `
        -Scalar)
    $successfulRuns = [int](Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT count(*) FROM etl_job_runs WHERE job_name = 'azure-cost-sync' AND status = 'Succeeded';" `
        -Scalar)

    if (
        $sampleCount -lt 14 -or
        $sampleDateCount -ne 7 -or
        $sampleServiceCount -lt 2 -or
        $sampleResourceGroupCount -lt 2 -or
        $successfulRuns -ne 3
    ) {
        throw (
            "Day 6 persistence verification failed. " +
            "sampleCount=$sampleCount, dates=$sampleDateCount, " +
            "services=$sampleServiceCount, resourceGroups=$sampleResourceGroupCount, " +
            "successfulRuns=$successfulRuns."
        )
    }

    $initialSource = if ($actualResult.usedSampleData) {
        "sample fallback"
    }
    else {
        "Azure Cost Management"
    }

    Write-Host (
        "Day 6 verified: $initialSource returned " +
        "$($actualResult.retrieved) rows; forced sample data covered 7 days, " +
        "$sampleServiceCount services and $sampleResourceGroupCount resource groups; " +
        "repeated sync was idempotent."
    )
    $verified = $true
}
finally {
    Stop-Day6Api

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
