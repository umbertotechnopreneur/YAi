# YAi Filesystem Skill Pack

| Field | Value |
| --- | --- |
| Purpose | Active shared specification pack for the current YAi filesystem tool, its approval boundary, and the narrower MVP scope wired today |
| Audience | Maintainers and contributors |
| Status | Active |
| Last reviewed | 2026-04-27 |

This pack replaces the earlier filesystem concept pack with a narrower description of the tool surface that exists now.

## Current status

- Implemented now: the `filesystem` tool is registered, supports `list_directory`, `read_metadata`, and `create_file`, checks the workspace boundary, and requires explicit approval for file writes.
- Implemented around it: workflow approval service, approval-card presenter integration, and test coverage for the write boundary and denial path.
- Present but not the main supported path yet: planner and command-plan services.
- Not current truth: a fully active plan-driven filesystem flow where the tool accepts model-authored plans as the default runtime path.

## Reading order

1. [Current skill surface](01-current-skill-surface.md)
2. [Workspace boundary and approval flow](02-workspace-boundary-and-approval-flow.md)
3. [Planning gap and follow-up scope](03-planning-gap-and-follow-up-scope.md)

## Diagram

- [Filesystem create-file approval flow](../../../diagrams/filesystem-create-file-approval-flow.md)

## Primary code anchors

- `src/YAi.Persona/Services/Tools/Filesystem/FilesystemTool.cs`
- `src/YAi.Persona/Services/Operations/Safety/WorkspaceBoundaryService.cs`
- `src/YAi.Persona/Services/Workflows/Services/WorkflowExecutor.cs`
- `src/YAi.Persona/Services/Workflows/Services/WorkflowApprovalService.cs`
- `src/YAi.Persona/Extensions/ServiceCollectionExtensions.cs`
- `src/YAi.Persona.Tests/FilesystemToolCreateFileTests.cs`
- `src/YAi.Persona.Tests/WorkflowExecutorTests.cs`
- `src/YAi.Client.CLI.Components/Components/Cards/ApprovalCard.razor`

## Why this pack was rewritten

The older filesystem pack was useful as a design direction, but it described a wider planner-driven architecture than the active MVP surface supports.

This rewrite keeps the safety goals and the approval model, then narrows the wording to the actions and boundaries that the current tool and tests actually defend.