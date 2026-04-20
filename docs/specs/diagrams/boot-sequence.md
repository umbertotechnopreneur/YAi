**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> YAi Client CLI Boot Sequence ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Architecture reference for the current YAi.Client.CLI startup and first-run bootstrap lifecycle.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# YAi.Client.CLI Boot Sequence

This document describes the boot path implemented in [src/YAi.Client.CLI/Program.cs](../../src/YAi.Client.CLI/Program.cs). It focuses on the CLI process start, the filesystem and logging bootstrap, the dependency injection setup, the workspace seeding step, and the explicit first-run bootstrap command.

The diagram below is intentionally detailed because the startup path touches both process-level concerns and persistent state. The key files and services are:

- [AppPaths](../../src/YAi.Persona/Services/AppPaths.cs) creates and locates the runtime roots, including `config`, `logs`, `history`, and `workspace`.
- [WorkspaceProfileService](../../src/YAi.Persona/Services/WorkspaceProfileService.cs) seeds markdown templates from the packaged asset workspace into the user runtime workspace.
- [ConfigService](../../src/YAi.Persona/Services/ConfigService.cs) reads and writes configuration and the persisted first-run marker.
- [BootstrapState](../../src/YAi.Persona/Models/BootstrapState.cs) is the serialized payload stored on first bootstrap.
- [RuntimeState](../../src/YAi.Persona/Models/RuntimeState.cs) is the in-memory state that marks the current process as bootstrapped.

## Notes on the current implementation

The current CLI is command-driven. It always performs the filesystem bootstrap and template seeding on launch, then dispatches based on the first command-line argument. The persisted `first-run.json` file is written only by the explicit `--bootstrap` command. That means the "already bootstrapped" path in this document represents the steady-state startup path after the workspace and first-run data already exist, not a separate code branch in `Program.cs` that gates startup.

The repository also contains a `LoadBootstrapState()` helper in `ConfigService`, but `Program.cs` does not currently call it during process start. This diagram documents the lifecycle the code is already supporting, while making the steady-state and first-run cases easy to compare.

## Boot Responsibilities

### Process bootstrap

- `Environment.GetCommandLineArgs()` captures the user intent for the current launch.
- `AppPaths` resolves all roots before any other application service is created.
- `EnsureDirectories()` creates the persistent directories and probes write access early so the process fails fast if the user data root is not writable.
- Serilog is configured before any of the application services are resolved so startup failures are captured in `yai.log`.
- The banner prints immediately after logging is initialized so the operator can visually confirm the CLI is alive.

### Runtime service setup

- `ServiceCollection` is used as a minimal container so the CLI can stay lightweight.
- The repository-specific services are registered through `AddYAiPersonaServices()`.
- `WorkspaceProfileService`, `ConfigService`, and `RuntimeState` are resolved from the container and reused for the rest of the launch.

### Workspace seeding

- `EnsureInitializedFromTemplates()` copies packaged markdown templates into the runtime workspace.
- Existing files are not overwritten, so repeated launches are safe.
- The workspace seed step is invoked both during normal startup and during the explicit bootstrap command.

### First-run bootstrap

- `DoBootstrapAsync()` writes a `BootstrapState` payload to `config/first-run.json`.
- The payload records the current time and the resolved agent and user names.
- `RuntimeState.IsBootstrapped` is flipped in memory for the current process after the first-run record is saved.

## Sequence Diagram

