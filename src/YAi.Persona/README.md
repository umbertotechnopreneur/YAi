# YAi.Persona

The `YAi.Persona` project provides the path model, bootstrap state, workspace seeding, prompt composition, built-in skill/tool registry, and the linear workflow runtime used by the YAi! CLI.

## What is this project / folder

- **Project name:** YAi.Persona
- **Purpose:** Provide the runtime workspace layout, skill loader, tool registry, and prompt-loading helpers used by the CLI.
- **Contents:** `Models/`, `Services/`, and the packaged `workspace/` source folder that is populated at build/runtime through the CLI packaging rules.

## Quick Start (placeholders)

- Prerequisites: .NET 10 SDK
- Build:

```bash
dotnet build YAi.Persona.csproj
```

- Run (if applicable):

```bash
dotnet run --project YAi.Persona.csproj
```

## Usage

- Consumed by `src/YAi.Client.CLI` through project reference.
- `AppPaths` resolves the packaged asset workspace from the CLI output, the runtime workspace from `%USERPROFILE%\.yai\workspace` unless `YAI_WORKSPACE_ROOT` overrides it, and the data root from `%LOCALAPPDATA%\YAi\data` unless `YAI_DATA_ROOT` overrides it.
- `WorkspaceProfileService` copies the shipped markdown templates into the user workspace on first run and preserves existing files.
- `SkillLoader` reads bundled and runtime `SKILL.md` files from `workspace/skills/` and exposes the available built-in skills for prompt injection.
- `ToolRegistry` exposes the registered built-in tools, starting with `system_info`.
- `WorkflowVariableResolver` resolves step outputs into later workflow inputs using structured JSON traversal.
- `WorkflowExecutor` runs linear workflows in order, `WorkflowApprovalService` gates approval-required steps, and `WorkflowAuditService` writes structured per-step audit records.

## Development Notes

- [Notes about coding style, conventions, or important developer guidelines]

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

Replace placeholder sections above with project-specific details before publishing.
