[CmdletBinding()]
param(
    [string]$ApiDisplayName = "cloud-governance-x-api-dev",
    [string]$ClientDisplayName = "cloud-governance-x-local-dev-client",
    [string]$ScopeName = "access_as_user",
    [string]$OutputPath = "tmp/day26-entra-development.json"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$scopeId = "7d94f58d-5d62-4aca-a03e-40e64d83071e"
$graphRoot = "https://graph.microsoft.com/v1.0"

function Get-GraphAccessToken {
    $token = & az account get-access-token `
        --resource-type ms-graph `
        --query accessToken `
        --output tsv
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($token)) {
        throw "Azure CLI could not obtain a Microsoft Graph token. Run 'az login'."
    }

    return $token
}

function Invoke-Graph {
    param(
        [Parameter(Mandatory)][ValidateSet("GET", "POST", "PATCH")]
        [string]$Method,
        [Parameter(Mandatory)][string]$Path,
        [object]$Body
    )

    $arguments = @{
        Method = $Method
        Uri = "$graphRoot/$Path"
        Headers = @{
            Authorization = "Bearer $script:graphToken"
        }
    }

    if ($null -ne $Body) {
        $arguments["ContentType"] = "application/json"
        $arguments["Body"] = $Body | ConvertTo-Json -Depth 20 -Compress
    }

    return Invoke-RestMethod @arguments
}

function Get-ApplicationByDisplayName {
    param([Parameter(Mandatory)][string]$DisplayName)

    $escapedName = $DisplayName.Replace("'", "''")
    $filter = [Uri]::EscapeDataString("displayName eq '$escapedName'")
    $result = Invoke-Graph `
        -Method GET `
        -Path "applications?`$filter=$filter&`$select=id,appId,displayName,api,identifierUris,isFallbackPublicClient,requiredResourceAccess"
    $applications = @($result.value)
    if ($applications.Count -gt 1) {
        throw "More than one Entra application is named '$DisplayName'."
    }

    return $applications | Select-Object -First 1
}

function Get-OrCreateServicePrincipal {
    param(
        [Parameter(Mandatory)][string]$ApplicationId,
        [Parameter(Mandatory)][string]$DisplayName
    )

    $filter = [Uri]::EscapeDataString("appId eq '$ApplicationId'")
    $result = Invoke-Graph `
        -Method GET `
        -Path "servicePrincipals?`$filter=$filter&`$select=id,appId,displayName"
    $servicePrincipals = @($result.value)
    if ($servicePrincipals.Count -gt 1) {
        throw "More than one Service Principal exists for '$DisplayName'."
    }

    $servicePrincipal = $servicePrincipals | Select-Object -First 1
    if ($null -eq $servicePrincipal) {
        $servicePrincipal = Invoke-Graph `
            -Method POST `
            -Path "servicePrincipals" `
            -Body @{
                appId = $ApplicationId
                tags = @("WindowsAzureActiveDirectoryIntegratedApp")
            }
    }

    return $servicePrincipal
}

