[CmdletBinding()]
param(
    [ValidatePattern("^[a-z][a-z0-9_]{2,62}$")]
    [string]$Database = "finops",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$migratorProject = Join-Path $repositoryRoot "src/FinOps.Migrator"
$previousDatabase = $env:PostgreSql__Database

try {
    $env:PostgreSql__Database = $Database
    $arguments = @(
        "run",
        "--no-launch-profile",
        "--project",
        $migratorProject
    )

    if ($NoBuild) {
        $arguments += "--no-build"
    }

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "FinOps.Migrator exited with code $LASTEXITCODE."
    }
}
finally {
    $env:PostgreSql__Database = $previousDatabase
}
