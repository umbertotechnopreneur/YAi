# YAi! Workspace Memory System — Implementation Suggestions and Episodic Memory V1

This document consolidates the recommended improvements to the current implementation plan and the proposal to add episodic memory from the beginning.

---

## 1. Assessment of the Current Plan

The proposed plan is solid and implementable. It is especially strong because it builds on the existing PoC instead of reinventing the system from scratch.

A large part of the PoC can be reused or ported with path and dependency injection adaptations, especially:

- `RegexRegistry`
- `KnowledgeExtractionPipeline`
- `WarmMemoryResolver`
- `MemoryFlushService`
- `DreamingService`
- `PromotionService`
- `FileTransactionManager`

The current direction is correct: file-first memory, deterministic extraction first, AI extraction as fallback, and human review before promotion.

The following suggestions are intended to make the V1 implementation stronger while keeping it compatible with future V2–V5 improvements.

---

## 2. Workspace and Data Location

The plan currently suggests keeping `%LOCALAPPDATA%/YAi/` as `UserDataRoot`. This is fine for runtime data, logs, indexes, cache, and generated files.

However, the `workspace` should preferably live in the user's home directory:

```text
%USERPROFILE%\.yai\workspace
```

The reason is simple: the workspace is user-owned, editable, portable, and potentially versionable. `%LOCALAPPDATA%` is better suited for runtime/cache/application data, not for files the user may want to open, synchronize, back up, or commit to Git.

Recommended split:

```text
%USERPROFILE%\.yai\workspace       # human-owned, editable, durable memory
%LOCALAPPDATA%\YAi\data            # runtime data, logs, indexes, history, cache
```

Also support explicit overrides:

```text
YAI_WORKSPACE_ROOT
YAI_DATA_ROOT
```

This keeps the architecture clean and future-proof.

---

## 3. Template Location

The plan asks whether template files should live alongside the CLI binary or inside `YAi.Resources`.

Recommended choice: use `YAi.Resources`.

Suggested structure:

```text
src/YAi.Resources/reference/templates/workspace/
  memory/
  prompts/
  regex/
```

Rationale:

```text
YAi.Resources = official source of bundled templates
YAi.Persona   = persona, memory, prompt, regex logic
YAi.Client.CLI = user interface only
```

This avoids coupling the workspace template system to the CLI project. If YAi! later gets a GUI, server mode, test runner, bootstrap utility, or background service, the templates remain reusable from the shared resources package.

---

## 4. Add Template and Schema Versioning Immediately

Every template file should include explicit versioning in its frontmatter.

Example:

```yaml
---
type: memory
schema_version: 1
template_version: 1
scope: global
priority: hot
language: common
---
```

This will be important later when the memory system evolves and old workspace files need validation, migration, or update suggestions.

At minimum, include:

```text
schema_version
template_version
type
scope
priority
language
tags
last_updated
```

---

## 5. Separate Templates from User Files

The application must never blindly overwrite existing user workspace files.

Recommended behavior:

```text
If a workspace file is missing:
  create it from the bundled template.

If a workspace file exists:
  leave it untouched.

If a newer bundled template exists:
  create an update proposal, not an overwrite.
```

Possible update mechanism:

```text
system-regex.it.md exists
bundled template has a newer template_version

Create one of:
- system-regex.it.md.template-update-20260425.md
- a DREAMS.md proposal saying that a template update is available
```

This is especially important for prompts and regex files because users may edit them manually.

---

## 6. Prompts and Regex Should Be Multilingual and Category-Based from V1

The plan already proposes multilingual prompt and regex files:

```text
prompts/system-prompts.common.md
prompts/system-prompts.it.md
prompts/system-prompts.en.md

regex/system-regex.common.md
regex/system-regex.it.md
regex/system-regex.en.md
```

This is good, but I recommend adding category folders immediately, even if some files are initially small or empty.

Recommended prompt structure:

```text
workspace/prompts/
  system-prompts.common.md
  system-prompts.it.md
  system-prompts.en.md
  categories/
    chat.common.md
    chat.it.md
    chat.en.md
    explain.common.md
    explain.it.md
    explain.en.md
    coding.common.md
    coding.it.md
    coding.en.md
    memory.common.md
    memory.it.md
    memory.en.md
    tools.common.md
    tools.it.md
    tools.en.md
    safety.common.md
    safety.it.md
    safety.en.md
```

Recommended regex structure:

```text
workspace/regex/
  system-regex.common.md
  system-regex.it.md
  system-regex.en.md
  categories/
    memory.common.md
    memory.it.md
    memory.en.md
    corrections.common.md
    corrections.it.md
    corrections.en.md
    preferences.common.md
    preferences.it.md
    preferences.en.md
    reminders.common.md
    reminders.it.md
    reminders.en.md
    errors.common.md
    errors.it.md
    errors.en.md
    projects.common.md
    projects.it.md
    projects.en.md
    episodes.common.md
    episodes.it.md
    episodes.en.md
```

