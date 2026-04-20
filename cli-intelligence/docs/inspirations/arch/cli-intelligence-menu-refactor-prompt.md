# CLI-Intelligence Menu Refactor Prompt

## Purpose

This document is a ready-to-use prompt for an AI coding agent to refactor the main terminal menu of a .NET 10 CLI application built with Spectre.Console.

The goal is to reduce fragmentation, improve navigation speed, group related actions more clearly, add numeric shortcuts, introduce submenus where appropriate, and display a richer contextual description area beneath the menu.

---

## Prompt for the Coding Agent

You are working on a .NET 10 terminal application built with C# and Spectre.Console on Windows.

Your task is to refactor the current root menu into a cleaner, more structured, data-driven terminal UI.

### High-level objective

The current menu feels too fragmented and flat. It should be reorganized into logical groups with stronger hierarchy, faster access, and better visual scanning.

The new menu must:

- reduce cognitive load
- prioritize the most-used actions at the top
- support numeric shortcuts
- support submenus for grouped functionality
- use visual separators between menu groups
- use color coding by group
- reserve a description area below the menu
- make HTTP server and local model testing immediately accessible
- prepare the memory area for the newer storage files now present in the project

---

## New top-level menu structure

Use the following order exactly.

### Group 1 — Primary actions

These are the most important actions and must appear first.

1. Ask one question to "{AI_NAME}"
2. Talk with "{AI_NAME}"
3. Translate tools
4. Explain something
5. HTTP Server
6. Test local model

### Group 2 — Knowledge and memory

7. History
8. Memory Center
9. Lessons
10. Rules

### Group 3 — Configuration

11. Settings

### Group 4 — Closing utilities

12. Help
13. Exit

---

## Visual rules

### Colors by group

Use distinct but tasteful Spectre.Console styles.

- Items 1 to 4: pink
- Items 5 and 6: a slightly different pink or muted magenta
- Knowledge and memory group: light green
- Settings: amber or dark yellow
- Help and Exit: neutral grey or standard light tone

Do not make the screen noisy. The result should still feel clean and terminal-friendly.

### Separators

Add a separator between the major groups.

Expected group layout:

- primary actions
- separator
- knowledge and memory
- separator
- settings
- separator
- help and exit

A simple rule, subtle line, or spacing-based separator is acceptable.

---

## Navigation rules

### Numeric shortcuts

Every top-level menu entry must display a visible number.

Examples:

- [1] Ask one question to "{AI_NAME}"
- [2] Talk with "{AI_NAME}"
- [3] Translate tools
- [4] Explain something
- [5] HTTP Server
- [6] Test local model

Allow the user to activate entries both by:

- arrow keys + Enter
- direct numeric selection when possible

### Context description area

Below the menu, leave approximately 3 to 4 empty lines.

Then render a contextual description area that updates as the user moves across menu items.

This description must be more informative than the menu label. It should explain:

- what the entry does
- when to use it
- what kind of result to expect

Example:

**Ask one question to "{AI_NAME}"**  
Send a single focused question to the AI without starting a persistent conversation. Best for quick technical requests, command help, short diagnostics, or one-off clarifications.

The description area should feel stable and readable, not flickery or cramped.

---

## Submenu requirements

### Translate tools

"Translate tools" must open a dedicated submenu rather than acting as a single flat action.

Suggested submenu items:

1. Translate text
2. Translate clipboard
3. Translate file content
4. Back

You may adjust the exact entries if needed, but the submenu concept is mandatory.

### Memory Center

"Memory Center" must open a dedicated submenu that reflects the newer memory-related markdown files already present in the project.

The submenu should include logical entries derived from these files:

- Memories
- Lessons
- Rules & Taboos
- Agents
- Mandatory Context
- System Prompts
- System Regex
- User Context
- Soul
- Limits

The current project contains files such as:

- AGENTS.md
- LESSONS.md
- LIMITS.md
- MANDATORY-CONTEXT.md
- MEMORIES.md
- SOUL.md
- SYSTEM-PROMPTS.md
- SYSTEM-REGEX.md
- USER.md

Design this submenu so the user can enter a dedicated memory area and select the relevant file or logical category from there.

This submenu should be visually distinct and should align with the green knowledge/memory styling.

---

## Information architecture guidance

The existing flat menu should be replaced by a structured model.

Do not keep the menu as a long list of hardcoded strings and switch statements scattered across the screen logic.

Refactor toward a data-driven structure, for example:

- MenuSection
- MenuItem
- MenuNode
- MenuAction

Each menu item should be able to hold:

- display number
- title
- description
- style or color
- optional submenu children
- callback or navigation target
- enabled / disabled state
- optional hotkey metadata

This refactor should make it easy to add or reorder menu items later without rewriting screen logic.

---

## Functional mapping guidance

Map the existing app features into the new menu carefully.

Use the existing functionality already present in the project where applicable.

Known relevant areas include:

- single-question flow
- talk/chat session flow
- translation flow
- explain flow
- history screen
- settings screen
- HTTP server screen or command path
- local model test path
- knowledge or storage-backed memory files

The "Explain something" item should be a first-class entry in the top group and should not remain buried or inconsistent with the other main AI actions.

HTTP Server and Test local model must be easy to reach immediately from the root menu without having to enter settings first.

---

## UX requirements

### Naming consistency

Use cleaner labels than the current fragmented naming.

Preferred wording:

- Ask one question to "{AI_NAME}"
- Talk with "{AI_NAME}"
- Translate tools
- Explain something
- HTTP Server
- Test local model
- History
- Memory Center
- Lessons
- Rules
- Settings
- Help
- Exit

### Stability

The root screen should feel more composed than before.

Keep:

- the top branding
- the signature / history panel if appropriate
- the terminal-first feel

But improve the menu body so it does not look like an unstructured stack of unrelated commands.

### Density

Do not overpack the content. Spacing matters.

The menu should remain readable even on a standard Windows Terminal or PowerShell window.

---

## Implementation constraints

- Use C#
- Target .NET 10
- Use Spectre.Console
- Keep classes small and focused
- Prefer explicit logic over clever abstractions
- Avoid overengineering
- Keep keyboard interaction intuitive
- Preserve existing app behavior while improving menu structure
- Keep the code maintainable and production-friendly

---

## Suggested implementation approach

A strong solution would likely include:

1. A menu model for sections and items
2. A renderer that understands sections, separators, colors, numbering, and descriptions
3. A navigator that supports submenu traversal cleanly
4. A description provider or metadata attached directly to menu items
5. Centralized mapping from menu item to screen or action

You may reuse existing screens where possible, but the root menu composition should be redesigned rather than lightly patched.

---

## Deliverable expectations

Produce the actual refactor in code.

At minimum, the implementation should include:

- the new grouped root menu
- numeric top-level items
- the added "Explain something" entry in the top group
- HTTP Server as a direct root item
- Test local model as a direct root item
- Translate tools submenu
- Memory Center submenu
- a contextual description area below the menu
- cleaner internal menu architecture

If needed, introduce supporting classes rather than forcing everything into the current root menu screen.

---

## Final note

The result should feel like a professional terminal application with clearer hierarchy and faster access, not just a reordered list.

The design goal is not flashy UI. The goal is a sharper operational interface.
