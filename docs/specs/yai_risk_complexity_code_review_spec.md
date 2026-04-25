# YAi! Tight Code Review Specification — Risk + Complexity Only

**Document type:** Review specification  
**Project:** YAi!  
**Scope:** Current MVP implementation after steps 06–11  
**Date:** 2026-04-25  
**Purpose:** Guide a focused code review that looks only at the two issues that matter now: **risk** and **complexity**.

---

## 1. Review Goal

Review the current implementation with a narrow mandate:

```text
Find what can cause unsafe behavior.
Find what is already too complex.
Ignore everything else.
```

Do not review for style, naming preferences, formatting, micro-optimizations, or speculative future features unless they directly increase risk or complexity.

---

## 2. Current State to Review

The current implementation reportedly includes:

```text
- SkillResult as the canonical execution result
- system_info.get_datetime structured output
- filesystem.create_file with workspace boundary checks
- VariableResolver
- Linear WorkflowExecutor
- Approval flow
- Minimal audit trail
- Cerbero V1 analyzer
- End-to-end dogfood scenario coverage
```

Known validation state:

```text
- Workflow tests passing
- Cerbero tests passing
- Full test suite passing
```

The review must verify the implementation, not trust the summary.

---

## 3. Review Priority

Use this priority order:

```text
P0 — Can this execute something unsafe?
P1 — Can this silently do the wrong thing?
P2 — Can this become hard to maintain within the next 2–3 iterations?
P3 — Everything else
```

Only report P0, P1, and significant P2 issues.

---

# PART A — Risk Review

## 4. Risk Area 1 — Workflow Execution Safety

Review:

```text
src/YAi.Persona/Services/Workflows/Services/WorkflowExecutor.cs
```

Check that the executor does exactly this order:

```text
1. Validate skill exists
2. Validate action exists
3. Resolve input
4. Validate input if schema exists
5. Determine risk and RequiresApproval from SkillAction metadata
6. Request approval BEFORE execution when required
7. Execute tool through ToolRegistry
8. Receive SkillResult directly
9. Validate output if schema exists
10. Store result in state bag
11. Stop on failure
12. Write audit records
```

Flag as P0 if:

```text
- approval can happen after execution
- writes can execute without approval
- tool/action names are hardcoded into approval logic
- workflow continues after a failed required step
- unresolved variables are allowed through
- tool execution bypasses ToolRegistry
```

Flag as P1 if:

```text
- error states are ambiguous
- failure result does not contain useful error code/message
- approval denial is treated as success
- cancellation is indistinguishable from failure
```

---

## 5. Risk Area 2 — Approval Boundary

Review:

```text
IApprovalService
ApprovalContext
WorkflowApprovalService
RazorConsoleApprovalCardPresenter
Approval UI adapter
```

The core workflow layer may depend on:

```text
IApprovalService
ApprovalContext
ApprovalDecision
```

The core workflow layer must NOT depend on:

```text
Razor
CLI components
Filesystem-specific UI models
FilesystemPlannerService
FilesystemOperation*
```

Flag as P0 if:

```text
- core workflow references concrete UI components
- approval is based on skill/action names instead of metadata
- Deny still executes the step
- Cancel still executes later steps
```

Flag as P1 if:

```text
- approval context omits resolved input
- approval context omits risk level
- approval context omits target path when available
- approval decisions are not audited
```

---

## 6. Risk Area 3 — Variable Resolver

Review:

```text
WorkflowVariableResolver.cs
WorkflowRunState.cs
```

Supported syntax must be only:

```text
${steps.<stepId>.variables.<name>}
${steps.<stepId>.data.<field>}
```

Check:

```text
- structured JSON traversal is used
- objects are handled
- arrays are handled
- nested values are handled
- mixed strings are handled
- invalid syntax fails
- missing step fails
- missing variable fails
- missing data field fails
```

Flag as P0 if:

```text
- unresolved placeholders survive into execution
- resolver evaluates expressions
- resolver reads environment variables
- resolver executes code
- resolver silently replaces missing values with empty string
```

Flag as P1 if:

