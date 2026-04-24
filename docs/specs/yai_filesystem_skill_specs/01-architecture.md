# 01 — Architecture

## Objective

Build a safe filesystem skill where an AI agent can propose concrete file operations, but the user approves each operation before execution.

The model must not execute arbitrary shell commands. It should produce a structured plan. The application should validate and execute typed operations through a controlled service.

## Main components

```text
User
  ↓
Chat Orchestrator
  ↓
Skill Router
  ↓
Context Manager
  ↓
Frontier Model via OpenRouter.ai
  ↓
CommandPlan Validator
  ↓
Approval Card Renderer
  ↓
Typed Filesystem Executor
  ↓
Verifier
  ↓
Audit Trail
```

## Component responsibilities

### 1. Chat Orchestrator

Receives the user request and keeps conversation state.

Example request:

```text
In this folder, create one folder per project: ProjectA, ProjectB, ProjectC.
```

The orchestrator should not immediately ask the model to generate shell commands. It should identify that the filesystem skill is relevant.

### 2. Skill Router

Selects the correct skill.

For this first version:

```yaml
selected_skill: filesystem
```

Later, this can route to Git, npm, dotnet, Azure CLI, etc.

### 3. Context Manager

Collects facts before planning.

For filesystem operations, useful context includes:

```yaml
context:
  os: windows
  shell_preference: powershell
  workspace_root: D:\Workspace\ClientWork
  current_folder: D:\Workspace\ClientWork
  existing_items:
    - ProjectA
    - README.md
  permissions:
    can_read: true
    can_write: true
```

The model should receive this context pack instead of guessing.

### 4. Frontier Model

The model receives:

- the user request
- the active skill instructions
- the context pack
- the required output schema
- the safety rules

The model returns a structured `CommandPlan`.

It may propose display commands such as:

```powershell
New-Item -ItemType Directory -Path "ProjectB"
```

But execution should use typed operations, not raw shell strings.

### 5. CommandPlan Validator

The validator checks the model output before anything reaches the user as executable.

It verifies:

- all target paths are inside the workspace root
- operation types are supported
- destructive operations have mitigation
- overwrites have backup steps
- delete means recoverable trash, not permanent delete
- every write operation requires approval
- every step has verification criteria
- no arbitrary shell code exists in executable fields

### 6. Approval Card Renderer

Each step becomes a card.

Each card contains:

- title
- action
- target path
- display command
- risk level
- expected effect
- mitigation
- rollback
- verification
- buttons: Run, Edit, Skip, Cancel Plan

The user approves one step at a time.

### 7. Typed Filesystem Executor

Executes only typed operations, for example:

```yaml
operation:
  type: create_directory
  path: D:\Workspace\ClientWork\ProjectB
```

The executor should not run arbitrary generated shell.

### 8. Verifier

After each step, the system verifies the expected result.

Example:

```yaml
verify:
  - type: path_exists
    path: D:\Workspace\ClientWork\ProjectB
```

If verification fails, execution stops and the model receives the updated context.

### 9. Audit Trail

Every plan and executed step should be persisted.

Suggested folder:

```text
.yai/
  audit/
    filesystem/
      20260425-003000/
        context.yaml
        plan.yaml
        approvals.yaml
        executed-steps.yaml
        verification.yaml
        errors.yaml
```

The audit trail allows the user to ask:

- What did we execute?
- What failed?
- Can we rollback?
- Which files were backed up?
