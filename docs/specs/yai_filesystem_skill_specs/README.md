# YAi Filesystem Skill — Concept Specification

This package describes a safe, reviewable filesystem skill architecture for YAi-style agents using frontier models through OpenRouter.ai.

The goal is not to give the model raw shell access. The goal is to let the model propose a structured sequence of filesystem actions, render each action as an approval card, and execute only the approved typed operation through a controlled C# service.

## Core idea

The model proposes:

```text
context → plan → mitigation → approval card → execute → verify → audit
```

The application enforces:

```text
workspace boundary → operation validation → mitigation requirements → approval state → execution → verification → audit trail
```

## Recommended first scope

Start with filesystem operations because they are simpler than Git but still exercise the important safety primitives:

- create folders
- create files
- copy files/folders
- move or rename files/folders
- backup files/folders
- move items to recoverable trash
- list folders
- read file metadata

Avoid in v1:

- permanent delete
- permission changes
- symlink creation
- shell execution
- recursive destructive operations without explicit item enumeration
- operations outside the approved workspace root

## Files in this package

| File | Purpose |
|---|---|
| `01-architecture.md` | Architecture and execution lifecycle |
| `02-command-plan-schema.md` | YAML-oriented CommandPlan, ContextPack, StepCard schema |
| `03-filesystem-skill.md` | Proposed filesystem skill instructions |
| `04-risk-and-mitigation.md` | Risk model and required mitigations |
| `05-card-ux.md` | Approval card behavior and fields |
| `06-agent-pseudocode.md` | High-level pseudocode for planner, validator, executor |
| `07-example-plans.md` | Example user requests and generated plans |
| `08-implementation-notes-csharp.md` | C# implementation notes without code |