This prevents large monolithic files such as `system-regex.it.md` from becoming hard to maintain.

---

## 7. Language Must Be First-Class Metadata

Every memory document, memory candidate, prompt file, regex file, and extracted item should carry language metadata.

Recommended values:

```text
common
it
en
auto
```

For extraction candidates, track at least:

```text
input_language
regex_language
target_language
fallback_language
```

This matters because a memory extracted from an Italian sentence may later be stored in an English persistent memory file, or vice versa.

---

## 8. Store Candidates as JSONL, Use DREAMS.md as a Human-Readable Projection

`DREAMS.md` is useful as a readable review file, but candidate state is easier to manage in structured form.

Recommended storage:

```text
data/dreams/candidates.jsonl
data/dreams/DREAMS.md
```

Each candidate should be one JSON object per line:

```json
{"id":"...","type":"preference","content":"...","language":"it","target":"MEMORIES.md","state":"pending"}
```

Then `DREAMS.md` can be generated or refreshed as a readable projection of pending items.

This makes it easier to filter, approve, reject, archive, and migrate candidates later.

---

## 9. Promotion Must Be Transactional

No memory write should happen without a controlled transaction.

Minimum transaction contract:

```text
1. preview
2. backup
3. atomic write
4. validation
5. rollback path
```

The user interface may simplify this for low-risk memories, but internally the transaction should always exist.

Recommended backup location:

```text
workspace/.backups/
```

Example:

```text
MEMORIES.md
MEMORIES.md.bak-20260425-004210
```

---

## 10. Do Not Load Regex Files into the Normal Prompt

System prompts belong in the prompt.

Regex files generally do not.

Regex files should be used by code, not sent to the model, except in special cases such as:

- asking the AI to propose new regex patterns;
- asking the AI to debug extraction behavior;
- asking the AI to explain the memory system;
- running a maintenance/reflection pass.

This avoids unnecessary prompt noise.

---

## 11. Add MemoryBudgetManager in V1

Even in V1, there should be a simple budget manager.

Suggested limits:

```text
MaxHotTokens
MaxWarmTokens
MaxDailyTokens
MaxPromptTokens
MaxToolTokens
MaxEpisodeTokens
```

Without a memory budget manager, the system may work initially but gradually suffer from context explosion.

The rule should be:

```text
Do not load everything that is relevant.
Load the most relevant information that fits the available budget.
```

---

## 12. WarmMemoryResolver Should Explain Retrieval Decisions

`WarmMemoryResolver` should not only return files or memory blocks. It should also return reasons.

Example:

```text
Loaded: topics/git/LESSONS.md
Reason: query matched tags [git, branch, merge]
Score: 0.82
```

This prepares the system for future V3/V5 explainable retrieval.

Recommended result shape:

```text
MemorySearchResult
- document
- selected_sections
- score
- reason
- matched_tags
- matched_project
- matched_language
- estimated_tokens
```

---

## 13. Add Tests Early

Before adding extraction and dreaming, test the foundation.

Minimum test coverage:

```text
- workspace bootstrap creates missing files
- existing files are not overwritten
- frontmatter parser reads type/scope/priority/language/tags
- prompt chain loads common → language → category
- regex chain loads common → language → fallback
- invalid regex fails with a clear diagnostic
- candidate JSONL can be written and read
- memory transaction creates backup before write
```

This is especially important because multilingual prompts and regex categories can otherwise become difficult to debug.

---

## 14. Avoid “Flush” as a User-Facing Term

`MemoryFlushService` is acceptable as an internal name copied from the PoC.

In the UI, avoid the word “flush”. It sounds automatic and opaque.

Better labels:

```text
Extract memories
Review memory proposals
Run memory maintenance
Promote selected memory
```

---

## 15. Candidate State Model

The plan already mentions `CandidateState`. Use a state model that can support future workflows.

Recommended states:

```text
pending
approved
rejected
archived
promoted
conflict
needs_edit
superseded
```

`superseded` will be useful when a newer memory replaces an older one.

---

## 16. Prepare V2 Without Implementing It Yet

Even if V1 does not use SQLite, all memory items and candidates should already carry future indexing fields.

Recommended fields:

```text
id
source_file
source_section
source_line_start
source_line_end
content_hash
normalized_hash
created_at
updated_at
last_seen_at
language
project
tags
priority
scope
type
confidence
```

This makes the later SQLite/FTS5 index easier to build without reinterpreting all previous files.

