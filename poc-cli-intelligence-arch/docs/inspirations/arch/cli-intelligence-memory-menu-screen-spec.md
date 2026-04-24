# CLI-Intelligence  
## Exhaustive Technical Specification for Memory UX, Menu Reorganization, and New Screens

**Audience:** developers and maintainers  
**Scope:** terminal UI architecture, memory control surfaces, model assignment visibility, maintenance actions, and root menu redesign  
**Target stack:** .NET 10, C#, Spectre.Console, Windows-first terminal UX

---

# 1. Executive Summary

The current application already contains substantial memory-related infrastructure, but the UI does not expose it coherently enough.

At present, memory behavior is split across:

- configuration in `AppConfig`
- startup bootstrapping in `Program.cs`
- runtime knowledge synchronization and storage mapping
- extraction pipeline settings
- heartbeat maintenance settings
- dreaming and memory flush services
- individual screens such as history, settings, memory status, knowledge editor, local model settings, and log management

The result is functional but operationally opaque.

The application now needs a dedicated **memory-first control surface** and a root menu structure that reflects the actual architecture:

1. assistant interaction
2. memory and self-improvement
3. capabilities and extensibility
4. system and server operations

This document proposes:

- a new root menu organization
- a complete “Brain & Memory” area
- specific new screens to add
- which existing screens should move
- which settings should be editable from UI
- how model assignment should be made visible
- how manual triggers for maintenance should work
- a recommended implementation order

---

# 2. Current Architecture Observed in Code

The codebase already supports the following relevant components.

## 2.1 Configuration sections already present

The application configuration already contains:

### `AppSection`
- app name
- user name
- default shell
- default OS
- default output style
- history enabled

### `OpenRouterSection`
- API key
- remote model
- verbosity
- cache enabled

### `ExtractionSection`
- enabled
- model
- confidence threshold
- use local
- flush threshold

### `HeartbeatSection`
- enabled
- run on startup
- decay interval days
- stale threshold days
- model

### `ServerSection`
- URL
- service name
- version

### `LlamaSection`
- enabled
- URL
- model
- context length
- temperature
- top-p
- max tokens
- timeout seconds

This means the app already has enough configuration surface to support a very capable memory administration UI.

---

## 2.2 Runtime memory-related services already wired

The application bootstrap currently creates and wires:

- `LocalKnowledgeService`
- `RegexRegistry`
- `ReminderService`
- `KnowledgeExtractionPipeline`
- `WarmMemoryResolver`
- `MemoryFlushService`
- `HeartbeatService`
- `DreamingService`
- `PromotionService`

This is the backbone of the memory and self-improvement system.

The UI should now expose these systems explicitly.

---

## 2.3 Existing memory-related startup behavior

The application already performs startup work that is highly relevant to UX:

- synchronizes storage files into runtime knowledge
- seeds first-start memory into `MEMORIES.md`
- maps specific storage files into logical sections
- resolves workspace storage directory
- supports heartbeat CLI trigger
- supports dreaming CLI trigger
- supports memory status CLI trigger

This indicates the memory architecture is no longer incidental. It is central enough to deserve a dedicated navigation model.

---

## 2.4 Existing screens already relevant

The following existing screens are directly relevant to the reorganization:

- `RootMenuScreen`
- `AskIntelligenceScreen`
- `ChatSessionScreen`
- `ExplainCommandScreen`
- `TranslateScreen`
- `HistoryScreen`
- `KnowledgeEditorScreen`
- `MemoryStatusScreen`
- `ToolsScreen`
- `SettingsScreen`
- `ServerScreen`
- `LocalModelSettingsScreen`
- `LogManagementScreen`
- `HelpScreen`
- `FileBrowserScreen`

These are sufficient to bootstrap a much stronger information architecture without requiring a full rewrite.

---

# 3. Problem Statement

The current UI has three structural problems:

## 3.1 Memory is operationally important but visually buried
The code already contains extraction, heartbeat, flush, dreaming, reminders, and knowledge file synchronization. However, the UI does not make these concerns legible as one coherent area.

