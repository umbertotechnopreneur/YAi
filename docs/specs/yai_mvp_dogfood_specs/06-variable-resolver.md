# 06 — Variable Resolver

## Purpose

Resolve values from previous workflow steps into later step inputs.

## Syntax

```text
${steps.<stepId>.variables.<name>}
${steps.<stepId>.data.<field>}
```

MVP required syntax:

```text
${steps.sysinfo.variables.timestamp_safe}
```

## Example

Input template:

```json
{
  "path": "./output/${steps.sysinfo.variables.timestamp_safe}_qualcosa.txt",
  "content": "Created by YAi."
}
```

Resolved:

```json
{
  "path": "./output/20260425_122000_qualcosa.txt",
  "content": "Created by YAi."
}
```

## Rules

```text
- No expressions.
- No function calls.
- No shell expansion.
- Missing step fails.
- Missing variable fails.
- Missing data field fails.
- Do not replace missing values with empty string.
```

## Acceptance Criteria

```text
- timestamp_safe resolves correctly.
- missing step produces clear error.
- missing variable produces clear error.
- expressions are not evaluated.
```
