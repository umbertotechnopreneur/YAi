**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Workspace Boundary and Approval Flow ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Explain the current safety boundary for filesystem actions, including path checks, approval handoff, and the tests that prove the main failure cases.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Workspace Boundary and Approval Flow

## Boundary checks come first

Every active filesystem action resolves a full path under the supplied workspace root and then asks `WorkspaceBoundaryService` to verify it.

That is true for:

- `list_directory`
- `read_metadata`
- `create_file`

So even when a caller supplies a path like `..\..\outside.txt`, the action is supposed to fail before any real side effect happens.

## Approval is a separate hard gate

For `create_file`, boundary validation is not enough.

The tool also checks for `approved=true`. If that flag is missing, the tool returns `approval_required` and does not write the file.

This matters because the approval boundary is enforced twice:

- by the workflow layer before the step runs,
- and by `FilesystemTool` itself during the write action.

## How the workflow layer sets approval

`WorkflowExecutor` decides whether a step needs approval by reading the action metadata from the loaded skill.

When approval is required:

1. it builds an `ApprovalContext`,
2. sends it to `IApprovalService`,
3. receives `Approve`, `Deny`, or `CancelWorkflow`,
4. and only when the result is `Approve` does it add `approved=true` to the tool parameters.

The default production implementation is `WorkflowApprovalService`, which turns that context into a presenter-friendly step and forwards it to the approval presenter.

On the CLI side, the current presentation layer already includes the reusable `ApprovalCard` component.

## What the tests prove

`FilesystemToolCreateFileTests` already covers the important local safety cases:

- a file is written inside the workspace when approval is present,
- path traversal is blocked,
- overwrite is refused by default,
- and `create_file` is blocked when `approved=true` is missing.

`WorkflowExecutorTests` then prove the higher-level path:

- a two-step workflow can approve and create a timestamped file,
- approval denial stops the write,
- bad variable references fail before the approval card is shown,
- and denial is recorded in the step audit record.

## Why this is stronger than the old wording

The earlier pack talked about safety mostly as an architectural intention.

The current code and tests let us say something more concrete:

- the workspace boundary is enforced,
- silent writes are blocked,
- denial prevents the side effect,
- and the happy path returns a structured artifact when the write succeeds.