```text
- error messages do not identify the missing step/field/variable
- arrays or nested objects are partially skipped without failure
- mixed strings behave differently from full-placeholder strings
```

---

## 7. Risk Area 4 — Filesystem Boundary

Review:

```text
FilesystemTool.cs
WorkspaceBoundaryService.cs
filesystem.create_file path
```

Check:

```text
- all file creation paths are normalized
- outside-workspace paths are rejected
- absolute paths outside workspace are rejected
- ../ traversal is rejected
- overwrite defaults to false
- file write requires approval
```

Flag as P0 if:

```text
- create_file can write outside workspace
- overwrite happens by default
- approval is optional for write
- boundary validation is a naive prefix check only
```

Flag as P1 if:

```text
- symlink/junction behavior is undefined
- relative path handling differs between Windows and Linux
- error codes are unstable or missing
```

---

## 8. Risk Area 5 — Cerbero

Review:

```text
src/YAi.Persona/Services/Operations/Safety/Cerbero/
CerberoCommandSafetyTests.cs
```

Current Cerbero scope is **analysis only**.

Check:

```text
- Cerbero does not execute commands
- Cerbero is not wired into shell execution yet
- blocked commands produce structured findings
- tests cover PowerShell and Bash cases
```

Flag as P0 if:

```text
- Cerbero executes or invokes shell
- Cerbero is bypassable in an existing shell execution path
- dangerous commands are classified as safe
```

Flag as P1 if:

```text
- finding does not include rule id/reason
- result shape is too weak for future approval/audit
- normalization is inconsistent across shells
```

Minimum dangerous command coverage:

```text
PowerShell:
- iwr/irm/curl/wget | iex
- Invoke-WebRequest | Invoke-Expression
- Remove-Item -Recurse -Force C:\
- Start-Process ... -Verb RunAs

Bash:
- curl/wget URL | bash/sh/zsh
- rm -rf /
- rm -rf ~
- dd if=/dev/zero of=/dev/sd*
- mkfs.*
```

---

## 9. Risk Area 6 — Audit Integrity

Review:

```text
WorkflowAuditService.cs
WorkflowStepAuditRecord.cs
WorkflowExecutor.cs audit calls
```

Audit must include:

```text
- workflow definition
- resolved inputs
- approval decisions
- step results
- artifacts
- errors
- summary
```

Expected files:

```text
workflow.json
resolved-inputs.json
approvals.json
step-results.json
errors.json
summary.json
```

Flag as P0 if:

```text
- write execution is not auditable
- approval decisions are not persisted
- resolved input is missing
- failed steps leave no audit trail
```

Flag as P1 if:

```text
- audit contains secrets
- audit file names are inconsistent
- errors are only in prose and not structured
- audit writes can silently fail
```

---

# PART B — Complexity Review

## 10. Complexity Area 1 — Too Many Models

Review workflow models:

```text
WorkflowDefinition
WorkflowStepDefinition
WorkflowExecutionResult
WorkflowRunState
WorkflowApprovalStep
WorkflowStepAuditRecord
ApprovalContext
```

Question:

```text
Is every model used by actual runtime code or tests?
```

Flag as P2 if:

```text
- a model only wraps another model without adding value
- a model exists only for speculative future use
- approval and audit models duplicate the same fields
```

Recommended MVP model set:

```text
WorkflowDefinition
WorkflowStepDefinition
WorkflowExecutionResult
WorkflowRunState
ApprovalContext
```

Audit-specific record models are acceptable only if they simplify file output and tests.

---

## 11. Complexity Area 2 — Service Proliferation

Review services:

```text
WorkflowExecutor
WorkflowVariableResolver
WorkflowApprovalService
WorkflowAuditService
MinimalSkillSchemaValidator
```

Flag as P2 if:

```text
- a service has only one trivial method and no clear seam value
- a service exists only because of naming symmetry
- workflow logic is scattered across too many services
```

Acceptable:

```text
WorkflowExecutor
WorkflowVariableResolver
IApprovalService
```

Questionable:

```text
WorkflowApprovalService
WorkflowAuditService
```

These are acceptable only if they reduce coupling and remain small.

---

