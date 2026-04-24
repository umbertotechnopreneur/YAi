# 03 — Filesystem Skill Specification

This document describes the first YAi operational skill: `filesystem`.

The skill allows the agent to help the user create, organize, copy, move, rename, backup, and safely remove files and folders inside an approved workspace.

---

## Skill metadata

```yaml
name: filesystem
description: Safe, reviewable filesystem operations inside an approved workspace.
version: 1.0.0

metadata:
  yai:
    os: [windows, macos, linux]
    executor: typed-csharp-filesystem-service
    requires_user_approval_for_writes: true
    supports_step_cards: true
    supports_audit_trail: true
```

---

## Purpose

The skill should transform natural language filesystem requests into a structured `CommandPlan`.

The skill must not assume it can run arbitrary shell commands.

The skill should propose actions as typed operations and include a display command only for user understanding.

---

## Supported user requests

Examples:

```text
Create one folder per project in this directory.
Create folders for Alpha, Beta, Gamma.
Create a README file in each project folder.
Rename folder OldName to NewName.
Move these documents into an Archive folder.
Copy this configuration file before editing it.
Delete this folder safely.
Clean this workspace by moving old files into a trash folder.
```

---

## Required context

Before planning write operations, the skill needs:

```yaml
required_context:
  - workspace_root
  - current_folder
  - operating_system
  - path_separator
  - existing_items_in_target_folder
  - write_permission
  - user_request
```

For destructive or overwrite-prone operations, also collect:

```yaml
additional_context:
  - target_exists
  - target_type
  - destination_exists
  - item_size_if_available
  - recursive_item_count_if_available
```

---

## Context probes

The application should collect context through safe internal APIs, not through shell commands.

Conceptual probes:

```yaml
context_probes:
  - get_current_folder
  - get_workspace_root
  - list_directory
  - check_path_exists
  - check_path_type
  - check_write_permission
  - resolve_absolute_path
  - normalize_path
  - check_path_inside_workspace
```

---

## Planning rules

The model should:

1. Understand the user intent.
2. Identify target paths.
3. Detect missing or ambiguous names.
4. Avoid overwriting unless explicitly requested.
5. Propose one step per action.
6. Add mitigation for risky operations.
7. Add verification for every step.
8. Add rollback when possible.
9. Keep operations inside the workspace.
10. Return structured YAML matching the `CommandPlan` schema.

---

## Approval rules

The skill must mark write operations as approval-required.

```yaml
approval_rules:
  read_only:
    requires_approval: false

  local_write:
    requires_approval: true

  overwrite_risk:
    requires_approval: true
    requires_mitigation: true

  destructive_recoverable:
    requires_approval: true
    requires_mitigation: true

  destructive_permanent:
    allowed: false
```

---

## Mitigation rules

```yaml
mitigation_rules:
  create_directory:
    mitigation_required: false
    reason: Does not overwrite if overwrite=false.

  create_file:
    mitigation_required_if_target_exists: true
    mitigation: backup existing file before overwrite.

  copy_file:
    mitigation_required_if_destination_exists: true
    mitigation: backup destination file before overwrite.

  move_file:
    mitigation_required_if_destination_exists: true
    mitigation: backup destination before move.

  rename_file:
    mitigation_required_if_destination_exists: true
    mitigation: backup destination before rename.

  trash_file:
    mitigation_required: true
    mitigation: move to .yai/trash instead of permanent delete.

  trash_directory:
    mitigation_required: true
    mitigation: move to .yai/trash instead of permanent delete.
```

---

## Forbidden behavior

The skill must not:

```yaml
forbidden:
  - execute raw shell commands
  - permanently delete files in v1
  - operate outside workspace root
  - overwrite without backup
  - hide destructive impact from the user
  - batch many destructive operations into one vague step
  - continue after failed verification
```

---

## Output requirement

The skill should output only a structured `CommandPlan` when planning is requested.

It should not output prose mixed with the YAML plan unless the caller explicitly requests a human explanation.
