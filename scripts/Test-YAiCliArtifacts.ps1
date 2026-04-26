#Requires -Version 7.0

<#
.SYNOPSIS
Verifies YAi CLI artifact checksums and ZIP integrity.

.DESCRIPTION
Reads a YAi CLI artifact batch directory, loads expected SHA-256 hashes from the
release manifest, batch checksum file, or per-file .sha256 sidecars, and verifies
that the ZIP files are intact and unmodified. When a detached signature is present
for release-manifest.json, the script verifies authenticity before trusting the
manifest's SHA-256 values.

.EXAMPLE
pwsh ./scripts/Test-YAiCliArtifacts.ps1 -ArtifactRoot ./artifacts/cli/20260427T120000Z

.EXAMPLE
pwsh ./scripts/Test-YAiCliArtifacts.ps1 -ArtifactRoot ./artifacts/cli/20260427T120000Z -ZipPath yai-cli-dotnet10-nativeaot-win-x64-v1.0.0-20260427T120000Z.zip

.EXAMPLE
pwsh ./scripts/Test-YAiCliArtifacts.ps1 --help
#>
[CmdletBinding ()]
param(
    [string]$ArtifactRoot = (Get-Location).Path,

    [string[]]$ZipPath,

    [switch]$AllowUnsigned,

    [ValidateSet('Ed25519', 'RSA-PSS-SHA256')]
    [string]$SignatureAlgorithm = 'RSA-PSS-SHA256',

    [Alias('h')]
    [switch]$Help,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Rule {
    Write-Host ('-' * 78) -ForegroundColor DarkGray
}

function Write-Section {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    Write-Host "`n> $Text" -ForegroundColor Cyan
}

function Write-Info {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    Write-Host $Text -ForegroundColor Gray
}

function Write-Success {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    Write-Host $Text -ForegroundColor Green
}

function Write-Warn {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    Write-Host $Text -ForegroundColor Yellow
}

function Write-Failure {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    Write-Host $Text -ForegroundColor Red
}

function Write-Usage {
    Write-Host
    Write-Rule
    Write-Host 'YAi! CLI Artifact Verifier' -ForegroundColor Magenta
    Write-Rule
    Write-Host 'Checks SHA-256 values for YAi artifact ZIP files and verifies that the archives are readable.' -ForegroundColor White
    Write-Host
    Write-Host 'Usage' -ForegroundColor Cyan
    Write-Host '  pwsh ./scripts/Test-YAiCliArtifacts.ps1 [options]' -ForegroundColor White
    Write-Host
    Write-Host 'Switches' -ForegroundColor Cyan
    Write-Host '  --help, -Help, -h   Show this help text and exit.' -ForegroundColor White
    Write-Host '  -ArtifactRoot <path> Artifact batch directory. Defaults to the current working directory.' -ForegroundColor White
    Write-Host '  -ZipPath <path>[,<...>] One or more ZIP files to verify. Defaults to all ZIPs in the artifact root.' -ForegroundColor White
    Write-Host '  -AllowUnsigned      Allow integrity-only verification when no detached manifest signature is present.' -ForegroundColor White
    Write-Host '  -SignatureAlgorithm <Ed25519|RSA-PSS-SHA256> Detached signature algorithm. Default: RSA-PSS-SHA256.' -ForegroundColor White
    Write-Host
    Write-Host 'What it does' -ForegroundColor Cyan
    Write-Host '  - Verifies release-manifest.json authenticity when release-manifest.json.sig and public-key.yai.pem are present.' -ForegroundColor White
    Write-Host '  - Loads expected SHA-256 values from the signed release manifest when available, otherwise from checksums.sha256 or .sha256 sidecars.' -ForegroundColor White
    Write-Host '  - Recomputes the SHA-256 checksum for each ZIP file.' -ForegroundColor White
    Write-Host '  - Opens each ZIP and reads every file entry to detect archive corruption.' -ForegroundColor White
    Write-Host
    Write-Host 'Examples' -ForegroundColor Cyan
    Write-Host '  pwsh ./scripts/Test-YAiCliArtifacts.ps1 -ArtifactRoot ./artifacts/cli/20260427T120000Z' -ForegroundColor White
    Write-Host '  pwsh ./scripts/Test-YAiCliArtifacts.ps1 -ArtifactRoot ./artifacts/cli/20260427T120000Z -ZipPath artifact-a.zip,artifact-b.zip' -ForegroundColor White
    Write-Host '  pwsh ./scripts/Test-YAiCliArtifacts.ps1 -ArtifactRoot ./artifacts/cli/20260427T120000Z -AllowUnsigned' -ForegroundColor White
    Write-Host '  pwsh ./scripts/Test-YAiCliArtifacts.ps1 -ArtifactRoot ./artifacts/cli/20260427T120000Z -SignatureAlgorithm Ed25519' -ForegroundColor White
    Write-Rule
}

function Test-HelpRequested {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$HelpSwitch,

        [AllowEmptyCollection()]
        [string[]]$Remaining
    )

    if ($HelpSwitch) {
        return $true
    }

    if ($null -eq $Remaining) {
        return $false
    }

    return @($Remaining) -contains '--help'
}

