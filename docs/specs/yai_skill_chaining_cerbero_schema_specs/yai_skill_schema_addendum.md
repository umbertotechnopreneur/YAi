# YAi! Skill Schema Addendum

**Document type:** Implementation addendum  
**Target audience:** ChatGPT 5.4 / GitHub Copilot Agent / AI coding agent  
**Project:** YAi!  
**Date:** 2026-04-25  
**Language:** English  
**Purpose:** Refine the previous implementation specification by making the `SKILL.md` input/output schema contract machine-readable, not only documentary.

---

## 1. Context

The current YAi! skill system already uses OpenClaw-style `SKILL.md` files for built-in and imported skills.

The current proposal correctly identified that there is no single global `skills.md` file. The built-in skills are individual `SKILL.md` files located under:

```text
src/YAi.Resources/reference/skills/
```

The first built-in skill that should receive an explicit input/output schema contract is:

```text
src/YAi.Resources/reference/skills/system_info/SKILL.md
```

The `filesystem` skill can be used as a style and contract reference, but the first implementation should remain narrow.

---

## 2. Main Correction

The previous plan is directionally correct but too documentation-oriented.

Adding `Input schema` and `Output schema` sections inside `SKILL.md` is useful, but it is not enough.

The schema must become part of the runtime skill model.

The correct target is:

```text
SKILL.md documents the contract.
SkillLoader extracts the contract.
Skill model exposes the contract.
Validator uses the contract.
WorkflowExecutor will consume the contract later.
```

The key requirement is that YAi! must be able to do this programmatically:

```csharp
skill.Actions["get_datetime"].InputSchema
skill.Actions["get_datetime"].OutputSchema
skill.Actions["get_datetime"].RiskLevel
skill.Actions["get_datetime"].RequiresApproval
```

If the schema only exists as Markdown text, it will not support chaining, validation, or safe tool orchestration.

---

## 3. Objective of This Addendum

Implement a small vertical slice that makes action-level skill schemas machine-readable.

This addendum does **not** implement the full workflow engine, Cerbero, DAG workflows, sandboxing, or advanced validation.

It prepares the skill system for those later features.

---

## 4. Revised Scope

### In scope

Implement:

```text
1. Update system_info/SKILL.md with an explicit action contract.
2. Add a SkillAction model.
3. Extend Skill.cs to expose action metadata.
4. Extend SkillLoader.cs to parse action-level schema blocks.
5. Preserve backward compatibility with existing skills.
6. Add a minimal schema validation abstraction.
7. Add tests for loading action schemas from system_info/SKILL.md.
```

### Out of scope

Do **not** implement yet:

```text
- full workflow chaining
- Cerbero
- command execution safety
- DAG workflows
- full JSON Schema validation library integration
- approval card changes
- filesystem write execution changes
- sandbox execution
```

---

## 5. Files to Inspect First

Before editing, inspect these files:

```text
src/YAi.Persona/Services/Skills/Skill.cs
src/YAi.Persona/Services/Skills/SkillLoader.cs
src/YAi.Persona/Services/Skills/OpenClawMetadata.cs
src/YAi.Resources/reference/skills/system_info/SKILL.md
src/YAi.Resources/reference/skills/filesystem/SKILL.md
src/YAi.Persona/Services/Tools/ToolRiskLevel.cs
```

If tests already exist for skill loading, inspect and extend them. If not, add focused tests.

---

## 6. Target `SKILL.md` Action Contract

Update:

```text
src/YAi.Resources/reference/skills/system_info/SKILL.md
```

Add or normalize an action section for `get_datetime`.

Recommended shape:

````markdown
## Actions

### get_datetime

Returns the current system date and time in a structured format.

Risk: SafeReadOnly  
Side effects: none  
Requires approval: false  

#### Input schema

```json
{
  "type": "object",
  "properties": {
    "timezone": {
      "type": "string",
      "description": "Timezone name, IANA timezone, or 'local'.",
      "default": "local"
    }
  },
  "additionalProperties": false
}
```

#### Output schema

