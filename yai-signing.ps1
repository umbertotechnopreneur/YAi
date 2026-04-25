# yai-signing.ps1
# YAi official resource signing management tool.
# Run from the YAi repository root:
#   cd E:\YAi!
#   .\yai-signing.ps1

#Requires -Version 5.1
$ErrorActionPreference = "Stop"

# ─────────────────────────────────────────────────────────────────────────────
# Paths
# ─────────────────────────────────────────────────────────────────────────────

$RepoRoot      = (Get-Location).Path
$SecretsDir    = Join-Path $RepoRoot ".secrets"
$ReferenceDir  = Join-Path $RepoRoot "src\YAi.Resources\reference"
$PrivateKey    = Join-Path $SecretsDir "yai-signing-private-key.pem"
$PublicKey     = Join-Path $ReferenceDir "public-key.yai.pem"
$ManifestFile  = Join-Path $ReferenceDir "manifest.yai.json"
$SignatureFile  = Join-Path $ReferenceDir "manifest.yai.sig"
$SignerProject = Join-Path $RepoRoot "src\YAi.Tools.ResourceSigner\YAi.Tools.ResourceSigner.csproj"
$GitIgnore     = Join-Path $RepoRoot ".gitignore"

# ─────────────────────────────────────────────────────────────────────────────
# Helpers
# ─────────────────────────────────────────────────────────────────────────────

function Write-Header {
    Clear-Host
    Write-Host ""
    Write-Host "  ╔══════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "  ║        YAi  Official Resource Signing        ║" -ForegroundColor Cyan
    Write-Host "  ╚══════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Status {
    Write-Host "  Key status   : " -NoNewline
    if (Test-Path $PrivateKey) {
        Write-Host "PRIVATE KEY EXISTS" -ForegroundColor Green -NoNewline
        Write-Host "  ($PrivateKey)"
    } else {
        Write-Host "no private key" -ForegroundColor Yellow -NoNewline
        Write-Host "  (.secrets\ is git-ignored)"
    }

    Write-Host "  Public key   : " -NoNewline
    if (Test-Path $PublicKey) {
        Write-Host "EXISTS" -ForegroundColor Green
    } else {
        Write-Host "missing" -ForegroundColor Red
    }

    Write-Host "  Manifest     : " -NoNewline
    if (Test-Path $ManifestFile) {
        Write-Host "EXISTS" -ForegroundColor Green
    } else {
        Write-Host "missing" -ForegroundColor Yellow
    }

    Write-Host "  Signature    : " -NoNewline
    if (Test-Path $SignatureFile) {
        Write-Host "EXISTS" -ForegroundColor Green
    } else {
        Write-Host "missing" -ForegroundColor Yellow
    }

    Write-Host ""
}

function Read-SecurePassphrase([string]$Prompt) {
    $s = Read-Host $Prompt -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($s)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        $s.Dispose()
    }
}

function Assert-OpenSSL {
    if (-not (Get-Command openssl -ErrorAction SilentlyContinue)) {
        throw "OpenSSL not found in PATH. Install OpenSSL or add openssl.exe to PATH."
    }
}

function Ensure-GitIgnore {
    if (-not (Test-Path $GitIgnore)) { New-Item -ItemType File -Path $GitIgnore | Out-Null }
    $current = Get-Content $GitIgnore -Raw -ErrorAction SilentlyContinue
    $required = @(".secrets/","*.key","*.private.pem","*yai-signing-private-key.pem","!src/YAi.Resources/reference/public-key.yai.pem")
    foreach ($line in $required) {
        if ($current -notmatch [regex]::Escape($line)) {
            Add-Content $GitIgnore $line
            $current += "`n$line"
        }
    }
}

