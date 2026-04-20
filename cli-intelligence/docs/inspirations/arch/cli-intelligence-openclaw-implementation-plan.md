# Architecting Self-Improving Memory, Context, and Learning for CLI-Intelligence
## An OpenClaw-Inspired Implementation Plan for the Current C#/.NET Architecture

## Executive Summary

CLI-Intelligence already has the right architectural base for a transparent, Markdown-driven memory system. It uses disk-backed knowledge files, a configurable prompt-building pipeline, deterministic regex extraction, history persistence, reminders, and a local knowledge service. That means the project does **not** need a redesign to adopt OpenClaw-style self-improving behavior. It needs a controlled evolution.

The recommended direction is to introduce **tiered memory**, **deterministic correction capture**, **pre-compaction memory flush**, **daily working context**, **heartbeat maintenance**, and a **conservative dreaming/promotion pipeline**. The key principle should remain the same: the assistant only “remembers” what is explicitly written to transparent files that the user can inspect, edit, and audit.

This document proposes a practical implementation strategy for CLI-Intelligence, aligned with its current codebase and with a bias toward safety, debuggability, and low operational complexity.

---

## 1. Current Strengths in the Existing Architecture

CLI-Intelligence already contains several components that map naturally to an OpenClaw-style system:

- `LocalKnowledgeService` for disk-backed knowledge storage
- `KnowledgeExtractionPipeline` for turning conversation signals into stored knowledge
- `PromptBuilder` for assembling model context
- `RegexRegistry` plus `system-regex.md` for deterministic extraction
- `ReminderService` and `TimerTool` for scheduled triggers
- `HistoryService` for session persistence
- Markdown storage files such as:
  - `memories.md`
  - `lessons.md`
  - `limits.md`
  - `system-prompts.md`
  - `system-regex.md`

This is important because it means the project already supports the hardest part conceptually: **explicit, file-based memory with inspectable behavior**.

The right next step is to formalize the knowledge model so that not all stored information is treated the same way.

---

## 2. Design Goals

The recommended memory and learning design should aim for the following:

1. Preserve transparency: everything important should live in files.
2. Reduce unnecessary prompt bloat.
3. Separate durable facts from temporary observations.
4. Learn from user corrections without overfitting.
5. Avoid unsafe automatic self-modification.
6. Keep the system easy to debug and easy to audit.
7. Prefer deterministic routing where possible, AI judgment only where useful.
8. Make promotion into stronger rules gradual, evidence-based, and reviewable.

---

## 3. Recommended Knowledge Model

The core improvement is to move from a mostly flat memory model to a **tiered memory model**.

### 3.1 HOT Memory

HOT memory is always loaded into prompts.

This should contain only information that is broadly useful and stable:

- global user preferences
- durable project facts
- non-negotiable rules
- core lessons that are frequently reused

Suggested files:

- `storage/memories.md`
- `storage/lessons.md`
- `storage/limits.md`

This is effectively what the app already does today, but it should become more intentional and more selective.

### 3.2 WARM Memory

WARM memory is loaded only when relevant.

This should contain project-specific or domain-specific context that should not consume tokens in unrelated sessions.

Suggested structure:

```text
storage/
  memories/
    projects/
      cli-intelligence.md
      iaviews.md
      qsee.md
    domains/
      dotnet.md
      windows.md
      powershell.md
```

Examples of WARM memories:

- conventions specific to the `cli-intelligence` repository
- preferred shell usage in Windows-centric repos
- project-specific naming or architectural decisions
- domain-specific reminders for .NET, Azure, PowerShell, or Vue.js work

### 3.3 COLD Memory

COLD memory is archival. It is **not** injected automatically into prompts.

It should be kept for traceability, analytics, and future re-evaluation.

Suggested structure:

```text
storage/
  archive/
    corrections/
    dreams/
    daily/
```

Examples:

- old corrections no longer frequently relevant
- previously proposed promotions
- historical daily notes
- low-confidence learnings that are not yet promotable

---

## 4. Recommended File Layout

A concrete file layout could look like this:

```text
storage/
  memories.md
  lessons.md
  limits.md
  system-prompts.md
  system-regex.md

  memories/
    projects/
      cli-intelligence.md
      iaviews.md
    domains/
      windows.md
      dotnet.md
      powershell.md

  learnings/
    corrections.md
    repeated-patterns.md
    errors.md

  daily/
    2026-04-17.md
    2026-04-16.md

  dreams/
    DREAMS.md

  archive/
    corrections/
    daily/
    dreams/
```

