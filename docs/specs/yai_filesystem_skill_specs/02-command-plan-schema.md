# 02 — CommandPlan YAML Schema

This is a conceptual schema, not final code.

The important design principle is to separate:

```text
display_command → what the user sees
typed_operation → what the app executes
```

The model may suggest the display command, but the app should execute only the typed operation after validation.

---

## ContextPack

The `ContextPack` is prepared by the application before asking the model to create a plan.

```yaml
context_pack:
  id: ctx-20260425-003000
  generated_at: 2026-04-25T00:30:00+07:00

  environment:
    os: windows
    os_version: Windows 11
    shell_preference: powershell
    runtime: dotnet

  workspace:
    root: D:\Workspace\ClientWork
    current_folder: D:\Workspace\ClientWork
    allow_outside_workspace: false

  filesystem_snapshot:
    current_folder_exists: true
    current_folder_writable: true
    existing_items:
      - name: ProjectA
        type: directory
      - name: README.md
        type: file

  user_request:
    raw: In this folder, create one folder per project: ProjectA, ProjectB, ProjectC.
    interpreted_intent: Create one directory for each named project.
```

---

## CommandPlan

```yaml
command_plan:
  id: plan-20260425-003010
  version: 1
  domain: filesystem
  title: Create project folders
  summary: Create missing project folders inside the current workspace.

  source:
    model_provider: openrouter.ai
    model_class: frontier
    skill: filesystem
    skill_version: 1.0.0

  workspace:
    root: D:\Workspace\ClientWork
    current_folder: D:\Workspace\ClientWork

  assumptions:
    - The user wants the folders created inside the current folder.
    - Existing folders should not be overwritten.
    - No operation should run outside the workspace root.

  known_facts:
    - ProjectA already exists.
    - ProjectB does not exist.
    - ProjectC does not exist.

  unknowns:
    - Whether existing ProjectA should be reused, renamed, or left untouched.

  risk_level: local-write
  requires_user_review: true

  steps:
    - step_id: fs-001
      title: Create ProjectB folder
      status: pending
      risk_level: local-write
      requires_approval: true

      display_command:
        shell: powershell
        text: New-Item -ItemType Directory -Path ".\ProjectB"

      typed_operation:
        type: create_directory
        path: D:\Workspace\ClientWork\ProjectB
        overwrite: false

      expected_effect:
        - A new empty folder named ProjectB will exist.

      mitigation:
        required: false
        reason: This operation creates a new folder and does not overwrite existing data.

      rollback:
        available: true
        operation:
          type: trash_directory
          path: D:\Workspace\ClientWork\ProjectB
          trash_path: D:\Workspace\ClientWork\.yai\trash\20260425-003010\ProjectB

      verify:
        - type: path_exists
          path: D:\Workspace\ClientWork\ProjectB
        - type: path_is_directory
          path: D:\Workspace\ClientWork\ProjectB
```

---

## Step status values

```yaml
status:
  - pending
  - approved
  - running
  - succeeded
  - failed
  - skipped
  - cancelled
```

---

## Risk levels

```yaml
risk_level:
  - read-only
  - local-write
  - overwrite-risk
  - destructive-recoverable
  - destructive-permanent
  - outside-workspace
```

For v1, block:

```yaml
blocked_by_default:
  - destructive-permanent
  - outside-workspace
```

---

## Supported typed operations for v1

```yaml
supported_operations:
  - list_directory
  - read_file_metadata
  - create_directory
  - create_file
  - copy_file
  - copy_directory
  - move_file
  - move_directory
  - rename_file
  - rename_directory
  - backup_file
  - backup_directory
  - trash_file
  - trash_directory
```

---

## Forbidden executable model output

The model must not output directly executable arbitrary shell operations as the execution source of truth.

Forbidden:

```yaml
typed_operation:
  type: shell
  command: Remove-Item -Recurse -Force C:\
```

The model may include display previews, but the app must execute only validated typed operations.
