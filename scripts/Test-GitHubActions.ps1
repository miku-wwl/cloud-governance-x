[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$version = "1.7.12"
$releaseBaseUri = "https://github.com/rhysd/actionlint/releases/download/v$version"

$platform = if ($IsWindows) {
    "windows"
}
elseif ($IsLinux) {
    "linux"
}
elseif ($IsMacOS) {
    "darwin"
}
else {
    throw "Unsupported actionlint operating system."
}

$architecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
$architectureName = switch ($architecture) {
    "X64" { "amd64" }
    "Arm64" { "arm64" }
    default { throw "Unsupported actionlint architecture: $architecture." }
}

$assetName = if ($IsWindows) {
    "actionlint_${version}_${platform}_${architectureName}.zip"
}
else {
    "actionlint_${version}_${platform}_${architectureName}.tar.gz"
}

$expectedHashes = @{
    "actionlint_1.7.12_windows_amd64.zip" = "6e7241b51e6817ea6a047693d8e6fed13b31819c9a0dd6c5a726e1592d22f6e9"
    "actionlint_1.7.12_windows_arm64.zip" = "cadcf7ea4efe3a68728893813643cebe1185e5b1d4be5b96245f65c9a4d5ea41"
    "actionlint_1.7.12_linux_amd64.tar.gz" = "8aca8db96f1b94770f1b0d72b6dddcb1ebb8123cb3712530b08cc387b349a3d8"
    "actionlint_1.7.12_linux_arm64.tar.gz" = "325e971b6ba9bfa504672e29be93c24981eeb1c07576d730e9f7c8805afff0c6"
    "actionlint_1.7.12_darwin_amd64.tar.gz" = "5b44c3bc2255115c9b69e30efc0fecdf498fdb63c5d58e17084fd5f16324c644"
    "actionlint_1.7.12_darwin_arm64.tar.gz" = "aba9ced2dee8d27fecca3dc7feb1a7f9a52caefa1eb46f3271ea66b6e0e6953f"
}

$expectedHash = $expectedHashes[$assetName]
if (-not $expectedHash) {
    throw "No trusted actionlint hash is registered for $assetName."
}

$tempDirectory = [IO.Path]::GetTempPath()
$toolDirectory = Join-Path $tempDirectory "cloud-governance-x/actionlint/$version/$platform-$architectureName"
$archivePath = Join-Path $toolDirectory $assetName
$executableName = if ($IsWindows) { "actionlint.exe" } else { "actionlint" }
$executablePath = Join-Path $toolDirectory $executableName

New-Item -ItemType Directory -Path $toolDirectory -Force | Out-Null

if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf) -or
    (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne
    $expectedHash) {
    Remove-Item -LiteralPath $archivePath -Force -ErrorAction SilentlyContinue
    Invoke-WebRequest `
        -Uri "$releaseBaseUri/$assetName" `
        -OutFile $archivePath
}

$actualHash = (
    Get-FileHash -LiteralPath $archivePath -Algorithm SHA256
).Hash.ToLowerInvariant()
if ($actualHash -ne $expectedHash) {
    throw "actionlint checksum mismatch for $assetName."
}

if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    if ($IsWindows) {
        Expand-Archive -LiteralPath $archivePath -DestinationPath $toolDirectory -Force
    }
    else {
        & tar -xzf $archivePath -C $toolDirectory
        if ($LASTEXITCODE -ne 0) {
            throw "Could not extract $assetName."
        }

        & chmod +x $executablePath
        if ($LASTEXITCODE -ne 0) {
            throw "Could not make actionlint executable."
        }
    }
}

Push-Location $repositoryRoot
try {
    & $executablePath -color
    if ($LASTEXITCODE -ne 0) {
        throw "actionlint failed with code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
