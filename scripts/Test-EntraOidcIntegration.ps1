[CmdletBinding(DefaultParameterSetName = "Complete")]
param(
    [Parameter(Mandatory, ParameterSetName = "Request")]
    [switch]$RequestDeviceCode,
    [Parameter(ParameterSetName = "Complete")]
    [ValidatePattern("^[a-z][a-z0-9_]{2,62}$")]
    [string]$Database = "finops_day26",
    [Parameter(ParameterSetName = "Complete")]
    [ValidateRange(1024, 65535)]
    [int]$Port = 5126,
    [string]$IdentityStatePath = "tmp/day26-entra-development.json",
    [string]$DeviceCodePath = "tmp/day26-device-code.json",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Resolve-RepositoryPath {
    param([Parameter(Mandatory)][string]$Path)

    if ([IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $repositoryRoot $Path
}

function ConvertFrom-Base64Url {
    param([Parameter(Mandatory)][string]$Value)

    $base64 = $Value.Replace("-", "+").Replace("_", "/")
    switch ($base64.Length % 4) {
        2 { $base64 += "==" }
        3 { $base64 += "=" }
    }

    return [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($base64))
}

function Get-JwtPart {
    param(
        [Parameter(Mandatory)][string]$Token,
        [Parameter(Mandatory)][ValidateSet("Header", "Payload")]
        [string]$Part
    )

    $segments = $Token.Split(".")
    if ($segments.Count -ne 3) {
        throw "Microsoft Entra returned a token that is not a compact JWT."
    }

    $index = if ($Part -eq "Header") { 0 } else { 1 }
    return ConvertFrom-Base64Url -Value $segments[$index] | ConvertFrom-Json
}

function Invoke-PostgreSql {
    param(
        [Parameter(Mandatory)][string]$TargetDatabase,
        [Parameter(Mandatory)][string]$Sql
    )

    $result = & docker compose exec -T postgres psql `
        -v ON_ERROR_STOP=1 `
        -U finops `
        -d $TargetDatabase `
        -c $Sql
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL command failed for database '$TargetDatabase'."
    }

    return $result
}

function Assert-CleanupRejectsWrongTenant {
    param([Parameter(Mandatory)][string]$ActualTenantId)

    $wrongTenantId = "00000000-0000-0000-0000-000000000000"
    if ($ActualTenantId -eq $wrongTenantId) {
        $wrongTenantId = "11111111-1111-1111-1111-111111111111"
    }

    $cleanupRejected = $false
    try {
        & (Join-Path $repositoryRoot "scripts/Remove-DevelopmentEntraIdentity.ps1") `
            -ConfirmTenantId $wrongTenantId `
            -ErrorAction Stop
    }
    catch {
        if (
            $_.Exception.Message -notmatch
            "does not match the confirmed Tenant"
        ) {
            throw
        }

        $cleanupRejected = $true
    }

    if (-not $cleanupRejected) {
        throw "The Entra cleanup script accepted an incorrect Tenant confirmation."
    }
}

$resolvedIdentityStatePath = Resolve-RepositoryPath -Path $IdentityStatePath
if (-not (Test-Path -LiteralPath $resolvedIdentityStatePath -PathType Leaf)) {
    throw (
        "Development Entra identity evidence was not found. Run " +
        "'./scripts/Initialize-DevelopmentEntraIdentity.ps1' first."
    )
}

$identity = Get-Content -LiteralPath $resolvedIdentityStatePath -Raw |
    ConvertFrom-Json
Assert-CleanupRejectsWrongTenant -ActualTenantId $identity.tenantId
$deviceEndpoint =
    "https://login.microsoftonline.com/$($identity.tenantId)/oauth2/v2.0/devicecode"
$tokenEndpoint =
    "https://login.microsoftonline.com/$($identity.tenantId)/oauth2/v2.0/token"
$requestedScope = "$($identity.api.scope) openid profile offline_access"
$resolvedDeviceCodePath = Resolve-RepositoryPath -Path $DeviceCodePath

if ($RequestDeviceCode) {
    $deviceCode = Invoke-RestMethod `
        -Method POST `
        -Uri $deviceEndpoint `
        -ContentType "application/x-www-form-urlencoded" `
        -Body @{
            client_id = $identity.localDevelopmentClient.clientId
            scope = $requestedScope
        }

    $deviceDirectory = Split-Path -Parent $resolvedDeviceCodePath
    New-Item -ItemType Directory -Path $deviceDirectory -Force | Out-Null
    $deviceCode | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath $resolvedDeviceCodePath -Encoding utf8

    Write-Host $deviceCode.message
    Write-Host (
        "After authentication completes, run: " +
        "./scripts/Test-EntraOidcIntegration.ps1"
    )
    exit 0
}

if (-not (Test-Path -LiteralPath $resolvedDeviceCodePath -PathType Leaf)) {
    throw (
        "No pending Device Code request exists. Run " +
        "'./scripts/Test-EntraOidcIntegration.ps1 -RequestDeviceCode' first."
    )
}

$deviceCode = Get-Content -LiteralPath $resolvedDeviceCodePath -Raw |
    ConvertFrom-Json
$tokenResponse = $null
$apiProcess = $null
$databaseCreated = $false
$verificationCompleted = $false
$apiStandardOutput = Join-Path $env:TEMP "finops-day26-api.out.log"
$apiStandardError = Join-Path $env:TEMP "finops-day26-api.err.log"
$apiAssembly = Join-Path $repositoryRoot "src/FinOps.Api/bin/Debug/net10.0/FinOps.Api.dll"
$previousEnvironment = @{
    DotnetEnvironment = $env:DOTNET_ENVIRONMENT
    Database = $env:PostgreSql__Database
    Urls = $env:ASPNETCORE_URLS
    OidcEnabled = $env:Authentication__Oidc__Enabled
    OidcAuthority = $env:Authentication__Oidc__Authority
    OidcAudience = $env:Authentication__Oidc__Audience
}

try {
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds([int]$deviceCode.expires_in)
    do {
        try {
            $tokenResponse = Invoke-RestMethod `
                -Method POST `
                -Uri $tokenEndpoint `
                -ContentType "application/x-www-form-urlencoded" `
                -Body @{
                    grant_type = "urn:ietf:params:oauth:grant-type:device_code"
                    client_id = $identity.localDevelopmentClient.clientId
                    device_code = $deviceCode.device_code
                }
        }
        catch {
            $errorBody = $_.ErrorDetails.Message
            $oauthError = if ($errorBody) {
                $errorBody | ConvertFrom-Json
            }
            else {
                $null
            }

            if ($oauthError.error -eq "authorization_pending") {
                Start-Sleep -Seconds ([Math]::Max(1, [int]$deviceCode.interval))
                continue
            }

            if ($oauthError.error -eq "slow_down") {
                Start-Sleep -Seconds ([Math]::Max(6, [int]$deviceCode.interval + 5))
                continue
            }

            if (
                $oauthError.error -in @(
                    "expired_token",
                    "authorization_declined",
                    "bad_verification_code"
                )
            ) {
                Remove-Item -LiteralPath $resolvedDeviceCodePath `
                    -Force `
                    -ErrorAction SilentlyContinue
                throw (
                    "The pending Device Code is no longer usable " +
                    "($($oauthError.error)). Request a new code."
                )
            }

            throw
        }
    }
    while ($null -eq $tokenResponse -and [DateTimeOffset]::UtcNow -lt $deadline)

    if ($null -eq $tokenResponse) {
        throw "The Device Code expired before Microsoft Entra authentication completed."
    }

    $header = Get-JwtPart -Token $tokenResponse.access_token -Part Header
    $claims = Get-JwtPart -Token $tokenResponse.access_token -Part Payload
    if ($claims.aud -ne $identity.api.clientId) {
        throw "The real Entra token has unexpected audience '$($claims.aud)'."
    }
    if ($claims.iss -ne $identity.authority) {
        throw "The real Entra token has unexpected issuer '$($claims.iss)'."
    }
    if (($claims.scp -split " ") -notcontains "access_as_user") {
        throw "The real Entra token does not contain the delegated access_as_user scope."
    }
    if (
        [string]::IsNullOrWhiteSpace($claims.sub) -or
        $claims.sub -notmatch "^[A-Za-z0-9_-]{8,256}$"
    ) {
        throw "The real Entra token subject is missing or unsafe for the fixture."
    }
    if (
        [string]::IsNullOrWhiteSpace($header.kid) -or
        $header.kid -notmatch "^[A-Za-z0-9_-]{8,256}$"
    ) {
        throw "The real Entra token signing-key identifier is missing or malformed."
    }

    $metadata = Invoke-RestMethod `
        "$($identity.authority)/.well-known/openid-configuration"
    $jwks = Invoke-RestMethod $metadata.jwks_uri
    if (@($jwks.keys).kid -notcontains $header.kid) {
        throw "The token signing key '$($header.kid)' is absent from current Entra JWKS."
    }

    & docker compose up -d postgres
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL could not be started."
    }

    Invoke-PostgreSql `
        -TargetDatabase "postgres" `
        -Sql "DROP DATABASE IF EXISTS $Database WITH (FORCE);"
    Invoke-PostgreSql `
        -TargetDatabase "postgres" `
        -Sql "CREATE DATABASE $Database;"
    $databaseCreated = $true

    $env:DOTNET_ENVIRONMENT = "Development"
    $env:PostgreSql__Database = $Database
    & (Join-Path $repositoryRoot "scripts/Invoke-DatabaseMigration.ps1") `
        -Database $Database
    if ($LASTEXITCODE -ne 0) {
        throw "Day 26 database migration failed."
    }

    & (Join-Path $repositoryRoot "scripts/Initialize-TestTenant.ps1") `
        -Database $Database
    if ($LASTEXITCODE -ne 0) {
        throw "Day 26 development Tenant initialization failed."
    }

    if (-not $SkipBuild) {
        & dotnet build (Join-Path $repositoryRoot "FinOpsPlatform.slnx")
        if ($LASTEXITCODE -ne 0) {
            throw "The solution build failed."
        }
    }

    if (-not (Test-Path -LiteralPath $apiAssembly -PathType Leaf)) {
        throw "The API assembly does not exist: $apiAssembly"
    }

    $env:ASPNETCORE_URLS = "http://127.0.0.1:$Port"
    $env:Authentication__Oidc__Enabled = "true"
    $env:Authentication__Oidc__Authority = $identity.authority
    $env:Authentication__Oidc__Audience = $identity.api.clientId
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
    $apiUri = "http://127.0.0.1:$Port"
    $readinessDeadline = (Get-Date).AddSeconds(45)
    do {
        Start-Sleep -Milliseconds 750
        if ($apiProcess.HasExited) {
            throw "The API exited with code $($apiProcess.ExitCode)."
        }

        try {
            $health = Invoke-WebRequest "$apiUri/health/live" -TimeoutSec 5
        }
        catch {
            $health = $null
        }
    }
    while ($null -eq $health -and (Get-Date) -lt $readinessDeadline)

    if ($null -eq $health) {
        throw "The Day 26 API did not become ready."
    }

    $requestHeaders = @{
        Authorization = "Bearer $($tokenResponse.access_token)"
        "X-FinOps-Tenant-Id" = "20000000-0000-0000-0000-000000000001"
    }
    $withoutMembershipResponse = Invoke-WebRequest `
        "$apiUri/api/costs/daily" `
        -Headers $requestHeaders `
        -SkipHttpErrorCheck
    if (
        $withoutMembershipResponse.StatusCode -ne
        [Net.HttpStatusCode]::Forbidden
    ) {
        throw (
            "Expected a real Entra token without Membership to return 403, " +
            "received $($withoutMembershipResponse.StatusCode)."
        )
    }

    $membershipSql = @"
INSERT INTO memberships
    (id, tenant_id, issuer, subject, subject_type, display_name, status, created_at, updated_at)
VALUES
    (gen_random_uuid(),
     '20000000-0000-0000-0000-000000000001',
     '$($claims.iss)',
     '$($claims.sub)',
     'Human',
     'Day 26 Entra Developer',
     'Active',
     now(),
     now())
ON CONFLICT (tenant_id, issuer, subject)
DO UPDATE SET
    display_name = EXCLUDED.display_name,
    status = 'Active',
    updated_at = now();
"@
    Invoke-PostgreSql -TargetDatabase $Database -Sql $membershipSql

    $response = Invoke-RestMethod `
        "$apiUri/api/costs/daily" `
        -Headers $requestHeaders

    Write-Host "Day 26 real Microsoft Entra integration verified."
    Write-Host "Issuer: $($claims.iss)"
    Write-Host "Audience: $($claims.aud)"
    Write-Host "Delegated scope: $($claims.scp)"
    Write-Host "Signing key ID: $($header.kid)"
    Write-Host "Without Membership: HTTP 403"
    Write-Host "Wrong cleanup Tenant confirmation: rejected"
    Write-Host "API result rows: $(@($response).Count)"
    $verificationCompleted = $true
}
finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
        $apiProcess.WaitForExit()
    }

    if ($databaseCreated) {
        $env:PostgreSql__Database = "postgres"
        try {
            Invoke-PostgreSql `
                -TargetDatabase "postgres" `
                -Sql "DROP DATABASE IF EXISTS $Database WITH (FORCE);" | Out-Null
        }
        catch {
            Write-Warning "Day 26 database cleanup failed: $($_.Exception.Message)"
        }
    }

    $env:DOTNET_ENVIRONMENT = $previousEnvironment.DotnetEnvironment
    $env:PostgreSql__Database = $previousEnvironment.Database
    $env:ASPNETCORE_URLS = $previousEnvironment.Urls
    $env:Authentication__Oidc__Enabled = $previousEnvironment.OidcEnabled
    $env:Authentication__Oidc__Authority = $previousEnvironment.OidcAuthority
    $env:Authentication__Oidc__Audience = $previousEnvironment.OidcAudience
    if ($verificationCompleted) {
        Remove-Item -LiteralPath $resolvedDeviceCodePath `
            -Force `
            -ErrorAction SilentlyContinue
    }
    Remove-Item -LiteralPath $apiStandardOutput, $apiStandardError `
        -Force `
        -ErrorAction SilentlyContinue
}
