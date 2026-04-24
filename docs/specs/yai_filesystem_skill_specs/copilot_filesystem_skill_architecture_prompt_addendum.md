# Prompt for GitHub Copilot Agent — Filesystem Skill Architecture Corrections

Use this prompt in GitHub Copilot Agent mode before continuing the filesystem skill implementation.

---

## Objective

We are implementing the first operational skill for YAi: a safe, reviewable, filesystem skill.

The core product concept is:

```text
ContextPack → CommandPlan → OperationStep → Approval Card → Typed Execution → Verification → Audit Trail
```

The AI model can propose a plan, but it must not directly execute commands. The application must validate the plan, render one approval card per step, and execute only approved typed operations through C# services.

The first implementation must stay small and stable. Do not overbuild.

---

## Critical Architecture Corrections

Before continuing, adjust the current implementation according to the corrections below.

---

## 1. Do not keep all shared models under Filesystem

Some models are generic and will be reused later by Git, dotnet, npm, Azure CLI, and other skills.

Move generic operation-planning models into a shared operations area, for example:

```text
src/YAi.Persona/Services/Operations/Models/
```

Generic models include:

```text
CommandPlan
OperationStep
IOperationStep
ApprovalDecision
StepStatus
VerificationCriterion
VerificationResult
MitigationStep
RollbackOperation
CommandPlanValidationResult
DisplayCommand
OperationRiskLevel
```

Only filesystem-specific models should remain under:

```text
src/YAi.Persona/Services/Tools/Filesystem/Models/
```

Filesystem-specific models include:

```text
FilesystemOperation
FilesystemOperationType
FilesystemContextPack
FilesystemOperationResult
FilesystemPathInfo
```

The reason is simple: `CommandPlan`, approval decisions, risk levels, mitigation, rollback, and verification are not filesystem concepts. They are YAi operational concepts.

---

## 2. Prefer `OperationStep`, not `StepCard`, in core models

A card is a UI concept. The domain model should not be named after the UI.

Use this conceptual naming:

```text
Core model: OperationStep
UI rendering: ApprovalCard
```

The core layer should produce and validate operation steps.

The CLI/RazorConsole layer should render each operation step as an approval card.

Avoid this coupling:

```text
YAi.Persona core model = StepCard
```

Prefer:

```text
YAi.Persona core model = OperationStep
YAi.Client.CLI.Components UI = ApprovalCard
```

---

## 3. Separate display command from executable operation

Each operation step may include a human-readable command preview, but it must never be the execution source of truth.

Correct concept:

```yaml
display_command:
  shell: powershell
  text: New-Item -ItemType Directory -Path ".\ProjectA"

typed_operation:
  type: create_directory
  path: D:\Workspace\ProjectA
```

The user sees `display_command`.

The executor uses only `typed_operation`.

The filesystem executor must never execute `display_command`.

Do not implement any generic shell execution for filesystem operations.

For filesystem operations, use C# APIs only:

```text
System.IO.Directory
System.IO.File
System.IO.Path
```

The display command is informational, useful for transparency, copy/paste, and user trust, but it is not executable authority.

---

## 4. Add a reusable WorkspaceBoundaryService

Create a reusable service responsible for path safety.

Suggested location:

```text
src/YAi.Persona/Services/Operations/Safety/WorkspaceBoundaryService.cs
```

Responsibilities:

```text
Normalize paths
Resolve relative paths against the workspace root
Convert paths to absolute canonical paths
Prevent path traversal
Reject paths outside the workspace root
Reject dangerous/system paths where appropriate
Provide reusable inside-workspace validation
```

Every filesystem operation must pass through this service before validation and before execution.

The validator should not duplicate this logic in several places.

The rule is:

```text
No operation may execute unless all target paths are normalized, absolute, and inside the approved workspace root.
```

For v1, do not support operations outside the workspace root.

---

## 5. Do not implement permanent delete in v1

For the first version, permanent deletion must not exist.

Do not implement:

```text
File.Delete
Directory.Delete
Remove-Item
rm
del
permanent_delete_file
permanent_delete_directory
```

If the user asks to delete a file or folder, the future behavior should be mapped to recoverable trash:

```text
trash_file
trash_directory
```

Conceptually:

```text
move target → .yai/trash/<timestamp>/<original-name>
```

But do not implement trash in the first milestone unless the basic foundation already builds cleanly.

For v1 milestone 1, delete should be unsupported and should return a safe message such as:

```text
Delete is not implemented yet. Future delete operations will use recoverable YAi trash, not permanent deletion.
```

---

## 6. Make audit generic, not filesystem-specific

