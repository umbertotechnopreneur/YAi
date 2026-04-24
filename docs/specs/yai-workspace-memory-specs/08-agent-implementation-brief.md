# 08 — Agent Implementation Brief

## Mission

Implement YAi!'s first local memory system by adapting the existing PoC memory code.

The result must be compatible with an OpenClaw-style local workspace, but should remain simpler and safer than a full memory engine.

## Non-negotiable requirements

1. Replace `storage/` as the user-memory concept with `workspace/`.
2. `workspace/` must live under the user's home folder by default.
3. Do not persist user memory under build output.
4. Keep memory file-first and Markdown-first.
5. Support frontmatter YAML metadata.
6. Support multilingual regex repositories.
7. Support multilingual system prompt repositories.
8. Support category-based prompt and regex files.
9. Run regex extraction before AI extraction.
10. Route low-confidence or inferred memory to review/dreams.
11. Require review for rules, limits, identity, conflicts, and AI-inferred durable memory.
12. Back up files before mutation.
13. Reuse the existing PoC services where possible.

## Implementation order

### Step 1 — Workspace paths

Add a `WorkspaceOptions` or equivalent configuration model.

Resolve:

```text
WorkspaceRoot
DataRoot
MemoryRoot
PromptRoot
RegexRoot
DreamsRoot
BackupRoot
```

Default:

```text
%USERPROFILE%\.yai\workspace
%USERPROFILE%\.yai\data
```

Use cross-platform APIs. Do not hardcode Windows separators.

### Step 2 — Bootstrap workspace

On first run:

- create workspace folders
- copy default template files if missing
- never overwrite existing user files
- create backups before any migration

### Step 3 — Frontmatter parser

Extend the existing memory parser to read and preserve YAML frontmatter.

The parser must tolerate old files.

### Step 4 — Prompt repository

Adapt prompt loading to support:

```text
system-prompts.common.md
system-prompts.it.md
system-prompts.en.md
categories/*.md
```

Keep compatibility with the old single-file H2-section model.

### Step 5 — Regex repository

Adapt `RegexRegistry` to support:

```text
system-regex.common.md
system-regex.it.md
system-regex.en.md
categories/*.md
```

Compile all safe patterns at boot.

Show clear errors for invalid patterns.

### Step 6 — Deterministic extraction

Run regex extraction before AI extraction.

Convert matches into normalized candidate events.

### Step 7 — Candidate review

Add a pending candidate store:

```text
data/dreams/pending-memory-candidates.jsonl
```

Expose review actions:

```text
Approve
Edit
Reject
Promote to Rule
Archive
```

### Step 8 — Promotion writer

Implement a single memory writer/promotion path.

It must:

- create backups
- write safely
- preserve frontmatter
- append to target section
- log mutations

### Step 9 — Tests

Add tests for:

- workspace path resolution
- bootstrap does not overwrite files
- frontmatter parsing
- prompt fallback chain
- regex fallback chain
- Italian regex extraction
- English regex extraction
- invalid regex boot diagnostic
- candidate routing
- backup-before-write

## First deliverable scope

Keep the first deliverable small:

```text
workspace path resolution
bootstrap folders/files
frontmatter parse support
multilingual prompt files
multilingual regex files
regex-first extraction
pending review file
safe promotion to MEMORIES.md and LESSONS.md
```

Do not implement:

```text
vector database
embeddings
SQLite index
cloud sync
automatic deletion/compaction
complex UI redesign
```

## Expected result

After implementation, YAi! should be able to:

1. Start with a clean user-home workspace.
2. Load system prompts by language and category.
3. Load regex patterns by language and category.
4. Detect explicit Italian and English memory commands without AI.
5. Stage inferred memory for review.
6. Promote approved candidates safely into Markdown memory files.
7. Keep all memory visible and editable by the user.
