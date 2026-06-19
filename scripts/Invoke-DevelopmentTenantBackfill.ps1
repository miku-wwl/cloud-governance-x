[CmdletBinding()]
param(
    [ValidatePattern("^[a-z][a-z0-9_]{2,62}$")]
    [string]$Database = "finops",
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [Guid]$OrganizationId,
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [Guid]$TenantId,
    [ValidatePattern("^[a-z0-9](?:[a-z0-9-]{1,61}[a-z0-9])$")]
    [string]$TenantSlug = "legacy-development",
    [ValidateLength(1, 256)]
    [string]$OrganizationDisplayName = "FinOps Development Organization",
    [ValidateLength(1, 256)]
    [string]$TenantDisplayName = "Legacy Development Tenant",
    [ValidateRange(0, [long]::MaxValue)]
    [long]$ExpectedResourceRows = 0,
    [ValidateRange(0, [long]::MaxValue)]
    [long]$ExpectedCostRows = 0,
    [ValidateRange(0, [long]::MaxValue)]
    [long]$ExpectedEtlRunRows = 0,
    [ValidateRange(1, 10000000)]
    [long]$MaximumLegacyRows = 100000,
    [string]$ConfirmDatabase,
    [switch]$Apply,
    [switch]$AcknowledgeLegacyWritersStopped,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
if (-not $AcknowledgeLegacyWritersStopped) {
    throw (
        "Backfill requires -AcknowledgeLegacyWritersStopped. " +
        "Stop every pre-Day24 API and Worker instance first."
    )
}
if ($Apply -and $ConfirmDatabase -cne $Database) {
    throw (
        "Apply requires -ConfirmDatabase with the exact case-sensitive " +
        "database name '$Database'."
    )
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$migratorProject = Join-Path $repositoryRoot "src/FinOps.Migrator"
$previousEnvironment = @{
    DotnetEnvironment = $env:DOTNET_ENVIRONMENT
    Database = $env:PostgreSql__Database
    Enabled = $env:LegacyTenantBackfill__Enabled
    Apply = $env:LegacyTenantBackfill__Apply
    WritersStopped = $env:LegacyTenantBackfill__LegacyWritersStopped
    DatabaseConfirmation = $env:LegacyTenantBackfill__DatabaseConfirmation
    ExpectedResourceRows = $env:LegacyTenantBackfill__ExpectedResourceRows
    ExpectedCostRows = $env:LegacyTenantBackfill__ExpectedCostRows
    ExpectedEtlRunRows = $env:LegacyTenantBackfill__ExpectedEtlRunRows
    MaximumLegacyRows = $env:LegacyTenantBackfill__MaximumLegacyRows
    OrganizationId = $env:LegacyTenantBackfill__OrganizationId
    TenantId = $env:LegacyTenantBackfill__TenantId
    OrganizationDisplayName =
        $env:LegacyTenantBackfill__OrganizationDisplayName
    TenantSlug = $env:LegacyTenantBackfill__TenantSlug
    TenantDisplayName = $env:LegacyTenantBackfill__TenantDisplayName
}

try {
    $env:DOTNET_ENVIRONMENT = "Development"
    $env:PostgreSql__Database = $Database
    $env:LegacyTenantBackfill__Enabled = "true"
    $env:LegacyTenantBackfill__Apply = $Apply.IsPresent.ToString()
    $env:LegacyTenantBackfill__LegacyWritersStopped = "true"
    $env:LegacyTenantBackfill__DatabaseConfirmation = $ConfirmDatabase
    $env:LegacyTenantBackfill__ExpectedResourceRows =
        $ExpectedResourceRows.ToString()
    $env:LegacyTenantBackfill__ExpectedCostRows =
        $ExpectedCostRows.ToString()
    $env:LegacyTenantBackfill__ExpectedEtlRunRows =
        $ExpectedEtlRunRows.ToString()
    $env:LegacyTenantBackfill__MaximumLegacyRows =
        $MaximumLegacyRows.ToString()
    $env:LegacyTenantBackfill__OrganizationId = $OrganizationId.ToString()
    $env:LegacyTenantBackfill__TenantId = $TenantId.ToString()
    $env:LegacyTenantBackfill__OrganizationDisplayName =
        $OrganizationDisplayName
    $env:LegacyTenantBackfill__TenantSlug = $TenantSlug
    $env:LegacyTenantBackfill__TenantDisplayName = $TenantDisplayName

    $arguments = @(
        "run",
        "--no-launch-profile",
        "--project",
        $migratorProject,
        "--",
        "--Operation=backfill-development-tenant"
    )
    if ($NoBuild) {
        $arguments = @(
            "run",
            "--no-launch-profile",
            "--no-build",
            "--project",
            $migratorProject,
            "--",
            "--Operation=backfill-development-tenant"
        )
    }

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Development Tenant backfill exited with code $LASTEXITCODE."
    }
}
finally {
    $env:DOTNET_ENVIRONMENT = $previousEnvironment.DotnetEnvironment
    $env:PostgreSql__Database = $previousEnvironment.Database
    $env:LegacyTenantBackfill__Enabled = $previousEnvironment.Enabled
    $env:LegacyTenantBackfill__Apply = $previousEnvironment.Apply
    $env:LegacyTenantBackfill__LegacyWritersStopped =
        $previousEnvironment.WritersStopped
    $env:LegacyTenantBackfill__DatabaseConfirmation =
        $previousEnvironment.DatabaseConfirmation
    $env:LegacyTenantBackfill__ExpectedResourceRows =
        $previousEnvironment.ExpectedResourceRows
    $env:LegacyTenantBackfill__ExpectedCostRows =
        $previousEnvironment.ExpectedCostRows
    $env:LegacyTenantBackfill__ExpectedEtlRunRows =
        $previousEnvironment.ExpectedEtlRunRows
    $env:LegacyTenantBackfill__MaximumLegacyRows =
        $previousEnvironment.MaximumLegacyRows
    $env:LegacyTenantBackfill__OrganizationId =
        $previousEnvironment.OrganizationId
    $env:LegacyTenantBackfill__TenantId = $previousEnvironment.TenantId
    $env:LegacyTenantBackfill__OrganizationDisplayName =
        $previousEnvironment.OrganizationDisplayName
    $env:LegacyTenantBackfill__TenantSlug = $previousEnvironment.TenantSlug
    $env:LegacyTenantBackfill__TenantDisplayName =
        $previousEnvironment.TenantDisplayName
}
