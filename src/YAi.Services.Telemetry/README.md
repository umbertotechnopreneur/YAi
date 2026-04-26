# YAi.Services.Telemetry

This shared project is currently a placeholder for telemetry-specific constants, names, and helper types. At the moment it has no source files, so it should not describe any implemented OpenTelemetry logic yet.

## What is this project / folder

- **Project name:** YAi.Services.Telemetry
- **Purpose:** Reserve a separate shared project for telemetry-related identifiers and helper types if the solution needs them later.
- **Contents:** The project file only. There are no source files in this folder today.

## Build

```bash
dotnet build YAi.Services.Telemetry.csproj
```

## Usage

- The solution already includes the project reference in `src/YAi.slnx`, but no runtime code depends on it yet.
- Add concrete telemetry names, labels, or helper types here only when they are implemented in code.
