**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> YAi.Client.CLI Local Changelog ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** The project-local changelog for CLI-owned documentation and implementation-history notes that should stay close to the CLI process and bootstrap surface.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# YAi.Client.CLI Local Changelog

This file tracks CLI-local documentation history that does not belong in the shared docs history layer.

## 2026-04-27

- Added the first real CLI-owned entry by recording the local docs index that anchors `Program.cs`, `appsettings.json`, `lenna.ps1`, `yai_logo_ansi_800x600.ps1`, and `splash-helpers.ps1`.
- Linked the CLI docs surface to the shared CLI flow and boot sequence diagrams so cross-project flow contracts stay shared while CLI process notes stay local.
- Refreshed the CLI chrome so the header and footer now surface persona identity, a shortened workspace path, model/provider/cache, bootstrap and app-lock state, token counts, turn duration, and talk navigation hints with emoji-coded markers.
- Kept this note in the CLI-local changelog because the owning surfaces are `Program.cs`, `AppHeaderState.cs`, and `StatusBarState.cs`.
- Documented that `--ask` now falls back to the reusable multiline prompt editor when no inline text is supplied, while `--bootstrap` and `--talk` continue to route prompt capture through the same shared editor core.
- Kept this history in the CLI-local changelog because the owned orchestration seam is `Program.cs` plus the CLI README/help surface, while the editor and screen internals stay in the components project.
- Documented that interactive `--ask` and `--translate` now route completed responses through the reusable Razor response screen host, while `--talk` and `--bootstrap` still keep their current REPL-style response output.
- Updated the talk and bootstrap REPL flows so completed assistant turns now render through the same shared response state and inline response panel formatting used by the reusable response screen, while keeping the existing prompt-entry loop intact.
- Updated the non-screen `--ask` and `--translate` fallback path so redirected output now uses the shared inline response panel and metadata layout instead of the old one-line assistant output.
- Migrated the interactive `--talk` and `--bootstrap` prompt/transcript loop to a combined conversation screen host that keeps prior turns, response metadata, and the next multiline prompt on one reusable RazorConsole surface.

## 2026-04-26

- Added the seeded local changelog for `YAi.Client.CLI`.
- Kept the history file in the CLI docs folder because CLI process, bootstrap, splash, and launch-helper notes belong to this project rather than the shared docs history.