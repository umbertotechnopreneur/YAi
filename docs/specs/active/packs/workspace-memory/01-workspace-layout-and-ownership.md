**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Workspace Layout and Ownership ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Explain the current YAi workspace, data, and config roots in plain language, with the actual layout grounded in `AppPaths` and template seeding.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Workspace Layout and Ownership

YAi currently uses three roots, not one:

- `workspace/` is the human-owned area.
- `data/` is the runtime-generated area.
- `config/` holds app-level machine-local settings that are not stored under the workspace root.

That split is implemented in `AppPaths`.

## Default locations

Current defaults are:

- Workspace root: `%USERPROFILE%\.yai\workspace`
- Data root: `%LOCALAPPDATA%\YAi\data`
- Config root: `%LOCALAPPDATA%\YAi\config`

Overrides currently supported by code:

- `YAI_WORKSPACE_ROOT`
- `YAI_DATA_ROOT`

## Ownership model

The code follows a practical ownership boundary:

- The workspace contains files the user can inspect, edit, back up, or sync.
- The data root contains generated artifacts such as logs, chat history, daily files, and dream proposals.
- The config root stores app-level caches and state such as the OpenRouter catalog cache and first-run marker.

## Current runtime layout

The current seeded workspace is a hybrid layout. It contains both structured subfolders and a few top-level compatibility files.

```text
workspace/
  AGENTS.md
  BOOTSTRAP.md
  IDENTITY.md
  SOUL.md
  SYSTEM-PROMPTS.md
  USER.md
  memory/
    IDENTITY.md
    SOUL.md
    USER.md
  prompts/
    system-prompts.common.md
    system-prompts.en.md
    system-prompts.it.md
  regex/
    system-regex.common.md
    categories/
      episodes.en.md
      episodes.it.md
  skills/
    filesystem/
      SKILL.md
    system_info/
      SKILL.md
  config/
    security.json
    secrets.json

data/
  daily/
  dreams/
    DREAMS.md
    candidates.jsonl
  history/
  logs/
  llm-calls.db

config/
  appconfig.json
  first-run.json
  openrouter-model-catalog.json
```

## What `WorkspaceProfileService` does today

On initialization, `WorkspaceProfileService`:

- creates the required workspace, data, and config directories,
- optionally blocks seeding if bundled resource verification fails,
- copies missing Markdown templates into the user workspace,
- copies missing bundled skills into `workspace/skills/`,
- and leaves existing user files in place.

If a bundled template has a higher `template_version` than the installed file, the service creates a sidecar proposal file such as `USER.template-update-20260426.md` instead of overwriting the user file.

## Important current behavior

- `BOOTSTRAP.md` is treated as a one-time runtime file and is deleted after a successful bootstrap flow.
- The `.backups` folder under the workspace root is reserved for memory edit backups.
- The workspace is not supposed to live under the build output folder, even though the bundled assets are loaded from the published app output.

## What is not current truth

The earlier draft pack described a broader layout than the current code supports.

These paths are not implemented in `AppPaths` today as first-class roots:

- `data/indexes/`
- `data/cache/`
- `data/sessions/`
- `workspace/config/workspace.json`

Those ideas can still be useful later, but they are not part of the current runtime contract.