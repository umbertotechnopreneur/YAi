**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> YAi.Persona Docs ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** The local documentation index for the YAi runtime library, including paths, workspace seeding, prompt assets, skills, tools, and workflow services.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# YAi.Persona Docs

## What belongs here

Use this folder for runtime-library details about:

- workspace and data roots
- prompt asset loading
- skill loading and tool registration
- workflow execution and approval services
- local security services

## Shared docs this project depends on

- [Shared docs index](../../../docs/README.md)
- [Documentation governance](../../../docs/DOCUMENTATION-GOVERNANCE.md)
- [Workspace memory pack](../../../docs/specs/active/packs/workspace-memory/README.md)
- [MVP dogfood pack](../../../docs/specs/yai_mvp_dogfood_specs/README.md)
- [Skill chaining spec](../../../docs/specs/yai_skill_chaining_cerbero_schema_specs/yai_skill_chaining_cerbero_schema_spec.md)

## Current code anchors

- `Services/AppPaths.cs`
- `Services/WorkspaceProfileService.cs`
- `Services/PromptAssetService.cs`
- `Services/Skills/SkillLoader.cs`
- `Services/Tools/ToolRegistry.cs`
- `Services/Workflows/Services/WorkflowExecutor.cs`

Cross-project contracts should stay shared. This folder is for local implementation detail and local change history.