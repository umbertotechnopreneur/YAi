**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Resource Signing and Verification ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** The shared operator guide for signing YAi bundled resources, understanding the verification chain, and troubleshooting local or CI signing problems.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Resource Signing and Verification

YAi treats the files under `src/YAi.Resources/reference/` as trusted bundled assets.

That includes shipped templates, bundled skills, and the manifest files that prove those assets were not changed after signing.

## Integrity chain

Three committed files form the public verification chain:

| File | Purpose | Committed |
|---|---|---|
| `public-key.yai.pem` | Public key used at runtime and in tests | Yes |
| `manifest.yai.json` | Hash and size list for every signed resource | Yes |
| `manifest.yai.sig` | Digital signature for the manifest | Yes |

The private signing key stays outside the repo by default at `.secrets/yai-signing-private-key.pem`.

## What happens at runtime

When YAi loads trusted bundled assets, it verifies the chain first:

1. the manifest signature is checked with the public key,
2. every listed file is checked for existence, size, and hash,
3. only then are built-in resources treated as trusted.

This verification path is what protects workspace seeding, bundled skill loading, and legacy bundled prompt fallback.

## Everyday local workflow

### If you did not change bundled resources

Run:

```powershell
dotnet build
```

Nothing extra happens when the manifest and signature are already current.

### If you changed a signed resource

Run:

```powershell
dotnet build
```

In Debug builds, `YAi.Resources.csproj` enables auto-signing by default when resources are stale.

`YAi.Resources.Signing.targets` then runs the signer tool and prompts for the private-key passphrase.

After a successful build, commit:

- the changed resource file,
- `manifest.yai.json`,
- and `manifest.yai.sig`.

## Maintainer helper script

For key creation, rotation, signing, and commit support, use `yai-signing.ps1` from the repo root.

It is the easiest current operator path because it already knows the standard repo layout.

Current helper behavior includes:

- creating a new signing key pair,
- backing up an existing private key before deletion,
- re-signing official resources,
- and optionally helping stage public artifacts for commit.

When option `[2]` is used to remove a private key, the script first creates a timestamped backup under `.secrets/backups/`.

## Manual first-time setup

If you need the low-level manual flow, the signer tool still supports it.

Example:

```powershell
dotnet run --project src/YAi.Tools.ResourceSigner -- sign \
  --root src/YAi.Resources/reference \
  --private-key .secrets/yai-signing-private-key.pem \
  --manifest src/YAi.Resources/reference/manifest.yai.json \
  --signature src/YAi.Resources/reference/manifest.yai.sig
```

Important current boundary:

- `--passphrase` is intentionally not supported
- interactive entry is the default local path
- `--passphrase-env` is the supported non-interactive CI path

## CI behavior

CI should usually verify, not sign.

Useful facts grounded in the current project files:

- `YaiAutoSignResources` defaults to `true` only for Debug when not explicitly set
- it defaults to `false` otherwise
- `YaiSkipResourceSigning=true` skips signing but does not disable runtime verification

The committed public artifacts must still be present in CI:

- `public-key.yai.pem`
- `manifest.yai.json`
- `manifest.yai.sig`

If a signed resource changes without a new manifest and signature, verification tests can fail with diagnostics such as `file_hash_mismatch` or `manifest_signature_invalid`.

## Troubleshooting

### Private key missing during a stale-resource build

If the build reports that official resources are stale but the signing key was not found, you have two realistic options:

- restore the private key under `.secrets/`, or
- build with `YaiSkipResourceSigning=true` when you only need a local compile.

Example:

```powershell
dotnet build /p:YaiSkipResourceSigning=true
```

That skips signing only. It does not disable verification in the runtime or tests.

### Non-interactive signing in CI

When CI really must sign, pass an environment-variable name instead of a raw passphrase:

```powershell
dotnet run --project src/YAi.Tools.ResourceSigner -- sign \
  --root src/YAi.Resources/reference \
  --private-key .secrets/yai-signing-private-key.pem \
  --manifest src/YAi.Resources/reference/manifest.yai.json \
  --signature src/YAi.Resources/reference/manifest.yai.sig \
  --passphrase-env YAI_SIGNING_KEY_PASSPHRASE
```

## Security boundaries

- The private key is not a repo artifact.
- The passphrase is not accepted as a CLI argument.
- Runtime verification uses only the public key.
- User-created workspace files are not part of the signed bundled-resource chain.
- Imported third-party skills are not treated as official YAi bundled assets.