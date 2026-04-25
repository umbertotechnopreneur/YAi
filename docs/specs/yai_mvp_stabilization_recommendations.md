# YAi! MVP Stabilization Recommendations

**Project:** YAi!  
**Document type:** Risk + complexity stabilization recommendations  
**Date:** 2026-04-25  
**Scope:** MVP dogfood path for steps 06–11: variable resolution, workflow execution, approval, filesystem write, audit, and Cerbero preparation.

---

## 1. Core Position

YAi! is a new product. The codebase does not need backward compatibility with old execution paths, old adapters, or legacy behavior.

The MVP should prefer:

```text
clean contracts
single execution path
strict runtime gates
fail-fast behavior
explicit errors
no silent fallback
no legacy compatibility layer unless explicitly justified
```

The immediate goal is not to preserve old mechanisms. The immediate goal is to prove that YAi! can execute one safe, deterministic, auditable workflow.

---

## 2. MVP Safety Principle

The MVP execution contract should be:

```text
Planner proposes.
WorkflowExecutor resolves and validates.
Approval gate authorizes.
Typed tool executes.
Verifier checks.
Audit records.
```

No other execution path should be active during MVP dogfood.

The model must never be treated as the enforcement authority. The C# runtime is the policy engine.

---

## 3. Non-Negotiable Rules

```text
1. No legacy execution paths.
2. No fallback execution path.
3. No filesystem write without runtime approval.
4. No write outside the workspace.
5. No shell execution for filesystem operations.
6. No permanent delete in MVP.
7. No silent variable substitution.
8. No silent audit failure for write operations.
9. No dependence on SKILL.md alone for critical enforcement.
10. No hidden continuation after failed validation, failed approval, failed execution, or failed verification.
```

If any critical precondition is missing or ambiguous, YAi! should fail fast with a clear error.

---

## 4. Recommended Fixes Before MVP Dogfood

## 4.1 Disable `filesystem.plan`

`filesystem.plan` currently represents a second execution path with a separate approval loop and separate audit behavior.

For MVP, this should be disabled.

Recommended behavior:

```text
filesystem.plan -> not_supported_for_mvp
```

Do not keep it as an executable path. Do not route workflow execution through it. Do not preserve it for compatibility.

Reason:

```text
A new product should not carry two execution models for the same operation.
```

The only active MVP path should be:

```text
WorkflowExecutor -> filesystem.create_file -> typed C# file write
```

---

## 4.2 Add a hard approval gate inside `filesystem.create_file`

Approval must not depend only on `SKILL.md` metadata.

`WorkflowExecutor` should request approval before execution, but `filesystem.create_file` should also refuse to write unless the execution request contains an explicit approved runtime context.

Required rule:

```text
filesystem.create_file must not write unless approval was granted for the resolved operation.
```

Recommended failure behavior:

```text
approval_required
no file written
clear error returned
workflow stops
```

This protects against direct invocation through `ToolRegistry.ExecuteAsync` or any future bypass.

---

## 4.3 Move approval abstractions out of filesystem namespace

The workflow layer should not depend on filesystem-specific namespaces.

Move:

```text
YAi.Persona.Services.Tools.Filesystem.IApprovalCardPresenter
```

To a generic operation approval namespace, for example:

```text
YAi.Persona.Services.Operations.Approval.IOperationApprovalPresenter
```

Reason:

```text
Approval is an operation concept, not a filesystem concept.
```

Do not keep a filesystem-specific alias for backward compatibility unless there is a concrete need. This is a new product; prefer a clean dependency graph.

---

## 4.4 Make audit failure visible and blocking for writes

Audit failures must not be swallowed silently for write operations.

Minimum MVP behavior:

```text
Before SafeWrite execution:
  verify audit folder can be created and written.
  if audit preflight fails, block execution.

After SafeWrite execution:
  if audit write fails, return a visible audit warning or failure state.
```

Preferred stricter behavior for MVP:

```text
If audit cannot be initialized, do not perform the write.
```

Reason:

```text
A write operation that cannot be audited violates the trust model.
```

---

## 4.5 Normalize filesystem path handling for all actions

All filesystem actions should use the same path resolution pipeline.

Required path pipeline:

```text
input path
-> if relative, combine with workspace root
-> Path.GetFullPath
-> WorkspaceBoundaryService validation
-> execution
```

This must apply to:

```text
create_file
list_directory
read_metadata
future copy/move/delete operations
```

No action should resolve relative paths against the process working directory.

---

## 4.6 Fix cancellation audit consistency

If workflow execution is cancelled before a step runs, the cancellation record should be written immediately, not only during final audit aggregation.

Required behavior:

```text
Cancellation detected
-> create cancellation step record
-> write step record immediately
-> write final audit summary
-> stop workflow
```

This keeps cancellation behavior consistent with all other failure paths.

