**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Skill Discovery and Action Metadata ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Explain the current split between skill metadata and executable tools, and how YAi discovers, loads, and overrides skill definitions.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Skill Discovery and Action Metadata

## Skills and tools are not the same object

YAi currently keeps two parallel concepts:

- `SkillLoader` reads `SKILL.md` files and extracts skill metadata such as actions and options.
- `ToolRegistry` holds the executable C# tools that can actually run.

`WorkflowExecutor` requires both.

It first looks up the skill metadata and then checks whether a tool with the same name is registered and available.

## Current load order

`SkillLoader` loads:

1. bundled skills from the trusted asset set,
2. workspace skills from `workspace/skills/`,
3. and lets workspace skills override bundled ones by name.

That override model is already active and grounded in the loader code.

## What the runtime actually registers

`ToolRegistry` is currently built around two bundled tool implementations:

- `system_info`
- `filesystem`

So the current skill-execution model is intentionally small and typed.

## Why action metadata matters

The workflow layer uses action metadata from the loaded skill to decide things like:

- whether the action exists,
- whether approval is required,
- and whether input or output schema validation should run.

That means a working step is not only a method call. It is the combination of:

- a loaded skill definition,
- action metadata,
- and a matching registered tool.

## What changed from the older draft

The older spec treated the execution layer more like a broad future platform.

The narrower truth today is simpler and easier to defend:

- bundled and workspace skills load,
- action metadata is real,
- tool registration is real,
- and the current platform only needs those pieces for a small set of built-in tools.