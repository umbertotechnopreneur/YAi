# 08 — C# Implementation Notes

This document intentionally avoids code. It describes implementation shape and boundaries.

---

## Recommended internal services

### SkillRouter

Selects the active skill.

Responsibilities:

```yaml
responsibilities:
  - classify user request
  - select filesystem skill
  - reject unsupported domains
```

### ContextManager

Builds the `ContextPack`.

Responsibilities:

```yaml
responsibilities:
  - determine workspace root
  - determine current folder
  - list relevant directory contents
  - normalize paths
  - check write permissions
  - provide OS metadata
```

### ModelPlanner

Calls OpenRouter.ai with:

```yaml
inputs:
  - user request
  - skill instructions
  - context pack
  - output schema
  - safety rules
```

Expected output:

```yaml
output:
  - CommandPlan YAML or JSON
```

Recommendation: internally parse to a strongly typed object even if the model emits YAML.

### CommandPlanValidator

Validates the plan before rendering cards.

Responsibilities:

```yaml
responsibilities:
  - validate schema completeness
  - validate operation types
  - validate path boundaries
  - validate approval requirements
  - validate mitigation rules
  - validate rollback and verification fields
```

### ApprovalCardService

Converts steps into user-facing cards.

Responsibilities:

```yaml
responsibilities:
  - render card fields
  - capture user approval
  - support edit / skip / cancel
  - persist approval decisions
```

### FileSystemExecutor

Executes typed operations.

Responsibilities:

```yaml
responsibilities:
  - create directory
  - create file
  - copy file
  - copy directory
  - move file
  - move directory
  - backup file
  - backup directory
  - trash file
  - trash directory
```

This service should not expose arbitrary shell execution.

### VerificationService

Checks whether each step succeeded.

Responsibilities:

```yaml
responsibilities:
  - path exists
  - path does not exist
  - path is file
  - path is directory
  - content hash matches when needed
  - backup exists
```

### AuditService

Persists context, plans, approvals, execution events, errors, and verification results.

---

## Typed operation execution

The UI may show:

```powershell
New-Item -ItemType Directory -Path ".\ProjectA"
```

But the executor should perform a typed operation:

```yaml
typed_operation:
  type: create_directory
  path: D:\Workspace\ProjectA
```

This is essential for multiplatform support.

---

## Multiplatform strategy

Use C# filesystem APIs as the execution layer.

The `display_command` can vary by OS:

```yaml
windows:
  shell: powershell
  text: New-Item -ItemType Directory -Path ".\ProjectA"

macos_linux:
  shell: bash
  text: mkdir "ProjectA"
```

But the executable operation remains the same:

```yaml
type: create_directory
path: <absolute-normalized-path>
```

---

## Workspace boundary

Before execution, every path should be:

```text
normalized → made absolute → checked against workspace root
```

Reject:

```yaml
reject:
  - paths using traversal to escape workspace
  - absolute paths outside workspace
  - system directories
  - user profile sensitive folders unless explicitly configured as workspace
```

---

## First implementation milestone

Implement the minimum viable loop:

```text
1. User asks to create folders.
2. App builds ContextPack.
3. Model returns CommandPlan.
4. App validates plan.
5. UI shows one card per folder.
6. User approves cards one by one.
7. Executor creates folders.
8. Verifier checks folder existence.
9. Audit trail records the operation.
```

This milestone proves the architecture without touching destructive operations.

---

## Second milestone

Add safe delete and backup:

```text
1. User asks to delete a file or folder.
2. Model proposes trash operation, not permanent delete.
3. App validates trash path under .yai/trash.
4. User approves.
5. Executor moves item to trash.
6. Verifier confirms original path is gone and trash copy exists.
7. Rollback card is available.
```

---

## Third milestone

Add file creation and overwrite protection:

```text
1. User asks to create or replace a file.
2. App detects whether file exists.
3. If file exists, plan must include backup step first.
4. User approves backup.
5. User approves write.
6. Verifier confirms file exists.
7. Rollback uses backup copy.
```

---

## Principle

The frontier model is a planner, not an executor.

The C# application is the policy engine, executor, verifier, and audit authority.
