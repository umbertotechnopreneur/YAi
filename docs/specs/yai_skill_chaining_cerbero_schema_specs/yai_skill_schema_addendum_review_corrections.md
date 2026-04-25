# YAi! Skill Schema Addendum — Corrections After Review

**Document type:** Follow-up implementation addendum  
**Target audience:** ChatGPT 5.4 / GitHub Copilot Agent / AI coding agent  
**Project:** YAi!  
**Date:** 2026-04-25  
**Language:** English  
**Purpose:** Refine the previous `yai_skill_schema_addendum.md` after repository review. This document keeps the original design direction, but fixes two implementation details: diagnostics handling and schema storage type.

---

## 1. Summary

The previous addendum is directionally correct.

The important architectural point remains valid:

```text
SKILL.md documents the contract.
SkillLoader extracts the contract.
Skill model exposes the contract.
Validator uses the contract.
WorkflowExecutor consumes the contract later.
```

However, the review identified two issues that should be corrected before implementation:

```text
1. The warning/diagnostics path is underspecified.
2. JsonDocument is not ideal for long-lived SkillAction schema storage because it is disposable.
```

This addendum replaces those weaker parts with a more precise implementation contract.

---

## 2. What Remains Valid

The following points from the previous addendum remain valid and should not be changed:

```text
- There is no single global skills.md file.
- Built-in skills are individual SKILL.md files under src/YAi.Resources/reference/skills/.
- system_info/SKILL.md is the first correct target for the schema contract.
- The schema must be machine-readable, not only prose in Markdown.
- Skill.cs needs action-level metadata.
- SkillLoader.cs needs to parse and preserve action-level contracts.
- Schemas must remain optional for backward compatibility.
- Existing skills must continue to load.
- Full workflow chaining and Cerbero should not be implemented in this same pass.
```

The implementation should still be a narrow vertical slice.

---

## 3. Correction 1 — Add Explicit Skill Loading Diagnostics

### 3.1 Problem

The previous addendum says:

```text
Report a clear warning if a schema block contains invalid JSON.
```

That is correct as a requirement, but incomplete as an implementation plan.

If `SkillLoader` currently returns only a list of skills, there is no structured place to put warnings or parse diagnostics.

Do not hide parse warnings in console output only.

Do not throw for optional schema errors in V1.

Do not silently ignore invalid schema blocks.

---

### 3.2 Required Design

Add a structured diagnostics path for skill loading.

Recommended new model:

```text
src/YAi.Persona/Services/Skills/SkillLoadDiagnostic.cs
```

Suggested shape:

```csharp
namespace YAi.Persona.Services.Skills;

public sealed class SkillLoadDiagnostic
{
    public string Severity { get; init; } = "warning"; // info, warning, error

    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? SkillName { get; init; }

    public string? ActionName { get; init; }

    public string? FilePath { get; init; }

    public int? LineNumber { get; init; }
}
```

Recommended diagnostic codes:

```text
skill.schema.input.invalid_json
skill.schema.output.invalid_json
skill.schema.variables.invalid_json
skill.action.duplicate
skill.action.invalid_risk_level
skill.action.invalid_requires_approval
skill.schema.missing_optional
```

---

### 3.3 Loader Result Model

If possible, introduce a richer loader result:

```text
src/YAi.Persona/Services/Skills/SkillLoadResult.cs
```

Suggested shape:

```csharp
namespace YAi.Persona.Services.Skills;

public sealed class SkillLoadResult
{
    public IReadOnlyList<Skill> Skills { get; init; }
        = Array.Empty<Skill>();

    public IReadOnlyList<SkillLoadDiagnostic> Diagnostics { get; init; }
        = Array.Empty<SkillLoadDiagnostic>();
}
```

Then either:

```csharp
public SkillLoadResult LoadAllWithDiagnostics(...)
```

or migrate the existing load method carefully.

Preferred low-risk approach:

