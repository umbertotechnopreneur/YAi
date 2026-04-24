# CLI-Intelligence Menu Refactor Addendum  
## Visibility for Memory, Scheduled Tasks, Capabilities, and System Areas

This addendum complements the main menu refactor prompt and adds a second layer of architectural guidance.

The core issue is not only menu fragmentation. It is also lack of visibility into important internal subsystems, especially:

- memory state
- scheduled and maintenance operations
- self-improvement flows
- tool registry and extensibility
- local model and server controls

At the moment, some important capabilities are either buried or unevenly placed. For example, items such as skill importing and local model configuration are hidden under broader settings paths, while HTTP server access is surfaced directly. At the same time, the newer self-improving architecture introduces concepts like heartbeat maintenance, dreaming proposals, memory tiers, and auditable knowledge files, but the current root navigation does not make them visible enough.

This addendum proposes a more architectural menu organization.

---

## Objective

Restructure the root navigation so the application reflects its real operating model.

The new design should expose four clear domains:

1. **Chat & Tasks**
2. **Brain & Memory**
3. **Capabilities**
4. **System & Server**

This structure should make the application more intuitive for daily use while also making the OpenClaw-inspired memory model transparent and auditable.

---

## Proposed top-level information architecture

### 1. Chat & Tasks
This section groups the primary assistant interactions.

Suggested entries:

1. Talk (Interactive Chat)
2. Ask a Question
3. Explain Command
4. Translate Text

#### Expected mapping
- **Talk (Interactive Chat)** → `ChatSessionScreen`
- **Ask a Question** → `AskIntelligenceScreen`
- **Explain Command** → `ExplainCommandScreen`
- **Translate Text** → `TranslateScreen`

#### Reasoning
These are the daily, front-of-house actions. They are the fastest path to user value and should remain immediate, coherent, and easy to scan.

---

### 2. Brain & Memory
This section makes the self-improving architecture visible.

Suggested entries:

1. Memory Status & Dashboard
2. Review Dreams (Proposals)
3. Knowledge Editor
4. Run Heartbeat Maintenance
5. Chat History

#### Expected mapping
- **Memory Status & Dashboard** → `MemoryStatusScreen`
- **Review Dreams (Proposals)** → direct view into `DREAMS.md` or a dedicated dreams review flow
- **Knowledge Editor** → `KnowledgeEditorScreen`
- **Run Heartbeat Maintenance** → manual trigger for `HeartbeatService`
- **Chat History** → `HistoryScreen`

#### Reasoning
The OpenClaw-style design depends on visible, inspectable memory rather than hidden state. If the assistant evolves, the user must be able to inspect what changed, where it came from, and whether it should be promoted, corrected, compacted, or rejected.

This section should make the following concepts explicit:

- HOT memory
- WARM memory
- COLD or archival context where applicable
- learned lessons
- promoted corrections
- pending dreams or proposals
- knowledge file editing
- maintenance operations

This is not just a technical section. It is a trust section.

---

### 3. Capabilities
This section is for extensibility and operational tools.

Suggested entries:

1. Tool Registry
2. Import Skill from ZIP

#### Expected mapping
- **Tool Registry** → `ToolsScreen`
- **Import Skill from ZIP** → current import flow using `FileBrowserScreen`

#### Reasoning
Tool discovery and skill importing are not generic settings. They are about what the application can do. Grouping them under Capabilities makes the system easier to understand and scales better as more skills and built-in tools are added.

This section is where future extensibility should live, including:

- built-in C# tools
- workspace skills
- bundled skills
- future OpenClaw-style plugin packs
- optional feature modules

---

### 4. System & Server
This section is for technical configuration, infrastructure, and diagnostics.

Suggested entries:

1. HTTP Server
2. Local Model (Llama) Settings
3. App Settings
4. Log Management