This preserves the current files while adding structured growth paths.

---

## 5. Prompt Loading Strategy

## 5.1 Current Behavior

The current system already loads the main knowledge files into prompt context through the prompt-building process. That is a good default, but it will eventually become too expensive if everything keeps growing.

## 5.2 Recommended Behavior

`PromptBuilder` should be upgraded so that:

- HOT files are always loaded
- WARM files are conditionally loaded
- COLD files are never automatically loaded

### 5.3 Suggested Relevance Signals for WARM Loading

WARM files should be loaded based on a simple relevance pipeline such as:

1. Active directory match
2. Explicit query keyword match
3. Screen context match
4. Optional file metadata tags

Examples:

- if current folder contains `cli-intelligence`, load `memories/projects/cli-intelligence.md`
- if the query mentions `RegexRegistry`, `PromptBuilder`, `OpenRouter`, or `ReminderService`, load project memory for CLI-Intelligence
- if the query mentions `.NET`, `C#`, or `worker service`, load `memories/domains/dotnet.md`
- if the query mentions PowerShell, Windows terminal, or scripts, load `memories/domains/powershell.md` and `windows.md`

### 5.4 Minimal First Version

A practical first version does not need embeddings or vector search. It can use:

- directory name checks
- keyword maps
- lightweight tags at the top of Markdown files

Example metadata block:

```md
---
tags: [dotnet, csharp, worker, background-service]
scope: domain
priority: warm
---
```

This is enough to get real value quickly.

---

## 6. Correction Capture and Deterministic Self-Improvement

## 6.1 Why This Matters

The highest-value learning signal in an assistant like this is not generic conversation. It is **user correction**.

If the user says:

- “No, that’s wrong.”
- “Actually, use PowerShell, not Bash.”
- “For this project, prefer concise output.”
- “Do not suggest Linux commands here.”

that is directly actionable learning.

## 6.2 Recommended Regex Extensions

The project already uses deterministic regex extraction through `system-regex.md`. This should be extended with new correction-oriented patterns.

Recommended new section:

```regex
## correction_command
\b(?:no[, ]+that'?s wrong|actually|correction|use this instead|that is incorrect|non è corretto|sbagliato|usa invece)\b.*(?<content>.+)
```

Also consider a second pattern for durable behavioral preferences:

```regex
## preference_correction_command
\b(?:from now on|going forward|prefer|always use|default to)\b.*(?<content>.+)
```

## 6.3 Routing Rules

Once extracted, corrections should not go into the same place as normal memories.

Recommended routing:

- explicit user facts → `memories.md`
- corrections and behavior fixes → `learnings/corrections.md`
- repeated operational mistakes → `learnings/errors.md`
- stable preferences with broad reuse → candidate for memory or rules

## 6.4 Suggested Entry Format

Use a structured, append-only Markdown format at first.

Example:

```md
- date: 2026-04-17
  context: chat
  input: "Actually, use PowerShell here, not Bash."
  extracted_correction: "Use PowerShell instead of Bash in this project."
  scope: project
  confidence: 0.95
  status: new
```

This is readable by humans and still easy for AI or deterministic parsing to consume later.

---

## 7. Separate Memory, Lessons, Corrections, and Rules

One of the most important improvements is semantic separation.

## 7.1 Memory

Memory is for durable facts and preferences.

Examples:

- user prefers PowerShell on Windows
- user prefers concise answers
- this repo is a .NET 10 CLI app
- this project uses Serilog and Spectre.Console

## 7.2 Lesson

A lesson is a reusable operational insight discovered from experience.

Examples:

- do not suggest destructive file operations without confirmation
- prefer targeted patching over full file rewrite
- explain shell commands with risk notes

## 7.3 Correction

A correction is a raw learning signal, usually recent, often user-provided.

Examples:

- “Use PowerShell, not Bash”
- “Do not assume Linux paths”
- “For this project, keep markdown files downloadable”

Corrections are evidence. They are not automatically permanent truth.

## 7.4 Rule

A rule is promoted behavior that the system should follow reliably.

Examples:

- default to PowerShell for Windows-focused technical instructions
- do not propose hidden state; persist important state to disk
- prefer additive changes over destructive edits

This separation prevents the common failure mode where every user remark is over-promoted into global behavior.

---

