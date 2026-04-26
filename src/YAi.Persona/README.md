# YAi.Persona

The `YAi.Persona` project is the shared runtime library behind the YAi! CLI. It owns path resolution, workspace seeding, prompt asset loading, OpenRouter catalog and balance caching, app-lock and secret storage, skill loading, tool registration, and the linear workflow runtime.

## What is this project / folder

- **Project name:** YAi.Persona
- **Purpose:** Provide the shared runtime services and models consumed by the CLI and any future host.
- **Contents:** `Extensions/`, `Models/`, and `Services/`.

## Build

```bash
dotnet build YAi.Persona.csproj
```

This is a class library, so it is not run directly with `dotnet run`.

## Usage

- Consumed by `src/YAi.Client.CLI` through project reference.
- `AppPaths` resolves the packaged asset workspace from the CLI output, the runtime workspace from `%USERPROFILE%\.yai\workspace` unless `YAI_WORKSPACE_ROOT` overrides it, the workspace config root at `workspace/config`, and the data root from `%LOCALAPPDATA%\YAi\data` unless `YAI_DATA_ROOT` overrides it.
- `WorkspaceProfileService` seeds the shipped markdown templates and bundled skills into the user workspace on first run without overwriting existing files.
- `PromptAssetService` loads prompt sections from the runtime workspace and falls back to the packaged `SYSTEM-PROMPTS.md` only when the bundled assets verify cleanly.
- `PromptBuilder` composes chat messages from prompt assets, runtime identity, and, for `ask` and `talk`, the bundled skills and tool registry.
- `ConfigService` loads `appsettings.json`, overlays `workspace/config/appconfig.json`, and persists bootstrap state.
- `OpenRouterCatalogService` caches the model catalog on disk for seven days, and `OpenRouterBalanceService` caches balance lookups in memory for ten minutes.
- `OpenRouterClient` resolves the API key from `YAI_OPENROUTER_API_KEY` or the app-lock protected secret store, then talks to the OpenRouter chat and credits endpoints.
- `AppLockService` manages `workspace/config/security.json`, the unlock passphrase verifier, and encrypted local secrets in `workspace/config/secrets.json`.
- `SkillLoader` reads bundled and runtime `SKILL.md` files from `workspace/skills/` and exposes the available built-in skills for prompt injection.
- `ToolRegistry` currently exposes the built-in `system_info` and `filesystem` tools.
- `WorkflowVariableResolver` resolves step outputs into later workflow inputs using structured JSON traversal.
- `WorkflowExecutor` runs linear workflows in order, `WorkflowApprovalService` gates approval-required steps, and `WorkflowAuditService` writes structured per-step audit records.

## Development Notes

- Keep prompt assets, workspace layout, and service registrations aligned with the files packaged from `YAi.Resources`.
- Update this library before changing CLI flows that depend on its paths, prompt content, or tool registry.

## Contributing

- Open a pull request with a clear description of changes.
- Follow repository coding and documentation rules.

## Copyright

**Copyright © 2026 UmbertoGiacobbiDotBiz. All rights reserved.**

## Contact

- Website: https://umbertogiacobbi.biz
- Email: hello@umbertogiacobbi.biz

## AI Disclaimer

This file may include content generated, refined, or reviewed with the assistance of one or more AI models.  
It should be reviewed and validated before external distribution or operational use.  
Final responsibility for verification, interpretation, and application remains with the author(s) and the organization.

---
