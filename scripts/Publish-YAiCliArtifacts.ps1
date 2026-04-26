#Requires -Version 7.0

<#
.SYNOPSIS
Builds zipped YAi CLI distribution artifacts for Windows.

.DESCRIPTION
Publishes framework-dependent, self-contained, and best-effort NativeAOT variants of
YAi.Client.CLI for the requested Windows runtime identifiers. Each successful publish
is zipped with a descriptive name that includes the current repository version and a
UTC timestamp. The script also writes SHA-256 checksum files and a release manifest
for each artifact batch. When the YAi signing keys are available, the script also
creates a detached signature for the batch release manifest and copies the public key
into the artifact batch.

.EXAMPLE
pwsh ./scripts/Publish-YAiCliArtifacts.ps1

.EXAMPLE
pwsh ./scripts/Publish-YAiCliArtifacts.ps1 -SkipAot -RuntimeIdentifier win-x64

.EXAMPLE
pwsh ./scripts/Publish-YAiCliArtifacts.ps1 -Variant SelfContained -RuntimeIdentifier win-arm64

.EXAMPLE
pwsh ./scripts/Publish-YAiCliArtifacts.ps1 -Variant SelfContained,Aot -RuntimeIdentifier win-x64,win-arm64

.EXAMPLE
pwsh ./scripts/Publish-YAiCliArtifacts.ps1 --help
#>
[CmdletBinding ()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$OutputRoot,

    [string[]]$Variant = @('FrameworkDependent', 'SelfContained', 'Aot'),

    [string[]]$RuntimeIdentifier = @('win-x64', 'win-arm64'),

    [switch]$SkipAot,

    [switch]$KeepPublishFolders,

    [switch]$SkipDetachedSignature,

    [string]$ReleaseSigningPrivateKeyPath,

    [string]$ReleaseSigningPublicKeyPath,

    [ValidateSet('Ed25519', 'RSA-PSS-SHA256')]
    [string]$ReleaseSignatureAlgorithm = 'RSA-PSS-SHA256',

    [string]$SigningPassphraseEnvironmentVariable = 'YAI_SIGNING_PASSPHRASE',

    [Alias('h')]
    [switch]$Help,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Set-ConsoleUtf8 {
    $utf8 = [System.Text.UTF8Encoding]::new($false)
    $script:OriginalOutputEncoding = [Console]::OutputEncoding
    $script:OriginalInputEncoding = [Console]::InputEncoding
    $script:OriginalPsOutputEncoding = $OutputEncoding

    [Console]::OutputEncoding = $utf8
    [Console]::InputEncoding = $utf8
    $OutputEncoding = $utf8
}

function Restore-ConsoleEncoding {
    if ($null -ne $script:OriginalOutputEncoding) {
        [Console]::OutputEncoding = $script:OriginalOutputEncoding
    }

    if ($null -ne $script:OriginalInputEncoding) {
        [Console]::InputEncoding = $script:OriginalInputEncoding
    }

    if ($null -ne $script:OriginalPsOutputEncoding) {
        $OutputEncoding = $script:OriginalPsOutputEncoding
    }
}

function Write-Rule {
    Write-Host ('─' * 78) -ForegroundColor DarkGray
}

function Write-Section {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    Write-Host "`n▶ $Text" -ForegroundColor Cyan
}

function Write-Info {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    Write-Host "ℹ $Text" -ForegroundColor Gray
}

function Write-Success {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    Write-Host "✅ $Text" -ForegroundColor Green
}

function Write-Warn {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    Write-Host "⚠ $Text" -ForegroundColor Yellow
}

function Write-Failure {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    Write-Host "❌ $Text" -ForegroundColor Red
}

function Write-Header {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version,

        [Parameter(Mandatory = $true)]
        [string]$Timestamp,

        [Parameter(Mandatory = $true)]
        [string]$TargetOutputRoot
    )

    Write-Host
    Write-Rule
    Write-Host '📦 YAi! CLI Artifact Publisher' -ForegroundColor Magenta
    Write-Host '🌐 https://umbertogiacobbi.biz' -ForegroundColor Blue
    Write-Host '✉  mailto:hello@umbertogiacobbi.biz' -ForegroundColor Blue
    Write-Host '🔗 https://github.com/umbertotechnopreneur/YAi' -ForegroundColor Blue
    Write-Host "🏷  Version   : $Version" -ForegroundColor White
    Write-Host "🕒 Timestamp : $Timestamp" -ForegroundColor White
    Write-Host "📁 Output    : $TargetOutputRoot" -ForegroundColor White
    Write-Rule
}

