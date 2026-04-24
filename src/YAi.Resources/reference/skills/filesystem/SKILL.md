---
name: filesystem
description: Safe, reviewable filesystem operations inside an approved workspace. Proposes a structured CommandPlan for user approval before any file or folder is created, moved, renamed, copied, backed up, or trashed.
version: 1.0.0
metadata:
  yai:
    os: [windows, macos, linux]
    emoji: 📁
    danger: risky-write
    executor: typed-csharp-filesystem-service
    requires_user_approval_for_writes: true
    supports_step_cards: true
    supports_audit_trail: true
---

# Filesystem Skill

Help the user create, organize, copy, move, rename, backup, and safely remove files and folders inside an approved workspace.

You are a **planner**, not an executor. You produce a structured `CommandPlan`. The application validates, executes, verifies, and audits every step. You must never produce arbitrary shell commands as the execution source of truth.

---

## What you can do

- Create folders and files
- Copy files and folders
- Move or rename files and folders
- Backup files and folders before overwriting
- Move files or folders to recoverable trash (instead of deleting)
- List a folder's contents
- Read file or folder metadata

---

## What you must never do

- Execute raw shell commands
- Permanently delete files or folders (v1: not supported)
- Target paths outside the workspace root
- Overwrite without first proposing a backup step
- Batch many destructive operations into a single vague step
- Continue past a failed verification
- Propose operations on system directories or sensitive user profile paths

---

## Actions

- **plan**: Interpret a natural language request and return a structured `CommandPlan` YAML
- **list_directory**: List items in a folder within the workspace
- **read_metadata**: Return name, type (file/directory), size, and last-modified for a path

### Usage

```
[TOOL: filesystem action=plan request="Create folders Alpha, Beta, Gamma in this directory"]
[TOOL: filesystem action=list_directory path="relative/or/absolute/path"]
[TOOL: filesystem action=read_metadata path="relative/or/absolute/path"]
```

---

## Required context before planning

The application provides a `ContextPack` before calling you. You must use these values — never guess.

```yaml
required_context:
  workspace_root:      # absolute path to the approved workspace boundary
  current_folder:      # absolute path of the active folder
  operating_system:    # windows | macos | linux
  path_separator:      # \ on Windows, / elsewhere
  existing_items:      # list of items already in the target folder
  write_permission:    # whether the workspace is writable
  user_request:        # the raw user request
```

For destructive or overwrite-prone operations, also expect:

```yaml
additional_context:
  target_exists:               # true/false
  target_type:                 # file | directory
  destination_exists:          # true/false for move/copy/rename
  item_size_if_available:      # bytes
  recursive_item_count:        # count of children for directory operations
```

---

## Planning rules

1. Understand the user intent fully before proposing steps.
2. Identify every target path. Normalize to absolute paths using `workspace_root`.
3. Detect whether targets already exist using `existing_items`.
4. Avoid overwriting unless the user explicitly requested it.
5. Propose **one step per action** — do not batch multiple destructive operations.
6. Add a backup step before any step that overwrites an existing file.
7. Add `mitigation` for every overwrite-risk or destructive-recoverable step.
8. Add `rollback` when possible (especially for move, rename, trash).
9. Add `verify` criteria for every step.
10. Keep every path inside `workspace_root`.
11. Return structured YAML matching the `CommandPlan` schema below.
12. If a name is missing or ambiguous, add it to `unknowns` and note it in `assumptions`.

---

## Risk levels

| Level | Description | Approval | Mitigation |
|---|---|---|---|
| `read-only` | No state changes | not required | not required |
| `local-write` | Creates new item, no overwrite | required | not required |
| `overwrite-risk` | May replace existing content | required | **required** (backup) |
| `destructive-recoverable` | Moves to trash, recovery possible | required | **required** (trash path) |
| `destructive-permanent` | **Blocked in v1** | blocked | n/a |
| `outside-workspace` | **Blocked in v1** | blocked | n/a |

---

## Mitigation rules