```json
{
  "type": "object",
  "properties": {
    "utc": {
      "type": "string",
      "description": "UTC timestamp in ISO-8601 format."
    },
    "local": {
      "type": "string",
      "description": "Local timestamp in ISO-8601 format."
    },
    "timezone": {
      "type": "string",
      "description": "Resolved timezone name."
    },
    "date": {
      "type": "string",
      "description": "Local date in yyyy-MM-dd format."
    },
    "time": {
      "type": "string",
      "description": "Local time in HH:mm:ss format."
    },
    "timestampSafe": {
      "type": "string",
      "description": "Filesystem-safe timestamp in yyyyMMdd_HHmmss format."
    },
    "unixSeconds": {
      "type": "integer",
      "description": "Unix timestamp in seconds."
    }
  },
  "required": [
    "utc",
    "local",
    "timezone",
    "date",
    "time",
    "timestampSafe"
  ],
  "additionalProperties": true
}
```

#### Emitted variables

```json
{
  "date": "yyyy-MM-dd",
  "time": "HH:mm:ss",
  "timestamp_safe": "yyyyMMdd_HHmmss",
  "timezone": "Resolved timezone name"
}
```

#### Example output data

```json
{
  "utc": "2026-04-25T05:20:00Z",
  "local": "2026-04-25T12:20:00+07:00",
  "timezone": "Asia/Ho_Chi_Minh",
  "date": "2026-04-25",
  "time": "12:20:00",
  "timestampSafe": "20260425_122000",
  "unixSeconds": 1777094400
}
```
````

Important: the exact Markdown headings should be stable because `SkillLoader` will parse them.

---

## 7. Recommended C# Model Changes

### 7.1 Add `SkillAction`

Suggested file:

```text
src/YAi.Persona/Services/Skills/SkillAction.cs
```

Suggested shape:

```csharp
using System.Text.Json;
using YAi.Persona.Services.Tools;

namespace YAi.Persona.Services.Skills;

public sealed class SkillAction
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public ToolRiskLevel RiskLevel { get; init; } = ToolRiskLevel.SafeReadOnly;

    public bool RequiresApproval { get; init; }

    public string? SideEffects { get; init; }

    public JsonDocument? InputSchema { get; init; }

    public JsonDocument? OutputSchema { get; init; }

    public JsonDocument? EmittedVariables { get; init; }
}
```

If namespace names differ in the repository, follow the existing repository namespaces.

---

### 7.2 Extend `Skill.cs`

Add an action dictionary:

```csharp
public IReadOnlyDictionary<string, SkillAction> Actions { get; init; }
    = new Dictionary<string, SkillAction>(StringComparer.OrdinalIgnoreCase);
```

Backward compatibility rule:

```text
Skills without action metadata must still load.
```

Do not make `Actions` required at startup.

---

## 8. SkillLoader Parsing Requirements

Extend `SkillLoader.cs` so it can parse action-level metadata from `SKILL.md`.

### 8.1 Parsing rules

The loader should recognize:

```text
## Actions
### <action_name>
Risk: <risk-level>
Side effects: <text>
Requires approval: true|false

#### Input schema
```json
...
```

#### Output schema
```json
...
```

#### Emitted variables
```json
...
```
```

### 8.2 Compatibility rules

The loader must:

```text
1. Continue loading old skills with no schema sections.
2. Not fail the entire skill if an optional schema is missing.
3. Report a clear warning if a schema block contains invalid JSON.
4. Preserve the normal skill description and OpenClaw metadata behavior.
5. Avoid changing existing skill import behavior unless necessary.
```

### 8.3 Invalid schema behavior

Recommended V1 behavior:

```text
Invalid input schema:
  - load the skill
  - skip that schema
  - add warning to diagnostics/logs

Invalid output schema:
  - load the skill
  - skip that schema
  - add warning to diagnostics/logs
```

Do not disable the whole skill in this phase.

---

## 9. Minimal Schema Validator Abstraction

Add the interface now, even if implementation is minimal.

Suggested folder:

```text
src/YAi.Persona/Services/Skills/Validation/
```

Suggested files:

```text
ISkillSchemaValidator.cs
SkillSchemaValidationResult.cs
NoOpSkillSchemaValidator.cs
```

### 9.1 Interface

```csharp
using System.Text.Json;

namespace YAi.Persona.Services.Skills.Validation;

public interface ISkillSchemaValidator
{
    SkillSchemaValidationResult ValidateInput(
        Skill skill,
        string actionName,
        JsonElement input);

    SkillSchemaValidationResult ValidateOutput(
        Skill skill,
        string actionName,
        JsonElement output);
}
```

### 9.2 Result model

```csharp
namespace YAi.Persona.Services.Skills.Validation;

public sealed class SkillSchemaValidationResult
{
    public bool IsValid { get; init; } = true;

    public IReadOnlyList<string> Errors { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; init; }
        = Array.Empty<string>();
}
```