function Write-Usage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version,

        [Parameter(Mandatory = $true)]
        [string]$DefaultOutputRoot
    )

    Write-Host
    Write-Rule
    Write-Host '📦 YAi! CLI Artifact Publisher' -ForegroundColor Magenta
    Write-Host '🌐 https://umbertogiacobbi.biz' -ForegroundColor Blue
    Write-Host '✉  mailto:hello@umbertogiacobbi.biz' -ForegroundColor Blue
    Write-Host '🔗 https://github.com/umbertotechnopreneur/YAi' -ForegroundColor Blue
    Write-Host "🏷  Version   : $Version" -ForegroundColor White
    Write-Rule
    Write-Host 'Builds zipped YAi.Client.CLI Windows artifacts from the current repository version.' -ForegroundColor White
    Write-Host
    Write-Host 'Usage' -ForegroundColor Cyan
    Write-Host '  pwsh ./scripts/Publish-YAiCliArtifacts.ps1 [options]' -ForegroundColor White
    Write-Host
    Write-Host 'Switches' -ForegroundColor Cyan
    Write-Host '  --help, -Help, -h             Show this help text and exit.' -ForegroundColor White
    Write-Host '  -Configuration <Debug|Release> Build configuration. Default: Release.' -ForegroundColor White
    Write-Host "  -OutputRoot <path>            Artifact root. Default: $DefaultOutputRoot" -ForegroundColor White
    Write-Host '  -Variant <FrameworkDependent|SelfContained|Aot>[,<...>]' -ForegroundColor White
    Write-Host '                               Select one or more publish variants. Comma-separated values are supported.' -ForegroundColor White
    Write-Host '  -RuntimeIdentifier <win-x64|win-arm64>[,<...>]' -ForegroundColor White
    Write-Host '                               Select one or more Windows target architectures. Comma-separated values are supported.' -ForegroundColor White
    Write-Host '  -SkipAot                     Skip NativeAOT attempts even if Aot is selected.' -ForegroundColor White
    Write-Host '  -KeepPublishFolders          Preserve unzipped publish folders next to zip artifacts.' -ForegroundColor White
    Write-Host '  -SkipDetachedSignature       Skip detached signature creation for release-manifest.json.' -ForegroundColor White
    Write-Host '  -ReleaseSigningPrivateKeyPath <path>' -ForegroundColor White
    Write-Host '                               Override the private key used to sign release-manifest.json.' -ForegroundColor White
    Write-Host '  -ReleaseSigningPublicKeyPath <path>' -ForegroundColor White
    Write-Host '                               Override the public key copied into the artifact batch.' -ForegroundColor White
    Write-Host '  -ReleaseSignatureAlgorithm <Ed25519|RSA-PSS-SHA256>' -ForegroundColor White
    Write-Host '                               Select the detached signature algorithm. Default: RSA-PSS-SHA256.' -ForegroundColor White
    Write-Host '  -SigningPassphraseEnvironmentVariable <name>' -ForegroundColor White
    Write-Host '                               Environment variable used for non-interactive signing. Default: YAI_SIGNING_PASSPHRASE.' -ForegroundColor White
    Write-Host
    Write-Host 'What it does' -ForegroundColor Cyan
    Write-Host '  - Reads the current version from Directory.Build.props.' -ForegroundColor White
    Write-Host '  - Publishes framework-dependent, self-contained, and optional NativeAOT CLI outputs.' -ForegroundColor White
    Write-Host '  - Creates one zip per artifact with version and UTC timestamp in the filename.' -ForegroundColor White
    Write-Host '  - Writes SHA-256 checksum files and a release-manifest.json file for each batch.' -ForegroundColor White
    Write-Host '  - Signs release-manifest.json and copies public-key.yai.pem into the batch when signing keys are available.' -ForegroundColor White
    Write-Host '  - Writes artifacts under artifacts/cli/<utc-timestamp>/.' -ForegroundColor White
    Write-Host
    Write-Host 'Examples' -ForegroundColor Cyan
    Write-Host '  pwsh ./scripts/Publish-YAiCliArtifacts.ps1' -ForegroundColor White
    Write-Host '  pwsh ./scripts/Publish-YAiCliArtifacts.ps1 -SkipAot -RuntimeIdentifier win-x64' -ForegroundColor White
    Write-Host '  pwsh ./scripts/Publish-YAiCliArtifacts.ps1 -Variant SelfContained,Aot -RuntimeIdentifier win-x64,win-arm64' -ForegroundColor White
    Write-Host '  pwsh ./scripts/Publish-YAiCliArtifacts.ps1 -SkipAot -Variant FrameworkDependent -RuntimeIdentifier win-x64 -SkipDetachedSignature' -ForegroundColor White
    Write-Host '  pwsh ./scripts/Test-YAiCliArtifacts.ps1 -ArtifactRoot ./artifacts/cli/<utc-timestamp>' -ForegroundColor White
    Write-Host
    Write-Host 'Notes' -ForegroundColor Cyan
    Write-Host '  NativeAOT is supported when the required Visual Studio C++ workloads and Windows SDK libraries are installed.' -ForegroundColor White
    Write-Host '  The script detects missing prerequisites before AOT publish starts and reports what is missing.' -ForegroundColor White
    Write-Host '  Detached signing uses the YAi resource signing tool and signs release-manifest.json.' -ForegroundColor White
    Write-Host '  RSA-PSS-SHA256 is the current default release-signing algorithm. Use -ReleaseSignatureAlgorithm Ed25519 only for older compatibility scenarios.' -ForegroundColor White
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

