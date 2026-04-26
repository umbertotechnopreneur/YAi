**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Approval and Audit Path ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Describe the current approval and audit boundaries around the MVP workflow execution path.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Approval and Audit Path

## Approval is part of the executor, not an afterthought

`WorkflowExecutor` requests approval when the action metadata says a step requires it.

It does this before the tool runs. If the user denies the step, the executor stops before the filesystem write happens.

If the user approves, the executor passes `approved=true` to the tool so the write-side hard gate can succeed.

## Possible decisions

The approval path can currently return:

- `Approve`
- `Deny`
- `CancelWorkflow`

The tests already prove the denial path. When approval is denied, the workflow stops and the file is not created.

## Audit output is already structured

`WorkflowAuditService` initializes an audit folder under:

```text
<workspaceRoot>/.yai/audit/workflows/<timestamp>/
```

It writes:

- `workflow.json`
- step records
- `resolved-inputs.json`
- `approvals.json`
- `step-results.json`
- `errors.json`
- `summary.json`

That is enough to describe a real audit trail, not just a future idea.

## Current production boundary

The production approval service is `WorkflowApprovalService`, which maps the workflow context into a presenter-friendly approval step.

The current CLI components already include the approval-card UI building blocks. The key point for the docs is not the exact look of the card, but that the workflow architecture already has a concrete request-response approval service rather than a vague placeholder.

## Honest current summary

The current MVP workflow story is strong where it matters most:

- data handoff is structured,
- risky steps request approval,
- denial prevents the side effect,
- and the run leaves an audit trail behind.

What is still missing is not the approval model itself, but the wider end-user product wrapping around this scenario.