## 8. Automatic Memory Flush Before Compaction

## 8.1 The Problem

As chat sessions grow, old messages eventually need to be pruned or summarized to stay within token budgets.

If the system prunes conversation without extracting the valuable signals first, it loses:

- user preferences
- corrected mistakes
- unresolved technical constraints
- failed command patterns
- short-lived but important session discoveries

## 8.2 Recommended Solution

Before conversation compaction or pruning, trigger a **silent memory flush**.

The silent memory flush should:

1. read the conversation window that is about to be removed
2. extract only durable or operationally useful items
3. write them into the correct Markdown stores
4. only then allow pruning or summarization

## 8.3 What the Silent Turn Should Extract

The extraction prompt should be limited to:

- durable user preferences
- stable project facts
- corrected technical guidance
- command failures
- unresolved constraints worth preserving

It should explicitly ignore:

- casual chatter
- one-off jokes
- transient emotion
- repeated content already stored

## 8.4 Recommended Output Format

Prefer structured JSON so the pipeline can route it safely.

Example:

```json
{
  "memories": [
    {
      "content": "User prefers PowerShell examples on Windows.",
      "scope": "global",
      "confidence": 0.96
    }
  ],
  "corrections": [
    {
      "content": "Do not suggest Bash for this repository.",
      "scope": "project",
      "confidence": 0.97
    }
  ],
  "lessons": [
    {
      "content": "When the user asks for a downloadable document, generate a file artifact instead of only inline text.",
      "scope": "workflow",
      "confidence": 0.92
    }
  ]
}
```

---

## 9. Daily Working Context Files

## 9.1 Why Daily Files Are Useful

Not everything deserves to become a durable memory. Some context is useful only for a day or two.

Examples:

- temporary implementation decisions
- experiments in progress
- issues encountered today
- reminders about what was just done
- current debugging focus

This is best handled through daily files.

## 9.2 Suggested Daily File Structure

```text
storage/daily/2026-04-17.md
```

Suggested contents:

- current tasks
- active experiments
- decisions made today
- unresolved issues
- references to related files

Example:

```md
# Daily Context — 2026-04-17

## Active Work
- Evaluating OpenClaw-inspired memory architecture for CLI-Intelligence
- Considering heartbeat and dreaming workflows

## Observations
- Current memory system is already Markdown-based and transparent
- Prompt growth risk will increase unless memory becomes tiered

## Temporary Decisions
- Prefer conservative promotion model
- Avoid automatic edits to system prompts without evidence and audit trail
```

## 9.3 Loading Rules

Recommended loading policy:

- load today’s file automatically
- optionally load yesterday’s file
- never load more than the two most recent daily files by default

This keeps context fresh without accumulating large prompt overhead.

---

## 10. Heartbeat Loops

## 10.1 Concept

OpenClaw uses heartbeat mechanics to trigger maintenance work on a schedule. CLI-Intelligence does not need a heavy always-on background service to gain value from this idea.

It already has a `ReminderService` and timer-related capabilities. That is enough to implement a lightweight heartbeat.

## 10.2 Recommended First Version: Opportunistic Heartbeat

Do not start with a resident background daemon. Start with a **heartbeat triggered opportunistically**:

- on app startup
- on entering chat mode
- every N user messages
- on session exit
- before compaction
- before shutdown

## 10.3 What Heartbeat Should Do

Each heartbeat can run a small maintenance pipeline:

1. scan recent corrections
2. detect duplicates or repeated patterns
3. move stale items to archive
4. update daily files
5. propose promotable rules
6. append audit entries to `dreams/DREAMS.md`

## 10.4 Safety Principle

Heartbeat should not directly rewrite critical instructions in its first implementation.

Its first role should be:

- observe
- organize
- score
- propose

Promotion should remain conservative.

---

## 11. Dreaming: Controlled Autonomous Learning

## 11.1 What “Dreaming” Should Mean Here

Dreaming should not mean unrestricted autonomous behavior.

In CLI-Intelligence, it should mean:

- periodic review of recent corrections, lessons, and patterns
- detection of repeated signals
- proposal of promotion candidates
- optional promotion only when thresholds are met

## 11.2 Recommended Dreaming Pipeline

### Phase A — Gather

Read recent files such as:

- `learnings/corrections.md`
- `learnings/errors.md`
- `lessons.md`
- today’s and yesterday’s daily notes

### Phase B — Cluster

Group similar learnings such as:

