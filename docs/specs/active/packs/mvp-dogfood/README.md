**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> YAi Workflow MVP Dogfood Pack ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** The active shared specification pack for the current minimal workflow scenario that chains system info, variable resolution, approval, filesystem write, and audit.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# YAi Workflow MVP Dogfood Pack

This pack replaces the earlier dogfood draft set with a smaller description of the workflow slice that the code and tests already exercise.

## Current status

- Implemented now: linear workflow execution, structured `SkillResult` output, variable resolution, approval gating, audit writes, and the two-step timestamped-file scenario used by the tests.
- Implemented but still mostly developer-facing: the executor is exercised directly in tests rather than exposed as a polished end-user CLI command for this scenario.
- Not current truth: a fully model-planned end-to-end dogfood flow already wired through the main CLI command path.

## Reading order

1. [Current workflow scenario](01-current-workflow-scenario.md)
2. [Result envelope and variable resolution](02-result-envelope-and-variable-resolution.md)
3. [Approval and audit path](03-approval-and-audit-path.md)

## Diagram

- [Linear workflow dogfood flow](../../../diagrams/linear-workflow-dogfood-flow.md)

## Primary code anchors

- `src/YAi.Persona/Services/Workflows/Models/WorkflowDefinition.cs`
- `src/YAi.Persona/Services/Workflows/Services/WorkflowExecutor.cs`
- `src/YAi.Persona/Services/Workflows/WorkflowVariableResolver.cs`
- `src/YAi.Persona/Services/Execution/SkillResult.cs`
- `src/YAi.Persona/Services/Tools/SystemInfo/SystemInfoTool.cs`
- `src/YAi.Persona/Services/Tools/Filesystem/FilesystemTool.cs`
- `src/YAi.Persona/Services/Workflows/Services/WorkflowAuditService.cs`
- `src/YAi.Persona.Tests/WorkflowExecutorTests.cs`
- `src/YAi.Persona.Tests/SkillResultTests.cs`

## Why this pack was rewritten

The older dogfood pack broke the scenario into many small draft files. That was useful while the workflow model was still being invented.

The current codebase lets us collapse the pack into the three pieces that matter now: the scenario, the structured data handoff, and the approval plus audit boundary.