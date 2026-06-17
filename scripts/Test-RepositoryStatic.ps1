[CmdletBinding()]
param(
    [switch]$SkipTerraformInit,
    [switch]$SkipDependencyOutdated
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$solution = Join-Path $repositoryRoot "FinOpsPlatform.slnx"
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Failure {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Message
    )

    $failures.Add("${Name}: ${Message}")
    Write-Host "FAIL: $Name" -ForegroundColor Red
    Write-Host "  $Message" -ForegroundColor Red
}

function Invoke-StaticStep {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Action
    )

    Write-Host ""
    Write-Host "==> $Name" -ForegroundColor Cyan

    try {
        & $Action
        Write-Host "PASS: $Name" -ForegroundColor Green
    }
    catch {
        Add-Failure -Name $Name -Message $_.Exception.Message
    }
}

function Invoke-External {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$Arguments,
        [string]$WorkingDirectory = $repositoryRoot
    )

    Push-Location $WorkingDirectory
    try {
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "'$FilePath $($Arguments -join ' ')' exited with code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-CapturedExternal {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$Arguments,
        [string]$WorkingDirectory = $repositoryRoot
    )

    Push-Location $WorkingDirectory
    try {
        $output = & $FilePath @Arguments 2>&1
        $exitCode = $LASTEXITCODE
        $output | ForEach-Object { Write-Host $_ }

        if ($exitCode -ne 0) {
            throw "'$FilePath $($Arguments -join ' ')' exited with code $exitCode."
        }

        return $output
    }
    finally {
        Pop-Location
    }
}

