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

function Get-RepositoryCandidateFiles {
    $files = & git -C $repositoryRoot ls-files --cached --others --exclude-standard
    if ($LASTEXITCODE -ne 0) {
        throw "git ls-files failed."
    }

    return @($files | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Get-GitWorkingTreeStatus {
    $status = & git -C $repositoryRoot status --porcelain=v1 --untracked-files=all
    if ($LASTEXITCODE -ne 0) {
        throw "git status failed."
    }

    return @($status | Sort-Object)
}

function Resolve-RepositoryPath {
    param([Parameter(Mandatory)][string]$RelativePath)

    return Join-Path $repositoryRoot ($RelativePath -replace "/", [IO.Path]::DirectorySeparatorChar)
}

function Get-MarkdownProseLines {
    param([Parameter(Mandatory)][AllowEmptyString()][string[]]$Lines)

    $insideFence = $false
    $fenceCharacter = $null
    $fenceLength = 0

    foreach ($line in $Lines) {
        $fenceMatch = [regex]::Match($line, '^\s{0,3}(`{3,}|~{3,})')
        if ($fenceMatch.Success) {
            $marker = $fenceMatch.Groups[1].Value
            $markerCharacter = $marker[0]

            if (-not $insideFence) {
                $insideFence = $true
                $fenceCharacter = $markerCharacter
                $fenceLength = $marker.Length
                continue
            }

            if (
                $markerCharacter -eq $fenceCharacter -and
                $marker.Length -ge $fenceLength
            ) {
                $insideFence = $false
                $fenceCharacter = $null
                $fenceLength = 0
            }

            continue
        }

        if (-not $insideFence) {
            $line
        }
    }
}

function Test-IsAllowedSecretValue {
    param([AllowEmptyString()][string]$Value)

    $normalized = $Value.Trim().Trim('"', "'", ",", ";").ToLowerInvariant()
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

    if ($allowedValues -contains $normalized) {
        return $true
    }

    return (
        $normalized -match "^(?:x+|\*+|<[^>]+>|\$\{[^}]+\}|%\w+%|\{\{.+\}\})$"
    )
}

function Test-IsPlaceholderToken {
    param([Parameter(Mandatory)][string]$Value)

    $body = $Value -replace "^(?i)(?:github_pat|gh[pousr]|azdpat|sk)[_-]", ""
    return $body -match "^(.)\1{15,}$"
}

function Get-SecretFindingsForLine {
    param(
        [Parameter(Mandatory)][AllowEmptyString()][string]$Line,
        [Parameter(Mandatory)][string]$Location,
        [switch]$IgnoreBareExpressions
    )

    $findings = [System.Collections.Generic.List[string]]::new()
    $privateKeyPattern = "-----BEGIN .*PRIVATE " + "KEY-----"
    if ($Line -match $privateKeyPattern) {
        $findings.Add("${Location}: private key block marker")
        return $findings
    }

    $tokenPatterns = @(
        "(?i)\bgh[pousr][_-][a-z0-9]{20,}\b",
        "(?i)\bgithub_pat_[a-z0-9_]{20,}\b",
        "(?i)\b(?:azdpat|sk)[_-][a-z0-9_-]{16,}\b"
    )
    foreach ($pattern in $tokenPatterns) {
        foreach ($match in [regex]::Matches($Line, $pattern)) {
            if (-not (Test-IsPlaceholderToken -Value $match.Value)) {
                $findings.Add("${Location}: token-like value")
            }
        }
    }

    $secretNamePattern = "(?:password|client[_-]?secret|access[_-]?key|private[_-]?key|connection[_-]?string|bearer[_-]?token|refresh[_-]?token|id[_-]?token|api[_-]?token|github[_-]?token|pat)"
    $assignmentPattern = "(?i)(?<![a-z0-9])['""]?$secretNamePattern['""]?(?![a-z0-9])\s*[:=]\s*(?:""([^""]*)""|'([^']*)'|([^,;\s#]+))"

    foreach ($match in [regex]::Matches($Line, $assignmentPattern)) {
        if ($IgnoreBareExpressions -and $match.Groups[3].Success) {
            continue
        }

        $value = @(
            $match.Groups[1].Value,
            $match.Groups[2].Value,
            $match.Groups[3].Value
        ) | Where-Object { $_ -ne "" } | Select-Object -First 1

        if ($null -eq $value) {
            $value = ""
        }

        if (-not (Test-IsAllowedSecretValue -Value $value)) {
            $findings.Add("${Location}: suspicious secret assignment")
        }
    }

    return $findings
}

$repositoryFiles = Get-RepositoryCandidateFiles
$initialGitStatus = @(Get-GitWorkingTreeStatus)

Invoke-StaticStep "Git diff whitespace" {
    Invoke-External -FilePath "git" -Arguments @("diff", "--check")
}

Invoke-StaticStep "Git candidate garbage files" {
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
    foreach ($file in $repositoryFiles) {
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
        throw "Candidate generated or sensitive files found: $($blocked -join ', ')"
    }
}

Invoke-StaticStep "Secret pattern scan" {
    $positiveFixtures = @(
        '"Pass' + 'word": "' + "Not" + "APlaceholder42!" + '"',
        "github_pat_" + "Ab3Cd4Ef5Gh6Ij7Kl8Mn9Op0Qr"
    )
    foreach ($fixture in $positiveFixtures) {
        $fixtureFindings = @(Get-SecretFindingsForLine `
            -Line $fixture `
            -Location "positive-fixture")
        if ($fixtureFindings.Count -eq 0) {
            throw "Secret scanner did not reject a positive regression fixture."
        }
    }

    $negativeFixtures = @(
        '"Password": "finops_dev_password"',
        "github_pat_" + ("x" * 24)
    )
    foreach ($fixture in $negativeFixtures) {
        $fixtureFindings = @(Get-SecretFindingsForLine `
            -Line $fixture `
            -Location "negative-fixture")
        if ($fixtureFindings.Count -gt 0) {
            throw "Secret scanner rejected an allowed placeholder fixture."
        }
    }

    $findings = [System.Collections.Generic.List[string]]::new()
    foreach ($file in $repositoryFiles) {
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
            $ignoreBareExpressions = $extension -in @(
                ".cs",
                ".csx",
                ".ps1",
                ".tf",
                ".csproj",
                ".props",
                ".targets"
            )
            $lineFindings = Get-SecretFindingsForLine `
                -Line $line `
                -Location "${file}:${lineNumber}" `
                -IgnoreBareExpressions:$ignoreBareExpressions
            foreach ($finding in @($lineFindings)) {
                $findings.Add($finding)
            }
        }
    }

    if ($findings.Count -gt 0) {
        throw "Suspicious secret patterns found: $($findings -join '; ')"
    }
}

Invoke-StaticStep "JSON parse" {
    foreach ($file in ($repositoryFiles | Where-Object { $_ -like "*.json" })) {
        $fullPath = Resolve-RepositoryPath $file
        Get-Content -LiteralPath $fullPath -Raw | ConvertFrom-Json | Out-Null
    }
}

Invoke-StaticStep "XML parse" {
    $xmlFiles = $repositoryFiles | Where-Object {
        $_ -match "\.(xml|csproj|props|targets|slnx)$"
    }

    foreach ($file in $xmlFiles) {
        $fullPath = Resolve-RepositoryPath $file
        [xml](Get-Content -LiteralPath $fullPath -Raw) | Out-Null
    }
}

Invoke-StaticStep "YAML validation" {
    $yamlFiles = @($repositoryFiles | Where-Object { $_ -match "\.(yml|yaml)$" })
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
            continue
        }

        $normalizedPath = $file.Replace('\', '/')
        if ($normalizedPath.StartsWith(".github/workflows/", [StringComparison]::Ordinal)) {
            continue
        }

        throw (
            "$file is YAML outside the validated Compose and GitHub Actions scopes. " +
            "Add an explicit parser before committing this YAML file."
        )
    }
}

Invoke-StaticStep "GitHub Actions workflow validation" {
    & (Join-Path $repositoryRoot "scripts/Test-GitHubActions.ps1")
    if ($LASTEXITCODE -ne 0) {
        throw "GitHub Actions workflow validation failed."
    }
}

Invoke-StaticStep "PowerShell parse" {
    foreach ($file in ($repositoryFiles | Where-Object { $_ -like "*.ps1" })) {
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
    $referenceDefinitionPattern = "^\s*\[([^\]]+)\]:\s*(\S+)"
    $referenceUsagePattern = "!?\[([^\]]+)\]\[([^\]]*)\]"
    $missingLinks = [System.Collections.Generic.List[string]]::new()

    $codeFenceFixture = @(
        '```powershell',
        '[void][scriptblock]::Create(',
        '```',
        '~~~~csharp',
        '[Fact][Trait]',
        '~~~~'
    )
    $codeFixtureMatches = @(
        Get-MarkdownProseLines -Lines $codeFenceFixture |
            ForEach-Object { [regex]::Matches($_, $referenceUsagePattern) }
    )
    if ($codeFixtureMatches.Count -ne 0) {
        throw "Markdown link scanner treated fenced code as reference-style links."
    }

    $brokenReferenceFixture = @(
        Get-MarkdownProseLines -Lines @("[guide][missing-definition]") |
            ForEach-Object { [regex]::Matches($_, $referenceUsagePattern) }
    )
    if ($brokenReferenceFixture.Count -ne 1) {
        throw "Markdown link scanner did not detect a broken reference-style link fixture."
    }

    foreach ($file in ($repositoryFiles | Where-Object { $_ -like "*.md" })) {
        $fullPath = Resolve-RepositoryPath $file
        $directory = Split-Path -Parent $fullPath
        $lines = [IO.File]::ReadAllLines($fullPath)
        $proseLines = @(Get-MarkdownProseLines -Lines $lines)
        $definitions = [System.Collections.Generic.Dictionary[string, string]]::new(
            [StringComparer]::OrdinalIgnoreCase)

        foreach ($line in $proseLines) {
            $definitionMatch = [regex]::Match($line, $referenceDefinitionPattern)
            if ($definitionMatch.Success) {
                $definitions[$definitionMatch.Groups[1].Value.Trim()] =
                    $definitionMatch.Groups[2].Value.Trim()
            }
        }

        $lineNumber = 0

        $insideFence = $false
        $fenceCharacter = $null
        $fenceLength = 0
        foreach ($line in $lines) {
            $lineNumber++
            $fenceMatch = [regex]::Match($line, '^\s{0,3}(`{3,}|~{3,})')
            if ($fenceMatch.Success) {
                $marker = $fenceMatch.Groups[1].Value
                $markerCharacter = $marker[0]
                if (-not $insideFence) {
                    $insideFence = $true
                    $fenceCharacter = $markerCharacter
                    $fenceLength = $marker.Length
                }
                elseif (
                    $markerCharacter -eq $fenceCharacter -and
                    $marker.Length -ge $fenceLength
                ) {
                    $insideFence = $false
                    $fenceCharacter = $null
                    $fenceLength = 0
                }

                continue
            }

            if ($insideFence) {
                continue
            }

            $targets = [System.Collections.Generic.List[string]]::new()
            foreach ($inlineMatch in [regex]::Matches($line, $linkPattern)) {
                $targets.Add($inlineMatch.Groups[1].Value.Trim())
            }

            foreach ($referenceMatch in [regex]::Matches($line, $referenceUsagePattern)) {
                $referenceId = $referenceMatch.Groups[2].Value.Trim()
                if ([string]::IsNullOrWhiteSpace($referenceId)) {
                    $referenceId = $referenceMatch.Groups[1].Value.Trim()
                }

                $target = $null
                if (-not $definitions.TryGetValue($referenceId, [ref]$target)) {
                    $missingLinks.Add(
                        "${file}:${lineNumber}: missing reference definition [$referenceId]"
                    )
                    continue
                }

                $targets.Add($target)
            }

            foreach ($target in $targets) {
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

Invoke-StaticStep ".NET SDK version" {
    $sdkVersion = (& dotnet --version).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet --version failed."
    }

    if (-not $sdkVersion.StartsWith("10.0.", [StringComparison]::Ordinal)) {
        throw "Expected a .NET 10 SDK selected by global.json, found $sdkVersion."
    }

    Write-Host "Selected .NET SDK: $sdkVersion"
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

Invoke-StaticStep "Verification leaves Git working tree unchanged" {
    $finalGitStatus = @(Get-GitWorkingTreeStatus)
    $statusChanges = Compare-Object `
        -ReferenceObject $initialGitStatus `
        -DifferenceObject $finalGitStatus

    if ($statusChanges) {
        $details = $statusChanges |
            ForEach-Object { "$($_.SideIndicator) $($_.InputObject)" }
        throw "Verification changed the Git working tree: $($details -join '; ')"
    }
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
