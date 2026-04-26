# YAi.Services

| Field | Value |
| --- | --- |
| Purpose | Aspire AppHost and orchestration entry point for the YAi solution |
| Audience | Maintainers and contributors |
| Status | Active |
| Last reviewed | 2026-04-27 |

This project is the Aspire AppHost for the YAi! solution. `AppHost.cs` boots the orchestration host and registers `YAi.Services.Core` as the current application resource, while the shared service defaults project supplies common hosting behavior.

For AppHost-local implementation notes, see [docs/README.md](docs/README.md).

## What is this project / folder

- **Project name:** YAi.Services
- **Purpose:** Provide the orchestration entry point for the solution.
- **Contents:** `AppHost.cs`, solution-level hosting configuration, and project references to `YAi.Services.Core` and `YAi.Services.Defaults`.

## Build

```bash
dotnet build YAi.Services.csproj
```

## Run

```bash
dotnet run --project YAi.Services.csproj
```

Running the AppHost starts the Aspire orchestration entry point for the backend service.

## Usage

- Keep new service registration centralized in `AppHost.cs`.
- Add additional projects to the AppHost when the solution grows, rather than wiring them up independently.
- `YAi.Services.Core` is currently a placeholder web app with a root endpoint, so the AppHost is the main integration surface for the solution.

## Development Notes

- Keep orchestration explicit and minimal.
- Reuse `YAi.Services.Defaults` for service discovery, OpenTelemetry, health checks, and HTTP resilience.