function Get-RepositoryVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PropsPath
    )

    if (-not (Test-Path -LiteralPath $PropsPath)) {
        throw "Version file not found: $PropsPath"
    }

    [xml]$propsXml = Get-Content -LiteralPath $PropsPath -Raw
    $version = $propsXml.Project.PropertyGroup.Version | Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "Could not resolve <Version> from $PropsPath"
    }

    return $version.Trim()
}

function Get-VariantSlug {
    param(
        [Parameter(Mandatory = $true)]
        [string]$VariantName
    )

    switch ($VariantName) {
        'FrameworkDependent' { return 'framework-dependent' }
        'SelfContained' { return 'self-contained' }
        'Aot' { return 'nativeaot' }
        default { throw "Unsupported variant: $VariantName" }
    }
}

function Get-PublishArguments {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$ConfigurationName,

        [Parameter(Mandatory = $true)]
        [string]$VariantName,

        [Parameter(Mandatory = $true)]
        [string]$Rid,

        [Parameter(Mandatory = $true)]
        [string]$PublishDirectory
    )

    $arguments = @(
        'publish',
        $ProjectPath,
        '-c', $ConfigurationName,
        '-r', $Rid,
        '--output', $PublishDirectory,
        '--nologo'
    )

    switch ($VariantName) {
        'FrameworkDependent' {
            $arguments += @('--self-contained', 'false', '-p:UseAppHost=true')
        }
        'SelfContained' {
            $arguments += @('--self-contained', 'true')
        }
        'Aot' {
            $arguments += @('--self-contained', 'true', '-p:PublishAot=true')
        }
        default {
            throw "Unsupported variant: $VariantName"
        }
    }

    return $arguments
}

function Get-OutputExcerpt {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [object[]]$OutputLines
    )

    $text = @($OutputLines | ForEach-Object { "$_" })
    if ($text.Count -eq 0) {
        return 'No publish output was captured.'
    }

    return (($text | Select-Object -Last 12) -join [Environment]::NewLine).Trim()
}

function Get-ChecksumLine {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Sha256,

        [Parameter(Mandatory = $true)]
        [string]$FileName
    )

    return '{0} *{1}' -f $Sha256.ToLowerInvariant(), $FileName
}

function New-ArtifactChecksum {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ZipPath
    )

    $zipItem = Get-Item -LiteralPath $ZipPath
    $sha256 = (Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $checksumLine = Get-ChecksumLine -Sha256 $sha256 -FileName $zipItem.Name
    $checksumPath = '{0}.sha256' -f $ZipPath

    Set-Content -LiteralPath $checksumPath -Value $checksumLine -Encoding utf8NoBOM

    return [pscustomobject]@{
        Sha256       = $sha256
        ChecksumLine = $checksumLine
        ChecksumPath = $checksumPath
        SizeBytes    = $zipItem.Length
    }
}

function Write-ChecksumBatchFile {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Results,

        [Parameter(Mandatory = $true)]
        [string]$RunOutputRoot
    )

    $successfulResults = @($Results | Where-Object {
            $_.Status -eq 'Succeeded' -and -not [string]::IsNullOrWhiteSpace($_.ChecksumLine)
        })

    if ($successfulResults.Count -eq 0) {
        return $null
    }

    $checksumsPath = Join-Path -Path $RunOutputRoot -ChildPath 'checksums.sha256'
    $lines = @($successfulResults | ForEach-Object { $_.ChecksumLine })
    Set-Content -LiteralPath $checksumsPath -Value $lines -Encoding utf8NoBOM

    return $checksumsPath
}

