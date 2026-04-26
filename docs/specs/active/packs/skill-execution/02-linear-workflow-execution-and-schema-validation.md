**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Linear Workflow Execution and Schema Validation ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Describe the current step-by-step workflow executor, the placeholder resolver, and the minimal schema validation path that already backs execution.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Linear Workflow Execution and Schema Validation

## The executor is intentionally sequential

`WorkflowExecutor` runs workflow steps one by one.

That is not a limitation hidden in the code. It is the current design.

The implementation keeps a state bag of prior `SkillResult` values, resolves the next step input, and stops on failures, denials, or cancellation.

## The execution order today

The current path is:

1. load skills,
2. initialize audit output,
3. resolve the step input,
4. validate input schema if present,
5. request approval if required,
6. execute the matching tool,
7. validate output schema if present,
8. write step audit records,
9. move to the next step or stop.

## Variable resolution is constrained on purpose

`WorkflowVariableResolver` only allows step-variable and step-data placeholders.

That keeps the chaining model predictable and makes failure reasons much clearer than a looser template system would.

## Schema validation is present, but still minimal

The default validator is `MinimalSkillSchemaValidator`.

The current tests already exercise this against `system_info.get_datetime`.

So the right way to describe the current state is:

- schema validation exists,
- it is intentionally small,
- and it is already in the executor path,
- but it is not yet a large general validation framework.

## What is still out of scope

The older draft discussed broader execution goals.

Those are still not part of the active contract:

- DAG workflows,
- parallel execution,
- and a larger autonomous planning layer.

The current runtime is a linear executor with structured handoff, approval, and audit.