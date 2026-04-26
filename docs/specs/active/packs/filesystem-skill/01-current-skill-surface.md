**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Current Skill Surface ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Describe the current filesystem tool surface as implemented, including the supported actions and the narrower MVP contract.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Current Skill Surface

## What is registered today

`ServiceCollectionExtensions` registers `FilesystemTool` and then adds it to `ToolRegistry` under the name `filesystem`.

That means the runtime has two layers:

- skill metadata loaded from `SKILL.md`,
- and the actual C# tool implementation that does the work.

Both layers have to line up for a workflow step to run cleanly.

## Supported actions now

The tool currently advertises four actions:

- `plan`
- `list_directory`
- `read_metadata`
- `create_file`

The important nuance is that only three of those are active MVP operations.

### `list_directory`

- read-only
- returns structured data
- checks the workspace boundary before listing

### `read_metadata`

- read-only
- returns structured metadata for a file or folder
- checks the workspace boundary before reading

### `create_file`

- write operation
- requires explicit approval
- creates parent directories when needed
- refuses to overwrite by default
- returns a `SkillResult` with a file artifact

### `plan`

`plan` is currently disabled for the MVP path.

`FilesystemTool.ExecuteAsync` returns `not_supported_for_mvp` and explicitly tells the caller to use the workflow executor with `filesystem.create_file` instead.

That is the clearest sign that the active surface is intentionally smaller than the older concept pack described.

## Parameters that matter most now

The current implementation expects these parameters across the active actions:

- `workspace_root`
- `path`
- `content` for `create_file`
- `approved=true` for `create_file`

For the current MVP, `approved=true` is not optional ceremony. It is a hard gate enforced by the tool itself.

## Risk posture

At the tool level, `FilesystemTool` is marked with `[ToolRisk(ToolRiskLevel.Destructive)]`.

At the action-result level, the active operations are more specific:

- read operations return `SafeReadOnly`
- `create_file` returns `SafeWrite` and `RequiresApproval = true`

That split matters because the workflow layer uses action metadata and runtime approval, not only the class-level attribute.