```text
1. Keep the existing public LoadAll method if other code depends on it.
2. Add LoadAllWithDiagnostics.
3. Make LoadAll call LoadAllWithDiagnostics().Skills.
4. Existing code remains compatible.
5. New tests and future tooling can inspect diagnostics.
```

This avoids breaking existing callers.

---

### 3.4 Invalid JSON Behavior

For V1:

```text
Invalid input schema JSON:
  - keep loading the skill
  - set InputSchema to null for that action
  - add SkillLoadDiagnostic with severity = warning

Invalid output schema JSON:
  - keep loading the skill
  - set OutputSchema to null for that action
  - add SkillLoadDiagnostic with severity = warning

Invalid emitted variables JSON:
  - keep loading the skill
  - set EmittedVariablesSchema/Text to null for that action
  - add SkillLoadDiagnostic with severity = warning
```

Do not disable the whole skill for optional schema block errors.

Later, strict mode can make invalid schema an error.

---

## 4. Correction 2 — Do Not Store JsonDocument Directly in Long-Lived SkillAction

### 4.1 Problem

The previous addendum suggested:

```csharp
public JsonDocument? InputSchema { get; init; }
public JsonDocument? OutputSchema { get; init; }
public JsonDocument? EmittedVariables { get; init; }
```

This is workable but not ideal.

`JsonDocument` implements `IDisposable`. A long-lived runtime model holding disposable documents creates ownership ambiguity:

```text
- Who disposes the document?
- When is it safe to dispose it?
- What happens if the Skill model is cached?
- What happens if multiple services read the schema?
```

Avoid making the `Skill` model own disposable JSON documents unless there is a clear disposal lifecycle.

---

### 4.2 Recommended Schema Storage

Use one of these safer options.

#### Option A — Raw JSON string plus lazy parsing

Recommended for V1.

```csharp
public string? InputSchemaJson { get; init; }

public string? OutputSchemaJson { get; init; }

public string? EmittedVariablesJson { get; init; }
```

Pros:

```text
- simple
- immutable
- serializable
- no disposal lifecycle
- easy to log/debug
- future validator can parse it
```

Cons:

```text
- validation service parses when needed
```

This is acceptable for V1.

---

#### Option B — JsonElement cloned from a temporary JsonDocument

Also acceptable if the team strongly prefers structured JSON values.

```csharp
public JsonElement? InputSchema { get; init; }

public JsonElement? OutputSchema { get; init; }

public JsonElement? EmittedVariables { get; init; }
```

If using `JsonElement`, the loader must clone the root element before disposing the document:

```csharp
using var document = JsonDocument.Parse(json);
var schema = document.RootElement.Clone();
```

Do not store a `JsonElement` pointing to a disposed `JsonDocument`.

---

### 4.3 Recommended Choice

Use **Option A** first:

```text
Store raw schema JSON strings in SkillAction.
Parse them in the validator.
```

This keeps the skill model simple and avoids lifetime bugs.

---

## 5. Revised SkillAction Model

Replace the previous `JsonDocument`-based proposal with this V1 model:

```csharp
using YAi.Persona.Services.Tools;

namespace YAi.Persona.Services.Skills;

public sealed class SkillAction
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public ToolRiskLevel RiskLevel { get; init; } = ToolRiskLevel.SafeReadOnly;

    public bool RequiresApproval { get; init; }

    public string? SideEffects { get; init; }

    public string? InputSchemaJson { get; init; }

    public string? OutputSchemaJson { get; init; }

    public string? EmittedVariablesJson { get; init; }
}
```

Optional future refinement:

```csharp
public string? InputSchemaHash { get; init; }
public string? OutputSchemaHash { get; init; }
```

Not required now.

---

## 6. Revised Skill.cs Change

Add:

```csharp
public IReadOnlyDictionary<string, SkillAction> Actions { get; init; }
    = new Dictionary<string, SkillAction>(StringComparer.OrdinalIgnoreCase);
```

Compatibility rule:

```text
Skills without action metadata must still load with an empty Actions dictionary.
```

