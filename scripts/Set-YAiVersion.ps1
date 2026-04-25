[CmdletBinding()]
param(
    [string]$Version,
    [switch]$Timestamp
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $PSScriptRoot -Parent
$propsPath = Join-Path -Path $repoRoot -ChildPath 'Directory.Build.props'

function Get-TimestampVersion {
    $utcNow = [DateTime]::UtcNow
    $major = 1
    $minor = $utcNow.Year - 2000
    $build = [int]$utcNow.ToString('MMdd')
    $revision = [int]$utcNow.ToString('HHmm')

    return "$major.$minor.$build.$revision"
}

if ($Timestamp -and -not [string]::IsNullOrWhiteSpace($Version)) {
    throw 'Specify either -Version or -Timestamp, not both.'
}

if (-not $Timestamp -and [string]::IsNullOrWhiteSpace($Version)) {
    throw 'Specify -Version <version> or -Timestamp.'
}

if ($Timestamp) {
    $Version = Get-TimestampVersion
}

if ($Version -notmatch '^\d+(\.\d+){2,3}$') {
    throw 'Version must be numeric and assembly-compatible, like 1.2.3 or 1.2.3.4.'
}

if (-not (Test-Path -LiteralPath $propsPath)) {
    throw "Version file not found: $propsPath"
}

$content = Get-Content -LiteralPath $propsPath -Raw
$updated = $content -replace '(?<=<Version>)[^<]+(?=</Version>)', $Version

if ($updated -eq $content) {
    Write-Host "Version already set to $Version."
    return
}

Set-Content -LiteralPath $propsPath -Value $updated -Encoding utf8NoBOM

Write-Host "Updated every project to version $Version via Directory.Build.props."
Write-Host 'Rebuild the solution to apply the new version.'
