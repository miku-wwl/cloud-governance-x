[CmdletBinding()]
param(
    [string]$Location = "australiaeast",
    [ValidatePattern("^[a-z0-9]{3,10}$")]
    [string]$NamePrefix = "finops",
    [ValidatePattern("^[a-z0-9-]{2,10}$")]
    [string]$Environment = "day4",
    [ValidatePattern("^[a-z][a-z0-9_]{2,62}$")]
    [string]$Database = "finops_day4"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$terraformDirectory = Join-Path $repositoryRoot "terraform/azure"
$planPath = Join-Path $terraformDirectory "day4.tfplan"
$workerProject = Join-Path $repositoryRoot "src/FinOps.Worker"
$applyAttempted = $false
$destroyVerified = $false
$resourceGroupName = $null

$terraformArguments = @(
    "-var=location=$Location",
    "-var=name_prefix=$NamePrefix",
    "-var=environment=$Environment",
    "-var=owner=cloud-governance-x",
    "-var=cost_center=learning",
    "-var=enable_log_analytics=false"
)

function Invoke-Terraform {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & terraform -chdir="$terraformDirectory" @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform command failed: terraform $($Arguments -join ' ')"
    }
}

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

function Invoke-Worker {
    $previousDatabase = $env:PostgreSql__Database

    try {
        $env:PostgreSql__Database = $Database
        & dotnet run `
            --no-build `
            --no-launch-profile `
            --project $workerProject

        if ($LASTEXITCODE -ne 0) {
            throw "FinOps.Worker exited with code $LASTEXITCODE."
        }
    }
    finally {
        $env:PostgreSql__Database = $previousDatabase
    }
}

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

    & dotnet build (Join-Path $repositoryRoot "FinOpsPlatform.slnx")
    if ($LASTEXITCODE -ne 0) {
        throw "The solution build failed."
    }

    & (Join-Path $repositoryRoot "scripts/Invoke-DatabaseMigration.ps1") `
        -Database $Database `
        -NoBuild

    Invoke-Terraform @("init", "-input=false")
    Invoke-Terraform @("fmt", "-check")
    Invoke-Terraform @("validate")
    Invoke-Terraform (@("plan", "-input=false", "-out=$planPath") + $terraformArguments)
    $applyAttempted = $true
    Invoke-Terraform @("apply", "-input=false", "-auto-approve", $planPath)
    $resourceGroupName = & terraform -chdir="$terraformDirectory" output -raw resource_group_name

    $matchingCount = 0
    for ($attempt = 1; $attempt -le 10; $attempt++) {
        Invoke-Worker

        $matchingCount = [int](Invoke-PostgreSql `
            -TargetDatabase $Database `
            -Sql "SELECT count(*) FROM cloud_resources WHERE lower(resource_group) = lower('$resourceGroupName');" `
            -Scalar)

        if ($matchingCount -ge 2) {
            break
        }

        Write-Host "Resource Graph has not indexed all demo resources yet. Attempt $attempt/10."
        Start-Sleep -Seconds 15
    }

    if ($matchingCount -lt 2) {
        throw "Resource Graph did not return the expected Day 4 resources."
    }

    $firstCount = [int](Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT count(*) FROM cloud_resources;" `
        -Scalar)
    $firstSeen = Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT min(first_seen_at)::text FROM cloud_resources WHERE lower(resource_group) = lower('$resourceGroupName');" `
        -Scalar

    Invoke-Worker

    $secondCount = [int](Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT count(*) FROM cloud_resources;" `
        -Scalar)
    $secondFirstSeen = Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT min(first_seen_at)::text FROM cloud_resources WHERE lower(resource_group) = lower('$resourceGroupName');" `
        -Scalar
    $duplicateCount = [int](Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT count(*) FROM (SELECT provider, resource_id_normalized FROM cloud_resources GROUP BY provider, resource_id_normalized HAVING count(*) > 1) duplicates;" `
        -Scalar)
    $expectedTypeCount = [int](Invoke-PostgreSql `
        -TargetDatabase $Database `
        -Sql "SELECT count(DISTINCT lower(resource_type)) FROM cloud_resources WHERE lower(resource_group) = lower('$resourceGroupName') AND lower(resource_type) IN ('microsoft.storage/storageaccounts', 'microsoft.servicebus/namespaces');" `
        -Scalar)

    if ($firstCount -ne $secondCount) {
        throw "Repeated sync changed row count from $firstCount to $secondCount."
    }

    if ($duplicateCount -ne 0) {
        throw "Repeated sync produced duplicate cloud resource rows."
    }

    if ($firstSeen -ne $secondFirstSeen) {
        throw "Repeated sync changed FirstSeenAt."
    }

    if ($expectedTypeCount -ne 2) {
        throw "Storage Account and Service Bus Namespace were not both persisted."
    }

    Write-Host "Day 4 verified: $secondCount Azure resources persisted, $matchingCount in $resourceGroupName, no duplicates."
}
finally {
    try {
        if ($applyAttempted) {
            Invoke-Terraform (@("destroy", "-input=false", "-auto-approve") + $terraformArguments)

            $remainingState = @(& terraform -chdir="$terraformDirectory" state list)
            if ($LASTEXITCODE -ne 0 -or $remainingState.Count -ne 0) {
                throw "Terraform destroy could not be verified."
            }

            if ($resourceGroupName) {
                $groupExists = [System.Convert]::ToBoolean(
                    (& az group exists --name $resourceGroupName)
                )
                if ($groupExists) {
                    throw "Azure Resource Group '$resourceGroupName' still exists."
                }
            }

            $destroyVerified = $true
        }
    }
    finally {
        if (docker compose ps --status running --services | Select-String -SimpleMatch "postgres") {
            Invoke-PostgreSql `
                -TargetDatabase "postgres" `
                -Sql "DROP DATABASE IF EXISTS $Database WITH (FORCE);"
        }
    }

    if ($destroyVerified) {
        @(
            (Join-Path $terraformDirectory ".terraform"),
            $planPath,
            (Join-Path $terraformDirectory "terraform.tfstate"),
            (Join-Path $terraformDirectory "terraform.tfstate.backup")
        ) |
            Where-Object { Test-Path -LiteralPath $_ } |
            ForEach-Object { Remove-Item -LiteralPath $_ -Recurse -Force }
    }
}