function Write-ReleaseManifest {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Results,

        [Parameter(Mandatory = $true)]
        [string]$RunOutputRoot,

        [Parameter(Mandatory = $true)]
        [string]$Version,

        [Parameter(Mandatory = $true)]
        [string]$ConfigurationName,

        [Parameter(Mandatory = $true)]
        [string]$Timestamp,

        [Parameter(Mandatory = $true)]
        [string[]]$SelectedVariants,

        [Parameter(Mandatory = $true)]
        [string[]]$SelectedRuntimeIdentifiers
    )

    $manifestPath = Join-Path -Path $RunOutputRoot -ChildPath 'release-manifest.json'
    $manifest = [ordered]@{
        product                     = 'YAi.Client.CLI'
        version                     = $Version
        configuration               = $ConfigurationName
        generatedAtUtc              = [DateTime]::UtcNow.ToString('o')
        batchTimestamp              = $Timestamp
        artifactRoot                = $RunOutputRoot
        requestedVariants           = @($SelectedVariants)
        requestedRuntimeIdentifiers = @($SelectedRuntimeIdentifiers)
        artifacts                   = @($Results | ForEach-Object {
                [ordered]@{
                    variant           = $_.Variant
                    runtimeIdentifier = $_.RuntimeIdentifier
                    status            = $_.Status
                    artifactName      = $_.ArtifactName
                    zipFileName       = if ([string]::IsNullOrWhiteSpace($_.ZipPath)) { $null } else { [System.IO.Path]::GetFileName($_.ZipPath) }
                    zipPath           = $_.ZipPath
                    sha256            = $_.Sha256
                    checksumFileName  = if ([string]::IsNullOrWhiteSpace($_.ChecksumPath)) { $null } else { [System.IO.Path]::GetFileName($_.ChecksumPath) }
                    checksumPath      = $_.ChecksumPath
                    sizeBytes         = $_.SizeBytes
                    message           = $_.Message
                }
            })
    }

    $json = $manifest | ConvertTo-Json -Depth 8
    Set-Content -LiteralPath $manifestPath -Value $json -Encoding utf8NoBOM

    return $manifestPath
}

function New-DetachedManifestSignature {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SignerProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$ManifestPath,

        [Parameter(Mandatory = $true)]
        [string]$RunOutputRoot,

        [Parameter(Mandatory = $true)]
        [string]$PrivateKeyPath,

        [Parameter(Mandatory = $true)]
        [string]$PublicKeyPath,

        [Parameter(Mandatory = $true)]
        [string]$Algorithm,

        [Parameter(Mandatory = $true)]
        [string]$PassphraseEnvironmentVariable
    )

    if (-not (Test-Path -LiteralPath $SignerProjectPath)) {
        return [pscustomobject]@{
            Status        = 'Failed'
            Message       = "Signer project not found: $SignerProjectPath"
            SignaturePath = $null
            PublicKeyPath = $null
            Algorithm     = $Algorithm
        }
    }

    if (-not (Test-Path -LiteralPath $PrivateKeyPath)) {
        return [pscustomobject]@{
            Status        = 'Skipped'
            Message       = "Detached signing skipped because the private key was not found at $PrivateKeyPath"
            SignaturePath = $null
            PublicKeyPath = $null
            Algorithm     = $Algorithm
        }
    }

    if (-not (Test-Path -LiteralPath $PublicKeyPath)) {
        return [pscustomobject]@{
            Status        = 'Skipped'
            Message       = "Detached signing skipped because the public key was not found at $PublicKeyPath"
            SignaturePath = $null
            PublicKeyPath = $null
            Algorithm     = $Algorithm
        }
    }

    $batchPublicKeyPath = Join-Path -Path $RunOutputRoot -ChildPath 'public-key.yai.pem'
    $signaturePath = Join-Path -Path $RunOutputRoot -ChildPath 'release-manifest.json.sig'
    Copy-Item -LiteralPath $PublicKeyPath -Destination $batchPublicKeyPath -Force

    if (Test-Path -LiteralPath $signaturePath) {
        Remove-Item -LiteralPath $signaturePath -Force
    }

    $signerArguments = @(
        'run',
        '--project', $SignerProjectPath,
        '--',
        'sign-file',
        '--file', $ManifestPath,
        '--private-key', $PrivateKeyPath,
        '--signature', $signaturePath,
        '--algorithm', $Algorithm
    )

    if (-not [string]::IsNullOrWhiteSpace($PassphraseEnvironmentVariable)) {
        $passphraseValue = [Environment]::GetEnvironmentVariable($PassphraseEnvironmentVariable)
        if (-not [string]::IsNullOrWhiteSpace($passphraseValue)) {
            $signerArguments += @('--passphrase-env', $PassphraseEnvironmentVariable)
        }
    }

    $null = & dotnet @signerArguments 2>&1 | Tee-Object -Variable capturedOutput
    if ($LASTEXITCODE -ne 0) {
        return [pscustomobject]@{
            Status        = 'Failed'
            Message       = Get-OutputExcerpt -OutputLines $capturedOutput
            SignaturePath = $null
            PublicKeyPath = $batchPublicKeyPath
            Algorithm     = $Algorithm
        }
    }

    return [pscustomobject]@{
        Status        = 'Succeeded'
        Message       = 'Detached release-manifest signature created.'
        SignaturePath = $signaturePath
        PublicKeyPath = $batchPublicKeyPath
        Algorithm     = $Algorithm
    }
}