```mermaid
sequenceDiagram
    autonumber

    %% The CLI is launched by the shell, then immediately prepares
    %% filesystem state, logging, dependency injection, and workspace files.
    participant Shell as User / shell
    participant Program as YAi.Client.CLI<br/>Program.cs
    participant Paths as AppPaths
    participant Disk as File system
    participant Log as Serilog
    participant DI as ServiceCollection<br/>+ service provider
    participant Workspace as WorkspaceProfileService
    participant Config as ConfigService
    participant Runtime as RuntimeState

    Shell->>Program: start yai [args]
    Note right of Program: Entry point in src/YAi.Client.CLI/Program.cs

    Program->>Paths: new AppPaths()
    Program->>Paths: EnsureDirectories()
    Paths->>Disk: create config/
    Paths->>Disk: create logs/
    Paths->>Disk: create history/
    Paths->>Disk: create workspace/
    Paths->>Disk: write / delete probe file
    Paths-->>Program: runtime roots ready

    Note over Paths,Disk: This is the earliest failure point for permissions or path issues.<br/>If the user data root is not writable, the process stops before any higher-level work begins.

    Program->>Log: configure file sink at logs/yai.log
    Program->>Log: log startup context
    Program->>Program: PrintBanner()

    Program->>DI: create minimal container
    Program->>DI: register AppPaths, Persona services, and logging
    DI-->>Program: service provider ready

    Program->>Workspace: resolve WorkspaceProfileService
    Program->>Config: resolve ConfigService
    Program->>Runtime: resolve RuntimeState

    Note over Workspace,Config: The CLI prepares both the runtime workspace and the persisted configuration layer before command dispatch begins.

    Program->>Workspace: EnsureInitializedFromTemplates()
    Workspace->>Disk: enumerate packaged markdown templates
    Workspace->>Disk: copy missing templates into runtime workspace
    Workspace-->>Program: workspace seeded or already up to date

    alt Already bootstrapped / steady-state startup
        Note over Program,Runtime: The workspace and first-run files already exist.<br/>Startup still performs the same seeding step, but existing files are preserved.

        Program->>Program: inspect command line arguments

        alt --ask / --translate / --talk
            Program->>Program: dispatch the requested non-bootstrap command
            Note right of Program: These commands reuse the services built during startup.<br/>They are outside the bootstrap lifecycle itself.
        else no recognized command
            Program-->>Shell: print usage
            Note right of Program: Usage output is the default fallback when no command is supplied.
        end

    else First-run bootstrap requested
        Note over Program,Config: This path is entered when the user explicitly runs the bootstrap command.<br/>It is the only path that persists a first-run marker.

        Program->>Program: DoBootstrapAsync()
        Program->>Workspace: EnsureInitializedFromTemplates()
        Workspace->>Disk: seed missing templates if needed
        Workspace-->>Program: bootstrap workspace ready

        Program->>Config: SaveBootstrapState(BootstrapState)
        Config->>Disk: atomically write config/first-run.json
        Config-->>Program: bootstrap state persisted

        Program->>Runtime: IsBootstrapped = true
        Runtime-->>Program: current process marked bootstrapped

        Note right of Config: The persisted payload records the bootstrap timestamp,<br/>agent name, user name, and any future metadata added to `BootstrapState`.
        Note right of Runtime: This flag only affects the in-memory process state.<br/>The persisted source of truth is `config/first-run.json`.

        Program-->>Shell: Bootstrap completed.
    end

    Program->>Log: close and flush
    Note over Log: The logger is closed in a finally block so startup and bootstrap failures are still flushed to disk.
```

## What the diagram is showing

The diagram separates the startup work into two human-level cases:

1. Already bootstrapped or steady state. The runtime workspace already exists, the templates are already seeded, and the process simply completes startup and dispatches the requested command.
2. First-run bootstrap. The operator explicitly invokes `--bootstrap`, causing the CLI to seed the workspace again if needed, persist `first-run.json`, and mark the current process as bootstrapped.

The sequence is deliberately ordered the same way the code executes it:

- filesystem preparation first,
- logging second,
- DI container creation third,
- workspace seeding fourth,
- command dispatch last.

That ordering matters because it keeps the application predictable when the user data root is missing, partially initialized, or read-only.

## Source Map

- [src/YAi.Client.CLI/Program.cs](../../src/YAi.Client.CLI/Program.cs)
- [src/YAi.Persona/Services/AppPaths.cs](../../src/YAi.Persona/Services/AppPaths.cs)
- [src/YAi.Persona/Services/WorkspaceProfileService.cs](../../src/YAi.Persona/Services/WorkspaceProfileService.cs)
- [src/YAi.Persona/Services/ConfigService.cs](../../src/YAi.Persona/Services/ConfigService.cs)
- [src/YAi.Persona/Models/BootstrapState.cs](../../src/YAi.Persona/Models/BootstrapState.cs)
- [src/YAi.Persona/Models/RuntimeState.cs](../../src/YAi.Persona/Models/RuntimeState.cs)