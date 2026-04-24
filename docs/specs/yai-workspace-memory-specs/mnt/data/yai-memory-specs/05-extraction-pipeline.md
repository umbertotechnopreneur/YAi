# 05 — Extraction Pipeline

## Purpose

Define how YAi! extracts memory from conversations.

The design must favor deterministic extraction first and model-based extraction second.

## Pipeline overview

```text
User message / conversation segment
  ↓
Normalize language and session metadata
  ↓
Run common regex patterns
  ↓
Run active-language regex patterns
  ↓
Run fallback-language regex patterns
  ↓
Classify deterministic matches into extraction events
  ↓
If needed, run AI extractor
  ↓
Merge, deduplicate, and conflict-check candidates
  ↓
Write explicit commands directly or stage candidates for review
  ↓
Promote approved candidates into memory files
```

## Language detection

Use a simple initial model:

```text
1. session language if set
2. user workspace default language
3. command-line option if provided
4. lightweight detection from text
5. fallback language
```

Do not depend on AI for language detection in V1.

## Extraction sources

Each candidate must record its source:

```text
regex
ai
manual
import
promotion
```

## Candidate schema

Recommended internal model:

```json
{
  "id": "generated-id",
  "eventType": "preference_candidate",
  "source": "regex",
  "language": "it",
  "patternName": "preference_correction_command",
  "content": "Prefer PowerShell commands on Windows.",
  "targetFile": "MEMORIES.md",
  "targetSection": "Preferences",
  "confidence": 1.0,
  "createdAt": "2026-04-25T10:30:00Z",
  "origin": {
    "sessionId": "...",
    "messageId": "..."
  },
  "metadata": {}
}
```

## Deterministic extraction

Use regex for explicit commands and declarations.

Examples:

```text
remember that X
ricorda che X
from now on X
d'ora in poi X
correction: X
sbagliato, X
this failed: X
non funziona: X
```

Explicit user commands should be trusted more than inferred AI extraction.

## AI extraction

Use AI extraction for:

- implicit durable preferences
- repeated technical lessons
- summarizing long conversations into candidate lessons
- detecting possible conflicts
- suggesting consolidation

Do not use AI extraction to silently create hard rules.

## Merge and deduplication

Before writing candidates:

- normalize whitespace
- normalize quotes
- compare lowercased content
- compare target section
- compare semantically similar simple duplicates where possible
- avoid writing repeated preferences

## Conflict detection

Detect conflicts such as:

```text
User prefers Bash.
User prefers PowerShell.
```

Conflicts should go to dreams/review, not direct memory.

## Target routing

Recommended routing:

```text
memory_candidate       -> MEMORIES.md
preference_candidate   -> MEMORIES.md / Preferences
correction_candidate   -> LESSONS.md or DREAMS.md
lesson_candidate       -> LESSONS.md
error_candidate        -> LESSONS.md / Errors
reminder_candidate     -> reminders system, not memory
project_candidate      -> MEMORIES.md / Projects or project memory file
person_candidate       -> MEMORIES.md / People
limit_candidate        -> LIMITS.md only after explicit approval
```

## Explicit vs inferred memory

Explicit memory command:

```text
remember that I prefer PowerShell
```

Can be written directly after optional visible confirmation.

Inferred memory:

```text
The user used PowerShell several times.
```

Must be staged for review.

## Reusing PoC services

The agent should inspect and reuse:

```text
KnowledgeExtractionPipeline
MetadataExtractor
RegexRegistry
MemoryFlushService
DreamingService
PromotionService
MemoryFileParser
WarmMemoryResolver
PromptBuilder
```

Prefer targeted changes over broad rewrites.
