[CmdletBinding()]
param(
    [string]$Location = "australiaeast",
    [string]$NamePrefix = "finops",
    [string]$Environment = "dev",
    [string]$Owner = "cloud-governance-x",
    [string]$CostCenter = "learning",
    [switch]$EnableLogAnalytics,
    [switch]$KeepResources
)

$ErrorActionPreference = "Stop"
$terraformDirectory = Join-Path $PSScriptRoot "../terraform/azure"
$evidenceDirectory = Join-Path $PSScriptRoot "../terraform/evidence/day2"
$planPath = Join-Path $terraformDirectory "day2.tfplan"
$applyAttempted = $false
$resourceGroupName = $null

New-Item -ItemType Directory -Force -Path $evidenceDirectory | Out-Null

$terraformArguments = @(
    "-var=location=$Location",
    "-var=name_prefix=$NamePrefix",
    "-var=environment=$Environment",
    "-var=owner=$Owner",
    "-var=cost_center=$CostCenter",
    "-var=enable_log_analytics=$($EnableLogAnalytics.IsPresent.ToString().ToLowerInvariant())"
)

function Invoke-Terraform {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & terraform -chdir="$terraformDirectory" @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform command failed: terraform $($Arguments -join ' ')"
    }
}

try {
    & az account show `
        --query "{subscriptionId:id,subscriptionName:name,tenantId:tenantId,state:state}" `
        --output json |
        Set-Content -Encoding utf8 (Join-Path $evidenceDirectory "azure-account.json")
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI is not authenticated. Run 'az login' first."
    }

    Invoke-Terraform @("init", "-input=false")
    Invoke-Terraform @("fmt", "-check")
    Invoke-Terraform @("validate")
    Invoke-Terraform (@("plan", "-input=false", "-out=$planPath") + $terraformArguments)
    $applyAttempted = $true
    Invoke-Terraform @("apply", "-input=false", "-auto-approve", $planPath)

    $outputs = & terraform -chdir="$terraformDirectory" output -json |
        ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read Terraform outputs."
    }

    $resourceGroupName = $outputs.resource_group_name.value
    $serviceBusNamespaceName = $outputs.service_bus_namespace_name.value
    $serviceBusQueueName = $outputs.service_bus_queue_name.value
    $resources = & az resource list `
        --resource-group $resourceGroupName `
        --query "[].{name:name,type:type,location:location,tags:tags}" `
        --output json |
        ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI resource verification failed."
    }

    $resources |
        ConvertTo-Json -Depth 10 |
        Set-Content -Encoding utf8 (Join-Path $evidenceDirectory "resources-after-apply.json")

    $expectedTypes = @(
        "Microsoft.Storage/storageAccounts",
        "Microsoft.ServiceBus/namespaces"
    )
    $actualTypes = @($resources.type)

    foreach ($expectedType in $expectedTypes) {
        if ($expectedType -notin $actualTypes) {
            throw "Expected Azure resource type was not found: $expectedType"
        }
    }

    $queue = & az servicebus queue show `
        --resource-group $resourceGroupName `
        --namespace-name $serviceBusNamespaceName `
        --name $serviceBusQueueName `
        --output json |
        ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $queue.name -ne $serviceBusQueueName) {
        throw "Service Bus queue verification failed."
    }

    $queue |
        ConvertTo-Json -Depth 10 |
        Set-Content -Encoding utf8 (Join-Path $evidenceDirectory "queue-after-apply.json")

    Write-Host "Verified $($resources.Count) top-level Azure resources and queue '$serviceBusQueueName'."
}
finally {
    if ($applyAttempted -and -not $KeepResources) {
        Invoke-Terraform (@("destroy", "-input=false", "-auto-approve") + $terraformArguments)

        if ($resourceGroupName) {
            $groupExists = & az group exists --name $resourceGroupName
            if ($LASTEXITCODE -ne 0) {
                throw "Unable to verify Resource Group deletion."
            }

            @{
                resourceGroup = $resourceGroupName
                exists        = [System.Convert]::ToBoolean($groupExists)
            } |
                ConvertTo-Json |
                Set-Content -Encoding utf8 (Join-Path $evidenceDirectory "resource-group-after-destroy.json")

            if ([System.Convert]::ToBoolean($groupExists)) {
                throw "Terraform destroy completed but Resource Group '$resourceGroupName' still exists."
            }
        }

        Write-Host "Destroy verified: the Day 2 Resource Group no longer exists."
    }
}