function Invoke-PublishArtifact {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$ConfigurationName,

        [Parameter(Mandatory = $true)]
        [string]$VariantName,

        [Parameter(Mandatory = $true)]
        [string]$Rid,

        [Parameter(Mandatory = $true)]
        [string]$Version,

        [Parameter(Mandatory = $true)]
        [string]$Timestamp,

        [Parameter(Mandatory = $true)]
        [string]$RunOutputRoot,

        [Parameter(Mandatory = $true)]
        [bool]$PreservePublishFolder
    )

    $variantSlug = Get-VariantSlug -VariantName $VariantName
    $artifactBaseName = "yai-cli-dotnet10-$variantSlug-$Rid-v$Version-$Timestamp"
    $publishRoot = Join-Path -Path $RunOutputRoot -ChildPath '_staging'
    $publishDirectory = Join-Path -Path $publishRoot -ChildPath $artifactBaseName
    $zipPath = Join-Path -Path $RunOutputRoot -ChildPath "$artifactBaseName.zip"

    if (Test-Path -LiteralPath $publishDirectory) {
        Remove-Item -LiteralPath $publishDirectory -Recurse -Force
    }

    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null

    Write-Section "$VariantName | $Rid"
    Write-Info "Publishing to $publishDirectory"

    $publishArgumentParameters = @{
        ProjectPath       = $ProjectPath
        ConfigurationName = $ConfigurationName
        VariantName       = $VariantName
        Rid               = $Rid
        PublishDirectory  = $publishDirectory
    }

    $arguments = Get-PublishArguments @publishArgumentParameters

    $null = & dotnet @arguments 2>&1 | Tee-Object -Variable capturedOutput
    if ($LASTEXITCODE -ne 0) {
        $excerpt = Get-OutputExcerpt -OutputLines $capturedOutput
        return [pscustomobject]@{
            Variant           = $VariantName
            RuntimeIdentifier = $Rid
            ArtifactName      = $artifactBaseName
            ZipPath           = $null
            Status            = 'Failed'
            Message           = $excerpt
        }
    }

    Compress-Archive -LiteralPath $publishDirectory -DestinationPath $zipPath -CompressionLevel Optimal -Force
    Write-Success "Created $zipPath"

    $checksumInfo = New-ArtifactChecksum -ZipPath $zipPath
    Write-Success "Created $($checksumInfo.ChecksumPath)"

    if (-not $PreservePublishFolder -and (Test-Path -LiteralPath $publishDirectory)) {
        Remove-Item -LiteralPath $publishDirectory -Recurse -Force
    }

    return [pscustomobject]@{
        Variant           = $VariantName
        RuntimeIdentifier = $Rid
        ArtifactName      = $artifactBaseName
        ZipPath           = $zipPath
        Status            = 'Succeeded'
        Sha256            = $checksumInfo.Sha256
        ChecksumLine      = $checksumInfo.ChecksumLine
        ChecksumPath      = $checksumInfo.ChecksumPath
        SizeBytes         = $checksumInfo.SizeBytes
        Message           = if ($PreservePublishFolder) { "Zip and publish folder created." } else { 'Zip created.' }
    }
}

function Test-BaselineVariant {
    param(
        [Parameter(Mandatory = $true)]
        [string]$VariantName
    )

    return $VariantName -in @('FrameworkDependent', 'SelfContained')
}

