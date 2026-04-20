---
type: rule
scope: global
priority: hot
last_updated: 2025-01-01
tags: [limits, constraints, safety]
---
## Limits

Use this file to record constraints, known limitations, and practical boundaries.

### Data Safety

- Never expose API keys, secrets, tokens, passwords, or connection strings in output, logs, screenshots, or committed files.
- Never hardcode secrets in source code, configuration files, scripts, or examples.
- Never leak internal paths, credentials, private endpoints, or personal data outside the workspace context.
- Never persist prompts, chat transcripts, command output, or generated summaries that may contain sensitive data without redaction.
- Never overwrite user data when a non-destructive alternative exists.

### Destructive Actions

- Never delete a file or folder without explicit confirmation from the user.
- If a delete operation is requested, ask twice to confirm that the user is sure before proceeding.
- Never use destructive commands such as force delete, hard reset, or irreversible cleanup without explicit approval.
- Never assume temporary files are safe to remove; verify ownership and purpose first.
- Prefer additive changes, backups, or staged edits over destructive edits.

### Scope and Editing

- Never modify unrelated files while working on a specific task.
- Never revert changes that were not made as part of the current task unless the user explicitly asks for it.
- Never rewrite a whole file when a targeted patch is sufficient.
- If requirements are ambiguous and the result could be destructive, ask for clarification instead of guessing.

### External and System Changes

- Never send workspace content to external services or APIs unless the task requires it and the user has clearly initiated that action.
- Never make global machine changes such as installing tools, changing PATH, or updating global config without clear justification.
- Prefer dry-run, preview, or validation modes before applying commands with broad side effects.
- If a command can incur external cost, rate limits, or remote side effects, call that out before running it.
- If an action affects files outside the workspace or changes system-wide state, confirm before proceeding.

### Security and Confirmation

- Never make security-sensitive changes without calling out the impact clearly.
- If an action can cause data loss, service interruption, or credential exposure, stop and confirm before continuing.

### App-Specific Storage Limits

- The app may create or modify files inside this `storage` folder when the current task requires it.
- Never wipe, replace, or remove existing entries from `storage/limits.md` in a single step.
- If the user asks to remove or clear any existing entry from `storage/limits.md`, ask twice for confirmation before proceeding.
- Never delete or rewrite `storage/history`, `storage/memories`, `storage/lessons`, session records, or cached knowledge without explicit confirmation.
- Never merge, summarize, or prune stored history or memories in a way that loses original meaning unless the user asked for it.
- Never store personal profile details in memory files unless the user intentionally provided them for persistence or handoff.
- Never copy sensitive details from chat into long-term storage by default; store only what is necessary for the task.
- Never keep raw logs that contain secrets, tokens, personal data, or full prompts when a redacted version is sufficient.
- Prefer separating durable knowledge, temporary session state, and disposable cache so cleanup decisions stay reversible.
- If retention, summarization, or sharing rules for stored data are unclear, stop and confirm before persisting anything.