Do not require all existing skills to define action schemas immediately.

---

## 7. Explicit Markdown Parser Contract

The previous addendum used a Markdown-heading approach. That is valid, but it must be stated as a strict parser contract.

For V1, the parser should support this exact structure:

````markdown
## Actions

### get_datetime

Short action description.

Risk: SafeReadOnly  
Side effects: none  
Requires approval: false  

#### Input schema

```json
{
  "type": "object"
}
```

#### Output schema

```json
{
  "type": "object"
}
```

#### Emitted variables

```json
{
  "timestamp_safe": "yyyyMMdd_HHmmss"
}
```
````

### 7.1 Heading rules

```text
- `## Actions` starts the action section.
- Each `### <action_name>` starts a new action.
- `#### Input schema` belongs to the current action.
- `#### Output schema` belongs to the current action.
- `#### Emitted variables` belongs to the current action.
```

### 7.2 Metadata line rules

Support these metadata lines inside the action section:

```text
Risk: <ToolRiskLevel>
Side effects: <text>
Requires approval: true|false
```

Parsing should be case-insensitive for labels:

```text
Risk:
risk:
Requires approval:
requires approval:
```

But stored action names should preserve the written action name.

### 7.3 JSON fence rules

The parser should accept only fenced JSON blocks for schemas:

````markdown
```json
...
```
````

If a schema heading exists but no JSON fence follows, create a diagnostic warning.

---

## 8. ToolRiskLevel Mapping

Map Markdown risk values to existing `ToolRiskLevel`.

Expected values:

```text
SafeReadOnly
SafeWrite
Risky
Destructive
```

If the current enum differs, use the current enum exactly.

Invalid risk value behavior:

```text
- default to SafeReadOnly
- add diagnostic:
  code = skill.action.invalid_risk_level
  severity = warning
```

Do not fail the skill load for an invalid optional risk string.

---

## 9. Requires Approval Parsing

Expected values:

```text
true
false
```

Accepted variants:

```text
yes/no
required/not required
```

Recommended V1 minimum:

```text
true/false only
```

Invalid value behavior:

```text
- infer from risk level if possible:
  SafeReadOnly => false
  SafeWrite/Risky/Destructive => true
- add diagnostic:
  code = skill.action.invalid_requires_approval
  severity = warning
