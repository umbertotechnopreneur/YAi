# YAi.Client.CLI Docs

| Field | Value |
| --- | --- |
| Purpose | Local documentation index for the YAi CLI process, command dispatch, and bootstrap orchestration surface |
| Audience | Project maintainers and contributors |
| Status | Active |
| Last reviewed | 2026-04-27 |

## What belongs here

Use this folder for CLI-specific implementation notes and local history about:

- command parsing and dispatch in `Program.cs`
- bootstrap orchestration from the CLI process
- prompt-editor orchestration for `--bootstrap`, `--ask`, and `--talk` in `Program.cs`
- banner, splash, and terminal presentation helpers
- shared header/footer chrome state in `AppHeaderState.cs` and `StatusBarState.cs`
- local launch helpers such as `jump-cli-output.ps1`

## Shared docs this project depends on

- [Shared docs index](../../../docs/README.md)
- [Documentation governance](../../../docs/DOCUMENTATION-GOVERNANCE.md)
- [CLI flow diagram](../../../docs/specs/diagrams/2026-04-25-cli-flows-and-architecture.md)
- [Boot sequence diagram](../../../docs/specs/diagrams/boot-sequence.md)

## Local files

- [Local changelog](local-changelog.md)

## Current code anchors

- `Program.cs`
- `AppHeaderState.cs`
- `StatusBarState.cs`
- `appsettings.json`
- `lenna.ps1`
- `yai_logo_ansi_800x600.ps1`
- `splash-helpers.ps1`

Cross-project contracts such as workflow behavior or memory rules should stay under the shared `docs/` tree.