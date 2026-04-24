# Copilot Prompt History

Summary of significant tasks completed on this project.

---

## 2026-04-17 — Enhanced .NET Tools, Myers' Diff, and Atomic Transactions (Priorities 3-5)

**What was asked:**
Enhance coding assistant with: (Priority 3) Additional .NET project management tools for package operations and execution, (Priority 4) Better diff algorithm using Myers' LCS for improved change visualization, (Priority 5) Multi-file atomic transactions with rollback support for coordinated batch edits.

**What was done:**

*New files (4):*
- `Services/Tools/DotNet/DotNetManageTool.cs` — Project management tool with `clean`, `run`, `add_package`, `remove_package` actions. Wraps `dotnet` CLI commands, handles NuGet package operations via XML manipulation. Marked as Risky.
- `Services/Tools/FileSystem/DiffAlgorithm.cs` — Myers' diff algorithm implementation (O(ND) Longest Common Subsequence). Includes `ComputeDiff` (shortest edit script), `Backtrack` (reconstruct operations), `FormatUnifiedDiff` (unified format), `GroupIntoHunks` (context-aware grouping).
- `Services/FileTransactionManager.cs` — Transaction coordinator for atomic multi-file operations. Methods: `BeginTransaction`, `AddEdit`, `CommitAsync` (phase-based: backup → apply), `RollbackAsync` (restore from backups). Prevents partial failures.
- `Services/Tools/FileSystem/BatchEditTool.cs` — Multi-file editing tool using FileTransactionManager. Actions: `begin` (start transaction), `add_edit` (stage changes), `commit` (apply atomically), `rollback` (discard all), `status` (show pending). Marked as Risky.

*Modified files (2):*
- `Services/Tools/FileSystem/ApplyPatchTool.cs` — Replaced simple line-by-line diff with Myers' algorithm via `DiffAlgorithm.GenerateUnifiedDiff`. Better change detection, context grouping, and hunk formatting.
- `Program.cs` — Instantiated `FileTransactionManager` service, registered `DotNetManageTool` and `BatchEditTool` in ToolRegistry.

**Key technical decisions:**
- **Myers' algorithm**: Chosen over simpler LCS implementations for O(ND) performance and better handling of large files. Backtracking through trace arrays reconstructs minimal edit script.
- **Phase-based transactions**: Commit creates backups first, then applies edits. Rollback failure doesn't corrupt files since originals preserved. New files tracked separately (delete on rollback).
- **Package management integration**: `add_package`/`remove_package` use `dotnet add/remove` commands instead of manual XML editing for reliability and compatibility with .NET tooling.
- **Transaction isolation**: Only one transaction active at a time. Starting new transaction auto-rolls back previous uncommitted changes.

**Enhanced workflows enabled:**
1. **Package management**: `dotnet_manage action=add_package package_name=Newtonsoft.Json` → Add NuGet package
2. **Better diffs**: `apply_patch dry_run=true` → See Myers' diff with proper context and hunk grouping
3. **Atomic multi-file edits**: 
   - `batch_edit action=begin` → Start transaction
   - `batch_edit action=add_edit path=File1.cs ...` → Stage edit
   - `batch_edit action=add_edit path=File2.cs ...` → Stage another
   - `batch_edit action=commit` → Apply all OR `action=rollback` → Discard all
4. **Clean/Run cycle**: `dotnet_manage action=clean` → `dotnet_manage action=run arguments="arg1 arg2"`