- repeated PowerShell preference
- repeated request for downloadable Markdown
- repeated correction about Windows path assumptions

### Phase C — Score

Score each cluster with fields such as:

- `times_seen`
- `times_applied_successfully`
- `last_seen`
- `scope`
- `confidence`

### Phase D — Propose

Write proposals to `dreams/DREAMS.md`.

### Phase E — Promote

Only promote if evidence and safety thresholds are met.

## 11.3 Recommended `DREAMS.md` Format

```md
# Dreams Log

- date: 2026-04-17
  candidate_rule: "Default to PowerShell for Windows-first technical guidance."
  evidence_count: 4
  times_applied_successfully: 3
  source_files:
    - learnings/corrections.md
    - lessons.md
  proposed_target: "rules/rules.md"
  status: proposed
```

If promoted:

```md
- date: 2026-04-18
  candidate_rule: "Default to PowerShell for Windows-first technical guidance."
  evidence_count: 5
  times_applied_successfully: 4
  proposed_target: "rules/rules.md"
  status: promoted
```

This gives you a visible, auditable learning diary.

---

## 12. Promotion Strategy

## 12.1 Do Not Promote Directly into `system-prompts.md` by Default

This is the biggest caution.

Not every repeated pattern belongs in the system prompt. Promoting too eagerly into system instructions causes:

- behavioral rigidity
- prompt bloat
- accidental conflict with explicit rules
- hard-to-debug drift

## 12.2 Recommended Promotion Ladder

Use a staged ladder:

1. raw correction
2. repeated correction
3. lesson candidate
4. rule candidate
5. promoted rule
6. only rarely, promoted system prompt instruction

## 12.3 Promotion Targets by Type

### Promote to `memories.md` when:
- it is a durable user or project fact

### Promote to `lessons.md` when:
- it is a reusable operational insight

### Promote to `rules/rules.md` when:
- it is a strong behavioral rule with repeated successful evidence

### Promote to `system-prompts.md` only when:
- it is globally applicable
- it is stable
- it does not conflict with safety constraints
- it is unlikely to need rollback often

---

## 13. Scoring and Confidence Model

The project already uses confidence in extraction. That should be extended into a more explicit learning score.

Each correction or lesson should track fields like:

- `confidence`
- `times_seen`
- `times_applied_successfully`
- `times_rejected`
- `last_seen`
- `scope`
- `promotable`
- `status`

Example Markdown entry:

```md
- rule: "Default to PowerShell on Windows."
  scope: global
  confidence: 0.94
  times_seen: 5
  times_applied_successfully: 4
  times_rejected: 0
  last_seen: 2026-04-17
  promotable: true
  status: warm
```

This makes future promotion logic much easier and safer.

---

## 14. Suggested Implementation Phases

## Phase 1 — Low-Risk Structural Improvements

This is the recommended starting point.

### Deliverables
- add WARM and COLD storage folders
- update `PromptBuilder` to load WARM memories conditionally
- add correction regex patterns
- route corrections into `learnings/corrections.md`
- add daily note file support

### Value
- immediate token efficiency improvement
- improved relevance
- better capture of user corrections
- no risky self-modification yet

---

## Phase 2 — Silent Memory Flush

### Deliverables
- detect when conversation window is near compaction threshold
- run silent extraction pass before pruning
- persist extracted memories, corrections, and lessons
- archive the compacted window if needed

### Value
- prevents context loss
- preserves useful signals from long sessions
- supports future autonomous learning

---

## Phase 3 — Heartbeat Maintenance

### Deliverables
- trigger lightweight maintenance on startup, session end, and every N messages
- deduplicate corrections
- summarize daily patterns
- update `DREAMS.md` with proposals

### Value
- keeps knowledge files healthy
- introduces self-maintenance without complexity
- prepares the ground for dreaming

---

## Phase 4 — Dreaming and Promotion

### Deliverables
- cluster repeated corrections
- score promotable rules
- write proposals to `DREAMS.md`
- optionally promote to `lessons.md` or `rules/rules.md`

### Value
- true self-improving behavior
- still transparent and reviewable
- gradual, evidence-based adaptation

---

## 15. Recommended C# Integration Points

Below is a practical mapping to likely integration areas in the existing project.

## 15.1 `PromptBuilder`

Responsibilities to add:

- always inject HOT memory
- detect relevant WARM memory files
- inject only matching WARM files
- never inject COLD archives automatically

