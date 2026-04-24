# Copilot Memory

Durable facts, conventions, and decisions for the cli-intelligence project.

## Architecture

- All AI calls go through `AiInteractionService.CallModelAsync` — single point for logging and extraction.
- `OpenRouterClient` is the raw HTTP transport to OpenRouter API. Never call it directly from screens.
- `KnowledgeExtractionPipeline` runs ONE cheap AI call per interaction to extract lessons/memories/limits/mandatory-context.
- `KnowledgeExtractionPipeline` also runs deterministic memory extraction from user input using pre-compiled regex patterns loaded from `system-regex.md` at boot.
- **RegexRegistry** (singleton service) parses `system-regex.md` at boot, compiles patterns with `RegexOptions.NonBacktracking | Compiled`, and provides ReDoS protection via O(n) linear-time execution. Rejects patterns with backreferences, lookaheads, or lookbehinds at boot.
- **Dynamic Metadata Binding**: `BuildDeterministicExtractions()` iterates over `RegexRegistry` patterns and uses reflection to extract named capture groups into `ExtractionItem.Metadata` dictionary. Enables zero-code expansion of extraction rules.
- Three pluggable extractors implement `IKnowledgeExtractor`: `MetadataExtractor` (generic, handles memory/lesson/limit types), `MandatoryContextExtractor` (field-update logic), `ReminderExtractor` (ReminderService dependency).
- `MetadataExtractor` routes extractions based on `Type` and `Section` properties, consuming `Metadata` dictionary for dynamic capture groups.
- Extraction is fire-and-forget async — never blocks the user.

## Tools & Skills

