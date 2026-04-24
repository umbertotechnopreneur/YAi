# 04 — Risk and Mitigation Model

## Principle

Every operation should have a risk classification. Risk classification must be validated by the application, not trusted blindly from the model.

The model can propose risk. The app enforces risk.

---

## Risk levels

### read-only

No filesystem state changes.

Examples:

```yaml
operations:
  - list_directory
  - read_file_metadata
```

Approval:

```yaml
requires_approval: false
```

---

### local-write

Creates a new item without overwriting existing content.

Examples:

```yaml
operations:
  - create_directory
  - create_file when target does not exist
```

Approval:

```yaml
requires_approval: true
```

Mitigation:

```yaml
required: false
```

---

### overwrite-risk

May replace existing content.

Examples:

```yaml
operations:
  - create_file when target exists
  - copy_file when destination exists
  - move_file when destination exists
  - rename_file when destination exists
```

Approval:

```yaml
requires_approval: true
requires_mitigation: true
```

Required mitigation:

```yaml
mitigation:
  - backup existing destination before overwrite
```

---

### destructive-recoverable

Removes an item from its original location but keeps a recovery path.

Examples:

```yaml
operations:
  - trash_file
  - trash_directory
```

Approval:

```yaml
requires_approval: true
requires_mitigation: true
```

Required mitigation:

```yaml
mitigation:
  - move item to .yai/trash
  - record original path
  - record rollback operation
```

---

### destructive-permanent

Permanently deletes data or makes recovery difficult.

Examples:

```yaml
operations:
  - permanent_delete_file
  - permanent_delete_directory
  - recursive_delete_without_backup
```

Default behavior:

```yaml
allowed: false
```

For v1, do not implement permanent delete.

---

### outside-workspace

Targets a path outside the approved workspace root.

Default behavior:

```yaml
allowed: false
```

The app may later support explicit user approval for outside-workspace operations, but this should not be part of v1.

---

## Mitigation catalog

### backup_file

Used before overwriting or editing a file.

```yaml
mitigation:
  type: backup_file
  source_path: D:\Workspace\App\appsettings.json
  backup_path: D:\Workspace\App\.yai\backups\filesystem\20260425-003000\appsettings.json
```

### backup_directory

Used before high-impact folder changes.

```yaml
mitigation:
  type: backup_directory
  source_path: D:\Workspace\App\Config
  backup_path: D:\Workspace\App\.yai\backups\filesystem\20260425-003000\Config
```

### trash_file

Used instead of delete.

```yaml
mitigation:
  type: trash_file
  source_path: D:\Workspace\App\old.txt
  trash_path: D:\Workspace\App\.yai\trash\20260425-003000\old.txt
```

### trash_directory

Used instead of delete.

```yaml
mitigation:
  type: trash_directory
  source_path: D:\Workspace\App\OldProject
  trash_path: D:\Workspace\App\.yai\trash\20260425-003000\OldProject
```

---

## Required mitigation by operation

```yaml
operation_mitigation_matrix:
  create_directory:
    if_target_missing: none
    if_target_exists: skip_or_ask

  create_file:
    if_target_missing: none
    if_target_exists: backup_file

  copy_file:
    if_destination_missing: none
    if_destination_exists: backup_file

  copy_directory:
    if_destination_missing: none
    if_destination_exists: backup_directory_or_ask

  move_file:
    if_destination_missing: rollback_move
    if_destination_exists: backup_file

  move_directory:
    if_destination_missing: rollback_move
    if_destination_exists: backup_directory_or_ask

  rename_file:
    if_destination_missing: rollback_rename
    if_destination_exists: backup_file

  rename_directory:
    if_destination_missing: rollback_rename
    if_destination_exists: backup_directory_or_ask

  trash_file:
    always: trash_instead_of_delete

  trash_directory:
    always: trash_instead_of_delete
```

---

## Stop conditions

The plan must stop if:

```yaml
stop_conditions:
  - target path resolves outside workspace
  - mitigation step fails
  - user denies a required approval
  - verification fails
  - actual state differs from expected preconditions
  - destination unexpectedly exists
  - file changed between planning and execution
```