function Assert-NoUnexpectedArguments {
    param(
        [AllowEmptyCollection()]
        [string[]]$Remaining
    )

    $unexpected = @($Remaining | Where-Object {
            -not [string]::IsNullOrWhiteSpace($_) -and $_ -ne '--help'
        })

    if ($unexpected.Count -eq 0) {
        return
    }

    throw "Unexpected arguments: $($unexpected -join ', ')"
}

function Add-ChecksumEntriesFromLines {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$Target,

        [Parameter(Mandatory = $true)]
        [string[]]$Lines
    )

    foreach ($line in $Lines) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        if ($line -match '^(?<hash>[A-Fa-f0-9]{64})\s+\*?(?<file>.+)$') {
            $Target[$Matches.file.Trim()] = $Matches.hash.ToLowerInvariant()
        }
    }
}

function Get-ManifestExpectedChecksums {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ManifestPath
    )

    $checksums = [ordered]@{}
    $manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
    foreach ($artifact in @($manifest.artifacts)) {
        if ($artifact.status -eq 'Succeeded' -and -not [string]::IsNullOrWhiteSpace($artifact.sha256) -and -not [string]::IsNullOrWhiteSpace($artifact.zipFileName)) {
            $checksums[$artifact.zipFileName] = $artifact.sha256.ToLowerInvariant()
        }
    }

    return $checksums
}

function Get-SidecarExpectedChecksums {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedArtifactRoot
    )

    $checksums = [ordered]@{}

    $batchChecksumsPath = Join-Path -Path $ResolvedArtifactRoot -ChildPath 'checksums.sha256'
    if (Test-Path -LiteralPath $batchChecksumsPath) {
        Add-ChecksumEntriesFromLines -Target $checksums -Lines (Get-Content -LiteralPath $batchChecksumsPath)
    }

    Get-ChildItem -Path $ResolvedArtifactRoot -Filter '*.sha256' -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -ne 'checksums.sha256' } |
        ForEach-Object {
            Add-ChecksumEntriesFromLines -Target $checksums -Lines (Get-Content -LiteralPath $_.FullName)
        }

    return $checksums
}

function Get-OutputExcerpt {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [object[]]$OutputLines
    )

    $text = @($OutputLines | ForEach-Object { "$_" })
    if ($text.Count -eq 0) {
        return 'No verifier output was captured.'
    }

    return (($text | Select-Object -Last 12) -join [Environment]::NewLine).Trim()
}

function Test-ManifestAuthenticity {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedArtifactRoot,

        [Parameter(Mandatory = $true)]
        [string]$SignerProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$Algorithm,

        [Parameter(Mandatory = $true)]
        [bool]$AllowUnsignedManifest
    )

    $manifestPath = Join-Path -Path $ResolvedArtifactRoot -ChildPath 'release-manifest.json'
    $signaturePath = Join-Path -Path $ResolvedArtifactRoot -ChildPath 'release-manifest.json.sig'
    $publicKeyPath = Join-Path -Path $ResolvedArtifactRoot -ChildPath 'public-key.yai.pem'

    if (-not (Test-Path -LiteralPath $manifestPath)) {
        if ($AllowUnsignedManifest) {
            return [pscustomobject]@{
                Status = 'Skipped'
                Message = 'release-manifest.json was not found. Falling back to checksum sidecars.'
                ManifestPath = $null
                SignaturePath = $null
                PublicKeyPath = $null
            }
        }

        throw "Authenticity verification requires release-manifest.json in $ResolvedArtifactRoot"
    }

    if (-not (Test-Path -LiteralPath $signaturePath) -or -not (Test-Path -LiteralPath $publicKeyPath)) {
        if ($AllowUnsignedManifest) {
            return [pscustomobject]@{
                Status = 'Skipped'
                Message = 'Detached manifest signature files were not found. Falling back to checksum sidecars.'
                ManifestPath = $manifestPath
                SignaturePath = $signaturePath
                PublicKeyPath = $publicKeyPath
            }
        }

        throw 'Authenticity verification requires release-manifest.json.sig and public-key.yai.pem in the artifact batch. Use -AllowUnsigned to accept integrity-only verification.'
    }

    if (-not (Test-Path -LiteralPath $SignerProjectPath)) {
        throw "Signer project not found: $SignerProjectPath"
    }

    $arguments = @(
        'run',
        '--project', $SignerProjectPath,
        '--',
        'verify-file',
        '--file', $manifestPath,
        '--signature', $signaturePath,
        '--public-key', $publicKeyPath,
        '--algorithm', $Algorithm
    )

    $null = & dotnet @arguments 2>&1 | Tee-Object -Variable capturedOutput
    if ($LASTEXITCODE -ne 0) {
        throw (Get-OutputExcerpt -OutputLines $capturedOutput)
    }

    return [pscustomobject]@{
        Status = 'Succeeded'
        Message = 'Detached release-manifest signature verified.'
        ManifestPath = $manifestPath
        SignaturePath = $signaturePath
        PublicKeyPath = $publicKeyPath
    }
}

