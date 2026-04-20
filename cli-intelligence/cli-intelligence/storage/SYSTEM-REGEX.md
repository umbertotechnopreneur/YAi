---
type: config
scope: global
priority: hot
last_updated: 2025-01-01
tags: [regex, system, extraction]
---
# System Regex

Deterministic extraction patterns for remembering facts without AI calls.
This file is the single source of truth — edit it to change extraction behavior without recompiling.

## How it works

1. **At boot**, this file is copied from `storage/` → `data/regex/system-regex.md`.
2. **At boot**, `RegexRegistry` parses each section below and pre-compiles patterns using the **NonBacktracking engine** for ReDoS protection.
3. The first fenced ` ```regex ``` ` block in each section is used as the pattern.
4. **If a section is missing, empty, or contains an invalid regex**, the app falls back to a hardcoded default compiled into the binary.
5. **If a pattern uses unsupported features** (backreferences, lookaheads, lookbehinds), the app will **fail at boot** with a clear error message identifying the offending section.
6. All patterns are evaluated with `.NET RegexOptions.NonBacktracking | IgnoreCase | CultureInvariant | Compiled`.

### ReDoS Protection

This application uses the **.NET NonBacktracking regex engine** to prevent Regular Expression Denial of Service (ReDoS) attacks. The NonBacktracking engine guarantees **O(n) linear-time execution** regardless of input complexity, eliminating catastrophic backtracking vulnerabilities.

**Supported Pattern Features:**
- Character classes: `\d`, `\w`, `\s`, `[A-Z]`, `[^0-9]`
- Quantifiers: `*`, `+`, `?`, `{n}`, `{n,}`, `{n,m}`
- Alternation: `(this|that)`
- Anchors: `^`, `$`, `\b`
- Capturing groups: `(?<name>...)`
- Non-capturing groups: `(?:...)`

**Unsupported Pattern Features (will cause boot failure):**
- ❌ Backreferences: `\1`, `\2`, `\k<name>`
- ❌ Lookaheads: `(?=...)`, `(?!...)`
- ❌ Lookbehinds: `(?<=...)`, `(?<!...)`
- ❌ Atomic groups: `(?>...)`
- ❌ Conditional expressions: `(?(condition)yes|no)`

**Example of Rejected Pattern:**
```regex
# ❌ WILL FAIL AT BOOT - uses backreference
\b(\w+)\s+\1\b
```

**Corrected Pattern:**
```regex
# ✅ SAFE - uses simple character classes and quantifiers
\b\w+\s+\w+\b
```

### Rules for editing

- **One pattern per section.** Only the first ` ```regex ``` ` block is read; others are ignored.
- **Keep named groups stable.** Code references groups by name (e.g. `<content>`, `<number>`).
  Renaming or removing a required group silently disables that extraction.
- **Test before saving.** Paste your pattern into a .NET-compatible regex tester
  (e.g. regex101.com with the ".NET / C#" flavor) to verify.
- **Avoid unsupported features.** The NonBacktracking engine will reject patterns with backreferences, lookarounds, or atomic groups.
- **Comments inside the fenced block are not stripped** — do not put `# ...` lines inside
  the regex block; they become part of the pattern.

### Section → code wiring

All sections below are automatically loaded by `RegexRegistry` at boot and bound dynamically to `MetadataExtractor`. Named capture groups are extracted into the `ExtractionItem.Metadata` dictionary.

| Section name          | Required named groups | Target Type   | Target Section    |
|-----------------------|-----------------------|---------------|-------------------|
| `remember_command`    | `content`             | memory        | auto-classified   |
| `phone_statement`     | `number`              | memory        | People            |
| `project_statement`   | `name`                | memory        | Projects          |
| `classifier_project`  | *(none — match only)* | *(internal)*  | Projects          |
| `classifier_people`   | *(none — match only)* | *(internal)*  | People            |
| `remind_command`      | `at_time`, `message`  | reminder      | Reminders         |

Adding a new section here **no longer requires code changes**. Simply add a new `## section_name` heading below with a regex block, and the dynamic metadata binding will automatically extract named capture groups.

---

## remember_command

Matches explicit "save this fact" intents from the user.
Captured group `content` becomes the stored memory text.

**Example inputs that match:**
- `remember that John's birthday is March 5`  →  content = `John's birthday is March 5`
- `please store my wifi password is hunter2`   →  content = `my wifi password is hunter2`
- `memorize the deploy command is make prod`   →  content = `the deploy command is make prod`

```regex
^(?:please\s+)?(?:remember|memorize|store)\s+(?:that\s+)?(?<content>.+)$
```

## phone_statement

Matches direct phone-number statements.
Captured group `number` is normalized (whitespace collapsed) before storage.

**Example inputs that match:**
- `my phone number is +1 555 123 4567`  →  number = `+1 555 123 4567`
- `the phone is (06) 1234-5678`         →  number = `(06) 1234-5678`

```regex
\b(?:my|the)\s+phone\s+(?:number\s+)?is\s+(?<number>\+?[0-9][0-9\s()\-]{5,})
```

## project_statement

Matches project-naming statements.
Captured group `name` is trimmed and stored under the Projects category.

**Example inputs that match:**
- `new project called Solar-Tracker`   →  name = `Solar-Tracker`
- `project named cli-intelligence`     →  name = `cli-intelligence`
- `project is HomeAssistant.Plugin`    →  name = `HomeAssistant.Plugin`