## 3.2 Capabilities are mixed with settings
Tool registry, skill importing, model controls, and server controls are not all system settings in the same sense. Some are runtime capabilities, some are infrastructure controls, some are assistant-facing features.

## 3.3 The user cannot inspect “who does what”
The code supports multiple AI roles:

- main assistant interaction
- extraction
- heartbeat
- dreaming
- memory flush
- local model test

But there is no single screen that clearly answers:

- which model is used for chat
- which model is used for extraction
- which model is used for heartbeat
- whether extraction is local or remote
- whether local model infrastructure is healthy

This creates unnecessary ambiguity.

---

# 4. Design Goal

The application should feel like an **inspectable operator console**, not just a terminal chat interface.

A user should be able to do all of the following without reading code or CLI flags:

- talk to the assistant
- inspect memory state
- understand which files shape behavior
- configure memory extraction and maintenance
- manually trigger maintenance passes
- review proposed memory changes
- inspect loaded tools and imported skills
- configure local model infrastructure
- operate the local HTTP server
- inspect logs

This is the target UX standard.

---

# 5. Proposed Root Menu Reorganization

The root menu should be reorganized into **four top-level domains**.

---

## 5.1 Assistant

This section contains direct user-to-AI interaction flows.

### Entries
1. Talk
2. Ask a Question
3. Explain Something
4. Translate Tools

### Mapping
- `Talk` → `ChatSessionScreen`
- `Ask a Question` → `AskIntelligenceScreen`
- `Explain Something` → `ExplainCommandScreen`
- `Translate Tools` → `TranslateScreen` or a translation submenu

### Rationale
These are the primary daily-use actions and should be grouped tightly.

---

## 5.2 Brain & Memory

This section becomes the primary UX home of the self-improving architecture.

### Entries
1. Brain & Memory Dashboard
2. Memory Status & Dashboard
3. Memory Files
4. Knowledge Editor
5. Review Dreams
6. Run Heartbeat Maintenance
7. Chat History
8. Scheduled Tasks & Reminders

### Notes
Some of these entries can map to new screens, while others can initially wrap existing screens or services.

### Rationale
This domain groups all “what the app knows”, “how it learns”, and “what maintenance it performs”.

---

## 5.3 Capabilities

This section separates extensibility and available tools from generic settings.

### Entries
1. Tool Registry
2. Import Skill from ZIP
3. HTTP Server
4. Test Local Model

### Mapping
- `Tool Registry` → `ToolsScreen`
- `Import Skill from ZIP` → existing import flow
- `HTTP Server` → `ServerScreen`
- `Test Local Model` → dedicated action or screen

### Rationale
These are operational capabilities, not generic preferences.

---

## 5.4 System

This section is for environment, configuration, diagnostics, and infrastructure.

### Entries
1. Memory Behavior Settings
2. Model Routing & Assignment
3. Local Model Settings
4. App Settings
5. Log Management
6. Help
7. Exit

### Mapping
- `Memory Behavior Settings` → new screen
- `Model Routing & Assignment` → new screen
- `Local Model Settings` → existing screen
- `App Settings` → existing screen
- `Log Management` → existing screen

### Rationale
This separates system-level controls from day-to-day assistant use.

---

# 6. New Screens to Add

This section describes the new screens that should be added.

---

## 6.1 `BrainMemoryDashboardScreen`

### Purpose
Central hub for memory visibility, maintenance visibility, and quick actions.

### Why it is needed
The app already has memory settings, memory files, heartbeat, dreams, and status logic, but no single operator dashboard.

### Primary content blocks

#### Block A — Current State Summary
Display:
- extraction enabled
- extraction model
- extraction uses local or remote
- flush threshold
- heartbeat enabled
- heartbeat model
- heartbeat run on startup
- heartbeat decay interval days
- heartbeat stale threshold days
- local model enabled
- local model URL
- current default remote model