function Get-NativeAotPreflightResult {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Rid
    )

    if (-not $IsWindows) {
        return [pscustomobject]@{
            RuntimeIdentifier = $Rid
            IsReady           = $false
            Message           = 'NativeAOT publishing in this workflow is currently supported only on Windows hosts.'
        }
    }

    $vsWherePath = Join-Path -Path ${env:ProgramFiles(x86)} -ChildPath 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vsWherePath)) {
        return [pscustomobject]@{
            RuntimeIdentifier = $Rid
            IsReady           = $false
            Message           = 'Visual Studio Installer tooling was not found. Install Visual Studio with Desktop Development with C++.'
        }
    }

    $anyInstallationPath = @(& $vsWherePath -prerelease -all -products * -property installationPath -format value 2>$null | Where-Object {
            -not [string]::IsNullOrWhiteSpace($_)
        } | Select-Object -First 1)

    $requiredComponents = @('Microsoft.VisualStudio.Workload.NativeDesktop')
    if ($Rid -eq 'win-arm64') {
        $requiredComponents += 'Microsoft.VisualStudio.Component.VC.Tools.ARM64'
    }
    else {
        $requiredComponents += 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64'
    }

    $installationPath = @(& $vsWherePath -prerelease -all -products * -requires $requiredComponents -property installationPath -format value 2>$null | Where-Object {
            -not [string]::IsNullOrWhiteSpace($_)
        } | Select-Object -First 1)

    if ($installationPath.Count -eq 0) {
        $message = if ($Rid -eq 'win-arm64') {
            if ($anyInstallationPath.Count -gt 0) {
                "The ARM64 MSVC build tools component was not detected. Install it with: Start-Process -Verb RunAs -FilePath `"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vs_installer.exe`" -ArgumentList 'modify --installPath `"$($anyInstallationPath[0])`" --add Microsoft.VisualStudio.Component.VC.Tools.ARM64 --passive --norestart' -Wait"
            }
            else {
                'Missing NativeAOT prerequisites. Install Desktop Development with C++ and the C++ ARM64 build tools in Visual Studio.'
            }
        }
        else {
            if ($anyInstallationPath.Count -gt 0) {
                "The x64 MSVC build tools component was not detected. Install it with: Start-Process -Verb RunAs -FilePath `"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vs_installer.exe`" -ArgumentList 'modify --installPath `"$($anyInstallationPath[0])`" --add Microsoft.VisualStudio.Component.VC.Tools.x86.x64 --passive --norestart' -Wait"
            }
            else {
                'The x64 MSVC build tools component was not detected. Install or repair Desktop Development with C++ in Visual Studio, including Microsoft.VisualStudio.Component.VC.Tools.x86.x64.'
            }
        }

        return [pscustomobject]@{
            RuntimeIdentifier = $Rid
            IsReady           = $false
            Message           = $message
        }
    }

    $targetArchitecture = if ($Rid -eq 'win-arm64') { 'arm64' } else { 'x64' }
    $msvcRoot = Join-Path -Path $installationPath[0] -ChildPath 'VC\Tools\MSVC'
    $msvcVersionDirectory = Get-ChildItem -Path $msvcRoot -Directory -ErrorAction SilentlyContinue |
    Sort-Object -Property Name -Descending |
    Select-Object -First 1

    if ($null -eq $msvcVersionDirectory) {
        return [pscustomobject]@{
            RuntimeIdentifier = $Rid
            IsReady           = $false
            Message           = 'MSVC build tools were not found under the selected Visual Studio installation.'
        }
    }

    $linkerPath = Join-Path -Path $msvcVersionDirectory.FullName -ChildPath "bin\Hostx64\$targetArchitecture\link.exe"
    if (-not (Test-Path -LiteralPath $linkerPath)) {
        $message = if ($Rid -eq 'win-arm64') {
            'The ARM64 MSVC linker was not found. Install the C++ ARM64 build tools in Visual Studio.'
        }
        else {
            'The x64 MSVC linker was not found. Install the Desktop Development with C++ build tools in Visual Studio.'
        }

        return [pscustomobject]@{
            RuntimeIdentifier = $Rid
            IsReady           = $false
            Message           = $message
        }
    }

    $windowsKitsLibRoot = Join-Path -Path ${env:ProgramFiles(x86)} -ChildPath 'Windows Kits\10\Lib'
    $sdkLibrary = Get-ChildItem -Path $windowsKitsLibRoot -Directory -ErrorAction SilentlyContinue |
    Sort-Object -Property Name -Descending |
    ForEach-Object {
        $candidatePath = Join-Path -Path $_.FullName -ChildPath "um\$targetArchitecture\advapi32.lib"
        if (Test-Path -LiteralPath $candidatePath) {
            Get-Item -LiteralPath $candidatePath
        }
    } |
    Select-Object -First 1

    if ($null -eq $sdkLibrary) {
        $message = if ($Rid -eq 'win-arm64') {
            'Windows SDK ARM64 libraries were not found. Install a Windows SDK in Visual Studio for NativeAOT arm64 publishing.'
        }
        else {
            'Windows SDK x64 libraries were not found. Install a Windows SDK in Visual Studio for NativeAOT x64 publishing.'
        }

        return [pscustomobject]@{
            RuntimeIdentifier = $Rid
            IsReady           = $false
            Message           = $message
        }
    }

    return [pscustomobject]@{
        RuntimeIdentifier = $Rid
        IsReady           = $true
        Message           = "NativeAOT prerequisites detected in $($installationPath[0])."
    }
}

function Get-SelectedVariants {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$RequestedVariants,

        [Parameter(Mandatory = $true)]
        [bool]$ExcludeAot
    )

    $selected = [System.Collections.Generic.List[string]]::new()
    $allowedVariants = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    @('FrameworkDependent', 'SelfContained', 'Aot') | ForEach-Object { $allowedVariants.Add($_) | Out-Null }

    $normalizedVariants = @($RequestedVariants |
        ForEach-Object { "$_" -split ',' } |
        ForEach-Object { $_.Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Unique)

    foreach ($variantName in $normalizedVariants) {
        if (-not $allowedVariants.Contains($variantName)) {
            throw "Unsupported variant: $variantName. Allowed values: FrameworkDependent, SelfContained, Aot."
        }

        if ($ExcludeAot -and $variantName -eq 'Aot') {
            continue
        }

        $selected.Add($variantName)
    }

    if ($selected.Count -eq 0) {
        throw 'No artifact variants remain after applying the current switches.'
    }

    return $selected.ToArray()
}

