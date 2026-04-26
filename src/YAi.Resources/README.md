# YAi.Resources

This project packages the reference assets that ship with YAi!, including the markdown templates and bundled skills copied into the CLI output, plus the signing material used to verify them at runtime.

## What is this project / folder

- **Project name:** YAi.Resources
- **Purpose:** Hold the canonical reference assets that are embedded or copied into the app output.
- **Contents:** `reference/templates/`, `reference/skills/`, and the signing material under `reference/`.

## Build

```bash
dotnet build YAi.Resources.csproj
```

## Usage

- `YAi.Resources.Signing.targets` signs changed reference assets before build when `YaiAutoSignResources=true`.
- Markdown templates under `reference/templates` are copied into the CLI output as `workspace/`.
- Bundled skills under `reference/skills` are copied into the output as `workspace/skills/`.
- The manifest and signature files are generated or refreshed by the signing target when resource signing runs, and `reference/public-key.yai.pem` ships for runtime verification.

## Development Notes

- Keep reference assets small, deterministic, and aligned with what the CLI seeds at runtime.
- Update the signed manifest when reference files change.
