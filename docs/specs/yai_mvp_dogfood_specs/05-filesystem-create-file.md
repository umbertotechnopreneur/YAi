# 05 — filesystem.create_file Minimal Implementation

## Purpose

Implement the smallest safe file creation operation needed for the MVP dogfood scenario.

## Action

```text
skill: filesystem
action: create_file
risk: SafeWrite
approval: true
```

## Input

```json
{
  "path": "./output/20260425_122000_qualcosa.txt",
  "content": "Created by YAi.",
  "overwrite": false
}
```

## Rules

```text
- Path must resolve inside workspace root.
- Parent directory may be created if needed.
- Default overwrite = false.
- Existing file with overwrite=false fails safely.
- Write operation requires approval.
- No shell execution.
- Use C# filesystem APIs only.
```

## Output Data

```json
{
  "path": "./output/20260425_122000_qualcosa.txt",
  "absolutePath": "C:/Users/.../.yai/workspace/output/20260425_122000_qualcosa.txt",
  "created": true,
  "bytesWritten": 15
}
```

## Artifact

```json
{
  "kind": "file",
  "path": "./output/20260425_122000_qualcosa.txt",
  "description": "Created file."
}
```

## Acceptance Criteria

```text
- create_file writes inside workspace.
- create_file rejects outside-workspace path.
- create_file does not overwrite by default.
- create_file returns artifact.
- approval is required before execution.
```
