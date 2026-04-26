# YAi!
# Opens the YAi.Client.CLI build output folder or runs the compiled app.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Show-Usage {
    Write-Host @'
YAi.Client.CLI output helper

Usage:
  .\jump-cli-output.ps1 [--release] [--run] [--help]

What it does:
  - Defaults to bin/Debug/net10.0 for this project.
  - Use --release to switch to bin/Release/net10.0.
  - Use --run to launch the compiled app from that folder with no extra arguments.
  - Use --help to show this message.

Examples:
  .\jump-cli-output.ps1
  .\jump-cli-output.ps1 --release
  .\jump-cli-output.ps1 --run
  .\jump-cli-output.ps1 --release --run
'@
}

function Get-ProjectRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$StartPath
    )

    $currentPath = (Resolve-Path -LiteralPath $StartPath).Path

    $solutionProjectFile = Join-Path -Path $currentPath -ChildPath 'src\YAi.Client.CLI\YAi.Client.CLI.csproj'
    if (Test-Path -LiteralPath $solutionProjectFile) {
        return Split-Path -Path $solutionProjectFile -Parent
    }

    while ($true) {
        $projectFile = Join-Path -Path $currentPath -ChildPath 'YAi.Client.CLI.csproj'
        if (Test-Path -LiteralPath $projectFile) {
            return $currentPath
        }

        $parentPath = Split-Path -Path $currentPath -Parent
        if ([string]::IsNullOrWhiteSpace($parentPath) -or $parentPath -eq $currentPath) {
            break
        }

        $currentPath = $parentPath
    }

    throw 'Could not locate YAi.Client.CLI.csproj. Run this script from the solution root or from inside the YAi.Client.CLI project tree.'
}

function Get-ProjectMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectRoot
    )

    $projectFile = Join-Path -Path $ProjectRoot -ChildPath 'YAi.Client.CLI.csproj'
    [xml]$projectXml = Get-Content -LiteralPath $projectFile -Raw

    $assemblyNameNode = $projectXml.SelectSingleNode('//AssemblyName')
    $assemblyName = $assemblyNameNode.InnerText

    if ([string]::IsNullOrWhiteSpace($assemblyName)) {
        $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($projectFile)
    }

    $targetFrameworkNode = $projectXml.SelectSingleNode('//TargetFramework')
    $targetFramework = $targetFrameworkNode.InnerText

    if ([string]::IsNullOrWhiteSpace($targetFramework)) {
        $targetFrameworksNode = $projectXml.SelectSingleNode('//TargetFrameworks')
        $targetFrameworks = $targetFrameworksNode.InnerText

        if (-not [string]::IsNullOrWhiteSpace($targetFrameworks)) {
            $targetFramework = ($targetFrameworks -split ';' |
                ForEach-Object { $_.Trim() } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
                Select-Object -First 1)
        }
    }

    if ([string]::IsNullOrWhiteSpace($targetFramework)) {
        throw "Could not determine target framework from $projectFile."
    }

    [pscustomobject]@{
        AssemblyName = $assemblyName
        TargetFramework = $targetFramework
    }
}

$showHelp = $false
$useRelease = $false
$runApp = $false
$unknownArguments = New-Object System.Collections.Generic.List[string]

foreach ($argument in $args) {
    switch -Regex ($argument) {
        '^(-{1,2})(h|help)$' {
            $showHelp = $true
            continue
        }

        '^(-{1,2})release$' {
            $useRelease = $true
            continue
        }

        '^(-{1,2})run$' {
            $runApp = $true
            continue
        }

        default {
            [void]$unknownArguments.Add($argument)
        }
    }
}

if ($showHelp) {
    Show-Usage
    return
}

if ($unknownArguments.Count -gt 0) {
    $unknownList = $unknownArguments -join ', '
    throw "Unknown argument(s): $unknownList. Run --help for usage."
}

$projectRoot = Get-ProjectRoot -StartPath $PSScriptRoot
$projectMetadata = Get-ProjectMetadata -ProjectRoot $projectRoot

$configName = if ($useRelease) {
    'Release'
}
else {
    'Debug'
}

$outputFolder = Join-Path -Path (Join-Path -Path (Join-Path -Path $projectRoot -ChildPath 'bin') -ChildPath $configName) -ChildPath $projectMetadata.TargetFramework

if (-not (Test-Path -LiteralPath $outputFolder)) {
    throw "Output folder not found: $outputFolder. Build the project first."
}

Set-Location -LiteralPath $outputFolder
Write-Host "Current folder: $outputFolder"

if (-not $runApp) {
    return
}

$launcherCandidates = @(
    (Join-Path -Path $outputFolder -ChildPath "$($projectMetadata.AssemblyName).exe")
    (Join-Path -Path $outputFolder -ChildPath $projectMetadata.AssemblyName)
    (Join-Path -Path $outputFolder -ChildPath "$($projectMetadata.AssemblyName).dll")
)

$launcherPath = $launcherCandidates |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($launcherPath)) {
    throw "Could not find a compiled launcher in $outputFolder. Build the project first."
}

if ($launcherPath.EndsWith('.dll', [System.StringComparison]::OrdinalIgnoreCase)) {
    & dotnet $launcherPath
    return
}

& $launcherPath