#### Block B — Memory File Summary
Display counts and rough sizes for:
- `MEMORIES.md`
- `LESSONS.md`
- `USER.md`
- `SOUL.md`
- `LIMITS.md`
- `SYSTEM-PROMPTS.md`
- `SYSTEM-REGEX.md`
- `AGENTS.md`
- `DREAMS.md` if present

#### Block C — Maintenance Summary
Display:
- last heartbeat run time
- last dreaming run time
- pending dream proposals count
- reminders count
- whether startup heartbeat is enabled

#### Block D — Quick Actions
Actions:
- open memory settings
- open memory files
- open knowledge editor
- run heartbeat now
- run dreaming now
- open history
- test local model

### Navigation behavior
This should be reachable directly from the root menu.

### Initial implementation approach
Can initially be read-only plus quick action buttons. Editable configuration can stay in child screens.

---

## 6.2 `MemoryBehaviorSettingsScreen`

### Purpose
Dedicated editor for all memory-related behavioral settings.

### Settings to expose

#### Extraction
- Extraction Enabled
- Extraction Model
- Extraction Confidence Threshold
- Use Local for Extraction
- Flush Threshold

#### Heartbeat
- Heartbeat Enabled
- Heartbeat Run On Startup
- Heartbeat Decay Interval Days
- Heartbeat Stale Threshold Days
- Heartbeat Model

### Recommended UX controls

#### Boolean values
Use:
- toggle prompt
- yes/no selection

#### Numeric values
Use:
- validated numeric prompt
- safe ranges
- help text

#### Model values
Use:
- free-text entry initially
- optionally later a model catalog selector

### Validation rules
- confidence threshold must be within a sane range, ideally `0.0` to `1.0`
- flush threshold should be `>= 0`
- decay interval and stale threshold should be `>= 1`
- model values cannot be empty if the associated subsystem is enabled

### Persistence
Changes should update `session.Config` and call `session.SaveConfig()`.

### Important note
This screen should not also include server or local model configuration. Keep it narrowly focused.

---

## 6.3 `MemoryModelRoutingScreen`

### Purpose
Explain and display which model is responsible for which subsystem.

### Why it is needed
The code currently makes model assignment decisions at bootstrap time, but the user has no direct visibility.

### Screen content

#### Section A — Assistant Interaction
Display:
- remote provider model from `OpenRouterSection.Model`
- whether the runtime routing policy may choose local vs remote
- whether local llama is enabled

#### Section B — Extraction
Display:
- extraction model
- `UseLocal` flag
- effective extraction route:
  - local llama if `Extraction.UseLocal == true` and llama enabled
  - otherwise dedicated OpenRouter extraction model

#### Section C — Heartbeat
Display:
- heartbeat model
- execution path
- whether it is currently using remote-only logic

#### Section D — Memory Flush
Display the effective flush model selection logic clearly.

#### Section E — Dreaming
Display the dreaming model source and any coupling to flush model logic.

### Optional action buttons
- edit memory behavior settings
- open local model settings
- test local model
- open main app settings

### Important output requirement
This screen must explain the effective model logic in plain technical language, not just dump config fields.

---

## 6.4 `DreamsReviewScreen`

### Purpose
Make dreaming proposals visible and reviewable.

### Why it is needed
The code already supports `DreamingService`, but the UX currently relies on CLI flags and file review.

### Minimum viable version
Provide:
- path to `DREAMS.md`
- count of proposals if easily derivable
- option to open the file
- option to refresh view
- option to run dreaming now

### Better version
List proposals as entries:
- title or first line
- category
- proposed destination
- created time if available

Then allow actions:
- promote
- reject
- postpone
- open raw file

### Implementation note
If proposal parsing is not yet available, start with a structured viewer around the raw file and improve later.

---

## 6.5 `MemoryFilesExplorerScreen`

### Purpose
Provide explicit visibility into the memory-defining markdown files.

