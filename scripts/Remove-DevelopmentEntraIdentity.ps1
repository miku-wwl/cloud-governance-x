[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern(
        "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$"
    )]
    [string]$ConfirmTenantId,
    [string]$ApiDisplayName = "cloud-governance-x-api-dev",
    [string]$ClientDisplayName = "cloud-governance-x-local-dev-client",
    [switch]$Apply
)

$ErrorActionPreference = "Stop"
$graphRoot = "https://graph.microsoft.com/v1.0"

$account = & az account show --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    throw "Azure CLI is not authenticated. Run 'az login' first."
}

if ($account.tenantId -ne $ConfirmTenantId) {
    throw (
        "The active Tenant '$($account.tenantId)' does not match the confirmed " +
        "Tenant '$ConfirmTenantId'."
    )
}

$token = & az account get-access-token `
    --resource-type ms-graph `
    --query accessToken `
    --output tsv
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($token)) {
    throw "Azure CLI could not obtain a Microsoft Graph token."
}

$headers = @{ Authorization = "Bearer $token" }
$applications = [System.Collections.Generic.List[object]]::new()
foreach ($displayName in @($ClientDisplayName, $ApiDisplayName)) {
    $escapedName = $displayName.Replace("'", "''")
    $filter = [Uri]::EscapeDataString("displayName eq '$escapedName'")
    $result = Invoke-RestMethod `
        -Method GET `
        -Uri "$graphRoot/applications?`$filter=$filter&`$select=id,appId,displayName" `
        -Headers $headers

    foreach ($application in @($result.value)) {
        $applications.Add($application)
    }
}

if ($applications.Count -eq 0) {
    Write-Host "No Day 26 development Entra applications were found."
    exit 0
}

Write-Host "The following Entra applications are in scope:"
$applications |
    Select-Object displayName, appId, id |
    Format-Table

if (-not $Apply) {
    Write-Host "Dry-run only. Re-run with -Apply to delete these directory objects."
    exit 0
}

foreach ($application in $applications) {
    Invoke-RestMethod `
        -Method DELETE `
        -Uri "$graphRoot/applications/$($application.id)" `
        -Headers $headers
    Write-Host "Deleted Entra application '$($application.displayName)'."
}
