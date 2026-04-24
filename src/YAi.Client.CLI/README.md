# YAi.Client.CLI

Command-line client for interacting with the YAi Persona services (bootstrap, ask, translate, talk).

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

Quick usage

- `--help` — show the colored Spectre.Console help screen.
- `--bootstrap` — initialize runtime workspace and copy identity templates into the user data root. If no OpenRouter model is configured, the CLI prompts you to choose one first. After preflight passes at startup, the CLI shows the current OpenRouter balance first, then continues into the boot flow.
- `--show-paths` — show the resolved asset, config, memory, skill, history, and storage paths.
- `--gonuclear` — show the custom data wipe screen, confirm interactively, and delete the user data root and all custom runtime state.
- `--lenna` — run the Lenna citation script and exit. This does not require an OpenRouter key.
- `--ask "text"` — single-shot prompt; requires a completed bootstrap, `YAI_OPENROUTER_API_KEY` in environment, and a selected OpenRouter model. The CLI shows the cached OpenRouter balance before sending the prompt.
- `--translate "text"` — translation-style prompt using persona prompts; requires a completed bootstrap, `YAI_OPENROUTER_API_KEY`, and a selected OpenRouter model. The CLI shows the cached OpenRouter balance before sending the prompt.
- `--talk` — interactive REPL (type `exit` to quit); requires a completed bootstrap, `YAI_OPENROUTER_API_KEY`, and a selected OpenRouter model. The CLI shows the cached OpenRouter balance before entering the loop.

Environment

- `YAI_OPENROUTER_API_KEY` — required API key for OpenRouter chat/bootstrap flows and the credits lookup. The CLI can still start without it so the model selector and cached catalog can load.
- Internet access is checked at startup and the CLI now warns instead of failing fast. Remote chat flows still need connectivity.
- `YAI_WORKSPACE_ROOT` — (optional) absolute path to override the runtime workspace root (where memory files, prompts, regex, and skills are stored). Defaults to `%USERPROFILE%\.yai\workspace`.
- `YAI_DATA_ROOT` — (optional) absolute path to override the data root (where logs, history, dreams, and the local SQLite database are written). Defaults to `%LOCALAPPDATA%\YAi\data`. Both roots must not be under the application install directory.

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

The first built-in skill is `system_info`. When `--ask`, `--translate`, or `--talk` runs after bootstrap, the prompt includes the available skills and tools, and the CLI can execute `[TOOL: system_info ...]` calls before producing the final answer.

Examples

Initialize the runtime workspace (creates the user workspace under `%LOCALAPPDATA%\YAi\workspace` and seeds the shipped markdown files):

```powershell
dotnet run --project src/YAi.Client.CLI -- --bootstrap
```

Run the Lenna citation splash screen and exit:

```powershell
dotnet run --project src/YAi.Client.CLI -- --lenna
```

Ask a one-shot question (requires `YAI_OPENROUTER_API_KEY`):

```powershell
$env:YAI_OPENROUTER_API_KEY = "<your-key>"
dotnet run --project src/YAi.Client.CLI -- --ask "What's the weather in Milan?"
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

If you want to exercise network flows, set a valid `YAI_OPENROUTER_API_KEY`. The CLI now warns during preflight instead of failing fast, but chat/bootstrap flows still need a working key and connectivity.

If the runtime `appsettings.json` does not yet contain a model, the CLI opens the model selector before bootstrap or chat flows and writes the selected model back to that runtime config file. After preflight passes, the CLI shows the current balance first at boot and still respects the 10-minute in-memory cache.

License / Contact

See repository root for license and contribution guidelines.