### Files to include
- `MEMORIES.md`
- `LESSONS.md`
- `USER.md`
- `SOUL.md`
- `LIMITS.md`
- `SYSTEM-PROMPTS.md`
- `SYSTEM-REGEX.md`
- `AGENTS.md`
- `DREAMS.md` if present

### For each file show
- logical category
- physical path
- size in bytes / KB
- estimated token count
- last modified time
- whether the file is conceptually HOT/WARM/COLD when applicable

### Actions
- open in viewer
- edit
- refresh metadata
- inspect path

### Rationale
This makes the knowledge architecture tangible to the user.

---

## 6.6 `ScheduledTasksScreen`

### Purpose
Expose reminders and recurring maintenance visibility.

### Current conceptual sources
The app already includes `ReminderService`, timers, and maintenance services. Even without a full scheduler UI, the user should see what is pending.

### Show
- reminder count
- timer/reminder entries
- whether heartbeat on startup is enabled
- whether recurring maintenance exists
- recent maintenance timestamps if available

### Actions
- open reminders
- run heartbeat
- run dreaming
- clear completed reminders if such logic exists
- return to dashboard

### Initial implementation note
This can begin as a simple status screen, then evolve.

---

## 6.7 `TestLocalModelScreen` or dedicated action wrapper

### Purpose
Give “Test Local Model” a consistent home in UI.

### Current state
You already have logic for `RunTestLocalModelAsync`.

### Recommendation
Either:
- create a dedicated `TestLocalModelScreen`, or
- wrap the existing action with a proper menu entry and consistent return flow

### Why
This action is important enough to be directly accessible from the root menu and from memory/model routing screens.

---

# 7. Existing Screens to Reuse or Move

---

## 7.1 `MemoryStatusScreen`
### Recommendation
Keep it, but do not treat it as the only memory UI.
It should become one child entry under **Brain & Memory**.

---

## 7.2 `KnowledgeEditorScreen`
### Recommendation
Keep it and expose it more prominently under **Brain & Memory**.

Also consider a future enhancement where the editor opens with a file preselected from `MemoryFilesExplorerScreen`.

---

## 7.3 `HistoryScreen`
### Recommendation
Move its navigation entry under **Brain & Memory** rather than leaving it as a generic root item.

History is part of operational memory visibility.

---

## 7.4 `ToolsScreen`
### Recommendation
Move it under **Capabilities** and rename the menu entry to **Tool Registry**.

---

## 7.5 `SettingsScreen`
### Recommendation
Narrow its role to true application settings, not everything technical.

---

## 7.6 `LocalModelSettingsScreen`
### Recommendation
Keep it under **System** and link to it from:
- Model Routing
- Brain & Memory Dashboard
- Test Local Model

---

## 7.7 `ServerScreen`
### Recommendation
Move under **Capabilities** if you want “things the app can do”, or under **System** if you want “technical infrastructure”.
Preferred placement: **Capabilities**, because the user actively operates it.

---

## 7.8 Skill import flow
### Recommendation
Move out of broad settings and place under **Capabilities**.

---

# 8. Memory Settings the UI Should Allow Changing

This section lists the exact memory-related parameters that should become editable.

---

## 8.1 Extraction settings

### `Extraction.Enabled`
- type: boolean
- effect: turns AI-assisted extraction on or off

### `Extraction.Model`
- type: string
- effect: remote extraction model when local extraction is not used

### `Extraction.ConfidenceThreshold`
- type: double
- effect: minimum confidence required before extracted items are accepted

### `Extraction.UseLocal`
- type: boolean
- effect: routes extraction through local llama when possible

### `Extraction.FlushThreshold`
- type: integer
- effect: controls automatic in-session flush timing

---

## 8.2 Heartbeat settings

### `Heartbeat.Enabled`
- type: boolean
- effect: enables or disables heartbeat maintenance logic

### `Heartbeat.RunOnStartup`
- type: boolean
- effect: allows automatic heartbeat pass at startup

### `Heartbeat.DecayIntervalDays`
- type: integer
- effect: minimum spacing between automatic heartbeat passes

