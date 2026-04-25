# create-yai-signing-keys.ps1
# Run from YAi repository root, for example:
# cd E:\YAi!
# .\create-yai-signing-keys.ps1

$ErrorActionPreference = "Stop"

$RepoRoot = (Get-Location).Path
$SecretsDir = Join-Path $RepoRoot ".secrets"
$ReferenceDir = Join-Path $RepoRoot "src\YAi.Resources\reference"

$PrivateKeyPath = Join-Path $SecretsDir "yai-signing-private-key.pem"
$PublicKeyPath = Join-Path $ReferenceDir "public-key.yai.pem"
$GitIgnorePath = Join-Path $RepoRoot ".gitignore"

Write-Host "YAi signing key setup"
Write-Host "Repo root: $RepoRoot"
Write-Host ""

if (-not (Get-Command openssl -ErrorAction SilentlyContinue)) {
    throw "OpenSSL was not found in PATH. Install OpenSSL or make sure openssl.exe is available."
}

New-Item -ItemType Directory -Force -Path $SecretsDir | Out-Null
New-Item -ItemType Directory -Force -Path $ReferenceDir | Out-Null

if (Test-Path $PrivateKeyPath) {
    throw "Private key already exists: $PrivateKeyPath. Remove it manually if you really want to regenerate it."
}

if (Test-Path $PublicKeyPath) {
    throw "Public key already exists: $PublicKeyPath. Remove it manually if you really want to regenerate it."
}

$securePass = Read-Host "Enter YAi signing key passphrase" -AsSecureString
$securePassConfirm = Read-Host "Confirm YAi signing key passphrase" -AsSecureString

$bstr1 = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePass)
$bstr2 = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassConfirm)

try {
    $plainPass = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr1)
    $plainPassConfirm = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr2)

    if ([string]::IsNullOrWhiteSpace($plainPass)) {
        throw "Passphrase cannot be empty."
    }

    if ($plainPass -ne $plainPassConfirm) {
        throw "Passphrases do not match."
    }

    Write-Host ""
    Write-Host "Generating encrypted Ed25519 private key..."

    & openssl genpkey `
        -algorithm Ed25519 `
        -aes-256-cbc `
        -pass "pass:$plainPass" `
        -out $PrivateKeyPath

    if ($LASTEXITCODE -ne 0) {
        throw "OpenSSL failed while generating the private key."
    }

    Write-Host "Extracting public key..."

    & openssl pkey `
        -in $PrivateKeyPath `
        -passin "pass:$plainPass" `
        -pubout `
        -out $PublicKeyPath

    if ($LASTEXITCODE -ne 0) {
        throw "OpenSSL failed while extracting the public key."
    }
}
finally {
    if ($bstr1 -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr1)
    }

    if ($bstr2 -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr2)
    }

    $plainPass = $null
    $plainPassConfirm = $null

    if ($securePass) {
        $securePass.Dispose()
    }

    if ($securePassConfirm) {
        $securePassConfirm.Dispose()
    }
}

if (-not (Test-Path $GitIgnorePath)) {
    New-Item -ItemType File -Path $GitIgnorePath | Out-Null
}

$gitIgnoreRequiredLines = @(
    ".secrets/",
    "*.key",
    "*.private.pem",
    "*yai-signing-private-key.pem",
    "!src/YAi.Resources/reference/public-key.yai.pem"
)

$currentGitIgnore = Get-Content $GitIgnorePath -Raw -ErrorAction SilentlyContinue

foreach ($line in $gitIgnoreRequiredLines) {
    if ($currentGitIgnore -notmatch [regex]::Escape($line)) {
        Add-Content -Path $GitIgnorePath -Value $line
    }
}

Write-Host ""
Write-Host "Done."
Write-Host "Private key: $PrivateKeyPath"
Write-Host "Public key:  $PublicKeyPath"
Write-Host ""
Write-Host "Commit this:"
Write-Host "  src\YAi.Resources\reference\public-key.yai.pem"
Write-Host "  .gitignore"
Write-Host ""
Write-Host "Do NOT commit this:"
Write-Host "  .secrets\yai-signing-private-key.pem"