### 9.3 V1 implementation

Use a no-op or minimal validator first:

```text
No schema:
  valid

Schema present:
  optionally check that the payload is a JSON object
  optionally check required fields if simple to implement
```

Do not introduce a heavy JSON Schema dependency in this addendum unless the repository already uses one.

The purpose is to establish the seam.

---

## 10. Tests

Add focused tests.

Recommended test cases:

```text
1. Built-in system_info skill loads successfully.
2. system_info exposes an action named get_datetime.
3. get_datetime has RiskLevel = SafeReadOnly.
4. get_datetime RequiresApproval = false.
5. get_datetime has a non-null input schema.
6. get_datetime has a non-null output schema.
7. get_datetime emitted variables include timestamp_safe.
8. Existing filesystem skill still loads even if not all actions have schemas.
9. Invalid JSON schema in a test skill produces a warning but does not crash loading.
10. A skill with no action schema remains compatible.
```

If the current test infrastructure is not ready, add tests in the nearest existing test project. If no test project exists, create a small one only if that matches repository convention.

---

## 11. Acceptance Criteria

The implementation is complete when:

```text
- system_info/SKILL.md contains a clear input/output schema for get_datetime.
- Skill.cs exposes action-level schema metadata.
- SkillLoader parses the get_datetime schema into structured JsonDocument fields.
- Existing skills continue to load.
- Missing schema is allowed.
- Invalid schema is reported clearly.
- Tests prove system_info schema extraction works.
- dotnet build succeeds.
```

---

## 12. Non-Goals for This Addendum

Do not implement these items in this pass:

```text
- SkillResult envelope
- WorkflowExecutor
- Cerbero
- approval card rendering changes
- shell command analyzer
- real JSON Schema validation through external package
- filesystem create_file chaining
```

Those belong to the larger V1/V2 implementation specification and should follow after the schema contract is machine-readable.

---

## 13. Recommended Implementation Order

Follow this order:

```text
1. Update system_info/SKILL.md.
2. Add SkillAction.cs.
3. Extend Skill.cs with Actions.
4. Extend SkillLoader.cs with action parsing.
5. Add schema validation abstraction.
6. Add tests.
7. Run dotnet build.
8. Run targeted tests.
```

Do not begin with workflow execution. The schema contract must be readable first.

---

## 14. Suggested Prompt for ChatGPT 5.4 / Copilot Agent

Use this prompt:

```text
Revise the previous plan into a vertical implementation slice.

Goal:
- Update the built-in SKILL.md contract for system_info.get_datetime.
- Add structured action schema support to the Skill model.
- Extend SkillLoader so it can parse and preserve input/output JSON schemas from SKILL.md action sections.
- Keep schemas optional for backward compatibility.
- Add a minimal ISkillSchemaValidator abstraction.
- Add tests proving that system_info.get_datetime exposes input schema, output schema, risk level, requiresApproval, and emitted variables.
- Do not implement full workflow chaining yet.
- Do not implement Cerbero yet.
- Do not introduce a heavy JSON Schema dependency unless strictly needed.
- Ensure existing built-in skills still load.

Before editing:
1. Inspect Skill.cs, SkillLoader.cs, OpenClawMetadata.cs.
2. Inspect src/YAi.Resources/reference/skills/system_info/SKILL.md.
3. Inspect src/YAi.Resources/reference/skills/filesystem/SKILL.md.
4. Identify the smallest safe model changes.

Deliver:
1. Short implementation plan.
2. Exact files to modify.
3. Patch the code.
4. Add or update tests.
5. Run dotnet build.
```

---

## 15. Final Recommendation

The correct sequence is:

```text
SKILL.md action schema
→ SkillLoader parsing
→ Skill model exposure
→ schema validation seam
→ SkillResult envelope
→ linear workflow executor
→ Cerbero
→ approval enforcement
→ strict JSON Schema validation
```

Do not skip the machine-readable schema step.

Without it, workflow chaining will depend on prose and will remain fragile.

With it, YAi! can safely move toward:

```text
system_info.get_datetime
→ structured SkillResult
→ variable resolver
→ filesystem.create_file
→ approval
→ audit
```

This keeps the project aligned with the core YAi! principles:

```text
local-first
deterministic where possible
safe by default
auditable
OpenClaw-compatible
workflow-ready
```
