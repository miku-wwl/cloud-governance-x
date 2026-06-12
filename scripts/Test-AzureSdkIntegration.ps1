[CmdletBinding()]
param(
    [int]$Port = 5103,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$apiProject = Join-Path $repositoryRoot "src/FinOps.Api"
$apiUri = "http://localhost:$Port"
$stdoutPath = Join-Path $env:TEMP "finops-day3-api.out.log"
$stderrPath = Join-Path $env:TEMP "finops-day3-api.err.log"
$apiProcess = $null

if (-not $SkipBuild) {
    & dotnet build (Join-Path $repositoryRoot "FinOpsPlatform.slnx")
    if ($LASTEXITCODE -ne 0) {
        throw "The solution build failed."
    }
}

$expectedSubscription = & az account show `
    --query "{subscriptionId:id,displayName:name,tenantId:tenantId,state:state}" `
    --output json |
    ConvertFrom-Json

if ($LASTEXITCODE -ne 0) {
    throw "Azure CLI is not authenticated. Run 'az login' first."
}

Remove-Item -LiteralPath $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue

try {
    $apiProcess = Start-Process dotnet `
        -ArgumentList @(
            "run",
            "--no-build",
            "--project",
            $apiProject,
            "--urls",
            $apiUri
        ) `
        -WorkingDirectory $repositoryRoot `
        -WindowStyle Hidden `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -PassThru

    $deadline = (Get-Date).AddSeconds(45)
    $subscriptions = $null

    do {
        Start-Sleep -Milliseconds 750

        if ($apiProcess.HasExited) {
            throw "The API exited with code $($apiProcess.ExitCode)."
        }

        try {
            $subscriptions = Invoke-RestMethod `
                "$apiUri/api/cloud/azure/subscriptions" `
                -TimeoutSec 15
        }
        catch {
            $lastRequestError = $_
        }
    }
    while ($null -eq $subscriptions -and (Get-Date) -lt $deadline)

    if ($null -eq $subscriptions) {
        throw "The Azure subscription endpoint did not succeed: $lastRequestError"
    }

    $actualSubscription = @($subscriptions) |
        Where-Object { $_.subscriptionId -eq $expectedSubscription.subscriptionId } |
        Select-Object -First 1

    if ($null -eq $actualSubscription) {
        throw "The API did not return the active Azure CLI subscription."
    }

    foreach ($property in @("subscriptionId", "displayName", "tenantId", "state")) {
        if ($actualSubscription.$property -ne $expectedSubscription.$property) {
            throw "Mismatch for $property between the API and Azure CLI."
        }
    }

    Write-Host "Day 3 Azure SDK integration verified:"
    $actualSubscription | Format-List
}
finally {
    if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
    }

    if (Test-Path -LiteralPath $stdoutPath) {
        Get-Content -LiteralPath $stdoutPath
        Remove-Item -LiteralPath $stdoutPath -Force
    }

    if (Test-Path -LiteralPath $stderrPath) {
        if ((Get-Item -LiteralPath $stderrPath).Length -gt 0) {
            Get-Content -LiteralPath $stderrPath
        }

        Remove-Item -LiteralPath $stderrPath -Force
    }
}