---

## 17. Recommended Revised Implementation Phases

The current plan can be improved by slightly reordering it.

### Phase 0 — Contracts

Implement the core contracts first:

```text
MemoryItem
MemoryCandidate
MemoryDocument
MemoryLanguage
MemoryPriority
MemoryScope
MemoryType
TokenBudget
Workspace/Data root contract
CandidateState
EpisodeType
```

### Phase 1 — Workspace Foundation

```text
AppPaths
WorkspaceProfileService recursive seeding
template versioning
backups
frontmatter parser
workspace/data split
```

### Phase 2 — Prompt and Regex Repositories

```text
multilingual prompt chain
category prompt chain
multilingual regex chain
category regex chain
diagnostics and tests
```

### Phase 3 — Retrieval and Budget

```text
hot loader
warm resolver
daily loader
episode resolver
MemoryBudgetManager
retrieval explanation
```

### Phase 4 — Extraction

```text
regex-first candidates
AI fallback candidates
episode candidates
JSONL candidate store
DREAMS.md readable projection
```

### Phase 5 — Promotion and UI

```text
review screen
edit/approve/reject/archive
transactional write
conflict handling
episode promotion
```

---

# Episodic Memory V1

## 18. Why Add Episodic Memory Now

It makes sense to add episodic memory now, but only in a simple V1 form.

Do not start with a memory graph or embeddings. Use Markdown-first episodic memory that is compatible with the existing plan and can later be indexed in V2.

---

## 19. What Episodic Memory Is

Episodic memory records what happened.

Semantic memory records what is generally true.

Example semantic memory:

```text
User prefers PowerShell commands on Windows.
```

Example episodic memory:

```text
On 2026-04-24, the user had a Git branch ahead by 7 commits and created a backup branch before continuing risky operations.
```

Simple distinction:

```text
Semantic memory = facts, preferences, rules
Episodic memory = events, decisions, problems, fixes, workflows
```

---

## 20. Where to Store Episodic Memory

Recommended V1 storage:

```text
workspace/memory/episodes/
  2026-04.md
  2026-05.md
```

Monthly files are preferable for V1 because they keep the number of files manageable.

A daily structure is also possible:

```text
workspace/memory/episodes/
  2026/
    04/
      2026-04-25.md
```

But for the first implementation, monthly logs are simpler.

---

## 21. Episode File Frontmatter

Each monthly episode log should use frontmatter.

Example:

```yaml
---
type: episode_log
scope: user
priority: warm
language: common
schema_version: 1
tags: [episodes, history, decisions]
period: 2026-04
---
```

Episodes should not be hot memory by default. They should usually be `warm` or `cold`.

---

## 22. Episode Entry Format

Example episode entry:

```markdown
## 2026-04-25 — Git branch safety workflow

Type: workflow  
Project: YAi  
Tags: git, branch, safety, backup  
Source: chat  
Confidence: 0.94  

The user had a local branch ahead of remote by 7 commits and created a backup branch before continuing with risky Git operations.

Outcome:
- Backup branch created.
- Working tree remained clean.
- The safety pattern should influence future Git command suggestions.

Related procedural rule:
- Before destructive Git operations, suggest a backup branch or stash.
```

Episodes should be compact summaries, not full transcripts.

---

## 23. Episode Types

Add an `EpisodeType` enum immediately.

Recommended values:

```text
decision
problem
fix
workflow
command_sequence
project_event
design_change
user_feedback
failure
milestone
```

These types will be useful later for indexing, retrieval, filtering, and conflict analysis.

---

## 24. What Should Become an Episode

Create an episode when at least one of these happens:

```text
- a technical decision is made
- an error is resolved
- an operational rule is defined
- a workflow is completed
- a recurring pattern emerges
- the user corrects the assistant
- an important specification is generated
- an implementation plan is approved
- a project milestone happens
```

Do not create an episode for every chat. Only store events that have future value.

---

## 25. How Episodic Memory Fits the Current Plan

Add a new candidate target:

```text
MemoryCandidate
  type: episode
  target: workspace/memory/episodes/YYYY-MM.md
```

Pipeline:

```text
Conversation
  ↓
Regex / AI extraction
  ↓
EpisodeCandidate
  ↓
DREAMS / candidate review
  ↓
Promote
  ↓
episodes/YYYY-MM.md
```

This keeps the same review and promotion model used for other memories.

---

## 26. Regex for Episodic Memory

Regex will not capture all episodes, but it can capture useful signals.

Add files:

```text
workspace/regex/categories/episodes.it.md
workspace/regex/categories/episodes.en.md
```

Italian trigger examples:

