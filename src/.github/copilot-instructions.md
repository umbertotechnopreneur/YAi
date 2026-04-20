# YAi! - Copilot Instructions

YAi! is currently a .NET 10 solution built around a single ASP.NET Aspire orchestrator. The active workspace contains:

- `YAi.Services/`: Aspire AppHost and orchestration entry point.
- `YAi.Services.Core/`: ASP.NET Core Web API service.
- `YAi.Services.Defaults/`: shared Aspire defaults for service discovery, health checks, resilience, and OpenTelemetry.

Planned additions include a terminal UI app built with `Terminal.Gui` and a frontend built with Vue 3, Vuetify, and Vite.

`Terminal.Gui` is a cross-platform .NET library for building rich terminal user interfaces (TUIs). It provides high-level controls (windows, dialogs, lists, text fields, menus) and handles input, layout, and rendering across Windows, macOS, and Linux terminals. We use it for interactive CLI experiences where a graphical terminal layout and keyboard-driven navigation improve usability without requiring a full graphical frontend. (https://github.com/gui-cs/Terminal.Gui)

For designing complex layouts visually, we also reference `TerminalGuiDesigner` — a companion tool that lets developers craft views interactively and export layouts compatible with `Terminal.Gui`. It speeds UI iteration and helps onboard designers to the terminal UX workflow. (https://github.com/gui-cs/TerminalGuiDesigner)

Use this file as the primary repository-specific instruction set for AI coding agents. Keep behavior concise, durable, and aligned with the current workspace.

---

## 1. Response Style

- Be concise and direct.
- Prefer making the change over describing the change.
- Ask clarifying questions only when the requirement is genuinely ambiguous.
- Do not invent missing architecture, services, or folders.

---

## 2. Working Rules

### Plan and execution

- Use a plan for non-trivial tasks.
- Re-plan when new information invalidates the current approach.
- Keep changes minimal and local to the request.
- Solve root causes instead of adding temporary workarounds.

### Verification

- Verify with the smallest relevant check first.
- Run a targeted build when a .NET project, shared contract, or configuration file changes.
- If the user asks for tests, for now the available verification is that the application compiles successfully.
- Run targeted tests only when the repo later adds real test projects and they are relevant.
- Do not claim success without evidence.

### Task tracking

- Use the agent planning workflow provided by the host.
- When the user explicitly asks for agent-mode tracking artifacts, use `.github/tasks/todo.md` and `.github/tasks/lessons.md`.
- Do not create ad hoc task files unless the user explicitly asks for them.
- Do not assume `tasks/todo.md` or `tasks/lessons.md` exist.

---

## 3. Current Repo Map

### Orchestration

- `YAi.Services/` is the Aspire AppHost project.
- `AppHost.cs` is the orchestration entry point in the current repo layout.
- Keep service wiring and resource registration centralized in the AppHost project.

### Services

- `YAi.Services.Core/` hosts the current Web API.
- Prefer extending the existing service before inventing new backend projects unless the user asks for a new boundary.

### Shared defaults

- `YAi.Services.Defaults/` contains common hosting extensions.
- Reuse these defaults for service discovery, OpenTelemetry, health checks, and HTTP resilience.

### Planned clients

- Future CLI work should use `Terminal.Gui` for terminal UX.
- Future web frontend work should use Vue 3, Vuetify, and Vite.
- Keep frontend code isolated from backend projects rather than mixing Node assets into .NET service folders.

---

## 4. Architecture Guidance

### Aspire and service composition

1. Assume there is only one orchestrator unless the user explicitly asks to introduce more.
2. Reuse existing Aspire patterns before adding custom hosting abstractions.
3. Keep project references and service registration explicit and easy to review.

### API and backend

1. Follow existing ASP.NET Core patterns already present in `YAi.Services.Core/`.
2. Keep endpoints, contracts, and registrations straightforward.
3. Do not introduce unnecessary frameworks or layered abstractions.

### Logging and persistence

1. Use Serilog as the primary logging approach when structured logging is needed.
2. Use SQLite only for local or application log storage.
3. Do not treat SQLite log storage as the primary application database unless the user explicitly asks for that change.
4. Keep logging configuration centralized and avoid duplicating sink setup across projects.

### Terminal UI

1. For CLI graphics, prefer `Terminal.Gui` over custom console drawing. `Terminal.Gui` offers a mature set of controls and layout management that reduces low-level terminal handling, speeds development of interactive text UIs, and keeps the UX consistent across platforms.
2. Keep terminal UI code separate from Web API concerns and Aspire orchestration.

### Frontend

1. For frontend work, assume Vue 3 + Vuetify + Vite.
2. Preserve a clean separation between frontend build tooling and .NET backend projects.
3. Avoid introducing alternative frontend frameworks unless requested.

---

## 5. Configuration and Environment

1. Respect `appsettings.json` and `appsettings.Development.json` patterns already in the solution.
2. Do not hardcode secrets, connection strings, or machine-specific paths.
3. Prefer configuration-driven behavior over hidden defaults when adding new settings.
4. Keep development-only behavior scoped to development environments.

---

## 6. Code Formatting and Structure

Apply these rules to C# code unless the user explicitly asks for a different style or an existing file clearly requires compatibility with another pattern.

1. Put a space before `()` and `[]`. Write `Method ()` instead of `Method()` and `array [i]` instead of `array[i]`.
2. Use Allman braces. Every opening brace goes on the next line.
3. Add blank lines before `return`, `break`, and `continue`, and add blank lines after control blocks when it improves readability.
4. Do not use `var` except for `int`, `string`, `bool`, `double`, `float`, `decimal`, `char`, and `byte`.
5. Use target-typed `new ()` instead of `new TypeName ()` when the type is already clear.
6. Use collection expressions like `[...]` instead of `new () { ... }` where supported.
7. Use `SubView` and `SuperView` for containment relationships. Reserve `Parent` and `Child` for non-containment references.
8. For unused lambda parameters, use `_`. Example: `(_, _) => { }`.
9. Prefer early return and guard clauses. Invert conditions and return or continue early. Do not wrap the happy path in a conditional.
10. Keep one type per file. Public and internal types each get their own file.

File organization and regions

- Put `using` directives inside a dedicated region at the top of the file:

	#region Using directives

	// usings here

	#endregion

- Use similar regions for other logical file sections (fields, properties, constructor):

	#region Fields

	// private fields

	#endregion

	#region Properties

	// public/internal properties

	#endregion

	#region Constructor

	// ctor(s)

	#endregion

Namespace style

- Use the file-scoped (short) namespace declaration for all new code. Example:

	namespace my.name.space;

Cleanup and automated maintenance

- When you open a file to modify it, remove unused `using` directives, sort remaining `using`s, and run an IDE/code-cleanup pass if available (format, remove unreachable/useless code, apply simple refactors).
- If, while working, you spot improvements or dead/legacy code, perform the cleanup and small refactors in-place rather than leaving TODOs. Prefer safe, local refactors that improve clarity and remove duplication.

Refactoring and backward compatibility

- This codebase is a new product and does not require preserving backward compatibility. When cleanup or refactor work simplifies the code, remove old, unused, or compatibility-layer code rather than preserving it for historical reasons. Ensure tests or a targeted build verify the change when you modify behavior that other projects may rely on.

---

## 7. Documentation Rules

1. Update existing documentation when a code change makes it inaccurate.
2. Do not create new documentation trees or process documents unless requested.
3. Keep repository instructions durable and based on the real current structure of YAi!.

---

## 8. Source File Headers

Use the following banner standard when creating a new source or documentation file unless the user explicitly asks for a different header or the file is generated or third-party.

1. Apply the header only to newly created files. Do not retroactively add it to existing files unless requested.
2. Keep the first line as the project, module, or product name.
3. Keep the second line as a short description of the file.
4. Do not expand the header with internal metadata unless explicitly required.
5. Keep the wording consistent across repositories unless a stricter legal notice is required.

### C# Header

```csharp
/*
 * [Project or Module Name]
 * [Short file purpose or description]
 *
 * Copyright © 2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file may include content generated, refined, or reviewed
 * with the assistance of one or more AI models. It should be
 * reviewed and validated before external distribution or
 * operational use. Final responsibility remains with the
 * author(s) and the organization.
 */
```

### JavaScript / TypeScript Header

```javascript
/*
 * [Project or Module Name]
 * [Short file purpose or description]
 *
 * Copyright © 2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file may include content generated, refined, or reviewed
 * with the assistance of one or more AI models. It should be
 * reviewed and validated before external distribution or
 * operational use. Final responsibility remains with the
 * author(s) and the organization.
 */
```

### HTML / CSS Header

```html
<!--
	[Project or Module Name]
	[Short file purpose or description]

	Copyright © 2026 UmbertoGiacobbiDotBiz. All rights reserved.
	Website: https://umbertogiacobbi.biz
	Email: hello@umbertogiacobbi.biz

	This file may include content generated, refined, or reviewed
	with the assistance of one or more AI models. It should be
	reviewed and validated before external distribution or
	operational use. Final responsibility remains with the
	author(s) and the organization.
-->
```

### Documentation Header

```md
**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> [Document title go here] ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** [Add here the intent and/or description]  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.
```

---

## 9. Avoid

- References to old project names, domains, or business contexts not present in this repo.
- Invented microservice topologies.
- Entity Framework, migrations, or database conventions not requested by the user.
- Speculative refactors unrelated to the task.
- Verbose completion summaries.

---

## 10. Default Completion Standard

Before finishing, check:

- Is the change minimal?
- Is it aligned with the current YAi! structure?
- Is it verified with an appropriate local check?
- Would another maintainer understand why it was done?

If not, improve the change before finishing.
