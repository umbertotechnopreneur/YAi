**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> YAi.Client.CLI Docs ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** The local documentation index for the YAi CLI process, command dispatch, and bootstrap orchestration surface.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# YAi.Client.CLI Docs

## What belongs here

Use this folder for CLI-specific implementation notes and local history about:

- command parsing and dispatch in `Program.cs`
- bootstrap orchestration from the CLI process
- banner, splash, and terminal presentation helpers
- local launch helpers such as `jump-cli-output.ps1`

## Shared docs this project depends on

- [Shared docs index](../../../docs/README.md)
- [Documentation governance](../../../docs/DOCUMENTATION-GOVERNANCE.md)
- [CLI flow diagram](../../../docs/specs/diagrams/2026-04-25-cli-flows-and-architecture.md)
- [Boot sequence diagram](../../../docs/specs/diagrams/boot-sequence.md)

## Local files

- [Local changelog](local-changelog.md)

## Current code anchors

- `Program.cs`
- `appsettings.json`
- `lenna.ps1`
- `yai_logo_ansi_800x600.ps1`
- `splash-helpers.ps1`

Cross-project contracts such as workflow behavior or memory rules should stay under the shared `docs/` tree.