function Get-SelectedRuntimeIdentifiers {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$RequestedRuntimeIdentifiers
    )

    $allowedRuntimeIdentifiers = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    @('win-x64', 'win-arm64') | ForEach-Object { $allowedRuntimeIdentifiers.Add($_) | Out-Null }

    $normalizedRuntimeIdentifiers = @($RequestedRuntimeIdentifiers |
        ForEach-Object { "$_" -split ',' } |
        ForEach-Object { $_.Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Unique)

    foreach ($runtimeIdentifier in $normalizedRuntimeIdentifiers) {
        if (-not $allowedRuntimeIdentifiers.Contains($runtimeIdentifier)) {
            throw "Unsupported runtime identifier: $runtimeIdentifier. Allowed values: win-x64, win-arm64."
        }
    }

    if ($normalizedRuntimeIdentifiers.Count -eq 0) {
        throw 'No runtime identifiers remain after applying the current switches.'
    }

    return $normalizedRuntimeIdentifiers
}

function Write-Summary {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Results,

        [Parameter(Mandatory = $true)]
        [string]$RunOutputRoot,

        [string]$ChecksumsPath,

        [string]$ManifestPath,

        $DetachedSignatureResult
    )

    Write-Section 'Summary'

    foreach ($result in $Results) {
        $label = "{0} | {1}" -f $result.Variant, $result.RuntimeIdentifier

        switch ($result.Status) {
            'Succeeded' {
                Write-Success "$label -> $($result.ZipPath)"
            }
            'Skipped' {
                Write-Warn "$label -> $($result.Message)"
            }
            default {
                Write-Failure "$label -> $($result.Message)"
            }
        }
    }

    Write-Info "Run output: $RunOutputRoot"

    if (-not [string]::IsNullOrWhiteSpace($ChecksumsPath)) {
        Write-Info "Checksums : $ChecksumsPath"
    }

    if (-not [string]::IsNullOrWhiteSpace($ManifestPath)) {
        Write-Info "Manifest  : $ManifestPath"
    }

    if ($null -ne $DetachedSignatureResult) {
        switch ($DetachedSignatureResult.Status) {
            'Succeeded' {
                Write-Info "Signature : $($DetachedSignatureResult.SignaturePath)"
                Write-Info "Public key: $($DetachedSignatureResult.PublicKeyPath)"
            }
            'Skipped' {
                Write-Warn $DetachedSignatureResult.Message
            }
            'Failed' {
                Write-Failure $DetachedSignatureResult.Message
            }
        }
    }
}

$script:OriginalOutputEncoding = $null
$script:OriginalInputEncoding = $null
$script:OriginalPsOutputEncoding = $null

Set-ConsoleUtf8