```

---

## 10. Minimal Validator Revision

Because `SkillAction` now stores raw JSON schema strings, the validator should accept raw schema text.

Suggested interface:

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

Implementation detail:

```text
- Look up skill.Actions[actionName].
- Read InputSchemaJson or OutputSchemaJson.
- If missing, return valid with optional warning.
- For V1, parse schema JSON to prove it is valid JSON.
- Optionally check that input/output is an object.
- Do not implement full JSON Schema validation yet unless already available.
```

---

## 11. Revised Tests

Add or update tests to cover both action parsing and diagnostics.

### 11.1 Happy path tests

```text
1. system_info skill loads.
2. system_info exposes get_datetime action.
3. get_datetime RiskLevel is SafeReadOnly.
4. get_datetime RequiresApproval is false.
5. get_datetime InputSchemaJson is not null or whitespace.
6. get_datetime OutputSchemaJson is not null or whitespace.
7. get_datetime EmittedVariablesJson contains timestamp_safe.
8. InputSchemaJson parses as valid JSON.
9. OutputSchemaJson parses as valid JSON.
```

### 11.2 Backward compatibility tests

```text
1. A skill with no `## Actions` section still loads.
2. A skill with actions but no schemas still loads.
3. Existing filesystem skill still loads.
4. Existing imported skill structure still loads.
```

### 11.3 Diagnostic tests

Create temporary test skill files for:

```text
1. invalid input schema JSON
2. invalid output schema JSON
3. schema heading without JSON fence
4. invalid risk level
5. invalid requires approval value
6. duplicate action name
```

Expected behavior:

```text
- loader returns the skill
- loader returns diagnostics
- no unhandled exception
```

---

## 12. Revised Acceptance Criteria

The addendum implementation is complete when:

```text
- system_info/SKILL.md has an explicit get_datetime action contract.
- SkillAction exists.
- Skill.cs exposes Actions.
- SkillLoader parses action metadata from SKILL.md.
- Schemas are stored as raw JSON strings or cloned JsonElement values, not undisposed JsonDocument references.
- SkillLoadDiagnostic and SkillLoadResult exist, or an equivalent diagnostics path exists.
- Invalid optional schema JSON creates diagnostics instead of crashing.
- Existing skills continue to load.
- Tests cover happy path, compatibility, and diagnostics.
- dotnet build succeeds.
```

---

## 13. Updated Implementation Order

Use this order:

```text
1. Add SkillAction with raw schema JSON string fields.
2. Add SkillLoadDiagnostic.
3. Add SkillLoadResult or equivalent diagnostics path.
4. Extend Skill.cs with Actions.
5. Extend SkillLoader.cs to parse action sections.
6. Update system_info/SKILL.md.
7. Add minimal schema validator abstraction.
8. Add happy path tests.
9. Add compatibility tests.
10. Add diagnostic tests.
11. Run dotnet build.
```

Do not start Cerbero in this same implementation slice.

---

## 14. Follow-Up: When to Start Cerbero

Cerbero should start after this schema addendum is implemented and build/tests are green.

Minimum readiness checklist before Cerbero:

```text
- system_info/SKILL.md schema is machine-readable.
- SkillLoader action parsing works.
- SkillAction metadata exists.
- Existing skills load.
- Diagnostics path works.
- Tests pass.
- dotnet build succeeds.
```

Then Cerbero can be implemented as an isolated module.

Cerbero should not depend on the skill schema parser, but it benefits from the same discipline:

```text
- structured models
- diagnostics
- tests first
- no command execution in the first pass
```

---

## 15. Updated Prompt for ChatGPT 5.4 / Copilot Agent

Use this prompt:

```text
The previous addendum is valid in direction, but please revise the implementation according to these corrections:

1. Do not store schema JSON as JsonDocument in long-lived SkillAction models.
   Prefer raw JSON string fields:
   - InputSchemaJson
   - OutputSchemaJson
   - EmittedVariablesJson

2. Add a structured diagnostics path for SkillLoader.
   If SkillLoader currently returns only skills, add SkillLoadResult and SkillLoadDiagnostic, or an equivalent non-breaking diagnostics mechanism.

3. Keep invalid optional schema JSON non-fatal in V1.
   The loader should keep loading the skill and return a warning diagnostic.

4. Make the Markdown parser contract explicit:
   - ## Actions
   - ### <action_name>
   - Risk: <ToolRiskLevel>
   - Side effects: <text>
   - Requires approval: true|false
   - #### Input schema
   - fenced json block
   - #### Output schema
   - fenced json block
   - #### Emitted variables
   - fenced json block

5. Keep backward compatibility.
   Existing skills without schemas or without action metadata must still load.

6. Add tests for:
   - system_info.get_datetime schema extraction
   - existing skills still loading
   - invalid schema JSON generating diagnostics
   - invalid risk value generating diagnostics
   - duplicate action names generating diagnostics

Do not implement Cerbero, WorkflowExecutor, SkillResult envelope, approval UI, or full JSON Schema validation in this pass.

After implementation, run dotnet build and targeted tests.
```

---

## 16. Final Recommendation

Proceed with this corrected schema addendum first.

The implementation should end with a machine-readable skill action contract and a reliable loader diagnostics path.

Only after that should the project move to Cerbero.

Correct sequence:

```text
1. Machine-readable SKILL.md action schema
2. Loader diagnostics
3. Minimal schema validator seam
4. Cerbero isolated module
5. SkillResult envelope
6. Linear workflow executor
7. Approval integration
8. Full JSON Schema validation
```

This keeps the architecture controlled and avoids mixing too many risk-sensitive changes in one pass.