```regex
\b(?:new\s+)?project\s+(?:called|named|is)\s+(?<name>[A-Za-z0-9._\- ]{2,80})
```

## classifier_project

Classifier (no capture groups). If the `remember_command` content matches this pattern,
the memory is filed under **Projects** instead of the default **Preferences** category.

```regex
\bproject\b
```

## classifier_people

Classifier (no capture groups). If the `remember_command` content matches this pattern,
the memory is filed under **People** instead of the default **Preferences** category.

```regex
\b(phone|person|people|contact|name)\b
```

## remind_command

Matches explicit reminder-setting intents from the user.
Captured group `at_time` is the raw time expression; `message` is what to remind.

The pipeline resolves `at_time` to an absolute `DateTime` using C# `DateTime.TryParseExact` against
common 12-hour and 24-hour formats. If the time-of-day has already passed today, the reminder is
scheduled for tomorrow.

**Example inputs that match:**
- `remind me at 3pm to call John`            →  at_time = `3pm`,  message = `call John`
- `remind me at 15:30 to submit the report`  →  at_time = `15:30`, message = `submit the report`
- `set a reminder at 9am for the standup`    →  at_time = `9am`,  message = `the standup`
- `set reminder for 14:00 to check email`    →  at_time = `14:00`, message = `check email`

```regex
\b(?:remind\s+me|set\s+(?:a\s+)?reminder)\s+(?:at|for)\s+(?<at_time>[0-9]{1,2}(?::[0-9]{2})?\s*(?:am|pm)?)\b[\s,]*(?:to\s+|for\s+)?(?<message>.+)$
```

## correction_command

Matches explicit user corrections and error acknowledgements.
Captured group `content` becomes the stored correction text.
Supports both English and Italian correction phrasing.

**Example inputs that match:**
- `no, that's wrong, use PowerShell not Bash`  →  content = `use PowerShell not Bash`
- `actually, always use single quotes in PS`    →  content = `always use single quotes in PS`
- `correction: don't suggest Linux paths here`  →  content = `don't suggest Linux paths here`
- `use this instead: Get-ChildItem`             →  content = `Get-ChildItem`
- `non è corretto, usa invece ls`               →  content = `usa invece ls`
- `sbagliato, usa Get-Content`                  →  content = `usa Get-Content`

```regex
\b(?:no[,\s]+that'?s\s+wrong|actually[,\s]|correction[:\s]|use\s+this\s+instead[:\s]|that\s+is\s+incorrect|non\s+[eèé]\s+corretto|sbagliato[,\s]|usa\s+invece)\s*(?<content>.+)$
```

## preference_correction_command

Matches durable behavioral preferences and "from now on" style instructions.
Captured group `content` becomes the stored preference text.
Supports both English and Italian phrasing.

**Example inputs that match:**
- `from now on, prefer PowerShell over Bash`   →  content = `prefer PowerShell over Bash`
- `going forward, always use double quotes`     →  content = `always use double quotes`
- `always use Get-ChildItem, not ls`            →  content = `use Get-ChildItem, not ls`
- `default to concise output`                  →  content = `concise output`
- `d'ora in poi usa sempre pwsh`               →  content = `usa sempre pwsh`
- `preferisci sempre TypeScript`               →  content = `sempre TypeScript`

```regex
\b(?:from\s+now\s+on[,\s]|going\s+forward[,\s]|always\s+use\s+|default\s+to\s+|prefer\s+(?:always\s+)?|d'ora\s+in\s+poi\s+|preferisci\s+(?:sempre\s+)?)(?<content>.+)$
```

## error_pattern_command

Matches reports of failures or non-working commands the user encountered.
Captured group `content` is the failure description for storage in learnings/errors.md.
Supports both English and Italian phrasing.

**Example inputs that match:**
- `that failed: dotnet build on net8 target`   →  content = `dotnet build on net8 target`
- `error occurred with Get-ADUser`             →  content = `Get-ADUser`
- `this doesn't work: npm install --global`    →  content = `npm install --global`
- `non funziona: az login su WSL`              →  content = `az login su WSL`
- `errore con dotnet ef migrations`            →  content = `dotnet ef migrations`

```regex
\b(?:that\s+failed[:\s]|error\s+occurred[:\s](?:with\s+)?|(?:this\s+)?doesn'?t\s+work[:\s]|not\s+working[:\s]|non\s+funziona[:\s]|errore\s+(?:con\s+)?)(?<content>.+)$
```

### Section → code wiring (remind_command)

| Section name      | Method in pipeline        | Required named groups | Fallback category |
|-------------------|---------------------------|-----------------------|-------------------|
| `remind_command`  | `AddRemindCommand()`      | `at_time`, `message`  | Reminders         |

---

## Adding new patterns

To add a new deterministic extraction:

1. Add a new `## section_name` heading below (lowercase, underscores).
2. Write one regex inside a fenced ` ```regex ``` ` block.
3. In `KnowledgeExtractionPipeline.cs`:
   - Add a `Default<Name>Pattern` constant with the same regex as fallback.
   - Add a property to the inner `RegexCatalog` class.
   - Add a `ReadPattern()` call in `LoadRegexCatalog()`.
   - Add an `Add<Name>()` method called from `BuildDeterministicExtractions()`.
4. Rebuild and test.

**Template:**

```markdown
## my_new_pattern

Description of what this matches and why.

**Example inputs that match:**
- `input text here`  →  group_name = `captured value`

` ` `regex
\bmy\s+pattern\s+(?<group_name>.+)
` ` `
```