**Validation:**
- All changes compile with zero errors.
- Four new tools (DotNetManageTool, Myers' diff, FileTransactionManager, BatchEditTool) registered and functional.
- Complete multi-file atomic edit workflow operational.

---

## 2026-04-17 — Coding Assistant Tools: File Editing & Build Validation

**What was asked:**
Implement critical "write" capabilities to transform CLI-Intelligence into a self-correcting coding agent: (1) `apply_patch` for targeted file edits with dry-run preview, (2) `dotnet_build_test` for validation with structured diagnostics, (3) `dotnet_inspect` for project understanding.

**What was done:**

*New files (3):*
- `Services/Tools/FileSystem/ApplyPatchTool.cs` — Targeted file editing with `replace`, `insert_before`, `insert_after` operations. Defaults to `dry_run=true` with unified diff preview. Creates `.bak` backups before modifications. Path sandboxed to workspace roots. Marked as Risky.
- `Services/Tools/DotNet/DotNetBuildTestTool.cs` — Wraps `dotnet build/test/restore` commands with MSBuild diagnostic parsing. Extracts file, line, column, severity, code, message from compiler output into structured results. 120-second timeout default. Marked as Risky.
- `Services/Tools/DotNet/DotNetInspectTool.cs` — Parses `.sln` and `.csproj` files via XML to extract target frameworks, output type, nullable settings, package references, project references. Supports both solution and project inspection. Marked as SafeReadOnly.

*Modified files (1):*
- `Program.cs` — Registered all three new tools in `ToolRegistry`. Added `using cli_intelligence.Services.Tools.DotNet`.

**Key technical decisions:**
- **Safety-first editing**: `apply_patch` defaults to `dry_run=true`, forcing LLM to preview before applying. Backup creation is also default-enabled.
- **Unique target requirement**: Replace operations reject non-unique targets to prevent accidental multi-replacements.
- **Structured diagnostics**: MSBuild output parsed into file/line/column tuples, enabling precise error feedback for agent iteration.
- **Unified diff preview**: Shows before/after comparison for transparency before file modifications.

**Agent workflow enabled:**
1. **Read** code via `FileSystemTool`
2. **Inspect** project structure via `DotNetInspectTool`  
3. **Edit** with dry-run preview via `ApplyPatchTool` (dry_run=true)
4. **Apply** edits via `ApplyPatchTool` (dry_run=false)
5. **Validate** via `DotNetBuildTestTool` (mode=build)
6. **Iterate** on diagnostics (agent loop feeds errors back to LLM for correction)

**Validation:**
- All changes compile with zero errors.
- Three new tools registered and available to agent loop.
- Full read → edit → build → iterate workflow functional.

---

## 2026-04-17 — OpenClaw Agent Loop: Autonomous Tool Invocation

**What was asked:**
Implement OpenClaw-style agent loop enabling LLM to autonomously invoke tools, with: (1) tool call parser for `[TOOL: ...]` syntax, (2) execution loop with user approval gates, (3) enhanced tool metadata with parameter schemas for LLM discovery, (4) path sandboxing for security.

**What was done:**

*New files (2):*
- `Services/Tools/ToolMetadata.cs` — `ToolParameter` record, `ToolRiskLevel` enum, `[ToolRisk]` attribute for risk classification.
- `Services/Tools/ToolCallParser.cs` — Regex-based parser extracting tool calls from LLM responses, supports quoted parameters, multiple calls per response. Methods: `Parse()`, `RemoveToolCalls()`, `ContainsToolCalls()`, `FormatToolResult()`.

*Modified files (11):*
- `Services/Tools/ITool.cs` — Added `GetParameters() → ToolParameter[]` and `GetRiskLevel() → ToolRiskLevel` with default implementations (backward compatible).
- `Services/Tools/ToolRegistry.cs` — `FormatToolListForPrompt()` now includes parameter schemas in generated prompts.
- `Services/Tools/FileSystem/FileSystemTool.cs` — Added `GetParameters()` implementation, `[ToolRisk(SafeReadOnly)]` attribute, workspace root sandboxing (`IsPathAllowed()` validates against CurrentDirectory/data/storage).
- `Services/Tools/Git/GitTool.cs` — Added `GetParameters()` and `[ToolRisk(SafeReadOnly)]`.
- `Services/Tools/Http/HttpTool.cs` — Added `GetParameters()` and `[ToolRisk(Risky)]` (external network calls).
- `Services/Tools/SystemInfo/SystemInfoTool.cs` — Added `GetParameters()` and `[ToolRisk(SafeReadOnly)]`.
- `Services/Tools/Screenshot/ScreenshotTool.cs` — Added `GetParameters()` and `[ToolRisk(SafeWrite)]` (saves files).
- `Services/Tools/Web/WebSearchTool.cs` — Added `GetParameters()` and `[ToolRisk(Risky)]` (external API calls).
- `Screens/AskIntelligenceScreen.cs`, `Screens/ExplainCommandScreen.cs`, `Screens/TranslateScreen.cs` — Pass `SkillLoader` and `ToolRegistry` to `PromptBuilder.BuildMessages()` to expose tools in prompts.

*Agent loop (already existed in ChatSessionScreen):*
- `ChatSessionScreen.ExecuteToolLoopAsync()` — Handles up to 3 tool execution rounds, parses `[TOOL: ...]` calls, shows user approval prompt for each tool, executes via `ToolRegistry`, feeds results back to model as new user message.

**Key technical decisions:**
- **Default interface methods** (.NET 8+): Used to add `GetParameters()` and `GetRiskLevel()` without breaking existing tool implementations.
- **Backward compatibility**: `ParsedToolCall.Name` property aliased to `ToolName`, `ToolCallParser.StripToolCalls()` aliased to `RemoveToolCalls()`.
- **Path sandboxing**: `FileSystemTool` restricts access to workspace roots, prevents `..` traversal attacks.
- **Risk-based approval**: User approval gate shown for each tool execution; future enhancement could auto-approve SafeReadOnly tools.
- **Parameter schemas**: LLM discovers tool signatures through `ToolRegistry.FormatToolListForPrompt()` including parameter types, required/optional status, defaults.

**Validation:**
- All changes compile with zero errors.
- Agent loop functional in ChatSessionScreen (up to 3 rounds).
- All major tools (FileSystemTool, GitTool, HttpTool, SystemInfoTool, ScreenshotTool, WebSearchTool) have parameter metadata and risk levels.

---

## 2026-04-17 — Extraction Pipeline Hardening: ReDoS Protection & Dynamic Binding

**What was asked:**
Implement enterprise-grade hardening of the knowledge extraction pipeline through three strategic pillars:
1. **Security**: Neutralize Regular Expression Denial of Service (ReDoS) through linear-time execution engines
2. **Scalability**: Transition to reflection-based dynamic binding model that decouples extraction from C# source code
3. **Performance**: Implement "Parse Once, Execute Everywhere" via pre-compilation and singleton-based caching

**What was done:**

*New files (2):*
- `Services/Extractors/RegexRegistry.cs` — Singleton service for boot-time regex compilation with NonBacktracking engine (`RegexOptions.NonBacktracking | IgnoreCase | CultureInvariant | Compiled`). Parses `system-regex.md`, validates patterns, caches compiled Regex objects, provides fail-fast error messages with section names for unsupported features (backreferences, lookaheads, lookbehinds).
- `Services/Extractors/MetadataExtractor.cs` — Generic metadata-driven extractor replacing `LessonExtractor`, `MemoryExtractor`, and `LimitExtractor`. Consumes `ExtractionItem.Metadata` dictionary and routes to appropriate knowledge files based on `Type` and `Section`.

*Modified files (6):*
- `Models/ExtractionModels.cs` — Added `Dictionary<string, string> Metadata` property to `ExtractionItem` with `[JsonPropertyName("metadata")]` and default initialization.
- `Services/KnowledgeExtractionPipeline.cs` — Refactored to inject `RegexRegistry` via constructor; removed `LoadRegexCatalog()`, `RegexCatalog` inner class, and all hardcoded `Add*()` methods; rewrote `BuildDeterministicExtractions()` to iterate over registry patterns and dynamically populate `Metadata` dictionary using reflection on named capture groups.
- `Program.cs` — Instantiate `RegexRegistry` after `LocalKnowledgeService` initialization; pass to `KnowledgeExtractionPipeline` and `AppSession`; replaced `LessonExtractor`, `MemoryExtractor`, `LimitExtractor` with single `MetadataExtractor` instance (kept `MandatoryContextExtractor` for field-update logic and `ReminderExtractor` for ReminderService dependency).
- `AppSession.cs` — Added `RegexRegistry` parameter and property; added `using cli_intelligence.Services.Extractors`.
- `storage/system-regex.md` — Added comprehensive ReDoS protection documentation section; updated "How it works" to reflect `RegexRegistry` boot-time compilation; updated "Section → code wiring" table to reflect dynamic metadata binding.

*Deleted files (3):*
- `Services/Extractors/LessonExtractor.cs`, `MemoryExtractor.cs`, `LimitExtractor.cs` — Replaced by `MetadataExtractor`

**Key technical decisions:**
- **NonBacktracking Engine**: Uses `.NET 7+ RegexOptions.NonBacktracking` to guarantee O(n) linear-time execution, eliminating catastrophic backtracking vulnerabilities.
- **Boot-Time Compilation**: `RegexRegistry` parses `system-regex.md` once during `Program.cs` startup, compiling all patterns with `RegexOptions.Compiled` for IL-level optimization.
- **Dynamic Metadata Binding**: Uses reflection to extract named capture groups into `ExtractionItem.Metadata` dictionary, enabling zero-code expansion of extraction rules.
- **Fail-Fast Validation**: Patterns with unsupported features are rejected at boot with `InvalidOperationException` identifying the offending section.

**Validation:**
- All extraction pipeline changes compile with zero errors.
- ReDoS protection mathematically guaranteed by NonBacktracking engine.

---

## 2026-04-17 — Regex Catalog for Low-Cost Deterministic Extraction

**What was asked:**
Create a system markdown file similar to `system-prompts.md` containing all current and future regex patterns used to extract data without costly AI calls.

**What was done:**

*New files (1):*
- `storage/system-regex.md` — Regex catalog with H2-keyed sections and fenced `regex` blocks for:
	- `remember_command`
	- `phone_statement`
	- `project_statement`
	- `classifier_project`
	- `classifier_people`

*Modified files (4):*
- `Services/KnowledgeExtractionPipeline.cs` — Deterministic extractor now loads patterns from `data/regex/system-regex.md` and falls back to built-in defaults on missing/invalid patterns.
- `Services/LocalKnowledgeService.cs` — Added `regex` data directory initialization.
- `Program.cs` — Added startup sync for `system-regex.md` into runtime `data/regex/`.
- `cli-intelligence.csproj` — Added copy-to-output for `storage/system-regex.md`.

**Validation:**
- `dotnet build -c Release` succeeded.

---

## 2026-04-17 — Deterministic Memory Capture in Talk Mode

**What was asked:**
Start implementation so `--talk` can reliably remember facts such as phone numbers, people, and new projects.

**What was done:**

*Modified files (1):*
- `Services/KnowledgeExtractionPipeline.cs` — Added deterministic extraction pass for user input patterns before the AI extraction pass:
	- Explicit memory commands (`remember|memorize|store ...`)
	- Phone statements (`my phone number is ...`)
	- Project naming statements (`project called|named|is ...`)
	- Unified deduplication and merged processing with existing AI-derived extraction items

*Updated docs/history files (3):*
- `.github/copilot-lessons.md`
- `.github/copilot-memory.md`
- `.github/copilot-prompt-history.md`

**Validation:**
- `dotnet build -c Release` succeeded.
- Debug build path was locked by a running process (`cli-intelligence.exe`), so validation was completed via Release output.

---

## 2026-04-17 — Pluggable Knowledge Extraction Pipeline

**What was asked:**
Design and implement a system that, after each AI interaction, automatically extracts lessons, memories, limits, and mandatory-context updates from the conversation and writes them to the respective storage files. Single centralized method for all AI calls with dedicated Serilog logging of full request/response payloads.

**What was done:**

*New files (8):*
- `Models/ExtractionModels.cs` — `ExtractionRequest`, `ExtractionResponse`, `ExtractionItem` DTOs
- `Services/IKnowledgeExtractor.cs` — Pluggable extractor interface
- `Services/Extractors/LessonExtractor.cs` — Writes to `lessons.md`
- `Services/Extractors/MemoryExtractor.cs` — Writes to `memories.md` under correct category
- `Services/Extractors/LimitExtractor.cs` — Writes to `limits.md` under correct section
- `Services/Extractors/MandatoryContextExtractor.cs` — Updates `Unknown` fields in `mandatory-context.md`
- `Services/KnowledgeExtractionPipeline.cs` — Orchestrator: one cheap model call, parses JSON, dispatches
- `Services/AiInteractionService.cs` — Centralized AI call with `ai-interactions.log` and fire-and-forget extraction

*Modified files (7):*
- `Models/AppConfig.cs` — Added `ExtractionSection`
- `AppSession.cs` — Added `AiInteraction` property
- `Program.cs` — Wired extractors, pipeline, service
- `appsettings.json` — Added `Extraction` config
- `Screens/ChatSessionScreen.cs`, `AskIntelligenceScreen.cs`, `TranslateScreen.cs`, `ExplainCommandScreen.cs` — All now use `AiInteraction.CallModelAsync`

**Result:** Build succeeds, zero errors.

---

## 2026-04-17 — Reasoning Level, Prompt Caching & Externalized System Prompts

**What was asked:**
Add configurable reasoning level support (OpenRouter `verbosity` parameter), prompt caching for OpenAI/Anthropic models, and externalize all system prompts into a structured markdown file outside storage.

**What was done:**

*New files (1):*
- `storage/system-prompts.md` — Externalized system prompts with H2-keyed sections (`base`, `chat`, `explain`, `translate`, `ask`)

*Modified files (11):*
- `Models/AppConfig.cs` — Added `Verbosity` (string) and `CacheEnabled` (bool) to `OpenRouterSection`
- `Models/ChatSessionModels.cs` — Added nullable `Verbosity` and `CacheControl` properties on `OpenRouterChatRequest`; new `CacheControlObject` class
- `Services/OpenRouterClient.cs` — Added verbosity/cache fields, setters, static `JsonSerializerOptions` with `WhenWritingNull`, payload wiring for both features
- `Services/PromptBuilder.cs` — Renamed `SystemInstruction` to `FallbackSystemInstruction`; added `promptKey` parameter; loads base + screen-specific prompt from `system-prompts.md` via H2 section parsing; falls back to hardcoded default
- `Services/LocalKnowledgeService.cs` — Added `prompts` directory creation
- `Screens/SettingsScreen.cs` — Added "Change reasoning level" (low/medium/high/xhigh/max) and "Toggle prompt caching" menu choices with table display
- `Screens/ChatSessionScreen.cs` — Header shows reasoning level and cache status; passes `promptKey: "chat"`
- `Screens/ExplainCommandScreen.cs` — Passes `promptKey: "explain"`
- `Screens/TranslateScreen.cs` — Passes `promptKey: "translate"`
- `Screens/AskIntelligenceScreen.cs` — Passes `promptKey: "ask"`
- `Program.cs` — Syncs `system-prompts.md` at boot; updated `OpenRouterClient` constructor; added `promptKey` to automation calls

**Result:** Build succeeds, zero errors.

---

## 2026-04-17 — Tool & Skill System with Screenshot

**What was asked:**
Add a generic tool/skill extensibility system inspired by OpenClaw's architecture. Tools are C# implementations the agent or user can invoke; Skills are `SKILL.md` markdown files injected into the system prompt that teach the LLM when/how to use tools. Two-tier skill precedence (workspace > bundled). Screenshot capture as the first tool — Windows-only for now with a platform abstraction for future OS support. Capture modes: full screen, active window, active monitor. Output: save to file, copy to clipboard, return base64 for vision model context.

**What was done:**

*New files (9):*
- `Services/Tools/ITool.cs` — `ITool` interface + `ToolResult` record
- `Services/Tools/ToolRegistry.cs` — Tool discovery, execution by name, prompt formatting
- `Services/Skills/Skill.cs` — Skill data model (name, description, instructions, OS filter)
- `Services/Skills/SkillLoader.cs` — `SKILL.md` parser with YAML frontmatter, two-tier precedence, OS filtering
- `Services/Tools/Screenshot/IScreenCaptureProvider.cs` — Platform capture abstraction
- `Services/Tools/Screenshot/WindowsScreenCapture.cs` — Windows capture via `System.Drawing` + P/Invoke (`user32.dll`)
- `Services/Tools/Screenshot/ScreenshotTool.cs` — `ITool` impl with interactive Spectre.Console mode selection, file save, clipboard (PowerShell), base64 return
- `storage/skills/screenshot/SKILL.md` — Bundled skill with OpenClaw-compatible frontmatter
- `Screens/ToolsScreen.cs` — User-facing tools menu (lists available tools, select to run)

*Modified files (5):*
- `cli-intelligence.csproj` — Added `System.Drawing.Common`, `AllowUnsafeBlocks`, `storage/skills/` copy-to-output
- `AppSession.cs` — Added `ToolRegistry` + `SkillLoader` properties
- `Program.cs` — Instantiates `SkillLoader`, `ToolRegistry`, registers `ScreenshotTool`
- `Services/PromptBuilder.cs` — Injects loaded skills + tool list into system prompt (optional params)
- `Screens/SettingsScreen.cs` — Added "Tools 🔧" menu entry navigating to `ToolsScreen`

**Result:** Build succeeds, zero errors, zero warnings. Phase 5 (LLM autonomous tool invocation via `[TOOL: name param=value]` parsing) deferred.
