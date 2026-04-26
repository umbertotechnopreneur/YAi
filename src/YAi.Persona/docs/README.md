# YAi.Persona Docs

| Field | Value |
| --- | --- |
| Purpose | Local documentation index for the YAi runtime library, including paths, workspace seeding, prompt assets, skills, tools, and workflow services |
| Audience | Project maintainers and contributors |
| Status | Active |
| Last reviewed | 2026-04-27 |

## What belongs here

Use this folder for runtime-library details about:

- workspace and data roots
- prompt asset loading
- skill loading and tool registration
- workflow execution and approval services
- local security services

## Shared docs this project depends on

- [Shared docs index](../../../docs/README.md)
- [Documentation governance](../../../docs/DOCUMENTATION-GOVERNANCE.md)
- [Workspace memory pack](../../../docs/specs/active/packs/workspace-memory/README.md)
- [Workflow MVP dogfood pack](../../../docs/specs/active/packs/mvp-dogfood/README.md)
- [Skill execution and Cerbero pack](../../../docs/specs/active/packs/skill-execution/README.md)

## Local files

- [Local changelog](local-changelog.md)

## Current code anchors

- `Services/AppPaths.cs`
- `Services/WorkspaceProfileService.cs`
- `Services/PromptAssetService.cs`
- `Services/Skills/SkillLoader.cs`
- `Services/Tools/ToolRegistry.cs`
- `Services/Workflows/Services/WorkflowExecutor.cs`

Cross-project contracts should stay shared. This folder is for local implementation detail and local change history.