**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> YAi Documentation Surface Impact Map ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** The shared map from major specs and documentation areas to the projects and code surfaces they affect.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# YAi Documentation Surface Impact Map

| Area | Current source | Affected projects | Primary code surfaces |
|---|---|---|---|
| Workspace memory | `docs/specs/active/packs/workspace-memory/` | `YAi.Persona`, `YAi.Client.CLI`, `YAi.Resources` | `AppPaths`, `WorkspaceProfileService`, `MemoryFileParser`, `WarmMemoryResolver`, `PromptAssetService`, `RegexRegistry`, `CandidateStore`, `PromotionService` |
| Filesystem skill | `docs/specs/yai_filesystem_skill_specs/` | `YAi.Persona`, `YAi.Client.CLI.Components`, `YAi.Resources` | `FilesystemTool`, workflow approval surfaces, bundled skill assets |
| MVP dogfood workflow | `docs/specs/yai_mvp_dogfood_specs/` | `YAi.Persona`, `YAi.Persona.Tests`, `YAi.Client.CLI` | `WorkflowExecutor`, `WorkflowVariableResolver`, `WorkflowAuditService` |
| Skill chaining and Cerbero | `docs/specs/yai_skill_chaining_cerbero_schema_specs/` | `YAi.Persona`, `YAi.Persona.Tests`, `YAi.Client.CLI.Components` | `SkillLoader`, `ToolRegistry`, `WorkflowExecutor`, Cerbero safety surfaces |
| CLI architecture | `docs/specs/diagrams/` | `YAi.Client.CLI`, `YAi.Client.CLI.Components`, `YAi.Persona` | `Program.cs`, screen hosts, header/status state |
| Resource integrity | `docs/operations/resource-signing-and-verification.md` | `YAi.Resources`, `YAi.Tools.ResourceSigner`, `YAi.Persona` | `reference/`, signing tool, runtime integrity verification |

This map starts as a manual document. It can later be paired with generated git references and spec metadata.