```text
abbiamo deciso di...
la decisione è...
problema risolto...
alla fine funziona...
questa è una lezione...
workflow corretto...
da ricordare per questo progetto...
```

Example Italian pattern:

```regex
\b(?:abbiamo\s+deciso\s+di|la\s+decisione\s+[eèé]|problema\s+risolto|alla\s+fine\s+funziona|questa\s+[eèé]\s+una\s+lezione|da\s+ricordare\s+per\s+questo\s+progetto)\s*(?<content>.+)$
```

Equivalent English files should include patterns for:

```text
we decided to...
the decision is...
problem solved...
this is a lesson...
remember this for the project...
the workflow is...
```

---

## 27. AI Fallback for Episodes

AI fallback is more useful for episodes than for simple preferences because episodes are often implicit.

Add a dedicated episode extraction prompt.

Recommended prompt:

```text
Extract only durable episodes from the conversation.
An episode is a past event, decision, problem, fix, workflow, or project milestone that may be useful later.
Do not extract generic facts or user preferences.
Return JSON only.
```

Recommended JSON schema:

```json
{
  "episodes": [
    {
      "title": "Git branch backup before risky operation",
      "type": "workflow",
      "project": "YAi",
      "summary": "The user created a backup branch before continuing risky Git operations.",
      "outcome": "Working tree remained clean.",
      "tags": ["git", "backup", "safety"],
      "confidence": 0.94,
      "should_promote_to_lesson": true,
      "should_promote_to_rule": false
    }
  ]
}
```

Store episode extraction prompts here:

```text
workspace/prompts/categories/episode-extraction.common.md
workspace/prompts/categories/episode-extraction.it.md
workspace/prompts/categories/episode-extraction.en.md
```

---

## 28. How to Use Episodes Without Exploding Context

Episodes must not be hot memory.

They should be loaded only when relevant:

```text
- the project matches
- the tags match
- the problem is similar
- the user asks what was decided
- the user asks what happened previously
- the workflow is relevant to the current task
```

Example:

```text
User asks: "How do I fix this merge?"

Load:
- semantic rule: prefer backup before destructive Git operations
- relevant episode: previous branch backup workflow
```

Do not load all episodes.

---

## 29. Add Phase 3.5 — Episodic Memory V1

Add this phase to the implementation plan.

```text
Phase 3.5 — Episodic Memory V1

1. Add EpisodeType enum.
2. Add EpisodeCandidate model or extend ExtractionCandidate with EventType = episode.
3. Add workspace/memory/episodes/ folder.
4. Add monthly episode files: YYYY-MM.md.
5. Add episode frontmatter contract.
6. Add regex category files:
   - regex/categories/episodes.it.md
   - regex/categories/episodes.en.md
7. Add AI episode extractor prompt:
   - prompts/categories/episode-extraction.common.md
   - prompts/categories/episode-extraction.it.md
   - prompts/categories/episode-extraction.en.md
8. Update PromotionService to promote approved episodes.
9. Update WarmMemoryResolver to retrieve episodes by project, tags, topic, and recency.
10. Do not load episodes as hot memory.
```

---

## 30. Important Rule: Episodes Are Summaries, Not Transcripts

Do not store full conversations as episodic memory.

Bad:

```text
Full conversation copied into memory.
```

Good:

```text
One compact event, with date, project, tags, outcome, and why it matters.
```

Recommended episode shape:

```text
date
title
type
project
tags
source
confidence
summary
outcome
related lesson/rule, if any
```

---

## 31. Recommended Final Direction

Add episodic memory now, but keep it simple:

```text
Markdown episodic log
+ candidate extraction
+ manual promotion
+ warm retrieval only
```

Do not add SQLite, embeddings, or graph logic yet.

However, the model must already include:

```text
EpisodeType
project
tags
date
outcome
source
confidence
```

These fields will be essential for V2/V3 later.

---

## 32. Final Recommendation

The plan is already good. The main improvements are:

```text
1. Put workspace in the user home, not only in LocalAppData.
2. Keep runtime data in LocalAppData.
3. Put templates in YAi.Resources.
4. Add schema/template versioning now.
5. Use multilingual + category-based prompts and regex from V1.
6. Store candidates in JSONL and render DREAMS.md as a readable projection.
7. Add MemoryBudgetManager immediately.
8. Make WarmMemoryResolver explain why it selected memory.
9. Add episodic memory now, but only as Markdown-first warm memory.
10. Keep SQLite, embeddings, and graph memory for later versions.
```

The most important architectural rule remains:

```text
Markdown is the source of truth.
SQLite, embeddings, and graph structures are future derived indexes/views.
```

This keeps YAi! readable, debuggable, inspectable, versionable, and compatible with an OpenClaw-style memory system.