Audit is not a filesystem-only concept.

Create a generic operation audit service, for example:

```text
src/YAi.Persona/Services/Operations/Audit/OperationAuditService.cs
```

The audit folder can still be domain-specific:

```text
.yai/audit/filesystem/
.yai/audit/git/
.yai/audit/dotnet/
```

For filesystem v1, write minimal audit records under:

```text
<workspace-root>/.yai/audit/filesystem/<timestamp-or-plan-id>/
```

Minimum audit artifacts:

```text
context.yaml or context.json
plan.yaml or plan.json
approvals.yaml or approvals.json
execution-log.yaml or execution-log.json
verification.yaml or verification.json
```

If YAML support is not already available, JSON is acceptable for implementation. The conceptual schema can remain YAML in docs.

---

## 7. Keep the first milestone intentionally small

Do not implement the full filesystem skill immediately.

Milestone 1 must include only:

```text
create_directory
list_directory
path_exists verification
path_is_directory verification
minimal approval card
minimal audit trail
workspace boundary validation
```

Do not implement yet:

```text
create_file
copy_file
copy_directory
move_file
move_directory
rename_file
rename_directory
backup_file
backup_directory
trash_file
trash_directory
overwrite handling
permanent delete
shell execution
```

The objective of milestone 1 is to prove the full loop:

```text
User request
→ context pack
→ model-generated or internally-created CommandPlan
→ validation
→ approval card
→ typed filesystem execution
→ verification
→ audit
```

Once this loop works, we can safely add backup, trash, file writing, and more complex operations.

---

## 8. Tool-level risk vs step-level risk

Do not treat the whole filesystem tool as permanently destructive.

The tool can be marked as requiring approval or risky-write capable, but actual risk must be evaluated per operation step.

Conceptual distinction:

```text
Tool-level capability:
Filesystem can perform write operations.

Step-level risk:
ReadOnly
LocalWrite
OverwriteRisk
DestructiveRecoverable
DestructivePermanent
OutsideWorkspace
```

For example:

```text
list_directory = ReadOnly
create_directory = LocalWrite
overwrite file = OverwriteRisk
trash folder = DestructiveRecoverable
permanent delete = DestructivePermanent, blocked in v1
outside workspace = OutsideWorkspace, blocked in v1
```

The approval UI should be driven by the step-level risk, not only by the tool-level attribute.

---

## 9. Shared RazorConsole components should be generic

The RazorConsole UI layer should not be filesystem-specific.

Create shared components reusable by future tools:

```text
Components/Dialogs/ConfirmationDialog.razor
Components/Dialogs/MessageBox.razor
Components/Dialogs/CancelPlanDialog.razor
Components/Cards/ApprovalCard.razor
Components/Progress/OperationProgressScreen.razor
```

Filesystem-specific Razor components should be thin wrappers only.

Example:

```text
Screens/Tools/Filesystem/ApprovalCardScreen.razor
```

This screen should feed a filesystem `OperationStep` into the generic `ApprovalCard.razor`.

Rule:

```text
Any tool that needs confirmation, cancellation, warning, progress, or approval must use the shared components.
Do not create one-off confirmation/messagebox UI per tool.
```

---

## 10. WebAPI / MCP future compatibility

Keep the core logic UI-agnostic.

The seam should remain something like:

```text
IApprovalPresenter
```

or:

```text
IOperationApprovalPresenter
```

It should accept an `OperationStep` and return an `ApprovalDecision`.

The CLI implementation uses RazorConsole.

A future WebAPI implementation can return the same operation step as JSON to a frontend and wait for approval.

A future MCP implementation can map the same approval model to MCP confirmation flows.

Core services must not reference RazorConsole.

Correct dependency direction:

```text
YAi.Persona
  contains operation models, filesystem services, validators, executor, audit, approval abstractions

YAi.Client.CLI.Components
  contains reusable RazorConsole UI components

YAi.Client.CLI
  wires CLI presenter implementation into YAi.Persona abstractions
```

---

## 11. Recommended folder layout

Use this as the target architecture.