Potential methods to add:

- `LoadHotMemory()`
- `LoadWarmMemory(string input, string? currentDirectory, string? screenContext)`
- `ResolveRelevantMemoryFiles(...)`

---

## 15.2 `RegexRegistry` and `system-regex.md`

Responsibilities to add:

- register correction extraction patterns
- register preference-correction patterns
- optionally register error-pattern extraction

This is the cleanest place for deterministic learning signal detection.

---

## 15.3 `KnowledgeExtractionPipeline`

Responsibilities to add:

- route different extraction types to different files
- support structured appenders for:
  - memories
  - lessons
  - corrections
  - errors
- maintain metadata like confidence, scope, and timestamps

---

## 15.4 `HistoryService` and Chat Flow

Responsibilities to add:

- expose a way to read the segment of conversation that is about to be compacted
- trigger silent memory flush before pruning
- optionally store compacted summaries in archive

---

## 15.5 `ReminderService`

Responsibilities to add:

- provide heartbeat scheduling trigger points
- optionally support a recurring maintenance reminder
- run non-interactive maintenance tasks when appropriate

This does not need to be a true background service in the first version.

---

## 15.6 New Optional Services

You may want to introduce a few focused services instead of putting all logic into existing classes.

Suggested additions:

- `WarmMemoryResolver`
- `CorrectionCaptureService`
- `MemoryFlushService`
- `HeartbeatService`
- `DreamingService`
- `PromotionService`

Each can be small and single-purpose.

---

## 16. Safety and Governance Rules

This architecture becomes more powerful as it learns, so guardrails matter.

## 16.1 Never Auto-Promote from a Single Signal

One correction is evidence, not truth.

## 16.2 Never Promote Security-Sensitive Behavior Automatically

Anything involving:

- secrets
- credentials
- destructive file operations
- system-wide changes
- network exposure
- execution permissions

should remain outside autonomous promotion.

## 16.3 Keep All Promotions Auditable

Every proposed or promoted change should be logged in a human-readable file.

## 16.4 Prefer Writing to Rules Before System Prompts

`rules/rules.md` is a safer destination than the base system prompt.

## 16.5 Allow Easy Rollback

All promotion steps should be reversible by editing Markdown files.

---

## 17. Minimal Viable Version Recommendation

If the goal is to start with the highest value and lowest risk, the best MVP is:

1. introduce WARM memory files
2. add correction capture regex
3. route corrections into `learnings/corrections.md`
4. add today/yesterday daily note loading
5. implement silent memory flush before conversation pruning
6. write dreaming proposals to `DREAMS.md` without auto-promotion yet

That gives you most of the practical value of the OpenClaw ideas without premature complexity.

---

## 18. Example Promotion Policy

A simple and conservative promotion policy could be:

- correction seen 1 time → store in `corrections.md`
- seen 2 times → mark as repeated
- seen 3 times and successfully applied at least 2 times → propose in `DREAMS.md`
- seen 4+ times, zero conflicts, safe scope → promote to `lessons.md` or `rules/rules.md`
- only after sustained stable use → consider promotion to `system-prompts.md`

This is slow by design. Slow promotion is safer.

---

## 19. Example End-to-End Flow

A realistic example flow in CLI-Intelligence could be:

1. user says: “Actually, use PowerShell, not Bash, for this project.”
2. regex captures correction
3. correction appended to `learnings/corrections.md`
4. project-specific WARM memory for `cli-intelligence` is updated or marked as relevant
5. after several repetitions, heartbeat sees the same correction multiple times
6. dreaming process proposes:
   - “Default to PowerShell for Windows-first technical instructions”
7. proposal is written to `dreams/DREAMS.md`
8. after enough successful reuse, rule is promoted to `rules/rules.md`
9. future answers naturally improve without bloating the global prompt too early

This is exactly the kind of transparent, inspectable self-improvement model that fits the project well.

---

## 20. Final Recommendation

CLI-Intelligence is already close to the right philosophy. It stores knowledge explicitly, uses Markdown, has a controllable prompt builder, and already contains extraction and timing primitives. The most valuable improvement is not “more AI.” It is **better structure around what gets remembered, when it gets loaded, and how repeated corrections become stable behavior**.

The best implementation path is:

- adopt HOT/WARM/COLD memory
- capture corrections deterministically
- preserve important context before compaction
- maintain daily working notes
- run heartbeat-style maintenance opportunistically
- implement dreaming as a proposal-and-promotion mechanism, not as unrestricted self-editing