- Two-layer extensibility: **Tools** (C# `ITool` implementations) and **Skills** (`SKILL.md` files injected into system prompt).
- `ToolRegistry` registers tools at startup; provides `GetAvailable()`, `ExecuteAsync(name, params)`, `FormatToolListForPrompt()`, `RegisterScriptSkills(skillLoader)`.
- `SkillLoader` loads `SKILL.md` files with YAML frontmatter from two tiers: `data/skills/` (workspace, highest) → `storage/skills/` (bundled, lowest). Same-name workspace skill overrides bundled. Filtered by OS and required binaries.
- `Skill` record: `Name`, `Description`, `Instructions`, `Os?`, `Version?`, `SkillDirectory?`, `Metadata?` (OpenClawMetadata). Has `HasScripts` property and `GetScripts()` method for discovering bundled `.ps1` scripts.
- `OpenClawMetadata` record: `Os?` (list), `RequiredBins?`, `RequiredEnv?`, `PrimaryEnv?`, `Emoji?`, `Homepage?`.
- SkillLoader parses OpenClaw `metadata.openclaw` YAML blocks (inline `[...]` arrays and block `- item` lists). Supports `{baseDir}` template variable replacement in skill body.
- **Agent Loop**: LLM can autonomously invoke tools via `[TOOL: name param1=value1 param2="value with spaces"]` syntax. `ToolCallParser` extracts tool calls from LLM responses. `ChatSessionScreen.ExecuteToolLoopAsync` handles up to 3 rounds of tool execution with user approval gates, feeding results back to model for iteration.
- `ITool` interface: `Name`, `Description`, `IsAvailable()`, `ExecuteAsync(params) → ToolResult`, `GetParameters() → ToolParameter[]`, `GetRiskLevel() → ToolRiskLevel`.
- `ToolParameter` record: `Name`, `Type`, `Required`, `Description`, `DefaultValue`. Used for prompt generation.
- `ToolRiskLevel` enum: `SafeReadOnly`, `SafeWrite`, `Risky`, `Destructive`. Declared via `[ToolRisk(level)]` attribute.
- `ToolResult` record: `Success`, `Message`, `FilePath?`, `Data?` (byte[]), `MimeType?`.
- **Available Tools**: 
  - **Read**: `FileSystemTool` (read/list/find, sandboxed to workspace roots), `GitTool` (read-only git operations), `SystemInfoTool` (system information), `DotNetInspectTool` (parse .sln/.csproj for frameworks/packages/references)
  - **Write**: `ApplyPatchTool` (targeted file edits with Myers' diff algorithm for better change detection), `BatchEditTool` (atomic multi-file edits with commit/rollback via FileTransactionManager)
  - **Build/Test/Manage**: `DotNetBuildTestTool` (build/test/restore with diagnostic parsing), `DotNetManageTool` (clean/run/add_package/remove_package)
  - **Network**: `HttpTool` (HTTP requests with SSRF protection), `WebSearchTool` (DuckDuckGo search)
  - **Media**: `ScreenshotTool` (screen capture, Windows only)
  - **Utility**: `ClipboardTool`, `TimeZoneTool`, `TimerTool`, `ScriptTool` (PowerShell script adapter)
- **FileTransactionManager**: Service for atomic multi-file operations. BeginTransaction → AddEdit (stage changes) → CommitAsync (apply all atomically with backups) OR RollbackAsync (restore all from backups). Phase-based commit prevents partial failures.
- **Myers' diff algorithm**: ApplyPatchTool uses O(ND) Longest Common Subsequence algorithm for unified diffs with better context grouping and change detection vs simple line-by-line comparison.
- **ScriptTool**: Universal PowerShell adapter wrapping `.ps1` scripts from skills. Uses `ScriptSafetyGuard` for full user approval with dry-run preview and double confirmation for destructive patterns.
- **Runtime registered tools**: `timezone_convert`, `set_reminder`, `screenshot` (Windows), `clipboard` (Windows), `filesystem`, `http_request`, `git`, `system_info`, `web_search`.
- `http_request` has SSRF protection — blocks private/internal IP ranges (10.x, 172.16-31.x, 192.168.x, localhost, link-local).
- `git` tool is read-only — only allows: status, log, diff, branch, show, blame, remote, tag, stash-list.
- `filesystem` tool is read-only — no create/modify/delete operations.
- `web_search` uses DuckDuckGo Instant Answer API (no API key).
- `system_info` redacts environment variables that look like secrets (KEY, TOKEN, PASSWORD, etc.).
- Screenshot tool uses `IScreenCaptureProvider` abstraction — currently `WindowsScreenCapture` (System.Drawing + P/Invoke). Saves PNG to `data/screenshots/`, copies to clipboard, returns base64.
- `PromptBuilder.BuildMessages` accepts optional `SkillLoader` + `ToolRegistry` to inject tools/skills into system prompt. All AI interaction screens (ChatSessionScreen, AskIntelligenceScreen, ExplainCommandScreen, TranslateScreen) pass these parameters.
- Tools accessible via Settings / Tools → Tools 🔧 menu (manual invocation) and via LLM agent loop (`[TOOL: ...]` parsing in ChatSessionScreen).
- Bundled skills live in `storage/skills/<name>/SKILL.md` and are copied to output via `.csproj` Content items.
- `FileSystemTool` path sandboxing: restricts access to `Environment.CurrentDirectory`, `data/`, and `storage/` directories. Prevents traversal attacks.
- **TimeZoneTool** (`Services/Tools/DateTime/TimeZoneTool.cs`, namespace `cli_intelligence.Services.Tools.Time`): converts times between time zones using `TimeZoneInfo` + OS tz database. Accepts Windows IDs, IANA IDs, or display name fragments. Bundled skill: `storage/skills/timezone/SKILL.md`.
- **TimerTool** (`Services/Tools/DateTime/TimerTool.cs`): sets a reminder via `ReminderService`. Bundled skill: `storage/skills/timer/SKILL.md`.
- **ReminderService** (`Services/ReminderService.cs`): stores `ReminderEntry` records as JSON in `data/reminders/reminders.json`. `CheckAndFireDueReminders()` is called at the top of each chat loop iteration in `ChatSessionScreen`.
- **ReminderExtractor** (`Services/Extractors/ReminderExtractor.cs`): applies `reminder` type extraction items. Content format: `ISO8601_datetime|message`. Registered in the pipeline alongside other extractors.
- `system-regex.md` now has a `remind_command` section wired to `AddRemindCommand()` in the pipeline. Uses `TimeOnly.TryParse` (not format arrays) to resolve time-of-day strings.

## Storage

- `storage/lessons.md`, `storage/memories.md`, `storage/limits.md`, `storage/mandatory-context.md` are the workspace source-of-truth files.
- `storage/system-regex.md` is the source-of-truth regex catalog for deterministic extraction patterns (remember command, phone statement, project statement, and section classifiers).
- At boot, `SyncStorageFilesAtBoot` copies workspace storage → runtime `data/` knowledge directories.
- `SyncStorageFilesAtBoot` also syncs `system-regex.md` to `data/regex/system-regex.md` for runtime use.
- Extractors write to both runtime knowledge files and workspace storage files.

## HTTP Server

- `--server` flag runs `ServerHost.RunAsync(session)` instead of the interactive loop. Blocks until Ctrl+C.
- `Server/ServerHost.cs` uses `WebApplication.CreateSlimBuilder` + Kestrel. URL from `appsettings.json` `Server.Url` (default `http://localhost:5080`).
- URL is set via `app.Urls.Clear(); app.Urls.Add(url)` after `Build()` — not via `builder.WebHost.UseUrls` (not available on slim builder).
- Endpoints: `GET /`, `GET /health`, `GET /ping`, `POST /echo`, `GET /headers`, `GET /ip`, `GET /error-demo`.
- Centralized error handler via `UseExceptionHandler` middleware returns consistent JSON `{error, status}` envelope.
- Framework request logging is silenced; per-request logging uses static Serilog for consistency with the rest of the app.
- `AppSession` is passed into `ServerHost.RunAsync` — all AI, tool, and knowledge services are available for future AI-over-HTTP endpoints.
- `Models/AppConfig.cs` `ServerSection`: `Url`, `ServiceName`, `Version`. Added to `AppConfig` and `appsettings.json`.
- `<FrameworkReference Include="Microsoft.AspNetCore.App" />` in `.csproj` — not a NuGet package.

## Configuration

- `appsettings.json` has four top-level sections: `App`, `OpenRouter`, `Extraction`, `Server`.
- `Extraction.Enabled` (bool), `Extraction.Model` (string, cheap model), `Extraction.ConfidenceThreshold` (double, 0-1).
- `OpenRouter.Verbosity` (string, default `"medium"`) — maps to OpenRouter `verbosity` API parameter. Valid: `low`, `medium`, `high`, `xhigh`, `max`. Omitted from payload when `"medium"`.
- `OpenRouter.CacheEnabled` (bool, default `true`) — adds top-level `cache_control: { type: "ephemeral" }` for Anthropic models. OpenAI caching is automatic via OpenRouter.

## System Prompts

- `storage/system-prompts.md` is the source-of-truth for all system prompts, synced to `data/prompts/` at boot.
- Prompts are keyed by H2 headers (`## base`, `## chat`, `## explain`, `## translate`, `## ask`).
- `PromptBuilder.LoadPromptSection` parses the file by H2 sections. The `base` section is always loaded; a screen-specific section is appended when a `promptKey` is provided.
- Falls back to the hardcoded `FallbackSystemInstruction` if the file is missing or `base` section is empty.
- New prompt sections can be added by inserting a `## section-name` header — no code changes required.

## Conventions

- Screens use Spectre.Console for all UI rendering.
- Every screen clears console, renders banner, then content (per copilot-instructions.md).
- ESC navigates back via `AppNavigator` stack.
- Serilog for all logging; dedicated `ai-interactions.log` for full request/response payloads.