function Press-AnyKey {
    Write-Host ""
    Write-Host "  Press any key to return to the menu..." -ForegroundColor DarkGray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

# ─────────────────────────────────────────────────────────────────────────────
# Menu actions
# ─────────────────────────────────────────────────────────────────────────────

function Invoke-CreateKey {
    Write-Host ""
    Write-Host "  ── Create new signing key pair ──" -ForegroundColor Cyan
    Write-Host ""

    Assert-OpenSSL

    if (Test-Path $PrivateKey) {
        Write-Host "  ERROR: A private key already exists at:" -ForegroundColor Red
        Write-Host "         $PrivateKey" -ForegroundColor Red
        Write-Host "  Use option [2] to delete it first." -ForegroundColor Yellow
        Press-AnyKey; return
    }

    if (Test-Path $PublicKey) {
        Write-Host "  ERROR: A public key already exists at:" -ForegroundColor Red
        Write-Host "         $PublicKey" -ForegroundColor Red
        Write-Host "  Remove it manually if you really want to regenerate." -ForegroundColor Yellow
        Press-AnyKey; return
    }

    $pass    = Read-SecurePassphrase "  Enter new passphrase"
    $confirm = Read-SecurePassphrase "  Confirm passphrase"

    if ([string]::IsNullOrWhiteSpace($pass)) { Write-Host "  Passphrase cannot be empty." -ForegroundColor Red; Press-AnyKey; return }
    if ($pass -ne $confirm)                  { Write-Host "  Passphrases do not match."   -ForegroundColor Red; Press-AnyKey; return }

    New-Item -ItemType Directory -Force -Path $SecretsDir   | Out-Null
    New-Item -ItemType Directory -Force -Path $ReferenceDir | Out-Null

    Write-Host ""
    Write-Host "  Generating Ed25519 private key (AES-256-CBC encrypted)..." -ForegroundColor Gray

    & openssl genpkey -algorithm Ed25519 -aes-256-cbc -pass "pass:$pass" -out $PrivateKey
    if ($LASTEXITCODE -ne 0) { Write-Host "  OpenSSL failed." -ForegroundColor Red; Press-AnyKey; return }

    Write-Host "  Extracting public key..." -ForegroundColor Gray
    & openssl pkey -in $PrivateKey -passin "pass:$pass" -pubout -out $PublicKey
    if ($LASTEXITCODE -ne 0) { Write-Host "  OpenSSL failed." -ForegroundColor Red; Press-AnyKey; return }

    $pass = $null; $confirm = $null

    Ensure-GitIgnore

    Write-Host ""
    Write-Host "  ✓ Private key : $PrivateKey" -ForegroundColor Green
    Write-Host "  ✓ Public key  : $PublicKey"  -ForegroundColor Green
    Write-Host ""
    Write-Host "  Remember to commit public-key.yai.pem and .gitignore." -ForegroundColor Yellow

    Press-AnyKey
}

function Invoke-DeleteKey {
    Write-Host ""
    Write-Host "  ── Delete existing private key ──" -ForegroundColor Cyan
    Write-Host ""

    if (-not (Test-Path $PrivateKey)) {
        Write-Host "  No private key found at $PrivateKey" -ForegroundColor Yellow
        Press-AnyKey; return
    }

    Write-Host "  WARNING: This will permanently delete:" -ForegroundColor Red
    Write-Host "           $PrivateKey" -ForegroundColor Red
    Write-Host ""
    $confirm = Read-Host "  Type DELETE to confirm"
    if ($confirm -ne "DELETE") {
        Write-Host "  Cancelled." -ForegroundColor Yellow
        Press-AnyKey; return
    }

    Remove-Item $PrivateKey -Force
    Write-Host "  ✓ Private key deleted." -ForegroundColor Green
    Write-Host "  The public key and manifest/signature are still in place." -ForegroundColor Gray

    Press-AnyKey
}

function Invoke-CheckStatus {
    Write-Host ""
    Write-Host "  ── Key and manifest status ──" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "  Private key  : " -NoNewline
    if (Test-Path $PrivateKey) {
        $info = Get-Item $PrivateKey
        Write-Host "EXISTS  ($([math]::Round($info.Length/1))B, modified $($info.LastWriteTime.ToString('yyyy-MM-dd HH:mm')))" -ForegroundColor Green
    } else {
        Write-Host "NOT FOUND" -ForegroundColor Red
    }

    Write-Host "  Public key   : " -NoNewline
    if (Test-Path $PublicKey) {
        $info = Get-Item $PublicKey
        Write-Host "EXISTS  ($([math]::Round($info.Length/1))B, modified $($info.LastWriteTime.ToString('yyyy-MM-dd HH:mm')))" -ForegroundColor Green
    } else {
        Write-Host "NOT FOUND" -ForegroundColor Red
    }

    Write-Host "  Manifest     : " -NoNewline
    if (Test-Path $ManifestFile) {
        $info = Get-Item $ManifestFile
        Write-Host "EXISTS  ($([math]::Round($info.Length/1024, 1))KB, modified $($info.LastWriteTime.ToString('yyyy-MM-dd HH:mm')))" -ForegroundColor Green
    } else {
        Write-Host "NOT FOUND" -ForegroundColor Yellow
    }

    Write-Host "  Signature    : " -NoNewline
    if (Test-Path $SignatureFile) {
        $info = Get-Item $SignatureFile
        Write-Host "EXISTS  ($([math]::Round($info.Length/1))B, modified $($info.LastWriteTime.ToString('yyyy-MM-dd HH:mm')))" -ForegroundColor Green
    } else {
        Write-Host "NOT FOUND" -ForegroundColor Yellow
    }

    Write-Host ""

    # Git status for the three public artifacts
    $tracked = @(
        "src/YAi.Resources/reference/public-key.yai.pem",
        "src/YAi.Resources/reference/manifest.yai.json",
        "src/YAi.Resources/reference/manifest.yai.sig"
    )
    Write-Host "  Git status of public artifacts:" -ForegroundColor Cyan
    foreach ($f in $tracked) {
        $status = git -C $RepoRoot status --porcelain -- $f 2>$null
        $label  = $f.Split("/")[-1]
        Write-Host "    $label : " -NoNewline
        if ([string]::IsNullOrWhiteSpace($status)) {
            Write-Host "committed / unchanged" -ForegroundColor DarkGray
        } else {
            Write-Host $status.Trim() -ForegroundColor Yellow
        }
    }

    Press-AnyKey
}

function Invoke-Sign {
    param([string]$PreCapturedPassphrase = "")

    Write-Host ""
    Write-Host "  ── Sign official resources ──" -ForegroundColor Cyan
    Write-Host ""

    if (-not (Test-Path $PrivateKey)) {
        Write-Host "  ERROR: Private key not found at $PrivateKey" -ForegroundColor Red
        Write-Host "  Use option [1] to create it first." -ForegroundColor Yellow
        Press-AnyKey; return
    }

    if (-not (Test-Path $SignerProject)) {
        Write-Host "  ERROR: Signer project not found at $SignerProject" -ForegroundColor Red
        Press-AnyKey; return
    }

    if ([string]::IsNullOrWhiteSpace($PreCapturedPassphrase)) {
        $pass = Read-SecurePassphrase "  Enter signing key passphrase"
    } else {
        $pass = $PreCapturedPassphrase
        Write-Host "  (using passphrase from key-creation step)" -ForegroundColor DarkGray
    }

    if ([string]::IsNullOrWhiteSpace($pass)) {
        Write-Host "  Passphrase cannot be empty." -ForegroundColor Red
        Press-AnyKey; return
    }

    Write-Host ""
    Write-Host "  Running signer..." -ForegroundColor Gray
    Write-Host ""

    $env:YAI_SIGNING_KEY_PASSPHRASE = $pass
    $pass = $null

    try {
        & dotnet run --project $SignerProject -- sign `
            --root $ReferenceDir `
            --private-key $PrivateKey `
            --manifest $ManifestFile `
            --signature $SignatureFile `
            --passphrase-env YAI_SIGNING_KEY_PASSPHRASE

        if ($LASTEXITCODE -ne 0) {
            Write-Host ""
            Write-Host "  ERROR: Signer exited with code $LASTEXITCODE" -ForegroundColor Red
            if ([string]::IsNullOrWhiteSpace($PreCapturedPassphrase)) { Press-AnyKey }
            return $false
        }
    } finally {
        $env:YAI_SIGNING_KEY_PASSPHRASE = $null
        [Environment]::SetEnvironmentVariable("YAI_SIGNING_KEY_PASSPHRASE", $null, "Process")
    }

    Write-Host ""
    Write-Host "  ✓ manifest.yai.json and manifest.yai.sig written." -ForegroundColor Green

    if ([string]::IsNullOrWhiteSpace($PreCapturedPassphrase)) { Press-AnyKey }
    return $true
}

function Invoke-Commit {
    Write-Host ""
    Write-Host "  ── Commit public artifacts ──" -ForegroundColor Cyan
    Write-Host ""

    $files = @(
        "src/YAi.Resources/reference/public-key.yai.pem",
        "src/YAi.Resources/reference/manifest.yai.json",
        "src/YAi.Resources/reference/manifest.yai.sig"
    )

    $toCommit = @()
    foreach ($f in $files) {
        $full = Join-Path $RepoRoot ($f -replace "/", "\")
        if (Test-Path $full) {
            $status = git -C $RepoRoot status --porcelain -- $f 2>$null
            if (-not [string]::IsNullOrWhiteSpace($status)) {
                $toCommit += $f
                Write-Host "  [staged] $f" -ForegroundColor Yellow
            } else {
                Write-Host "  [clean]  $f" -ForegroundColor DarkGray
            }
        } else {
            Write-Host "  [skip]   $f  (file not found)" -ForegroundColor DarkGray
        }
    }

    if ($toCommit.Count -eq 0) {
        Write-Host ""
        Write-Host "  Nothing to commit — all artifacts are up-to-date." -ForegroundColor Green
        Press-AnyKey; return
    }

    Write-Host ""
    $msg = Read-Host "  Commit message [Update signed resource manifest]"
    if ([string]::IsNullOrWhiteSpace($msg)) { $msg = "Update signed resource manifest" }

    foreach ($f in $toCommit) {
        git -C $RepoRoot add $f
    }

    git -C $RepoRoot commit -m $msg

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  git commit failed." -ForegroundColor Red
    } else {
        Write-Host ""
        Write-Host "  ✓ Committed." -ForegroundColor Green
    }

    Press-AnyKey
}

function Invoke-SignAndCommit {
    Invoke-Sign
    if (Test-Path $ManifestFile) {
        Invoke-Commit
    }
}

function Invoke-FirstTimeSetup {
    Write-Host ""
    Write-Host "  ── First-time setup ──" -ForegroundColor Cyan
    Write-Host "  This will: 1) generate a key pair  2) sign resources  3) commit public artifacts" -ForegroundColor Gray
    Write-Host ""

    # ── Guard: already have a key ────────────────────────────────────────────
    if (Test-Path $PrivateKey) {
        Write-Host "  A private key already exists at:" -ForegroundColor Yellow
        Write-Host "  $PrivateKey" -ForegroundColor Yellow
        Write-Host "  Use [4] Sign (or [6] Sign and commit) if you only need to re-sign." -ForegroundColor Gray
        Press-AnyKey; return
    }

    Assert-OpenSSL

    if (Test-Path $PublicKey) {
        Write-Host "  A public key already exists at $PublicKey" -ForegroundColor Yellow
        Write-Host "  Remove it manually before running first-time setup." -ForegroundColor Yellow
        Press-AnyKey; return
    }

    # ── Step 1: capture passphrase ────────────────────────────────────────────
    $pass    = Read-SecurePassphrase "  [Step 1/3] Enter new signing key passphrase"
    $confirm = Read-SecurePassphrase "             Confirm passphrase"

    if ([string]::IsNullOrWhiteSpace($pass)) { Write-Host "  Passphrase cannot be empty." -ForegroundColor Red; Press-AnyKey; return }
    if ($pass -ne $confirm)                  { Write-Host "  Passphrases do not match."   -ForegroundColor Red; Press-AnyKey; return }
    $confirm = $null

    # ── Step 2: generate key pair ─────────────────────────────────────────────
    New-Item -ItemType Directory -Force -Path $SecretsDir   | Out-Null
    New-Item -ItemType Directory -Force -Path $ReferenceDir | Out-Null

    Write-Host ""
    Write-Host "  [Step 1/3] Generating Ed25519 private key (AES-256-CBC encrypted)..." -ForegroundColor Gray

    & openssl genpkey -algorithm Ed25519 -aes-256-cbc -pass "pass:$pass" -out $PrivateKey
    if ($LASTEXITCODE -ne 0) { Write-Host "  OpenSSL failed." -ForegroundColor Red; $pass = $null; Press-AnyKey; return }

    Write-Host "             Extracting public key..." -ForegroundColor Gray
    & openssl pkey -in $PrivateKey -passin "pass:$pass" -pubout -out $PublicKey
    if ($LASTEXITCODE -ne 0) { Write-Host "  OpenSSL failed." -ForegroundColor Red; $pass = $null; Press-AnyKey; return }

    Ensure-GitIgnore
    Write-Host "  ✓ Key pair created." -ForegroundColor Green

    # ── Step 3: sign ─────────────────────────────────────────────────────────
    Write-Host ""
    Write-Host "  [Step 2/3] Signing official resources..." -ForegroundColor Gray

    $signed = Invoke-Sign -PreCapturedPassphrase $pass
    $pass = $null

    if (-not $signed) { Press-AnyKey; return }

    # ── Step 4: commit ────────────────────────────────────────────────────────
    Write-Host ""
    Write-Host "  [Step 3/3] Committing public artifacts..." -ForegroundColor Gray
    Write-Host ""
    Invoke-Commit
}

# ─────────────────────────────────────────────────────────────────────────────
# Main menu loop
# ─────────────────────────────────────────────────────────────────────────────

while ($true) {
    Write-Header
    Write-Status

    Write-Host "  ┌─────────────────────────────────────────────┐" -ForegroundColor DarkCyan
    Write-Host "  │  [1]  Create new signing key pair           │" -ForegroundColor White
    Write-Host "  │  [2]  Delete existing private key           │" -ForegroundColor White
    Write-Host "  │  [3]  Check key / manifest status           │" -ForegroundColor White
    Write-Host "  │  [4]  Sign official resources               │" -ForegroundColor White
    Write-Host "  │  [5]  Commit public artifacts               │" -ForegroundColor White
    Write-Host "  │  [6]  Sign and commit (combined)            │" -ForegroundColor Cyan
    Write-Host "  │  [7]  First-time setup (key + sign + commit)│" -ForegroundColor Green
    Write-Host "  │  [Q]  Quit                                  │" -ForegroundColor DarkGray
    Write-Host "  └─────────────────────────────────────────────┘" -ForegroundColor DarkCyan
    Write-Host ""

    $choice = Read-Host "  Choose"

    switch ($choice.Trim().ToUpper()) {
        "1" { Invoke-CreateKey }
        "2" { Invoke-DeleteKey }
        "3" { Invoke-CheckStatus }
        "4" { Invoke-Sign }
        "5" { Invoke-Commit }
        "6" { Invoke-SignAndCommit }
        "7" { Invoke-FirstTimeSetup }
        "Q" { Write-Host ""; Write-Host "  Bye." -ForegroundColor DarkGray; Write-Host ""; exit 0 }
        default { Write-Host "  Invalid choice." -ForegroundColor Red; Start-Sleep 1 }
    }
}
