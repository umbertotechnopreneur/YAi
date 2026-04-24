# 04 — System Regex Repository

## Purpose

Implement deterministic multilingual memory extraction using workspace-managed regex files.

Regex extraction should run before AI extraction.

The existing PoC already has `SYSTEM-REGEX.md`. Reuse that design and evolve it into language-specific and category-specific files.

## Why regex first

Regex extraction is:

- cheap
- fast
- deterministic
- auditable
- testable
- safer than model interpretation for explicit user commands

Use it for clear statements such as:

- remember this
- from now on
- correction
- project is called
- phone number is
- remind me at
- this failed
- non funziona
- d'ora in poi

## Recommended structure

```text
workspace/regex/
  system-regex.common.md
  system-regex.it.md
  system-regex.en.md

  categories/
    memory.common.md
    corrections.common.md
    preferences.common.md
    reminders.common.md
    errors.common.md
    projects.common.md
    people.common.md

    memory.it.md
    corrections.it.md
    preferences.it.md
    reminders.it.md
    errors.it.md
    projects.it.md
    people.it.md

    memory.en.md
    corrections.en.md
    preferences.en.md
    reminders.en.md
    errors.en.md
    projects.en.md
    people.en.md
```

## Loading order

For an Italian user session:

```text
1. system-regex.common.md
2. categories/*.common.md
3. system-regex.it.md
4. categories/*.it.md
5. optional fallback: system-regex.en.md
```

The fallback language should be configurable.

## Pattern contract

Each pattern must live under an H2 section and contain one fenced `regex` block.

Example:

```markdown
## remember_command

Matches explicit Italian memory commands.

```regex
^(?:ricorda|memorizza|salva|annota)\s+(?:che\s+)?(?<content>.+)$
```
```

## Required event output shape

Every regex match should produce a normalized extraction event:

```json
{
  "eventType": "memory_candidate",
  "source": "regex",
  "language": "it",
  "patternName": "remember_command",
  "confidence": 1.0,
  "content": "User prefers PowerShell",
  "metadata": {
    "group_content": "User prefers PowerShell"
  }
}
```

Recommended event types:

```text
memory_candidate
preference_candidate
correction_candidate
lesson_candidate
error_candidate
reminder_candidate
project_candidate
person_candidate
limit_candidate
```

## Regex engine requirement

Use the .NET NonBacktracking regex engine where possible.

Recommended options:

```text
RegexOptions.NonBacktracking
RegexOptions.IgnoreCase
RegexOptions.CultureInvariant
RegexOptions.Compiled
```

The existing PoC already documents this safety direction. Keep it.

## Unsupported features

Reject or fail clearly when a user-provided regex uses features incompatible with NonBacktracking:

```text
backreferences
lookaheads
lookbehinds
atomic groups
conditional expressions
```

Do not silently ignore broken patterns. Report the file, section, and pattern name.

## Multilingual naming strategy

Preferred strategy:

Same section names across languages.

Example:

```text
system-regex.it.md     -> ## remember_command
system-regex.en.md     -> ## remember_command
```

The pipeline should treat them as variants of the same extraction intent.

Avoid names like:

```text
remember_command_italian
remember_command_english
```

Language belongs in metadata/file path, not in the pattern name.

## Category split

As the regex library grows, split by category.

Do not allow a single monolithic regex file to become unmaintainable.

Recommended category files:

```text
memory.it.md
preferences.it.md
corrections.it.md
reminders.it.md
errors.it.md
projects.it.md
people.it.md
```

## Initial Italian patterns

The first Italian implementation should cover:

```text
ricorda che ...
memorizza ...
salva ...
annota ...
d'ora in poi ...
da adesso ...
preferisci ...
usa sempre ...
non è corretto ...
sbagliato ...
usa invece ...
non funziona ...
errore con ...
ricordami alle ...
imposta un promemoria alle ...
```

## Initial English patterns

The first English implementation should cover:

```text
remember that ...
memorize ...
store ...
from now on ...
going forward ...
always use ...
default to ...
no, that's wrong ...
actually ...
correction: ...
that failed ...
this doesn't work ...
remind me at ...
set reminder for ...
```

## AI fallback

If no regex matches, the AI extractor may inspect the message for possible durable memory.

However:

- explicit regex matches should win
- AI extraction should have lower default confidence
- AI-suggested memory should often go to review/dreams first
- AI must not silently promote rules or limits
