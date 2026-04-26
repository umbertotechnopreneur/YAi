# YAi Workspace Memory Pack

| Field | Value |
| --- | --- |
| Purpose | Active shared specification pack for how YAi currently stores, loads, stages, and promotes local workspace memory |
| Audience | Maintainers and contributors |
| Status | Active |
| Last reviewed | 2026-04-27 |

This pack replaces the earlier workspace-memory draft set with a smaller, grounded reading order.

It describes the code that exists today, not the broader future platform that earlier notes sometimes mixed in.

## Current status

- Implemented now: workspace and data path resolution, first-run template seeding, prompt loading, regex loading, warm memory resolution, dream proposal staging, and promoted-memory writes with backup and rollback.
- Implemented but only partly wired into the CLI: `ExtractionPipelineService`, `MemoryFlushService`, and the review UI surface.
- Not current truth: a larger multilingual memory platform with full automatic extraction, full review commands, and every planned workspace subtree already active.

## Reading order

1. [Workspace layout and ownership](01-workspace-layout-and-ownership.md)
2. [Memory loading and file contracts](02-memory-loading-and-file-contracts.md)
3. [Prompt, regex, and skill assets](03-prompt-regex-and-skill-assets.md)
4. [Candidate staging and review](04-candidate-staging-and-review.md)

## Diagram

- [Workspace memory runtime flow](../../../diagrams/workspace-memory-runtime-flow.md)

## Primary code anchors

- `src/YAi.Persona/Services/AppPaths.cs`
- `src/YAi.Persona/Services/WorkspaceProfileService.cs`
- `src/YAi.Persona/Services/MemoryFileParser.cs`
- `src/YAi.Persona/Services/WarmMemoryResolver.cs`
- `src/YAi.Persona/Services/PromptAssetService.cs`
- `src/YAi.Persona/Services/RegexRegistry.cs`
- `src/YAi.Persona/Services/CandidateStore.cs`
- `src/YAi.Persona/Services/DreamingService.cs`
- `src/YAi.Persona/Services/PromotionService.cs`
- `src/YAi.Persona/Services/MemoryTransactionManager.cs`
- `src/YAi.Client.CLI/Program.cs`

## Why this pack was rewritten

The earlier pack contained useful design intent, but it also described planned behavior as if it were already live.

This rewrite keeps the useful ownership model and safety goals, then narrows each file to the parts that can be defended directly against the current codebase.