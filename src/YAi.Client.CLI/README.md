# YAi.Client.CLI

| Field | Value |
| --- | --- |
| Purpose | User-facing command-line client for the YAi local runtime |
| Audience | Users, maintainers, and contributors |
| Status | Active |
| Last reviewed | 2026-04-27 |

Command-line client for interacting with the YAi Persona services (bootstrap, show-banner, manifesto, ask, translate, talk).

For CLI-local implementation notes, see [docs/README.md](docs/README.md).

Prerequisites
- .NET 10 SDK on Windows, macOS, or Linux

Build

```bash
dotnet build src/YAi.Client.CLI/YAi.Client.CLI.csproj -c Debug
```

Run (development)

```bash
dotnet run --project src/YAi.Client.CLI -- --help
```

PowerShell output helper

Run the launcher from the solution root to move into the compiled output directory.

```powershell
.\jump-cli-output.ps1
.\jump-cli-output.ps1 --release
.\jump-cli-output.ps1 --run
.\jump-cli-output.ps1 --release --run
```

The script defaults to `bin/Debug/net10.0`, switches to `bin/Release/net10.0` with `--release`, and launches the compiled app with no arguments when `--run` is present. Use `--help` to print the built-in usage text.

Quick usage

- `--help` — show the colored Spectre.Console help screen.
- `--version` — show the banner splash from `BannerScreen.razor`, then the compiled CLI and assembly version, and exit.
- Unrecognized arguments fail fast with the banner, a short explanation, and the help screen.
- `--bootstrap` — initialize runtime workspace and copy identity templates into the user data root. If no OpenRouter model is configured, the CLI prompts you to choose one first. After preflight passes at startup, the CLI shows the current OpenRouter balance first, then continues into the boot flow. On first launch, the bootstrap intro clears the screen and scrollback, then tries to print the centered 800x600 YAi logo splash before the workspace setup message, falling back to the banner when the script cannot be rendered. Bootstrap input now accepts multi-line paste as one turn, sends on `Enter`, uses `Shift+Enter` to insert a newline, and restores the current draft when you browse prompt history with `Up` and `Down`. Interactive bootstrap now runs through a combined conversation screen that keeps the transcript, response metadata, and next prompt on one host surface, while non-screen fallback output still uses the shared inline response panel.
- `--show-banner` — show the CLI banner splash and the top app header, then exit. This does not require an OpenRouter key.
- `--manifesto` — show the CLI banner splash, then the manifesto excerpt with the repository link at the bottom, and exit. This does not require an OpenRouter key.
- `--show-paths` — show the resolved asset, workspace, memory, data, config, and logs paths, including the workspace security and secret-store files.
- `--show-cli-path` — show whether the CLI executable directory is already visible on PATH and where it was found.
- `--add-to-path` — add the current CLI directory to the current user PATH on Windows, removing older YAi CLI entries when found. This fails fast on macOS and Linux.
- `--security status` — show app-lock state, KDF settings, and secret-vault paths.
- `--security setup-lock` — enable app lock, create `workspace/config/security.json`, and import the current OpenRouter key into encrypted storage when one is available.
- `--security disable-lock` — disable app lock after verifying the current passphrase.
- `--security change-passphrase` — change the unlock passphrase and re-encrypt local secrets.
- `--gonuclear` — show the custom data wipe screen, optionally create a zip backup with the workspace/data/config folder structure first, then delete the user data root and all custom runtime state. The backup archive is written outside the deleted roots under `%LOCALAPPDATA%\YAi\backups\yyyyMMdd\`.
- `--lenna` — run the Lenna citation script and exit. This does not require an OpenRouter key.
- `--ask [text]` — single-shot prompt when text is provided, or the reusable multiline prompt editor when text is omitted; requires a completed bootstrap, a configured OpenRouter secret or `YAI_OPENROUTER_API_KEY`, and a selected OpenRouter model. When app lock is enabled, the CLI prompts for the unlock passphrase first. The interactive ask editor sends on `Enter`, uses `Shift+Enter` for a newline, restores the current draft when you browse prompt history with `Up` and `Down`, and cancels with `Esc`. Interactive ask responses now open the reusable response screen, where `Enter` continues, `Esc` closes, `C` copies the formatted response on Windows, and `J` toggles raw JSON when the active turn exposes it. Redirected or other non-screen ask output now uses the same shared inline response panel and metadata layout instead of the old single-line assistant output.
- `--translate "text"` — translation-style prompt using persona prompts; requires a completed bootstrap, a configured OpenRouter secret or `YAI_OPENROUTER_API_KEY`, and a selected OpenRouter model. When app lock is enabled, the CLI prompts for the unlock passphrase first. Interactive translate responses now use the same reusable response screen and action hints as `--ask`, and non-screen fallback output now uses the same shared inline response panel and metadata layout.
- `--talk` — interactive REPL (type `exit` to quit); requires a completed bootstrap, a configured OpenRouter secret or `YAI_OPENROUTER_API_KEY`, and a selected OpenRouter model. When app lock is enabled, the CLI prompts for the unlock passphrase first. Talk input now accepts multi-line paste as one turn, sends on `Enter`, uses `Shift+Enter` to insert a newline, and restores the current draft when you browse prompt history with `Up` and `Down`. Interactive talk now runs through a combined conversation screen that keeps the transcript, response metadata, and next prompt on one host surface, while non-screen fallback output still uses the shared inline response panel.

Chat display

- Chat prompts now render the user and assistant names with two-tone labels, such as `umber:` and `YAi!:`, and the CLI shows a `thinking...` spinner while waiting for the model.
- Interactive ask and translate responses render through the reusable response screen, while redirected or other non-screen ask and translate output reuses the shared inline response panel.
- Interactive talk and bootstrap now render through a combined conversation screen that reuses the same response state and metadata formatting for transcript entries, while non-screen fallback output still uses the shared inline response panel.
- Screen-based flows render a reusable top app header with persona identity when available, a shortened workspace path, the active model provider and model name, bootstrap and app-lock state, cache state, current date/time, and a clickable link to [umbertogiacobbi.biz/YAi](https://umbertogiacobbi.biz/YAi).
- Chat and bootstrap turns also render a reusable status bar that uses emoji-coded scope and activity, shows sent and received token counts in different colors, includes the total token count and last response duration when available, and keeps the current local date/time plus navigation hints such as the talk REPL exit cue.

Versioning

- The CLI version comes from [Directory.Build.props](../../Directory.Build.props), so every project in the solution builds with the same version number.
- Run `scripts/Set-YAiVersion.ps1 -Version 1.2.3` for a semver-style update or `scripts/Set-YAiVersion.ps1 -Timestamp` for a timestamp-derived build version.

Packaging

- Run `pwsh ./scripts/Publish-YAiCliArtifacts.ps1` from the repository root to build zipped Windows release artifacts.
- Run `pwsh ./scripts/Publish-YAiCliArtifacts.ps1 --help` to print the script purpose, switches, and examples.
- The script writes each run to `artifacts/cli/<utc-timestamp>/` and names each zip with the variant, RID, version, and UTC packaging timestamp.
- Each successful artifact also gets a companion `.zip.sha256` checksum file, and each batch gets `checksums.sha256` plus `release-manifest.json`.
- When the YAi signing key is available, the batch also includes `release-manifest.json.sig` and `public-key.yai.pem` so the release metadata can be authenticity-verified.
- Detached manifest signing now defaults to `RSA-PSS-SHA256`, which is the current validated release-signing path.
- Default output includes framework-dependent and self-contained packages for `win-x64` and `win-arm64`, plus NativeAOT packages for the same RIDs when prerequisites are installed.
- Use `-SkipAot` when you only want the baseline release artifacts.
- Use `-Variant FrameworkDependent`, `-Variant SelfContained`, or `-Variant Aot` to restrict the publish matrix.
- Pass multiple variants as a comma-separated list to choose more than one publish mode in the same run, for example `-Variant SelfContained,Aot`.
- Use `-RuntimeIdentifier win-x64` or `-RuntimeIdentifier win-arm64` to restrict the target architecture.
- Pass multiple runtime identifiers as a comma-separated list, for example `-RuntimeIdentifier win-x64,win-arm64`.
- Use `-KeepPublishFolders` if you want the unzipped publish directories preserved next to the generated zip files.
- Use `-SkipDetachedSignature` when you intentionally want an unsigned local artifact batch.
- Use `-ReleaseSignatureAlgorithm Ed25519` only when you intentionally need a legacy Ed25519-signed batch instead of the default RSA batch.
- NativeAOT packaging is supported when the required Visual Studio C++ workloads and Windows SDK libraries are installed.
- The script now performs a NativeAOT prerequisite preflight and reports missing Visual Studio C++ workloads or Windows SDK libraries before any AOT publish begins.
- Run `pwsh ./scripts/Test-YAiCliArtifacts.ps1 -ArtifactRoot ./artifacts/cli/<utc-timestamp>` to verify the manifest signature when present, confirm that the SHA-256 values still match, and confirm that the generated ZIP files are readable.
- The verifier also defaults to `RSA-PSS-SHA256`; use `-SignatureAlgorithm Ed25519` only for older Ed25519-signed batches.
- Use `-AllowUnsigned` with the verifier only for older or intentionally unsigned batches that do not include `release-manifest.json.sig` and `public-key.yai.pem`.

Environment

- `YAI_OPENROUTER_API_KEY` — optional fallback API key for OpenRouter chat/bootstrap flows and the credits lookup when the protected secret store has not been configured yet.
- `workspace/config/security.json` — workspace-local app-lock verifier file. Created by `--security setup-lock`.
- `workspace/config/secrets.json` — workspace-local encrypted secret store for OpenRouter and future provider credentials.
- Internet access is checked at startup and the CLI now warns instead of failing fast. Remote chat flows still need connectivity.
- `YAI_WORKSPACE_ROOT` — (optional) absolute path to override the runtime workspace root (where memory files, prompts, regex, and skills are stored). Defaults to `%USERPROFILE%\.yai\workspace`.
- `YAI_DATA_ROOT` — (optional) absolute path to override the data root (where logs, history, dreams, and the local SQLite database are written). Defaults to `%LOCALAPPDATA%\YAi\data`. Both roots must not be under the application install directory.
- PATH registration is Windows-only. `--add-to-path` updates the current user PATH and removes older YAi CLI directory entries when it can. `--show-cli-path` is read-only and can still report whether the CLI directory is already visible on PATH on macOS and Linux. Open a new terminal session, or refresh the shell environment, after running `--add-to-path` so the updated user PATH is picked up.

Catalog cache

- The OpenRouter model list is cached under the user data root at `config/openrouter-model-catalog.json`.
- The cache includes the catalog retrieval timestamp and is refreshed every 7 days when the CLI needs model data.
- If refresh fails but a cache already exists, the selector can still use the stale cache.

Balance cache

- The OpenRouter credits endpoint is queried before bootstrap and before chat-style flows, then cached in memory for 10 minutes.
- The balance screen shows the remaining balance and total spent, together with the last balance check timestamp.
- If the credits endpoint cannot be reached, the last cached balance is reused when available so repeated checks do not flood OpenRouter.

Assets

The shipped markdown templates live in `src/YAi.Resources/reference/templates` and are copied into the CLI output as `workspace/` during build. The bundled built-in skills live in `src/YAi.Resources/reference/skills` and are copied into `workspace/skills/` during build. On first run, the CLI seeds `%LOCALAPPDATA%\YAi\workspace` from that packaged workspace without overwriting existing files.

The splash scripts `lenna.ps1` and `yai_logo_ansi_800x600.ps1` share `splash-helpers.ps1` for centered rendering and console/scrollback clearing.

Bundled skills currently include `filesystem` and `system_info`. The `--ask` and `--talk` flows inject the available skills and built-in tools before the model responds, while `--translate` uses the translation prompt without that extra tool context. When app lock is enabled, the CLI unlocks before loading memory or tools.


Examples

Initialize the runtime workspace (creates the user workspace under `%LOCALAPPDATA%\YAi\workspace` and seeds the shipped markdown files):

```powershell
dotnet run --project src/YAi.Client.CLI -- --bootstrap
```

Run the Lenna citation splash screen and exit:

```powershell
dotnet run --project src/YAi.Client.CLI -- --lenna
```

Show the CLI banner splash and exit:

```powershell
dotnet run --project src/YAi.Client.CLI -- --show-banner
```

Show the CLI banner splash and manifesto excerpt, then exit:

```powershell
dotnet run --project src/YAi.Client.CLI -- --manifesto
```

Ask a one-shot question (requires `YAI_OPENROUTER_API_KEY`):

```powershell
$env:YAI_OPENROUTER_API_KEY = "<your-key>"
dotnet run --project src/YAi.Client.CLI -- --ask "What's the weather in Milan?"
```

Open the multiline ask editor:

```powershell
dotnet run --project src/YAi.Client.CLI -- --ask
```

Start interactive REPL:

```powershell
dotnet run --project src/YAi.Client.CLI -- --talk
```

Development notes

- Use `dotnet build` to compile. The CLI references the `YAi.Persona` project directly to reuse services and models.
- The app enforces atomic writes to the workspace and data roots and will refuse to write under the app install directory unless `YAI_WORKSPACE_ROOT` or `YAI_DATA_ROOT` are set to absolute external paths.

Testing / Smoke checks

Run the bootstrap command to verify templates are copied and the app can write to the user data root.

```powershell
dotnet run --project src/YAi.Client.CLI -- --bootstrap
```

If you want to exercise network flows, set a valid `YAI_OPENROUTER_API_KEY` or configure the encrypted OpenRouter secret through `--security setup-lock`. The CLI now warns during preflight instead of failing fast, but chat/bootstrap flows still need a working key or secret and connectivity.

If the runtime `appsettings.json` does not yet contain a model, the CLI opens the model selector before bootstrap or chat flows and writes the selected model back to that runtime config file. After preflight passes, the CLI shows the current balance first at boot and still respects the 10-minute in-memory cache.

License / Contact

See repository root for license and contribution guidelines.

