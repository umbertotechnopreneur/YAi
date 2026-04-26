# YAi Workflow MVP Dogfood Pack

| Field | Value |
| --- | --- |
| Purpose | Active shared specification pack for the current minimal workflow scenario that chains system info, variable resolution, approval, filesystem write, and audit |
| Audience | Maintainers and contributors |
| Status | Active |
| Last reviewed | 2026-04-27 |

This pack replaces the earlier dogfood draft set with a smaller description of the workflow slice that the code and tests already exercise.

## Current status

- Implemented now: linear workflow execution, structured `SkillResult` output, variable resolution, approval gating, audit writes, and the two-step timestamped-file scenario used by the tests.
- Implemented but still mostly developer-facing: the executor is exercised directly in tests rather than exposed as a polished end-user CLI command for this scenario.
- Not current truth: a fully model-planned end-to-end dogfood flow already wired through the main CLI command path.

## Reading order

1. [Current workflow scenario](01-current-workflow-scenario.md)
2. [Result envelope and variable resolution](02-result-envelope-and-variable-resolution.md)
3. [Approval and audit path](03-approval-and-audit-path.md)

## Diagram

- [Linear workflow dogfood flow](../../../diagrams/linear-workflow-dogfood-flow.md)

## Primary code anchors

- `src/YAi.Persona/Services/Workflows/Models/WorkflowDefinition.cs`
- `src/YAi.Persona/Services/Workflows/Services/WorkflowExecutor.cs`
- `src/YAi.Persona/Services/Workflows/WorkflowVariableResolver.cs`
- `src/YAi.Persona/Services/Execution/SkillResult.cs`
- `src/YAi.Persona/Services/Tools/SystemInfo/SystemInfoTool.cs`
- `src/YAi.Persona/Services/Tools/Filesystem/FilesystemTool.cs`
- `src/YAi.Persona/Services/Workflows/Services/WorkflowAuditService.cs`
- `src/YAi.Persona.Tests/WorkflowExecutorTests.cs`
- `src/YAi.Persona.Tests/SkillResultTests.cs`

## Why this pack was rewritten

The older dogfood pack broke the scenario into many small draft files. That was useful while the workflow model was still being invented.

The current codebase lets us collapse the pack into the three pieces that matter now: the scenario, the structured data handoff, and the approval plus audit boundary.