try {
    $repoRoot = Split-Path -Path $PSScriptRoot -Parent
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\YAi.Client.CLI\YAi.Client.CLI.csproj'
    $propsPath = Join-Path -Path $repoRoot -ChildPath 'Directory.Build.props'
    $signerProjectPath = Join-Path -Path $repoRoot -ChildPath 'src\YAi.Tools.ResourceSigner\YAi.Tools.ResourceSigner.csproj'
    $defaultOutputRoot = Join-Path -Path $repoRoot -ChildPath 'artifacts\cli'
    $defaultReleaseSigningPrivateKeyPath = Join-Path -Path $repoRoot -ChildPath '.secrets\yai-signing-private-key.pem'
    $defaultReleaseSigningPublicKeyPath = Join-Path -Path $repoRoot -ChildPath 'src\YAi.Resources\reference\public-key.yai.pem'

    $version = if (Test-Path -LiteralPath $propsPath) {
        Get-RepositoryVersion -PropsPath $propsPath
    }
    else {
        'unknown'
    }

    if (Test-HelpRequested -HelpSwitch:$Help.IsPresent -Remaining $RemainingArguments) {
        Write-Usage -Version $version -DefaultOutputRoot $defaultOutputRoot
        exit 0
    }

    Assert-NoUnexpectedArguments -Remaining $RemainingArguments

    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        $OutputRoot = $defaultOutputRoot
    }

    if ([string]::IsNullOrWhiteSpace($ReleaseSigningPrivateKeyPath)) {
        $ReleaseSigningPrivateKeyPath = $defaultReleaseSigningPrivateKeyPath
    }

    if ([string]::IsNullOrWhiteSpace($ReleaseSigningPublicKeyPath)) {
        $ReleaseSigningPublicKeyPath = $defaultReleaseSigningPublicKeyPath
    }

    if (-not (Get-Command -Name dotnet -ErrorAction SilentlyContinue)) {
        throw 'dotnet was not found on PATH.'
    }

    if (-not (Test-Path -LiteralPath $projectPath)) {
        throw "CLI project not found: $projectPath"
    }

    $timestamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    $runOutputRoot = Join-Path -Path $OutputRoot -ChildPath $timestamp
    $stagingRoot = Join-Path -Path $runOutputRoot -ChildPath '_staging'
    $selectedVariants = Get-SelectedVariants -RequestedVariants $Variant -ExcludeAot:$SkipAot.IsPresent
    $selectedRids = Get-SelectedRuntimeIdentifiers -RequestedRuntimeIdentifiers $RuntimeIdentifier
    $results = [System.Collections.Generic.List[object]]::new()
    $blockedAotRids = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $checksumsPath = $null
    $manifestPath = $null
    $detachedSignatureResult = $null

    New-Item -ItemType Directory -Path $runOutputRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

    Write-Header -Version $version -Timestamp $timestamp -TargetOutputRoot $runOutputRoot
    Write-Info "Configuration : $Configuration"
    Write-Info "Variants      : $($selectedVariants -join ', ')"
    Write-Info "Architectures : $($selectedRids -join ', ')"

    if ($selectedVariants -contains 'Aot') {
        Write-Section 'NativeAOT preflight'

        foreach ($rid in $selectedRids) {
            $preflight = Get-NativeAotPreflightResult -Rid $rid
            if ($preflight.IsReady) {
                Write-Success "$rid -> $($preflight.Message)"
                continue
            }

            $blockedAotRids.Add($rid) | Out-Null
            Write-Warn "$rid -> $($preflight.Message)"
            $results.Add([pscustomobject]@{
                    Variant           = 'Aot'
                    RuntimeIdentifier = $rid
                    ArtifactName      = $null
                    ZipPath           = $null
                    Status            = 'Skipped'
                    Message           = $preflight.Message
                })
        }
    }

    foreach ($variantName in $selectedVariants) {
        foreach ($rid in $selectedRids) {
            if ($variantName -eq 'Aot' -and $blockedAotRids.Contains($rid)) {
                continue
            }

            try {
                $publishParameters = @{
                    ProjectPath           = $projectPath
                    ConfigurationName     = $Configuration
                    VariantName           = $variantName
                    Rid                   = $rid
                    Version               = $version
                    Timestamp             = $timestamp
                    RunOutputRoot         = $runOutputRoot
                    PreservePublishFolder = $KeepPublishFolders.IsPresent
                }

                $result = Invoke-PublishArtifact @publishParameters

                $results.Add($result)

                if ($result.Status -eq 'Failed' -and $variantName -eq 'Aot') {
                    Write-Warn 'NativeAOT is best-effort in this workflow. Baseline artifacts will continue.'
                }
            }
            catch {
                $message = $_.Exception.Message
                $status = if ($variantName -eq 'Aot') { 'Skipped' } else { 'Failed' }

                $results.Add([pscustomobject]@{
                        Variant           = $variantName
                        RuntimeIdentifier = $rid
                        ArtifactName      = $null
                        ZipPath           = $null
                        Status            = $status
                        Message           = $message
                    })

                if ($variantName -eq 'Aot') {
                    Write-Warn "NativeAOT publish did not complete for $rid. $message"
                    continue
                }

                throw
            }
        }
    }

    if (-not $KeepPublishFolders.IsPresent -and (Test-Path -LiteralPath $stagingRoot)) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }

    $checksumsPath = Write-ChecksumBatchFile -Results $results.ToArray() -RunOutputRoot $runOutputRoot

    $manifestParameters = @{
        Results                    = $results.ToArray()
        RunOutputRoot              = $runOutputRoot
        Version                    = $version
        ConfigurationName          = $Configuration
        Timestamp                  = $timestamp
        SelectedVariants           = $selectedVariants
        SelectedRuntimeIdentifiers = $selectedRids
    }

    $manifestPath = Write-ReleaseManifest @manifestParameters

    if (-not $SkipDetachedSignature.IsPresent) {
        $signatureParameters = @{
            SignerProjectPath             = $signerProjectPath
            ManifestPath                  = $manifestPath
            RunOutputRoot                 = $runOutputRoot
            PrivateKeyPath                = $ReleaseSigningPrivateKeyPath
            PublicKeyPath                 = $ReleaseSigningPublicKeyPath
            Algorithm                     = $ReleaseSignatureAlgorithm
            PassphraseEnvironmentVariable = $SigningPassphraseEnvironmentVariable
        }

        $detachedSignatureResult = New-DetachedManifestSignature @signatureParameters
    }
    else {
        $detachedSignatureResult = [pscustomobject]@{
            Status        = 'Skipped'
            Message       = 'Detached manifest signing was skipped by request.'
            SignaturePath = $null
            PublicKeyPath = $null
            Algorithm     = $ReleaseSignatureAlgorithm
        }
    }

    $summaryParameters = @{
        Results                 = $results.ToArray()
        RunOutputRoot           = $runOutputRoot
        ChecksumsPath           = $checksumsPath
        ManifestPath            = $manifestPath
        DetachedSignatureResult = $detachedSignatureResult
    }

    Write-Summary @summaryParameters

    $baselineFailures = @($results | Where-Object {
            $_.Status -ne 'Succeeded' -and (Test-BaselineVariant -VariantName $_.Variant)
        })

    if ($baselineFailures.Count -gt 0) {
        exit 1
    }

    if ($null -ne $detachedSignatureResult -and $detachedSignatureResult.Status -eq 'Failed') {
        exit 1
    }

    exit 0
}
finally {
    Restore-ConsoleEncoding
}