function Ensure-Owner {
    param(
        [Parameter(Mandatory)][string]$ApplicationObjectId,
        [Parameter(Mandatory)][string]$UserObjectId
    )

    $owners = Invoke-Graph `
        -Method GET `
        -Path "applications/$ApplicationObjectId/owners?`$select=id"
    if (@($owners.value).id -contains $UserObjectId) {
        return
    }

    Invoke-Graph `
        -Method POST `
        -Path "applications/$ApplicationObjectId/owners/`$ref" `
        -Body @{
            "@odata.id" = "$graphRoot/directoryObjects/$UserObjectId"
        } | Out-Null
}

$account = & az account show --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    throw "Azure CLI is not authenticated. Run 'az login' first."
}

$signedInUser = & az ad signed-in-user show --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    throw "The signed-in Entra user could not be resolved."
}

$script:graphToken = Get-GraphAccessToken

$apiApplication = Get-ApplicationByDisplayName -DisplayName $ApiDisplayName
if ($null -eq $apiApplication) {
    $apiApplication = Invoke-Graph `
        -Method POST `
        -Path "applications" `
        -Body @{
            displayName = $ApiDisplayName
            signInAudience = "AzureADMyOrg"
            api = @{
                requestedAccessTokenVersion = 2
                oauth2PermissionScopes = @(
                    @{
                        id = $scopeId
                        value = $ScopeName
                        type = "User"
                        isEnabled = $true
                        adminConsentDisplayName = "Access Cloud Governance X as the signed-in user"
                        adminConsentDescription = "Allows the local development client to call the Cloud Governance X API for the signed-in user."
                        userConsentDisplayName = "Access Cloud Governance X"
                        userConsentDescription = "Allow the local development client to call Cloud Governance X on your behalf."
                    }
                )
            }
            tags = @("cloud-governance-x", "development", "day26")
        }

    Invoke-Graph `
        -Method PATCH `
        -Path "applications/$($apiApplication.id)" `
        -Body @{
            identifierUris = @("api://$($apiApplication.appId)")
        } | Out-Null
}
else {
    $scope = @($apiApplication.api.oauth2PermissionScopes) |
        Where-Object { $_.value -eq $ScopeName } |
        Select-Object -First 1
    if ($null -eq $scope -or $scope.id -ne $scopeId -or -not $scope.isEnabled) {
        throw (
            "Existing API application '$ApiDisplayName' does not own the " +
            "expected enabled scope '$ScopeName' with ID '$scopeId'."
        )
    }

    if (@($apiApplication.identifierUris) -notcontains "api://$($apiApplication.appId)") {
        throw "Existing API application '$ApiDisplayName' has an unexpected identifier URI."
    }
}

Ensure-Owner `
    -ApplicationObjectId $apiApplication.id `
    -UserObjectId $signedInUser.id
$apiServicePrincipal = Get-OrCreateServicePrincipal `
    -ApplicationId $apiApplication.appId `
    -DisplayName $ApiDisplayName

$clientApplication = Get-ApplicationByDisplayName -DisplayName $ClientDisplayName
if ($null -eq $clientApplication) {
    $clientApplication = Invoke-Graph `
        -Method POST `
        -Path "applications" `
        -Body @{
            displayName = $ClientDisplayName
            signInAudience = "AzureADMyOrg"
            isFallbackPublicClient = $true
            publicClient = @{
                redirectUris = @()
            }
            requiredResourceAccess = @(
                @{
                    resourceAppId = $apiApplication.appId
                    resourceAccess = @(
                        @{
                            id = $scopeId
                            type = "Scope"
                        }
                    )
                }
            )
            tags = @("cloud-governance-x", "development", "day26")
        }
}
else {
    $requiredScope = @($clientApplication.requiredResourceAccess) |
        Where-Object { $_.resourceAppId -eq $apiApplication.appId } |
        ForEach-Object { $_.resourceAccess } |
        Where-Object { $_.id -eq $scopeId -and $_.type -eq "Scope" } |
        Select-Object -First 1
    if ($null -eq $requiredScope -or -not $clientApplication.isFallbackPublicClient) {
        throw (
            "Existing local client '$ClientDisplayName' is not the expected " +
            "public client with the '$ScopeName' delegated permission."
        )
    }
}

Ensure-Owner `
    -ApplicationObjectId $clientApplication.id `
    -UserObjectId $signedInUser.id
$clientServicePrincipal = Get-OrCreateServicePrincipal `
    -ApplicationId $clientApplication.appId `
    -DisplayName $ClientDisplayName

$grantFilter = [Uri]::EscapeDataString(
    "clientId eq '$($clientServicePrincipal.id)' and " +
    "resourceId eq '$($apiServicePrincipal.id)' and " +
    "principalId eq '$($signedInUser.id)'"
)
$existingGrants = Invoke-Graph `
    -Method GET `
    -Path "oauth2PermissionGrants?`$filter=$grantFilter&`$select=id,scope,consentType,principalId"
$matchingGrant = @($existingGrants.value) |
    Where-Object { ($_.scope -split " ") -contains $ScopeName } |
    Select-Object -First 1

if ($null -eq $matchingGrant) {
    Invoke-Graph `
        -Method POST `
        -Path "oauth2PermissionGrants" `
        -Body @{
            clientId = $clientServicePrincipal.id
            consentType = "Principal"
            principalId = $signedInUser.id
            resourceId = $apiServicePrincipal.id
            scope = $ScopeName
        } | Out-Null
}

$evidence = [ordered]@{
    tenantId = $account.tenantId
    signedInUserObjectId = $signedInUser.id
    api = [ordered]@{
        displayName = $ApiDisplayName
        applicationObjectId = $apiApplication.id
        clientId = $apiApplication.appId
        servicePrincipalObjectId = $apiServicePrincipal.id
        applicationIdUri = "api://$($apiApplication.appId)"
        scope = "api://$($apiApplication.appId)/$ScopeName"
        scopeId = $scopeId
    }
    localDevelopmentClient = [ordered]@{
        displayName = $ClientDisplayName
        applicationObjectId = $clientApplication.id
        clientId = $clientApplication.appId
        servicePrincipalObjectId = $clientServicePrincipal.id
        publicClient = $true
    }
    authority = "https://login.microsoftonline.com/$($account.tenantId)/v2.0"
    createdOrVerifiedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
}

$resolvedOutputPath = if ([IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
}
else {
    Join-Path $repositoryRoot $OutputPath
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$evidence | ConvertTo-Json -Depth 10 |
    Set-Content -LiteralPath $resolvedOutputPath -Encoding utf8

Write-Host "Development Entra identity is ready."
Write-Host "Tenant ID: $($evidence.tenantId)"
Write-Host "API client ID: $($evidence.api.clientId)"
Write-Host "Local client ID: $($evidence.localDevelopmentClient.clientId)"
Write-Host "Scope: $($evidence.api.scope)"
Write-Host "Evidence: $resolvedOutputPath"