### `Heartbeat.StaleThresholdDays`
- type: integer
- effect: defines when lessons/corrections are stale

### `Heartbeat.Model`
- type: string
- effect: model used for heartbeat AI analysis

---

## 8.3 Local-model-adjacent settings that affect memory behavior

These belong primarily in local model settings, but must be visible from memory-related screens because they change effective routing.

### `Llama.Enabled`
### `Llama.Url`
### `Llama.Model`
### `Llama.ContextLength`
### `Llama.Temperature`
### `Llama.TopP`
### `Llama.MaxTokens`
### `Llama.TimeoutSeconds`

---

# 9. Manual Triggers the UI Should Surface

The following should be first-class UI actions.

---

## 9.1 Run Heartbeat Now
Wrap the existing heartbeat maintenance call in a screen-accessible action.

### Recommended behavior
- show confirmation
- run with status spinner
- show result summary
- return to dashboard or keep on result screen

---

## 9.2 Run Dreaming Now
Wrap the existing dreaming flow in a screen-accessible action.

### Recommended behavior
- show confirmation
- run with spinner
- show proposal count
- offer jump to dreams review

---

## 9.3 Test Local Model
Already implemented as logic.
Expose more prominently.

---

## 9.4 Open Memory Status
Quick jump into detailed status.

---

## 9.5 Open Memory Files
Quick jump into explorer.

---

# 10. Recommended Root Menu Example

Below is the recommended target structure.

```text
CLI-Intelligence

Assistant
  [1] Talk
  [2] Ask a Question
  [3] Explain Something
  [4] Translate Tools

Brain & Memory
  [5] Brain & Memory Dashboard
  [6] Memory Status
  [7] Memory Files
  [8] Knowledge Editor
  [9] Review Dreams
  [10] Run Heartbeat Maintenance
  [11] Chat History
  [12] Scheduled Tasks & Reminders

Capabilities
  [13] Tool Registry
  [14] Import Skill from ZIP
  [15] HTTP Server
  [16] Test Local Model

System
  [17] Memory Behavior Settings
  [18] Model Routing & Assignment
  [19] Local Model Settings
  [20] App Settings
  [21] Log Management
  [22] Help
  [23] Exit
```

---

# 11. Detailed Recommended Navigation Model

Use a data-driven menu architecture.

## 11.1 Suggested models

### `MenuCategory`
Properties:
- `Id`
- `Title`
- `Description`
- `Color`
- `Items`

### `MenuItem`
Properties:
- `Id`
- `Number`
- `Title`
- `Description`
- `CategoryId`
- `ActionType`
- `TargetScreenFactory`
- `Callback`
- `IsEnabled`
- `StatusBadge`

### `MenuActionType`
Examples:
- `Navigate`
- `Execute`
- `OpenSubmenu`
- `Exit`

### Optional: `MenuStatusBadge`
Examples:
- `Heartbeat On`
- `3 Dreams`
- `Local Enabled`
- `Extraction Local`

---

## 11.2 Why data-driven is important
This prevents:
- hardcoded root menu sprawl
- repeated switch logic
- inconsistent descriptions
- fragile reorderings

It also makes future memory/plugin additions much easier.

---

# 12. Suggested Behavior for the Dashboard Description Area

Every menu item should have a longer technical description shown below the menu.

Examples:

### Brain & Memory Dashboard
View the current memory architecture at a glance, including extraction settings, active models, maintenance status, and quick links to files, dreams, and manual actions.

### Memory Behavior Settings
Configure extraction, flush, and heartbeat behavior. These settings control how knowledge is captured, filtered, compacted, and maintained over time.

### Model Routing & Assignment
Inspect which AI model is responsible for each subsystem, including assistant replies, extraction, heartbeat analysis, and local inference paths.

### Review Dreams
Inspect AI-generated proposals before they become part of long-term behavior. This keeps self-improvement transparent and auditable.

This matters because the app is becoming operationally richer.

---

# 13. Technical Implementation Notes

---

## 13.1 Persistence model
All editable settings should mutate `session.Config` and persist through `session.SaveConfig()`.

