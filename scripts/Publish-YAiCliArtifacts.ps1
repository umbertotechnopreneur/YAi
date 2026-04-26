#Requires -Version 7.0

<#
.SYNOPSIS
Builds zipped YAi CLI distribution artifacts for Windows.

.DESCRIPTION
Publishes framework-dependent, self-contained, and best-effort NativeAOT variants of
YAi.Client.CLI for the requested Windows runtime identifiers. Each successful publish
is zipped with a descriptive name that includes the current repository version and a
UTC timestamp.

.EXAMPLE
pwsh ./scripts/Publish-YAiCliArtifacts.ps1

.EXAMPLE
pwsh ./scripts/Publish-YAiCliArtifacts.ps1 -SkipAot -RuntimeIdentifier win-x64

.EXAMPLE
pwsh ./scripts/Publish-YAiCliArtifacts.ps1 -Variant SelfContained -RuntimeIdentifier win-arm64

.EXAMPLE
pwsh ./scripts/Publish-YAiCliArtifacts.ps1 --help
#>
[CmdletBinding ()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$OutputRoot,

    [ValidateSet('FrameworkDependent', 'SelfContained', 'Aot')]
    [string[]]$Variant = @('FrameworkDependent', 'SelfContained', 'Aot'),

    [ValidateSet('win-x64', 'win-arm64')]
    [string[]]$RuntimeIdentifier = @('win-x64', 'win-arm64'),

    [switch]$SkipAot,

    [switch]$KeepPublishFolders,

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
    Write-Host '  -Variant <FrameworkDependent|SelfContained|Aot>' -ForegroundColor White
    Write-Host '                               Select one or more publish variants.' -ForegroundColor White
    Write-Host '  -RuntimeIdentifier <win-x64|win-arm64>' -ForegroundColor White
    Write-Host '                               Select one or more Windows target architectures.' -ForegroundColor White
    Write-Host '  -SkipAot                     Skip NativeAOT attempts even if Aot is selected.' -ForegroundColor White
    Write-Host '  -KeepPublishFolders          Preserve unzipped publish folders next to zip artifacts.' -ForegroundColor White
    Write-Host
    Write-Host 'What it does' -ForegroundColor Cyan
    Write-Host '  - Reads the current version from Directory.Build.props.' -ForegroundColor White
    Write-Host '  - Publishes framework-dependent, self-contained, and optional NativeAOT CLI outputs.' -ForegroundColor White
    Write-Host '  - Creates one zip per artifact with version and UTC timestamp in the filename.' -ForegroundColor White
    Write-Host '  - Writes artifacts under artifacts/cli/<utc-timestamp>/.' -ForegroundColor White
    Write-Host
    Write-Host 'Examples' -ForegroundColor Cyan
    Write-Host '  pwsh ./scripts/Publish-YAiCliArtifacts.ps1' -ForegroundColor White
    Write-Host '  pwsh ./scripts/Publish-YAiCliArtifacts.ps1 -SkipAot -RuntimeIdentifier win-x64' -ForegroundColor White
    Write-Host '  pwsh ./scripts/Publish-YAiCliArtifacts.ps1 -Variant SelfContained -Variant Aot -RuntimeIdentifier win-arm64' -ForegroundColor White
    Write-Host
    Write-Host 'Notes' -ForegroundColor Cyan
    Write-Host '  NativeAOT is currently best-effort. Missing C++ toolchain prerequisites are detected before AOT publish starts.' -ForegroundColor White
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
        ProjectPath = $ProjectPath
        ConfigurationName = $ConfigurationName
        VariantName = $VariantName
        Rid = $Rid
        PublishDirectory = $publishDirectory
    }

    $arguments = Get-PublishArguments @publishArgumentParameters

    $null = & dotnet @arguments 2>&1 | Tee-Object -Variable capturedOutput
    if ($LASTEXITCODE -ne 0) {
        $excerpt = Get-OutputExcerpt -OutputLines $capturedOutput
        return [pscustomobject]@{
            Variant = $VariantName
            RuntimeIdentifier = $Rid
            ArtifactName = $artifactBaseName
            ZipPath = $null
            Status = 'Failed'
            Message = $excerpt
        }
    }

    Compress-Archive -LiteralPath $publishDirectory -DestinationPath $zipPath -CompressionLevel Optimal -Force
    Write-Success "Created $zipPath"

    if (-not $PreservePublishFolder -and (Test-Path -LiteralPath $publishDirectory)) {
        Remove-Item -LiteralPath $publishDirectory -Recurse -Force
    }

    return [pscustomobject]@{
        Variant = $VariantName
        RuntimeIdentifier = $Rid
        ArtifactName = $artifactBaseName
        ZipPath = $zipPath
        Status = 'Succeeded'
        Message = if ($PreservePublishFolder) { "Zip and publish folder created." } else { 'Zip created.' }
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
            IsReady = $false
            Message = 'NativeAOT publishing in this workflow is currently supported only on Windows hosts.'
        }
    }

    $vsWherePath = Join-Path -Path ${env:ProgramFiles(x86)} -ChildPath 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vsWherePath)) {
        return [pscustomobject]@{
            RuntimeIdentifier = $Rid
            IsReady = $false
            Message = 'Visual Studio Installer tooling was not found. Install Visual Studio with Desktop Development with C++.'
        }
    }

    $requiredComponents = @('Microsoft.VisualStudio.Workload.NativeDesktop')
    if ($Rid -eq 'win-arm64') {
        $requiredComponents += 'Microsoft.VisualStudio.Component.VC.Tools.ARM64'
    }

    $installationPath = @(& $vsWherePath -prerelease -all -products * -requires $requiredComponents -property installationPath -format value 2>$null | Where-Object {
            -not [string]::IsNullOrWhiteSpace($_)
        } | Select-Object -First 1)

    if ($installationPath.Count -eq 0) {
        $message = if ($Rid -eq 'win-arm64') {
            'Missing NativeAOT prerequisites. Install Desktop Development with C++ and the C++ ARM64 build tools in Visual Studio.'
        }
        else {
            'Missing NativeAOT prerequisites. Install Desktop Development with C++ in Visual Studio.'
        }

        return [pscustomobject]@{
            RuntimeIdentifier = $Rid
            IsReady = $false
            Message = $message
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
            IsReady = $false
            Message = 'MSVC build tools were not found under the selected Visual Studio installation.'
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
            IsReady = $false
            Message = $message
        }
    }

    $windowsKitsLibRoot = Join-Path -Path ${env:ProgramFiles(x86)} -ChildPath 'Windows Kits\10\Lib'
    $sdkLibrary = Get-ChildItem -Path $windowsKitsLibRoot -Recurse -Filter 'advapi32.lib' -ErrorAction SilentlyContinue |
        Where-Object {
            $_.FullName -match [regex]::Escape("\\um\\$targetArchitecture\\")
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
            IsReady = $false
            Message = $message
        }
    }

    return [pscustomobject]@{
        RuntimeIdentifier = $Rid
        IsReady = $true
        Message = "NativeAOT prerequisites detected in $($installationPath[0])."
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

    foreach ($variantName in ($RequestedVariants | Select-Object -Unique)) {
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

function Write-Summary {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Results,

        [Parameter(Mandatory = $true)]
        [string]$RunOutputRoot
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
}

$script:OriginalOutputEncoding = $null
$script:OriginalInputEncoding = $null
$script:OriginalPsOutputEncoding = $null

Set-ConsoleUtf8

try {
    $repoRoot = Split-Path -Path $PSScriptRoot -Parent
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\YAi.Client.CLI\YAi.Client.CLI.csproj'
    $propsPath = Join-Path -Path $repoRoot -ChildPath 'Directory.Build.props'
    $defaultOutputRoot = Join-Path -Path $repoRoot -ChildPath 'artifacts\cli'

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
    $selectedRids = $RuntimeIdentifier | Select-Object -Unique
    $results = [System.Collections.Generic.List[object]]::new()
    $blockedAotRids = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

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
                Variant = 'Aot'
                RuntimeIdentifier = $rid
                ArtifactName = $null
                ZipPath = $null
                Status = 'Skipped'
                Message = $preflight.Message
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
                    ProjectPath = $projectPath
                    ConfigurationName = $Configuration
                    VariantName = $variantName
                    Rid = $rid
                    Version = $version
                    Timestamp = $timestamp
                    RunOutputRoot = $runOutputRoot
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
                    Variant = $variantName
                    RuntimeIdentifier = $rid
                    ArtifactName = $null
                    ZipPath = $null
                    Status = $status
                    Message = $message
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

    Write-Summary -Results $results.ToArray() -RunOutputRoot $runOutputRoot

    $baselineFailures = @($results | Where-Object {
            $_.Status -ne 'Succeeded' -and (Test-BaselineVariant -VariantName $_.Variant)
        })

    if ($baselineFailures.Count -gt 0) {
        exit 1
    }

    exit 0
}
finally {
    Restore-ConsoleEncoding
}