#### Expected mapping
- **HTTP Server** → `ServerScreen`
- **Local Model (Llama) Settings** → `LocalModelSettingsScreen`
- **App Settings** → `SettingsScreen`
- **Log Management** → `LogManagementScreen`

#### Reasoning
This is the correct home for infrastructure-oriented controls. These items are operational and system-facing rather than conversational or memory-facing.

This section should hold:

- local server controls
- endpoint configuration
- local inference configuration
- diagnostics
- logging
- environment-level settings

---

## Scheduled tasks and maintenance visibility

A major current gap is visibility into scheduled or recurring operations.

The menu should expose the existence of background or deferred maintenance concepts, even if the app is not yet running a full scheduler UI.

At minimum, the user should be able to see or access:

- heartbeat readiness or last run time
- dreaming proposals status
- whether reminders or timers exist
- whether maintenance is manual only or partially automated
- which operations can be triggered safely from the UI

### Suggested improvements
Add a small status area, badge, or dashboard summary under the **Brain & Memory** section or inside **Memory Status & Dashboard** that includes:

- last heartbeat run
- number of pending dreams
- count of active reminders
- number of loaded hot memory files
- number of warm project/domain files available
- current local vs remote routing mode if useful

This can start simple. The important part is visibility.

---

## Why this structure better matches the architecture

### 1. Auditable intelligence
The self-improving system must remain inspectable. A dedicated **Brain & Memory** area makes the assistant’s learning model visible to the user instead of burying it behind CLI flags or scattered files.

### 2. Reduced menu ambiguity
The current structure mixes concepts from different domains. Importing a skill, testing a local model, editing knowledge, and viewing history are not the same category of action. Separating them reduces friction and makes intent clearer.

### 3. Better scaling
As new features arrive, they can fit naturally into one of the four domains without flattening the root menu again.

Examples:
- more memory viewers → Brain & Memory
- more imported skills or registries → Capabilities
- more server, model, or log features → System & Server
- more user-facing AI workflows → Chat & Tasks

### 4. Stronger mental model
The user should be able to answer these questions instantly:
- Where do I talk to the assistant?
- Where do I inspect what it learned?
- Where do I add or inspect tools?
- Where do I control the system itself?

This four-part structure answers those questions directly.

---

## Recommended UI treatment

If you apply this addendum, the root menu should visually separate the four domains with clear headers and subtle spacing.

Suggested section headers:

- **Chat & Tasks**
- **Brain & Memory**
- **Capabilities**
- **System & Server**

Each section can keep numeric shortcuts at the top level or expose a two-stage navigation:

- select category
- select entry inside category

Either approach is acceptable, provided it remains fast and keyboard-friendly.

### Important UX note
Do not hide memory visibility behind a generic label alone. The menu should clearly suggest that the user can inspect learned material, proposals, and maintenance state.

For example, **Memory Status & Dashboard** is much better than a vague **Memory** label.

---

## Guidance for the coding agent

If you implement this addendum, update the root navigation model so it supports category-based grouping cleanly.

A good model could include:

- `MenuCategory`
- `MenuItem`
- `MenuAction`
- optional `MenuStatusBadge`

Each category should support:

- title
- description
- color
- child items

Each item should support:

- label
- description
- target screen or callback
- optional status text or badge
- numeric shortcut where relevant

---

## Implementation priorities

If the full architecture cannot be done in one pass, prioritize in this order:

1. Create the four categories
2. Move memory-related functionality into **Brain & Memory**
3. Move skill importing into **Capabilities**
4. Move local model and logs into **System & Server**
5. Add manual heartbeat trigger
6. Add dreams visibility
7. Add dashboard-style status summaries

---

## Final design intent

This application is not only a terminal chat app anymore. It is becoming an inspectable, self-improving operator console.

The root menu should reflect that evolution.

The user must be able to:
- chat with the assistant
- inspect and govern memory
- manage tools and extensibility
- control local infrastructure and system behavior

That is the design standard this addendum is proposing.