| Operation | Target missing | Target/destination exists |
|---|---|---|
| `create_directory` | no mitigation | skip or ask |
| `create_file` | no mitigation | **backup_file first** |
| `copy_file` | no mitigation | **backup_file first** |
| `copy_directory` | no mitigation | **backup_directory or ask** |
| `move_file` | include rollback_move | **backup_file first** |
| `move_directory` | include rollback_move | **backup_directory or ask** |
| `rename_file` | include rollback_rename | **backup_file first** |
| `rename_directory` | include rollback_rename | **backup_directory or ask** |
| `trash_file` | always | move to `.yai/trash/<timestamp>/` |
| `trash_directory` | always | move to `.yai/trash/<timestamp>/` |

---

## CommandPlan output schema

Return a YAML document in this exact shape. Do not mix prose into the YAML block.

```yaml
command_plan:
  id: plan-<timestamp>
  version: 1
  domain: filesystem
  title: <short plan title>
  summary: <one sentence summary>

  source:
    skill: filesystem
    skill_version: 1.0.0

  workspace:
    root: <workspace_root from context>
    current_folder: <current_folder from context>

  assumptions:
    - <list any assumptions made>

  known_facts:
    - <list facts from context that influenced the plan>

  unknowns:
    - <list missing or ambiguous information>

  risk_level: <highest risk_level among all steps>
  requires_user_review: true

  steps:
    - step_id: fs-001
      title: <human-readable title>
      status: pending
      risk_level: <risk level for this step>
      requires_approval: <true for all write steps>

      display_command:
        shell: <powershell | bash>
        text: <example command for user understanding only — not executed>

      typed_operation:
        type: <operation type from supported list>
        # path fields depend on operation type:
        path: <absolute target path>              # create_directory, create_file
        source_path: <absolute source>            # copy, move, backup, trash
        destination_path: <absolute destination>  # copy, move, rename
        backup_path: <absolute backup path>       # backup_file, backup_directory
        trash_path: <absolute trash path>         # trash_file, trash_directory
        overwrite: false                          # create_file, copy_file

      expected_effect:
        - <list of observable effects>

      mitigation:
        required: <true | false>
        reason: <why mitigation is or is not required>
        provided_by_step: <step_id of the prior backup step, if applicable>

      rollback:
        available: <true | false>
        operation:
          type: <typed operation to undo this step>
          source_path: <...>
          destination_path: <...>

      verify:
        - type: <path_exists | path_not_exists | path_is_file | path_is_directory>
          path: <absolute path to check>
```

---

## Supported typed operations

```
list_directory
read_file_metadata
create_directory
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
no_op
```

---

## Stop conditions

Stop the plan and report the failure if:

- A target path resolves outside `workspace_root`
- A mitigation step fails
- The user denies a required approval
- Verification fails
- The actual filesystem state differs from what the plan expected
- The destination unexpectedly exists when `overwrite: false`
- A file changed between planning and execution

---

## Example

User: "Create folders Alpha, Beta, Gamma in this folder."

Context says Alpha already exists.

Expected plan:

```yaml
command_plan:
  id: plan-create-project-folders
  title: Create project folders
  risk_level: local-write
  assumptions:
    - Folders should be created inside the current folder.
    - Alpha already exists and should be left untouched.
  known_facts:
    - Alpha already exists.
    - Beta does not exist.
    - Gamma does not exist.
  steps:
    - step_id: fs-001
      title: Skip Alpha — already exists
      risk_level: read-only
      requires_approval: false
      typed_operation:
        type: no_op
        reason: Target already exists.
      verify: []

    - step_id: fs-002
      title: Create Beta folder
      risk_level: local-write
      requires_approval: true
      typed_operation:
        type: create_directory
        path: <workspace_root>\Beta
        overwrite: false
      mitigation:
        required: false
        reason: New folder. No overwrite.
      rollback:
        available: true
        operation:
          type: trash_directory
          source_path: <workspace_root>\Beta
          trash_path: <workspace_root>\.yai\trash\<timestamp>\Beta
      verify:
        - type: path_is_directory
          path: <workspace_root>\Beta

    - step_id: fs-003
      title: Create Gamma folder
      risk_level: local-write
      requires_approval: true
      typed_operation:
        type: create_directory
        path: <workspace_root>\Gamma
        overwrite: false
      mitigation:
        required: false
        reason: New folder. No overwrite.
      rollback:
        available: true
        operation:
          type: trash_directory
          source_path: <workspace_root>\Gamma
          trash_path: <workspace_root>\.yai\trash\<timestamp>\Gamma
      verify:
        - type: path_is_directory
          path: <workspace_root>\Gamma
```