---

## 4.7 Add stable Cerbero rule IDs before shell execution is enabled

This is not required for the first filesystem-only dogfood, but it should be fixed before any shell-capable skill is activated.

Recommended finding shape:

```text
ruleId: powershell.remote-download.pipe-execute
severity: critical
message: Remote content is piped directly to execution.
matchedSegment: ...
```

Do not rely on regex text or prose as the identifier.

---

## 5. Fail-Fast Policy

YAi! should fail fast when any of the following occurs:

```text
missing skill
missing action
missing variable
missing data field
invalid input schema
invalid output schema
approval required but not granted
workspace boundary failure
audit preflight failure for write operation
attempt to use disabled action
attempt to execute unsupported operation
attempt to use shell execution from filesystem skill
```

Avoid behavior such as:

```text
try old path
fallback to legacy executor
ignore audit failure
replace missing variable with empty string
continue after denied approval
continue after failed verification
execute with partial validation
```

A failed MVP run is acceptable. A successful run through the wrong path is not.

---

## 6. Recommended MVP Execution Surface

Only these should be active for the first dogfood:

```text
system_info.get_datetime
filesystem.create_file
WorkflowVariableResolver
WorkflowExecutor
WorkflowApprovalService / generic approval presenter
WorkflowAuditService
WorkspaceBoundaryService
```

Everything else should be disabled, unreachable, or analysis-only.

Specifically:

```text
filesystem.plan                 disabled
shell execution                  disabled
permanent delete                 unavailable
move/copy/rename/delete          out of scope
background autonomous execution  unavailable
DAG workflows                    unavailable
AI memory promotion              unavailable
```

---

## 7. Tests to Add or Verify

## 7.1 Direct write bypass test

```text
Scenario:
  filesystem.create_file invoked directly without approved runtime context.

Expected:
  blocked
  no file created
  clear approval_required error
```

## 7.2 Disabled legacy path test

```text
Scenario:
  workflow calls filesystem.plan.

Expected:
  not_supported_for_mvp
  no operation executed
  workflow stops
```

## 7.3 Approval deny test

```text
Scenario:
  workflow reaches filesystem.create_file and user denies approval.

Expected:
  no file created
  denial recorded in audit
  workflow stops
```

## 7.4 Outside workspace path test

```text
Scenario:
  filesystem.create_file path = ../outside.txt

Expected:
  blocked
  no file created
  boundary failure recorded
```

## 7.5 Missing variable test

```text
Scenario:
  path uses ${steps.bad.variables.timestamp_safe}

Expected:
  variable resolution fails
  no approval card shown
  no file created
```

## 7.6 Audit preflight failure test

```text
Scenario:
  audit folder cannot be created/written before SafeWrite.

Expected:
  write blocked
  clear audit failure returned
```

## 7.7 Read path normalization test

```text
Scenario:
  list_directory uses relative path ./output

Expected:
  path resolves under workspace root
```

## 7.8 Read outside workspace test

```text
Scenario:
  read_metadata path = ../outside.txt

Expected:
  blocked by WorkspaceBoundaryService
```

---

## 8. Suggested Implementation Order

```text
1. Disable filesystem.plan execution.
2. Add approved runtime context / hard approval gate to filesystem.create_file.
3. Move approval presenter abstraction to generic operation namespace.
4. Add audit preflight for write operations.
5. Make audit write failures visible to WorkflowExecutor.
6. Normalize read action path handling.
7. Fix cancellation step audit write.
8. Add tests for bypass, disabled plan, audit failure, path boundary, and missing variables.
9. Run dotnet build.
10. Run dotnet test.
11. Dogfood only with a prebuilt workflow.
12. Add LLM planner after deterministic workflow is stable.
```

---

## 9. MVP Definition of Done

The MVP is ready for first dogfood only when this works:

```text
User prompt or selected workflow:
Create a timestamped file in ./output with today's date.
```

Expected behavior:

```text
1. system_info.get_datetime runs without approval.
2. timestamp_safe is produced as a variable.
3. filesystem.create_file input is resolved.
4. resolved path is validated inside workspace.
5. approval card shows the resolved path.
6. deny prevents execution.
7. approve allows execution.
8. file is created inside workspace.
9. artifact is returned.
10. audit records workflow, resolved input, approval, result, and summary.
```

No legacy path should be involved.

---

## 10. Final Recommendation

Before proceeding with MVP dogfood, close these two gaps:

```text
1. Remove or disable the legacy `filesystem.plan` execution path.
2. Enforce approval inside `filesystem.create_file`, not only in WorkflowExecutor metadata.
```

After these are fixed, the system has a much stronger trust boundary.

The correct MVP target is not broad capability. It is a narrow, deterministic, auditable proof that YAi! can safely execute one approved local action without legacy bypasses or silent fallbacks.
