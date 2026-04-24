# 07 — Example Plans

## Example 1 — Create one folder per project

### User request

```text
In this folder, create one folder per project: Alpha, Beta, Gamma.
```

### Context

```yaml
context_pack:
  workspace:
    root: D:\Workspace\ClientWork
    current_folder: D:\Workspace\ClientWork
  filesystem_snapshot:
    existing_items:
      - name: Alpha
        type: directory
```

### Plan

```yaml
command_plan:
  id: plan-create-project-folders
  domain: filesystem
  title: Create project folders
  risk_level: local-write
  assumptions:
    - Folders should be created inside the current folder.
    - Existing folder Alpha should be left untouched.
  known_facts:
    - Alpha already exists.
    - Beta does not exist.
    - Gamma does not exist.
  steps:
    - step_id: fs-001
      title: Skip Alpha because it already exists
      risk_level: read-only
      requires_approval: false
      typed_operation:
        type: no_op
        reason: Target already exists.

    - step_id: fs-002
      title: Create Beta folder
      risk_level: local-write
      requires_approval: true
      display_command:
        shell: powershell
        text: New-Item -ItemType Directory -Path ".\Beta"
      typed_operation:
        type: create_directory
        path: D:\Workspace\ClientWork\Beta
        overwrite: false
      mitigation:
        required: false
        reason: New folder creation does not overwrite existing data.
      verify:
        - type: path_is_directory
          path: D:\Workspace\ClientWork\Beta

    - step_id: fs-003
      title: Create Gamma folder
      risk_level: local-write
      requires_approval: true
      display_command:
        shell: powershell
        text: New-Item -ItemType Directory -Path ".\Gamma"
      typed_operation:
        type: create_directory
        path: D:\Workspace\ClientWork\Gamma
        overwrite: false
      mitigation:
        required: false
        reason: New folder creation does not overwrite existing data.
      verify:
        - type: path_is_directory
          path: D:\Workspace\ClientWork\Gamma
```

---

## Example 2 — Create a file that already exists

### User request

```text
Create a README.md file in this folder with a short project description.
```

### Context

```yaml
context_pack:
  workspace:
    root: D:\Workspace\App
    current_folder: D:\Workspace\App
  filesystem_snapshot:
    existing_items:
      - name: README.md
        type: file
```

### Plan

```yaml
command_plan:
  id: plan-create-readme
  domain: filesystem
  title: Create README.md
  risk_level: overwrite-risk
  known_facts:
    - README.md already exists.
  steps:
    - step_id: fs-001
      title: Backup existing README.md
      risk_level: local-write
      requires_approval: true
      display_command:
        shell: powershell
        text: Copy-Item ".\README.md" ".\.yai\backups\filesystem\20260425-003000\README.md"
      typed_operation:
        type: backup_file
        source_path: D:\Workspace\App\README.md
        backup_path: D:\Workspace\App\.yai\backups\filesystem\20260425-003000\README.md
      verify:
        - type: path_exists
          path: D:\Workspace\App\.yai\backups\filesystem\20260425-003000\README.md

    - step_id: fs-002
      title: Replace README.md
      risk_level: overwrite-risk
      requires_approval: true
      display_command:
        shell: powershell
        text: Set-Content -Path ".\README.md" -Value "<content>"
      typed_operation:
        type: create_file
        path: D:\Workspace\App\README.md
        overwrite: true
        content_source: generated_content
      mitigation:
        required: true
        provided_by_step: fs-001
      rollback:
        available: true
        operation:
          type: copy_file
          source_path: D:\Workspace\App\.yai\backups\filesystem\20260425-003000\README.md
          destination_path: D:\Workspace\App\README.md
          overwrite: true
      verify:
        - type: path_exists
          path: D:\Workspace\App\README.md
```

---

## Example 3 — Safe delete

### User request

```text
Delete the OldProject folder.
```

### Plan

```yaml
command_plan:
  id: plan-safe-delete-oldproject
  domain: filesystem
  title: Move OldProject to YAi trash
  risk_level: destructive-recoverable
  steps:
    - step_id: fs-001
      title: Move OldProject to recoverable trash
      risk_level: destructive-recoverable
      requires_approval: true
      display_command:
        shell: powershell
        text: Move-Item ".\OldProject" ".\.yai\trash\20260425-003000\OldProject"
      typed_operation:
        type: trash_directory
        source_path: D:\Workspace\App\OldProject
        trash_path: D:\Workspace\App\.yai\trash\20260425-003000\OldProject
      mitigation:
        required: true
        reason: Folder is moved to recoverable trash instead of being permanently deleted.
      rollback:
        available: true
        operation:
          type: move_directory
          source_path: D:\Workspace\App\.yai\trash\20260425-003000\OldProject
          destination_path: D:\Workspace\App\OldProject
      verify:
        - type: path_not_exists
          path: D:\Workspace\App\OldProject
        - type: path_is_directory
          path: D:\Workspace\App\.yai\trash\20260425-003000\OldProject
```
