# Official Resource Signing

YAi bundles official resources — skill definitions, workspace templates, and prompt assets — under
`src/YAi.Resources/reference/`. These files are protected by a signed manifest so the application
can verify they have not been tampered with before loading them as trusted.

---

## How it works

Three files in `src/YAi.Resources/reference/` form the integrity chain:

| File | Purpose | Committed? |
|------|---------|------------|
| `public-key.yai.pem` | Public key used for runtime verification | ✅ Yes |
| `manifest.yai.json` | Lists every official file with its SHA-256 hash and size | ✅ Yes |
| `manifest.yai.sig` | Digital signature of `manifest.yai.json` | ✅ Yes |

The private signing key lives in `.secrets/yai-signing-private-key.pem` — **outside the repository,
never committed**.

At runtime YAi:
1. Loads `public-key.yai.pem` from the binary output directory.
2. Verifies `manifest.yai.sig` against `manifest.yai.json` using the public key.
3. For each file in the manifest: checks existence, size, and SHA-256 hash.
4. Loads built-in skills and seeds workspace templates **only** if all checks pass.

**No passphrase is ever needed at runtime.**

---

## Normal development workflow

### No resource changes

```
dotnet build
```

MSBuild detects that `manifest.yai.json` and `manifest.yai.sig` are up-to-date → no signing prompt
→ build succeeds normally.

### After editing an official resource file

```
# Edit a bundled file, e.g.:
# src/YAi.Resources/reference/skills/filesystem/SKILL.md

dotnet build
```

MSBuild detects that resource files are newer than the manifest/signature → invokes the signing tool:

```
Enter YAi signing key passphrase:
```

Enter the passphrase (it is not echoed). The tool regenerates `manifest.yai.json` and
`manifest.yai.sig`, then the build continues.

Commit the three changed files:

```
git add src/YAi.Resources/reference/skills/filesystem/SKILL.md
git add src/YAi.Resources/reference/manifest.yai.json
git add src/YAi.Resources/reference/manifest.yai.sig
git commit -m "Update filesystem skill and resign manifest"
```

---

## First-time setup (maintainer)

Generate a key pair once and keep the private key in `.secrets/` (which is git-ignored):

```bash
# Create the secrets directory
mkdir -p .secrets

# Generate Ed25519 private key (optionally password-protected)
openssl genpkey -algorithm Ed25519 -out .secrets/yai-signing-private-key.pem

# Extract and commit the public key
openssl pkey -in .secrets/yai-signing-private-key.pem -pubout \
  -out src/YAi.Resources/reference/public-key.yai.pem

# Run the signer once to produce the initial manifest and signature
dotnet run --project src/YAi.Tools.ResourceSigner -- sign \
  --root src/YAi.Resources/reference \
  --private-key .secrets/yai-signing-private-key.pem \
  --manifest src/YAi.Resources/reference/manifest.yai.json \
  --signature src/YAi.Resources/reference/manifest.yai.sig

# Commit the public artifacts
git add src/YAi.Resources/reference/public-key.yai.pem
git add src/YAi.Resources/reference/manifest.yai.json
git add src/YAi.Resources/reference/manifest.yai.sig
git commit -m "Add initial signed resource manifest"
```

---

## CI / automated builds

CI does not sign resources. It only verifies them.

Set these MSBuild properties to disable auto-signing in CI:

```xml
<YaiAutoSignResources>false</YaiAutoSignResources>
```

Or simply rely on the default: `YaiAutoSignResources` is `false` for non-Debug configurations.

CI requirements:
- `public-key.yai.pem`, `manifest.yai.json`, and `manifest.yai.sig` must be committed.
- No private key is needed.
- No passphrase is needed.
- `dotnet test` verifies the committed signatures automatically.

If a resource was edited without updating the manifest/signature, `dotnet test` will fail with
`file_hash_mismatch` or `manifest_signature_invalid` diagnostics.

---

## Non-interactive CI signing (advanced)

If CI must sign resources (e.g., a release pipeline), use `--passphrase-env`:

```bash
dotnet run --project src/YAi.Tools.ResourceSigner -- sign \
  --root src/YAi.Resources/reference \
  --private-key .secrets/yai-signing-private-key.pem \
  --manifest src/YAi.Resources/reference/manifest.yai.json \
  --signature src/YAi.Resources/reference/manifest.yai.sig \
  --passphrase-env YAI_SIGNING_KEY_PASSPHRASE
```

With `YAI_SIGNING_KEY_PASSPHRASE` set as a CI secret. **Never log or print the value.**

---

## Troubleshooting

### "resources are stale but the signing private key was not found"

Restore `.secrets/yai-signing-private-key.pem` to your local machine.

If you don't have the key and just want to build without signing (e.g., to work on unrelated code),
set `YaiSkipResourceSigning=true`:

```
dotnet build /p:YaiSkipResourceSigning=true
```

**Warning:** runtime integrity verification is still active. Built-in skills and templates will not
load as trusted until a valid manifest/signature are in place.

### "manifest_signature_invalid" in tests

Someone edited a resource file without re-signing. Run `dotnet build` locally (with the private key
available) to regenerate the manifest and signature, then commit the updated files.

---

## Security notes

- The passphrase is never stored, logged, or written to any file.
- The passphrase is never accepted as a command-line argument (`--passphrase` does not exist).
- The private key is protected by `.gitignore` patterns: `.secrets/`, `*.key`, `*.private.pem`,
  `*yai-signing-private-key.pem`.
- Use a strong, unique passphrase: `<YAI_SIGNING_KEY_PASSPHRASE>` (placeholder — choose your own).
- Runtime verification uses only `public-key.yai.pem` — no passphrase ever required at runtime.
- User-created workspace files are not required to be signed (V1).
- Imported third-party skills are never treated as official YAi built-in resources.