function Get-SelectedZipPaths {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedArtifactRoot,

        [string[]]$RequestedZipPaths
    )

    $normalizedZipPaths = @($RequestedZipPaths |
        ForEach-Object { "$_" -split ',' } |
        ForEach-Object { $_.Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

    if ($normalizedZipPaths.Count -eq 0) {
        return ,@(
            Get-ChildItem -Path $ResolvedArtifactRoot -Filter '*.zip' -File -ErrorAction Stop |
                ForEach-Object { $_.FullName }
        )
    }

    return ,@(
        $normalizedZipPaths | ForEach-Object {
            if ([System.IO.Path]::IsPathRooted($_)) {
                $_
            }
            else {
                Join-Path -Path $ResolvedArtifactRoot -ChildPath $_
            }
        }
    )
}

function Test-ZipIntegrity {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ZipFullPath
    )

    $archive = $null
    try {
        $archive = [System.IO.Compression.ZipFile]::OpenRead($ZipFullPath)
        $buffer = [byte[]]::new(81920)

        foreach ($entry in $archive.Entries) {
            if ([string]::IsNullOrEmpty($entry.Name)) {
                continue
            }

            $stream = $entry.Open()
            try {
                while ($stream.Read($buffer, 0, $buffer.Length) -gt 0) {
                }
            }
            finally {
                $stream.Dispose()
            }
        }

        return [pscustomobject]@{
            IsValid = $true
            Message = 'ZIP integrity check passed.'
        }
    }
    catch {
        return [pscustomobject]@{
            IsValid = $false
            Message = $_.Exception.Message
        }
    }
    finally {
        if ($null -ne $archive) {
            $archive.Dispose()
        }
    }
}

try {
    if (Test-HelpRequested -HelpSwitch:$Help.IsPresent -Remaining $RemainingArguments) {
        Write-Usage
        exit 0
    }

    Assert-NoUnexpectedArguments -Remaining $RemainingArguments

    $repoRoot = Split-Path -Path $PSScriptRoot -Parent
    $signerProjectPath = Join-Path -Path $repoRoot -ChildPath 'src\YAi.Tools.ResourceSigner\YAi.Tools.ResourceSigner.csproj'
    $resolvedArtifactRoot = (Resolve-Path -LiteralPath $ArtifactRoot).Path
    $authenticityResult = Test-ManifestAuthenticity -ResolvedArtifactRoot $resolvedArtifactRoot -SignerProjectPath $signerProjectPath -Algorithm $SignatureAlgorithm -AllowUnsignedManifest:$AllowUnsigned.IsPresent

    if ($authenticityResult.Status -eq 'Succeeded') {
        $expectedChecksums = Get-ManifestExpectedChecksums -ManifestPath $authenticityResult.ManifestPath
    }
    else {
        $expectedChecksums = Get-SidecarExpectedChecksums -ResolvedArtifactRoot $resolvedArtifactRoot
    }

    $zipPaths = Get-SelectedZipPaths -ResolvedArtifactRoot $resolvedArtifactRoot -RequestedZipPaths $ZipPath

    if (@($zipPaths).Count -eq 0) {
        throw "No ZIP files were found under $resolvedArtifactRoot"
    }

    Write-Rule
    Write-Host 'YAi! CLI Artifact Verifier' -ForegroundColor Magenta
    Write-Rule
    Write-Info "Artifact root : $resolvedArtifactRoot"

    switch ($authenticityResult.Status) {
        'Succeeded' {
            Write-Success $authenticityResult.Message
        }
        'Skipped' {
            Write-Warn $authenticityResult.Message
        }
    }

    $failures = 0

    foreach ($zipFullPath in $zipPaths) {
        Write-Section ([System.IO.Path]::GetFileName($zipFullPath))

        if (-not (Test-Path -LiteralPath $zipFullPath)) {
            Write-Failure "ZIP file not found: $zipFullPath"
            $failures++
            continue
        }

        $zipFileName = [System.IO.Path]::GetFileName($zipFullPath)
        $expectedHash = $expectedChecksums[$zipFileName]
        if ([string]::IsNullOrWhiteSpace($expectedHash)) {
            Write-Failure "No expected SHA-256 value was found for $zipFileName"
            $failures++
            continue
        }

        $actualHash = (Get-FileHash -LiteralPath $zipFullPath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actualHash -ne $expectedHash) {
            Write-Failure "SHA-256 mismatch. Expected $expectedHash but found $actualHash"
            $failures++
            continue
        }

        Write-Success "SHA-256 verified: $actualHash"

        $zipIntegrity = Test-ZipIntegrity -ZipFullPath $zipFullPath
        if (-not $zipIntegrity.IsValid) {
            Write-Failure "ZIP integrity failed: $($zipIntegrity.Message)"
            $failures++
            continue
        }

        Write-Success $zipIntegrity.Message
    }

    Write-Section 'Summary'
    if ($failures -gt 0) {
        Write-Failure "Verification failed for $failures artifact(s)."
        exit 1
    }

    Write-Success 'All selected artifacts passed SHA-256 and ZIP integrity verification.'
    exit 0
}
catch {
    Write-Failure $_.Exception.Message
    exit 1
}