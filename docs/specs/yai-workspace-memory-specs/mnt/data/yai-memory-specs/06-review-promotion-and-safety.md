# 06 — Review, Promotion, and Safety

## Purpose

Memory must be safe, visible, and reversible.

YAi! should not silently mutate important user memory unless the user made an explicit memory command.

## Candidate states

Recommended lifecycle:

```text
extracted
pending_review
approved
rejected
promoted
archived
conflict
```

## Review UX

Every non-trivial memory candidate should be reviewable as a card:

```text
Candidate memory
Source: regex / ai
Language: it
Target: MEMORIES.md / Preferences
Confidence: 1.0

Content:
Prefer PowerShell commands on Windows.

Actions:
[Approve] [Edit] [Reject] [Promote to Rule] [Archive]
```

## Direct-write rules

Allowed direct writes:

- explicit user command: `remember that ...`
- explicit user command: `memorizza ...`
- explicit reminder command to reminder system

Still log the write.

## Review-required rules

Always require review for:

- inferred memory
- AI-generated summaries
- corrections that override existing memory
- changes to `LIMITS.md`
- changes to `SOUL.md`
- destructive tool lessons
- security-relevant instructions
- credentials or secrets

## Sensitive data rule

Never encourage storage of secrets.

If a regex captures likely secrets, credentials, tokens, passwords, or API keys, route to blocked/review state and warn the user.

Examples:

```text
password
api key
token
secret
private key
connection string
```

## Backups before writes

Before modifying an existing workspace file:

```text
1. create a timestamped backup
2. write to a temp file
3. validate temp file
4. atomically replace target where possible
5. log the mutation
```

Example backup path:

```text
workspace/.backups/2026-04-25/MEMORIES.20260425-103000.md
```

## File transaction model

Reuse or adapt the existing PoC `FileTransactionManager` if present.

All memory writes should go through a central writer. Do not scatter `File.WriteAllText` across screens and services.

## Promotion rules

Promotion means moving a candidate from `data/dreams/` or pending review into durable workspace memory.

Promotion targets:

```text
MEMORIES.md
LESSONS.md
LIMITS.md
SOUL.md
USER.md
project-specific files
```

`LIMITS.md`, `SOUL.md`, and `USER.md` should require stricter review than `MEMORIES.md`.

## Audit log

Every memory mutation should produce a log entry:

```json
{
  "timestamp": "2026-04-25T10:30:00Z",
  "operation": "promote_memory",
  "targetFile": "workspace/memory/MEMORIES.md",
  "source": "regex",
  "patternName": "remember_command",
  "backupFile": "workspace/.backups/...",
  "status": "success"
}
```

## No hidden destructive behavior

The memory system must not:

- delete memory automatically
- rewrite entire memory files unnecessarily
- silently resolve conflicts
- silently promote AI-inferred rules
- store secrets without warning
- depend on build output paths