## 12. Complexity Area 3 — Core/UI Coupling

Review dependencies.

The workflow core should not reference:

```text
YAi.Client.CLI
YAi.Client.CLI.Components
Razor
Console-specific UI
Filesystem approval screens
```

Flag as P2/P1 depending on severity if:

```text
- workflow core imports UI namespaces
- approval context is shaped around the current UI instead of runtime needs
- tests require Razor components to validate workflow execution
```

---

## 13. Complexity Area 4 — Duplicate Concepts

Look for duplicate or overlapping concepts:

```text
ToolRiskLevel vs OperationRiskLevel
ApprovalDecision used by old filesystem planner and new workflow
AuditService vs WorkflowAuditService
FilesystemPlannerService vs WorkflowExecutor
OperationStep vs WorkflowStepDefinition
```

Flag as P2 if duplication is harmless but confusing.

Flag as P1 if duplication can cause different behavior in different paths.

Review question:

```text
Can the same operation behave differently depending on old filesystem planner vs new workflow executor?
```

---

## 14. Complexity Area 5 — Tests That Hide Complexity

Review tests:

```text
WorkflowExecutorTests
WorkflowVariableResolverTests
CerberoCommandSafetyTests
SkillSchemaValidatorTests
FilesystemToolCreateFileTests
```

Flag as P2 if:

```text
- tests require excessive setup
- tests seed too much fake skill metadata
- tests duplicate production parsing logic
- tests assert prose error messages instead of stable error codes
```

Good tests should assert:

```text
- stable error codes
- success/failure
- file created/not created
- approval requested/not requested
- audit file exists
```

Avoid tests that assert fragile wording.

---

# PART C — Required Review Output

## 15. Required Report Format

Return only this structure:

```markdown
# Risk + Complexity Review

## Executive Summary

- Overall risk: Low / Medium / High
- Overall complexity: Low / Medium / High
- Recommendation: Proceed / Stabilize before proceeding / Refactor before proceeding

## P0 — Must Fix Before Continuing

1. ...
2. ...

## P1 — Should Fix Before Dogfood

1. ...
2. ...

## P2 — Complexity Reduction Opportunities

1. ...
2. ...

## Keep

Things that are correctly designed and should not be changed.

## Do Not Change Yet

Contracts that should remain frozen for the next iteration.

## Suggested Next Action

One clear next step only.
```

Do not provide a generic code review.

Do not list every nit.

Do not suggest new features.

---

## 16. Severity Rules

### P0

Must fix immediately.

Examples:

```text
unsafe execution
write without approval
outside-workspace write
shell execution bypass
workflow continues after failure
```

### P1

Fix before real dogfood.

Examples:

```text
ambiguous error states
incomplete audit
weak approval context
unstable result semantics
```

### P2

Clean up soon if it slows development.

Examples:

```text
unnecessary models
duplicated concepts
core/UI coupling
too much setup in tests
```

---

# PART D — Explicit Non-Goals

Do not review:

```text
formatting
minor naming preferences
large refactors unrelated to risk/complexity
future plugin architecture
future DAG workflows
future shell execution
performance micro-optimizations
```

Do not recommend:

```text
Cerbero integration into executor
DAG workflow support
background workers
multi-agent runtime
plugin marketplace
sandbox/container execution
```

Those are future concerns.

---

# PART E — Final Review Checklist

Before submitting the review, answer these questions:

```text
1. Can a workflow write without approval?
2. Can a workflow write outside workspace?
3. Can an unresolved variable reach tool execution?
4. Can approval be bypassed by naming a step differently?
5. Can shell commands execute anywhere today?
6. Can audit reconstruct what happened?
7. Is workflow core independent from UI?
8. Are there redundant models/services already?
9. Are tests deterministic and LLM-free?
10. Is this simple enough to dogfood?
```

If the answer to any of 1–6 is unsafe, mark P0/P1 accordingly.

If 7–10 are weak, mark P2 unless they block dogfood.

---

## 17. Expected Result

The review should help decide one thing:

```text
Can we dogfood this now, or must we reduce risk/complexity first?
```

Do not broaden the scope.