That path gives CLI-Intelligence a credible, practical version of OpenClaw-style autonomous learning while staying aligned with the current architecture, remaining auditable, and keeping risk under control.

Here is the addendum prepared for your implementation plan. It integrates the tactical refinements and OpenClaw schema alignment into your existing C# architecture.

---

# Appendix A: Refinements for CLI-Intelligence & OpenClaw Alignment

This appendix provides additional tactical guidance to ensure the "OpenClaw-style" implementation remains robust, conflict-free, and fully compatible with the broader ecosystem standards.

## A.1 Conflict Resolution & Logic Guardrails
While the dreaming process proposes new rules, it must handle contradictions between recent signals and established truth.
* **Contradiction Check:** Before any promotion to `rules/rules.md`, the `PromotionService` must perform a keyword scan against existing entries in `memories.md` and `limits.md`.
* **Conflict Flagging:** If a high-confidence correction (e.g., "Use Bash") contradicts a promoted rule (e.g., "Default to PowerShell"), the system must append a "Conflict" entry to `dreams/DREAMS.md` for manual user resolution rather than auto-updating.
* **Evidence Weighting:** Use a "decay" score in the heartbeat maintenance; older corrections that haven't been reinforced should lose weight compared to newer, repeated user corrections.

## A.2 Environmental Context Capture
In a CLI environment, context is more than just text; it is the state of the machine.
* **Dynamic Metadata:** The `PromptBuilder` should automatically capture the current OS (Windows/Linux/macOS), the active Shell (pwsh/bash/zsh), and the current directory path as transient "Daily Context".
* **Relevance Mapping:** Use this environment data as a primary signal for WARM memory loading—for example, only loading `memories/domains/powershell.md` when the active shell is detected as `pwsh`.

## A.3 System Transparency (The Validation Command)
To maintain the design goal of "easy to audit," the user needs an interface to the memory system.
* **Command Interface:** Implement a CLI command (e.g., `ai --status` or `ai --memories`) that utilizes `LocalKnowledgeService` to list currently active HOT and WARM files.
* **Visibility:** This command should display the "Learning Score" of recent corrections and any pending proposals in `DREAMS.md`, ensuring the user is never surprised by an "evolved" behavior.

## A.4 Cold Storage Lifecycle (Pruning)
To prevent `storage/archive/` from growing indefinitely, a lifecycle policy should be integrated into the Heartbeat.
* **Monthly Compaction:** During the first heartbeat of a new month, the `HeartbeatService` should summarize the previous month's `daily/*.md` files into a single `archive/monthly/YYYY-MM.md` summary and delete the individual daily logs.
* **Stale Correction Cleanup:** Corrections that have a low confidence score and have not been "seen" for over 30 days should be moved from `learnings/corrections.md` to `archive/corrections/` to keep the active learning file lean.

## A.5 OpenClaw Schema Compatibility
To align with the OpenClaw "file-first" standard, the system should adopt specific YAML front-matter and file conventions.

### A.5.1 Standard YAML Front-matter
All memory and rule files should include a metadata block for efficient C# parsing:
```yaml
---
type: memory        # Options: memory, lesson, rule, correction
tags: [dotnet, cli] # Used for WARM memory relevance
scope: project      # Options: global, project, session
priority: warm      # Options: hot, warm, cold
last_updated: 2026-04-17
---
```

### A.5.2 Core OpenClaw Workspace Files
If they do not already exist, the system should support the following root files for maximum ecosystem compatibility:
* **`SOUL.md`**: Stores the agent's core identity and non-negotiable safety boundaries.
* **`USER.md`**: A central hub for durable facts about the user's personal preferences.
* **`AGENTS.md`**: Specific operational instructions for different tasks (e.g., "The C# Refactorer").

## A.6 Implementation Toolkit (C#)
Recommended libraries to support this plan within the current .NET architecture:
* **YamlDotNet:** For robust parsing of the metadata blocks in Markdown files.
* **FileSystemWatcher:** To trigger `WarmMemoryResolver` updates the moment a user changes project directories.
* **Spectre.Console:** To render the `DREAMS.md` proposals and memory status tables in a clear, developer-friendly CLI format.

---

**Does this Appendix cover all the specific technical details you need for your implementation, or would you like to drill down into the logic for the `PromotionService`?**