function Get-TrackedRepositoryFiles {
    $files = & git -C $repositoryRoot ls-files
    if ($LASTEXITCODE -ne 0) {
        throw "git ls-files failed."
    }

    return @($files | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Resolve-RepositoryPath {
    param([Parameter(Mandatory)][string]$RelativePath)

    return Join-Path $repositoryRoot ($RelativePath -replace "/", [IO.Path]::DirectorySeparatorChar)
}

$trackedFiles = Get-TrackedRepositoryFiles

Invoke-StaticStep "Git diff whitespace" {
    Invoke-External -FilePath "git" -Arguments @("diff", "--check")
}

Invoke-StaticStep "Git tracked garbage files" {
    $blockedPatterns = @(
        "^tmp/",
        "(^|/)bin/",
        "(^|/)obj/",
        "(^|/)\.terraform/",
        "^terraform/evidence/",
        "\.tfstate(\..*)?$",
        "\.tfplan$",
        "\.log$",
        "\.nupkg$",
        "\.user$",
        "\.coverage(xml)?$"
    )

    $blocked = [System.Collections.Generic.List[string]]::new()
    foreach ($file in $trackedFiles) {
        if ($file -match "^\.env(\..*)?$" -and $file -ne ".env.example") {
            $blocked.Add($file)
            continue
        }

        foreach ($pattern in $blockedPatterns) {
            if ($file -match $pattern) {
                $blocked.Add($file)
                break
            }
        }
    }

    if ($blocked.Count -gt 0) {
        throw "Tracked generated or sensitive files found: $($blocked -join ', ')"
    }
}

Invoke-StaticStep "Secret pattern scan" {
    $secretNamePattern = "(?i)(?:password|client[_-]?secret|access[_-]?key|private[_-]?key|connectionstring|connection[_-]?string|bearer[_-]?token|refresh[_-]?token|id[_-]?token|api[_-]?token|github[_-]?token|pat)"
    $assignmentPattern = "$secretNamePattern\s*[:=]\s*['""]?([^'"",;\s]+)"
    $privateKeyPattern = "-----BEGIN .*PRIVATE " + "KEY-----"
    $allowedValues = @(
        "",
        "false",
        "true",
        "null",
        "localhost",
        "finops_dev_password",
        "example",
        "sample",
        "changeme",
        "default",
        "placeholder",
        "<placeholder>"
    )
    $findings = [System.Collections.Generic.List[string]]::new()

    foreach ($file in $trackedFiles) {
        $fullPath = Resolve-RepositoryPath $file
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            continue
        }

        $extension = [IO.Path]::GetExtension($file).ToLowerInvariant()
        if ($extension -in @(".png", ".jpg", ".jpeg", ".gif", ".ico", ".dll", ".pdb")) {
            continue
        }

        $lineNumber = 0
        foreach ($line in [IO.File]::ReadLines($fullPath)) {
            $lineNumber++

            if ($line -match $privateKeyPattern) {
                $findings.Add("${file}:${lineNumber}: private key block marker")
                continue
            }

            if ($line -match "(?i)(ghp|github_pat|azdpat|sk)-[a-z0-9_]{16,}") {
                $findings.Add("${file}:${lineNumber}: token-like value")
                continue
            }

            $match = [regex]::Match($line, $assignmentPattern)
            if ($match.Success) {
                $value = $match.Groups[2].Value.Trim()
                $normalized = $value.Trim('"', "'", ",", ";").ToLowerInvariant()
                if (
                    $allowedValues -notcontains $normalized -and
                    -not $normalized.Contains("example") -and
                    -not $normalized.Contains("finops_dev_password")
                ) {
                    $findings.Add("${file}:${lineNumber}: suspicious secret assignment")
                }
            }
        }
    }

    if ($findings.Count -gt 0) {
        throw "Suspicious secret patterns found: $($findings -join '; ')"
    }
}

Invoke-StaticStep "JSON parse" {
    foreach ($file in ($trackedFiles | Where-Object { $_ -like "*.json" })) {
        $fullPath = Resolve-RepositoryPath $file
        Get-Content -LiteralPath $fullPath -Raw | ConvertFrom-Json | Out-Null
    }
}

Invoke-StaticStep "XML parse" {
    $xmlFiles = $trackedFiles | Where-Object {
        $_ -match "\.(xml|csproj|props|targets|slnx)$"
    }

    foreach ($file in $xmlFiles) {
        $fullPath = Resolve-RepositoryPath $file
        [xml](Get-Content -LiteralPath $fullPath -Raw) | Out-Null
    }
}

Invoke-StaticStep "YAML parse" {
    $yamlFiles = @($trackedFiles | Where-Object { $_ -match "\.(yml|yaml)$" })
    foreach ($file in $yamlFiles) {
        $fullPath = Resolve-RepositoryPath $file
        $lines = Get-Content -LiteralPath $fullPath
        for ($index = 0; $index -lt $lines.Count; $index++) {
            if ($lines[$index] -match "^\t+") {
                throw "${file}:$($index + 1) starts indentation with a tab."
            }
        }

        if ([IO.Path]::GetFileName($file) -eq "compose.yaml") {
            Invoke-External -FilePath "docker" -Arguments @("compose", "-f", $fullPath, "config", "--quiet")
        }
    }
}

Invoke-StaticStep "PowerShell parse" {
    foreach ($file in ($trackedFiles | Where-Object { $_ -like "*.ps1" })) {
        $fullPath = Resolve-RepositoryPath $file
        $tokens = $null
        $parseErrors = $null
        [System.Management.Automation.Language.Parser]::ParseFile(
            $fullPath,
            [ref]$tokens,
            [ref]$parseErrors
        ) | Out-Null

        if ($parseErrors.Count -gt 0) {
            $messages = $parseErrors | ForEach-Object {
                "${file}:$($_.Extent.StartLineNumber): $($_.Message)"
            }
            throw ($messages -join "; ")
        }
    }
}

Invoke-StaticStep "Markdown local links" {
    $linkPattern = "\[[^\]]+\]\(([^)]+)\)"
    $missingLinks = [System.Collections.Generic.List[string]]::new()

    foreach ($file in ($trackedFiles | Where-Object { $_ -like "*.md" })) {
        $fullPath = Resolve-RepositoryPath $file
        $directory = Split-Path -Parent $fullPath
        $lineNumber = 0

        foreach ($line in [IO.File]::ReadLines($fullPath)) {
            $lineNumber++
            foreach ($match in [regex]::Matches($line, $linkPattern)) {
                $target = $match.Groups[1].Value.Trim()
                if (
                    [string]::IsNullOrWhiteSpace($target) -or
                    $target.StartsWith("#") -or
                    $target -match "^[a-z][a-z0-9+.-]*:" -or
                    $target.StartsWith("<")
                ) {
                    continue
                }

                $pathOnly = ($target -split "#", 2)[0]
                if ([string]::IsNullOrWhiteSpace($pathOnly)) {
                    continue
                }

                $pathOnly = [Uri]::UnescapeDataString($pathOnly)
                $candidate = Join-Path $directory ($pathOnly -replace "/", [IO.Path]::DirectorySeparatorChar)
                if (-not (Test-Path -LiteralPath $candidate)) {
                    $missingLinks.Add("${file}:${lineNumber}: $target")
                }
            }
        }
    }

    if ($missingLinks.Count -gt 0) {
        throw "Missing local markdown targets: $($missingLinks -join '; ')"
    }
}

Invoke-StaticStep "dotnet tool restore" {
    Invoke-External -FilePath "dotnet" -Arguments @("tool", "restore")
}

Invoke-StaticStep "dotnet restore" {
    Invoke-External -FilePath "dotnet" -Arguments @("restore", $solution)
}

Invoke-StaticStep "NuGet vulnerable packages" {
    $output = Invoke-CapturedExternal `
        -FilePath "dotnet" `
        -Arguments @("list", $solution, "package", "--vulnerable", "--include-transitive")

    if ($output | Where-Object { $_ -match "^\s*>" }) {
        throw "Vulnerable packages were reported by dotnet list package."
    }
}

Invoke-StaticStep "NuGet deprecated packages report" {
    Invoke-CapturedExternal `
        -FilePath "dotnet" `
        -Arguments @("list", $solution, "package", "--deprecated") | Out-Null
}

if (-not $SkipDependencyOutdated) {
    Invoke-StaticStep "NuGet outdated packages report" {
        Invoke-CapturedExternal `
            -FilePath "dotnet" `
            -Arguments @("list", $solution, "package", "--outdated") | Out-Null
    }
}

Invoke-StaticStep "dotnet format" {
    Invoke-External -FilePath "dotnet" -Arguments @("format", $solution, "--verify-no-changes")
}

Invoke-StaticStep "dotnet build" {
    Invoke-External -FilePath "dotnet" -Arguments @("build", $solution, "--no-restore")
}

Invoke-StaticStep "dotnet test" {
    Invoke-External -FilePath "dotnet" -Arguments @("test", $solution, "--no-build")
}

Invoke-StaticStep "Terraform version" {
    Invoke-External -FilePath "terraform" -Arguments @("-chdir=terraform/azure", "version")
}

Invoke-StaticStep "Terraform fmt" {
    Invoke-External -FilePath "terraform" -Arguments @("-chdir=terraform/azure", "fmt", "-check", "-recursive")
}

if (-not $SkipTerraformInit) {
    Invoke-StaticStep "Terraform init without backend" {
        Invoke-External `
            -FilePath "terraform" `
            -Arguments @("-chdir=terraform/azure", "init", "-backend=false", "-input=false")
    }
}

Invoke-StaticStep "Terraform validate" {
    Invoke-External -FilePath "terraform" -Arguments @("-chdir=terraform/azure", "validate")
}

Write-Host ""
if ($failures.Count -gt 0) {
    Write-Host "Static verification failed with $($failures.Count) failing step(s):" -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host " - $failure" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Static verification passed." -ForegroundColor Green
