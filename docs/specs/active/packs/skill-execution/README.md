# YAi Skill Execution and Cerbero Pack

| Field | Value |
| --- | --- |
| Purpose | Active shared specification pack for YAi skill loading, tool execution, linear workflow chaining, schema validation, and the current Cerbero command-safety scope |
| Audience | Maintainers and contributors |
| Status | Active |
| Last reviewed | 2026-04-27 |

This pack replaces the older combined skill-chaining and Cerbero draft spec with a grounded description of what YAi actually has today.

## Current status

- Implemented now: `SKILL.md` loading, workspace override behavior, tool registration, standardized `SkillResult`, linear workflow execution, placeholder resolution, minimal schema validation, and the regex-based Cerbero analyzer.
- Implemented but not fully universal yet: Cerbero exists as a tested analyzer, but it is not the active gate in every command or workflow path.
- Not current truth: a fully general multi-step execution system with DAGs, parallelism, and universally enforced command safety across every runtime surface.

## Reading order

1. [Skill discovery and action metadata](01-skill-discovery-and-action-metadata.md)
2. [Linear workflow execution and schema validation](02-linear-workflow-execution-and-schema-validation.md)
3. [Cerbero current role](03-cerbero-current-role.md)

## Primary code anchors

- `src/YAi.Persona/Services/Skills/SkillLoader.cs`
- `src/YAi.Persona/Services/Tools/ToolRegistry.cs`
- `src/YAi.Persona/Services/Execution/SkillResult.cs`
- `src/YAi.Persona/Services/Workflows/Services/WorkflowExecutor.cs`
- `src/YAi.Persona/Services/Workflows/WorkflowVariableResolver.cs`
- `src/YAi.Persona/Services/Skills/Validation/MinimalSkillSchemaValidator.cs`
- `src/YAi.Persona/Services/Operations/Safety/Cerbero/RegexCommandSafetyAnalyzer.cs`
- `src/YAi.Persona.Tests/WorkflowExecutorTests.cs`
- `src/YAi.Persona.Tests/SkillSchemaValidatorTests.cs`
- `src/YAi.Persona.Tests/CerberoCommandSafetyTests.cs`

## Why this pack was rewritten

The older combined spec mixed implemented pieces with broader future ambitions.

The current pack keeps the same themes, but splits them into the three places where the code now gives a defensible story: discovery, execution, and Cerbero's current non-universal safety role.