```text
src/
  YAi.Persona/
    Services/
      Operations/
        Models/
          CommandPlan.cs
          OperationStep.cs
          IOperationStep.cs
          ApprovalDecision.cs
          StepStatus.cs
          DisplayCommand.cs
          OperationRiskLevel.cs
          VerificationCriterion.cs
          VerificationResult.cs
          MitigationStep.cs
          RollbackOperation.cs
          CommandPlanValidationResult.cs

        Safety/
          WorkspaceBoundaryService.cs

        Audit/
          OperationAuditService.cs

        Approval/
          IOperationApprovalPresenter.cs

      Tools/
        Filesystem/
          FilesystemTool.cs

          Models/
            FilesystemOperation.cs
            FilesystemOperationType.cs
            FilesystemContextPack.cs
            FilesystemOperationResult.cs
            FilesystemPathInfo.cs

          Services/
            FilesystemContextManager.cs
            FilesystemCommandPlanValidator.cs
            FileSystemExecutor.cs
            FilesystemVerificationService.cs
            FilesystemPlannerService.cs

  YAi.Client.CLI.Components/
    Components/
      Dialogs/
        ConfirmationDialog.razor
        MessageBox.razor
        CancelPlanDialog.razor

      Cards/
        ApprovalCard.razor

      Progress/
        OperationProgressScreen.razor

    Screens/
      RazorScreen.cs
      Tools/
        Filesystem/
          ApprovalCardScreen.razor
          CommandPlanOverviewScreen.razor

  YAi.Client.CLI/
    Services/
      RazorConsoleOperationApprovalPresenter.cs
```

Adjust names if they conflict with existing code, but preserve the separation.

---

## 12. First user scenario to support

Implement this scenario first:

```text
User: In this folder, create one folder per project: Alpha, Beta, Gamma.
```

Expected system behavior:

1. Determine workspace root and current folder.
2. List current folder.
3. Detect which folders already exist.
4. Create a CommandPlan with one operation step per missing folder.
5. Render one approval card per create-directory step.
6. User approves one by one.
7. Executor creates each directory using C# filesystem APIs.
8. Verifier checks each folder exists and is a directory.
9. Audit trail records plan, approvals, execution, and verification.

If a folder already exists:

```text
Do not overwrite.
Do not fail the whole plan.
Represent it as a read-only/no-op or skipped step.
```

---

## 13. First supported typed operation

For milestone 1, implement only:

```yaml
typed_operation:
  type: create_directory
  path: D:\Workspace\ClientWork\Alpha
  overwrite: false
```

Verification:

```yaml
verify:
  - type: path_exists
    path: D:\Workspace\ClientWork\Alpha
  - type: path_is_directory
    path: D:\Workspace\ClientWork\Alpha
```

Risk:

```yaml
risk_level: local-write
requires_approval: true
```

Mitigation:

```yaml
mitigation:
  required: false
  reason: This operation creates a new directory and does not overwrite existing content.
```

Rollback may be described but not executed automatically in milestone 1.

---

## 14. Validation rules for milestone 1

A plan is invalid if:

```text
workspace root is missing
current folder is missing
operation type is not create_directory or list_directory
target path is outside workspace root
target path already exists and overwrite is requested
write operation does not require approval
verification criteria are missing
typed operation is missing
display command is treated as executable authority
```

---

## 15. Stop rules

Stop the plan if:

```text
the user cancels the plan
the user denies a required approval
workspace boundary validation fails
execution fails
verification fails
actual filesystem state differs from expected preconditions
```

After stopping, do not continue blindly. Return a clear result and preserve audit data.

---

## 16. OpenRouter / frontier model role

The frontier model is the planner, not the executor.

The model may:

```text
interpret the user request
propose a CommandPlan
explain risks
generate user-facing summaries
suggest mitigation
suggest recovery plans later
```

The model must not:

```text
execute shell commands
bypass workspace validation
decide final safety alone
approve its own steps
ignore failed verification
operate outside the approved workspace
```

The C# application is the enforcement authority.

---

## 17. Implementation priority

Proceed in this order:

1. Reorganize generic vs filesystem-specific models.
2. Add operation-level risk model.
3. Add workspace boundary service.
4. Add minimal filesystem context manager.
5. Add minimal command plan validator.
6. Add create-directory typed executor.
7. Add path verification.
8. Add minimal audit service.
9. Add generic approval presenter abstraction.
10. Add minimal RazorConsole approval card.
11. Wire filesystem tool into registry.
12. Test the Alpha/Beta/Gamma folder creation scenario.
13. Run `dotnet build`.
14. Only after build is green, consider the next operation type.

---

## Non-negotiable constraints

```text
No raw shell execution for filesystem operations.
No permanent delete in v1.
No operation outside workspace root in v1.
No overwrite without backup.
No destructive operation without mitigation.
No continuation after failed verification.
No filesystem-specific approval UI duplicated when generic components can be used.
```

---

## Expected final result for this phase

At the end of this phase, the system should support a safe, reviewable folder creation workflow where the user approves each folder creation through a card.

This is enough to prove the architecture.

Do not implement advanced filesystem operations until this foundation is clean, builds successfully, and is easy to extend.
