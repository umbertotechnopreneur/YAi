# YAi.Services.Defaults

This shared project contains the Aspire service defaults used by the YAi! backend host projects. `Extensions.cs` wires up OpenTelemetry, service discovery, standard HTTP resilience, and development health endpoints.

## What is this project / folder

- **Project name:** YAi.Services.Defaults
- **Purpose:** Provide the shared hosting defaults consumed by `YAi.Services.Core` and any other app host or web service in the solution.
- **Contents:** `Extensions.cs`, which adds `AddServiceDefaults`, `ConfigureOpenTelemetry`, and `MapDefaultEndpoints`.

## Build

```bash
dotnet build YAi.Services.Defaults.csproj
```

## Usage

- `AddServiceDefaults` configures OpenTelemetry, service discovery, and the standard HTTP resilience handler.
- `ConfigureOpenTelemetry` adds ASP.NET Core, HTTP client, and runtime instrumentation, and uses `OTEL_EXPORTER_OTLP_ENDPOINT` when present.
- `MapDefaultEndpoints` maps `/health` and `/alive` only in development.
- The project is marked as an Aspire shared project and is referenced by the service host projects rather than run directly.

## Development Notes

- Keep shared hosting behavior here so the API project stays thin.
- Add new host-wide telemetry or health behavior here before duplicating it in service projects.
