**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> YAi Skill Execution and Cerbero Pack ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** The active shared specification pack for YAi skill loading, tool execution, linear workflow chaining, schema validation, and the current Cerbero command-safety scope.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# YAi Skill Execution and Cerbero Pack

This pack replaces the older combined skill-chaining and Cerbero draft spec with a grounded description of what YAi actually has today.

## Current status

- Implemented now: `SKILL.md` loading, workspace override behavior, tool registration, standardized `SkillResult`, linear workflow execution, placeholder resolution, minimal schema validation, and the regex-based Cerbero analyzer.
- Implemented but not fully universal yet: Cerbero exists as a tested analyzer, but it is not the active gate in every command or workflow path.
- Not current truth: a fully general multi-step execution system with DAGs, parallelism, and universally enforced command safety across every runtime surface.

## Reading order

1. [Skill discovery and action metadata](01-skill-discovery-and-action-metadata.md)
2. [Linear workflow execution and schema validation](02-linear-workflow-execution-and-schema-validation.md)
3. [Cerbero current role](03-cerbero-current-role.md)

## Primary code anchors

- `src/YAi.Persona/Services/Skills/SkillLoader.cs`
- `src/YAi.Persona/Services/Tools/ToolRegistry.cs`
- `src/YAi.Persona/Services/Execution/SkillResult.cs`
- `src/YAi.Persona/Services/Workflows/Services/WorkflowExecutor.cs`
- `src/YAi.Persona/Services/Workflows/WorkflowVariableResolver.cs`
- `src/YAi.Persona/Services/Skills/Validation/MinimalSkillSchemaValidator.cs`
- `src/YAi.Persona/Services/Operations/Safety/Cerbero/RegexCommandSafetyAnalyzer.cs`
- `src/YAi.Persona.Tests/WorkflowExecutorTests.cs`
- `src/YAi.Persona.Tests/SkillSchemaValidatorTests.cs`
- `src/YAi.Persona.Tests/CerberoCommandSafetyTests.cs`

## Why this pack was rewritten

The older combined spec mixed implemented pieces with broader future ambitions.

The current pack keeps the same themes, but splits them into the three places where the code now gives a defensible story: discovery, execution, and Cerbero's current non-universal safety role.