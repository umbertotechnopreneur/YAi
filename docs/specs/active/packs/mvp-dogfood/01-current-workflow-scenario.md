**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Current Workflow Scenario ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Describe the current two-step workflow scenario that functions as the practical MVP dogfood slice in YAi today.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Current Workflow Scenario

The current MVP dogfood scenario is still the cleanest one to describe:

1. run `system_info.get_datetime`
2. emit `timestamp_safe`
3. resolve that variable into a filesystem path
4. request approval for the write step
5. run `filesystem.create_file`
6. return artifacts and write audit records

## What the workflow definition looks like

The current tests use a small `WorkflowDefinition` with two steps:

- `sysinfo`
- `file`

The second step uses a path like:

```text
./output/${steps.sysinfo.variables.timestamp_safe}_qualcosa.txt
```

That expression is the current proof that the system is no longer only passing prose between steps.

## Where this scenario is grounded

`WorkflowExecutorTests` already exercises the success path and several failure cases.

The success test proves that the workflow can:

- run the first step,
- carry the timestamp variable forward,
- show approval for the second step,
- create the file,
- and return a file artifact.

## Important current limit

This scenario is implemented and test-backed, but it is still mostly a service-layer and test-layer workflow.

The current repo does not yet expose this exact dogfood scenario as a polished single CLI command that a normal user can invoke without building the workflow definition first.

That distinction matters because the earlier pack implied a more end-user-ready surface than the current CLI actually offers.