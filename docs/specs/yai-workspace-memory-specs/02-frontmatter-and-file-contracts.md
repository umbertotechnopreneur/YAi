# 02 — Frontmatter and File Contracts

## What frontmatter is

Frontmatter YAML is a metadata block placed at the top of a Markdown file.

It is delimited by `---` lines.

Example:

```yaml
---
type: config
scope: global
priority: hot
language: it
last_updated: 2026-04-25
tags: [regex, system, extraction]
---
```

The Markdown body follows after the frontmatter.

## Why YAi! should use frontmatter

Frontmatter allows the application to understand a file without relying only on file names.

It tells the app:

- what the file is
- whether it is global or project-scoped
- whether it is hot or warm
- what language it targets
- what category it belongs to
- when it was last updated
- what tags apply

## Required common fields

All workspace Markdown files should support this metadata shape:

```yaml
---
type: memory | prompt | regex | config | skill | daily | dreams
scope: global | user | project | session | tool | language
priority: hot | warm | cold
language: common | en | it | fr | vi
category: base | chat | explain | translate | developer | tools | safety | memory | corrections | preferences | reminders | errors | projects | people
last_updated: YYYY-MM-DD
tags: [tag1, tag2]
---
```

Not every field must be required for every file, but the parser should tolerate and preserve all fields.

## Memory file contract

Example:

```yaml
---
type: memory
scope: user
priority: hot
language: common
category: preferences
last_updated: 2026-04-25
tags: [user, preferences, shell]
---
# User Memories

## Preferences

- User prefers PowerShell syntax for Windows commands.
```

## Prompt file contract

Example:

```yaml
---
type: prompt
scope: global
priority: hot
language: en
category: chat
last_updated: 2026-04-25
tags: [prompt, chat]
---
# Chat Prompt

## base

You are YAi!, a local-first terminal AI assistant.

## chat

Maintain continuity across the current conversation.
```

## Regex file contract

Example:

```yaml
---
type: regex
scope: global
priority: hot
language: it
category: memory
last_updated: 2026-04-25
tags: [regex, memory, italian]
---
# Italian Memory Regex

## remember_command

```regex
^(?:ricorda|memorizza|salva|annota)\s+(?:che\s+)?(?<content>.+)$
```
```

## Parser requirements

The parser must:

- read frontmatter if present
- tolerate files without frontmatter during migration
- preserve unknown metadata fields
- preserve body content exactly where possible
- fail gracefully on invalid YAML
- never erase a file because metadata parsing failed

## Migration rule

Existing PoC files without perfect metadata should still load.

Do not require a complete migration before the app can start.

Use fallback assumptions:

```text
SOUL.md              -> type: memory, priority: hot, scope: global
USER.md              -> type: memory, priority: hot, scope: user
AGENTS.md            -> type: memory, priority: hot, scope: global
MEMORIES.md          -> type: memory, priority: hot, scope: user
LESSONS.md           -> type: memory, priority: hot, scope: global
LIMITS.md            -> type: memory, priority: hot, scope: global
SYSTEM-PROMPTS.md    -> type: prompt, priority: hot, scope: global
SYSTEM-REGEX.md      -> type: regex, priority: hot, scope: global
```