Do not duplicate persistence logic in each screen.

---

## 13.2 Service invocation
For manual actions:
- use the existing services already constructed in `AppSession`
- do not instantiate duplicate services inside UI screens

The screens should use:
- `navigator.Session.HeartbeatService`
- existing dreaming entry logic or a service reference if added to session
- `navigator.Session.MemoryFlushService`
- `navigator.Session.ReminderService`
- `navigator.Session.Knowledge`

If `DreamingService` is not stored in `AppSession` today, consider adding it there for symmetry.

---

## 13.3 Strong recommendation: add missing service references to `AppSession`
If you want the UI to control memory subsystems cleanly, consider storing these as session properties if not already present:

- `DreamingService`
- `PromotionService`
- optionally a small memory dashboard aggregation service

This reduces awkward duplication of bootstrapping logic.

---

## 13.4 View-model or helper layer
To keep screens clean, add helper classes for dashboard data assembly.

Suggested helper:
- `MemoryDashboardBuilder`

Potential output model:
- `MemoryDashboardViewModel`

Containing:
- config summary
- file metadata
- maintenance summary
- model routing summary
- quick action counts

This prevents bloated screen files.

---

# 14. Optional but Strongly Recommended Enhancements

---

## 14.1 Add last-run metadata files
Store maintenance metadata such as:
- last heartbeat run timestamp
- last dreaming run timestamp
- last flush timestamp

This makes dashboards much more useful.

---

## 14.2 Add dream proposal metadata
If not already present, structure dreams so the UI can parse proposal units rather than showing a raw file only.

---

## 14.3 Add memory file metadata viewer
You already have `MemoryFileMetadata`.
Use it more visibly in UI to classify files as:
- hot
- warm
- cold

This would strengthen the “Brain & Memory” concept.

---

## 14.4 Add status badges to root menu
Examples:
- `Review Dreams [3]`
- `Scheduled Tasks [2]`
- `Memory Status [HOT: 5 | WARM: 3]`

Only do this if it remains clean.

---

# 15. Recommended Delivery Order

Implement in this order.

## Phase 1 — Highest value
1. Root menu reorganization
2. `BrainMemoryDashboardScreen`
3. `MemoryBehaviorSettingsScreen`
4. Move Tool Registry and Skill Import into Capabilities
5. Promote Local Model Settings and Test Local Model visibility

## Phase 2 — Memory transparency
6. `MemoryFilesExplorerScreen`
7. `MemoryModelRoutingScreen`
8. `DreamsReviewScreen`

## Phase 3 — Operational maturity
9. `ScheduledTasksScreen`
10. last-run metadata tracking
11. badge/status enhancements
12. dream proposal parsing and promote/reject UX

---

# 16. Acceptance Criteria

A satisfactory implementation should meet all of the following:

## Root menu
- grouped into Assistant, Brain & Memory, Capabilities, System
- no important memory controls buried in generic settings
- HTTP server and local model testing easy to reach

## Brain & Memory area
- user can see current memory behavior
- user can inspect memory-defining files
- user can trigger heartbeat manually
- user can inspect or review dreams
- user can access history

## Configuration
- extraction settings editable
- heartbeat settings editable
- local model settings still accessible
- changes persist to config correctly

## Model clarity
- user can tell which model is responsible for each subsystem
- local vs remote behavior is legible

## Architecture quality
- screens are modular
- menu is data-driven
- no large monolithic switch expansion
- existing services are reused cleanly

---

# 17. Final Recommendation

Do not treat memory as just another submenu.

In this codebase, memory is now an operational subsystem with:

- ingestion
- routing
- maintenance
- auditing
- proposals
- persistence
- manual and automatic behavior

The UI should reflect that.

The correct direction is:

- make memory visible
- make model assignment explicit
- make maintenance actions accessible
- separate assistant usage from system and extensibility concerns
- turn the application into a terminal operator console with auditable intelligence

That is the architectural direction this specification recommends.
