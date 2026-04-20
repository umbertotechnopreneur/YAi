# YAi.Client.CLI

Command-line client for interacting with the YAi Persona services (bootstrap, ask, translate, talk).

Prerequisites
- .NET 10 SDK

Build

```bash
dotnet build src/YAi.Client.CLI/YAi.Client.CLI.csproj -c Debug
```

Run (development)

```bash
dotnet run --project src/YAi.Client.CLI -- - -help
```

Quick usage

- `--bootstrap` — initialize runtime workspace and copy identity templates into the user data root.
- `--ask "text"` — single-shot prompt; requires `OPENROUTER_API_KEY` in environment.
- `--translate "text"` — translation-style prompt using persona prompts; requires `OPENROUTER_API_KEY`.
- `--talk` — interactive REPL (type `exit` to quit).

Environment

- `OPENROUTER_API_KEY` — (optional) API key for OpenRouter. If not set, network calls will be disabled and the CLI will report missing configuration.
- `YAI_USER_DATA_ROOT` — (optional) absolute path to override user data root (where runtime workspace, logs, history, and config are written). Must not be under the application install directory.

Assets

The shipped markdown templates live in `src/YAi.Resources/reference/templates` and are copied into the CLI output as `workspace/` during build. On first run, the CLI seeds `%LOCALAPPDATA%\YAi\workspace` from that packaged workspace without overwriting existing files.

Examples

Initialize the runtime workspace (creates the user workspace under `%LOCALAPPDATA%\YAi\workspace` and seeds the shipped markdown files):

```powershell
dotnet run --project src/YAi.Client.CLI -- --bootstrap
```

Ask a one-shot question (requires `OPENROUTER_API_KEY`):

```powershell
$env:OPENROUTER_API_KEY = "<your-key>"
dotnet run --project src/YAi.Client.CLI -- --ask "What's the weather in Milan?"
```

Start interactive REPL:

```powershell
dotnet run --project src/YAi.Client.CLI -- --talk
```

Development notes

- Use `dotnet build` to compile. The CLI references the `YAi.Persona` project directly to reuse services and models.
- The app enforces atomic writes to the user data root and will refuse to write under the app install directory unless `YAI_USER_DATA_ROOT` is set to an absolute external path.

Testing / Smoke checks

Run the bootstrap command to verify templates are copied and the app can write to the user data root.

```powershell
dotnet run --project src/YAi.Client.CLI -- --bootstrap
```

If you want to exercise network flows, set a valid `OPENROUTER_API_KEY`. The CLI will report clear errors when network keys are missing.

License / Contact

See repository root for license and contribution guidelines.

