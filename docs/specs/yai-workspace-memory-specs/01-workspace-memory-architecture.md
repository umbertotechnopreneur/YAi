# 01 — Workspace Memory Architecture

## Purpose

Implement the first YAi! memory system by adapting the existing PoC memory architecture into an OpenClaw-compatible local workspace model.

The previous `storage/` concept must be replaced by a user-owned `workspace/` directory located under the user's home folder.

This change is not only a rename. It changes the ownership model:

- `workspace/` is human-owned durable state.
- `data/` is runtime-generated state.
- application defaults can still be bundled internally, but user-editable memory and configuration must live in `workspace/`.

## Target root layout

Preferred default location:

```text
~/.yai/
  workspace/
    memory/
    prompts/
    regex/
    skills/
    config/

  data/
    history/
    sessions/
    dreams/
    logs/
    indexes/
    cache/
```

On Windows this resolves to something like:

```text
C:\Users\<User>\.yai\workspace
C:\Users\<User>\.yai\data
```

Do not store user memory under the build output folder. Do not rely on `AppContext.BaseDirectory` for durable user memory.

## Workspace responsibilities

The workspace contains files the user can read, edit, back up, sync, or version-control.

Recommended structure:

```text
workspace/
  memory/
    SOUL.md
    USER.md
    AGENTS.md
    MEMORIES.md
    LESSONS.md
    LIMITS.md
    daily/
      2026-04-25.md

  prompts/
    system-prompts.common.md
    system-prompts.it.md
    system-prompts.en.md
    categories/
      chat.md
      explain.md
      translate.md
      developer.md
      tools.md
      safety.md

  regex/
    system-regex.common.md
    system-regex.it.md
    system-regex.en.md
    categories/
      memory.it.md
      corrections.it.md
      preferences.it.md
      reminders.it.md
      errors.it.md
      projects.it.md
      people.it.md

  skills/
    imported-or-local-skills/

  config/
    workspace.json
```

## Data responsibilities

The `data/` directory contains generated or runtime-owned files.

Recommended structure:

```text
data/
  history/
  sessions/
  dreams/
    DREAMS.md
    pending-memory-candidates.jsonl
  logs/
  indexes/
    memory-fts.sqlite
  cache/
```

Generated files can be deleted and rebuilt where possible. User-authored knowledge must not depend on `data/`.

## Memory tiers

### HOT memory

Always included in the prompt.

Examples:

```text
SOUL.md
USER.md
AGENTS.md
LIMITS.md
MEMORIES.md
LESSONS.md
```

Use this only for short, durable, high-value context.

### WARM memory

Loaded only when relevant.

Examples:

```text
project-specific notes
language-specific instructions
tool-specific lessons
recent daily memory
```

The existing `WarmMemoryResolver` should be adapted rather than replaced.

### DAILY memory

A lightweight rolling work log.

Examples:

```text
workspace/memory/daily/2026-04-25.md
workspace/memory/daily/2026-04-26.md
```

Load today's and yesterday's daily memory by default. Older daily files should be searchable but not always injected.

### DREAMS / pending proposals

A staging area for memory suggestions that should not be immediately promoted.

Examples:

```text
data/dreams/DREAMS.md
data/dreams/pending-memory-candidates.jsonl
```

Use this for:

- low-confidence extracted memory
- conflicts
- possible preferences
- possible rules
- repeated errors
- suggested lessons

## Core rule

The memory system must be transparent.

No hidden automatic memory mutation should happen without either:

1. a clearly explicit user command, or
2. a visible review/promotion flow.
