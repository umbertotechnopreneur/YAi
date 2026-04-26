**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> YAi in Plain English ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** A plain-language overview of what YAi is today, how it starts, how it stores local state, and why it behaves cautiously around tools and approvals.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# YAi in Plain English

## What YAi is

YAi is a local-first AI assistant project built around a .NET runtime and a CLI-first experience.

Today the most active pieces are:

- the YAi CLI,
- the `YAi.Persona` runtime library,
- the bundled templates and skills in `YAi.Resources`,
- and a small Aspire service layer that is still much thinner than the CLI runtime.

## How it starts

When the CLI starts, it first handles the fast local commands such as help, version, banner, and manifesto.

If the run needs the full runtime, YAi then:

- resolves paths,
- prepares logging,
- loads the Persona services,
- checks the workspace,
- and runs bootstrap on first use.

The main point is that YAi tries to make setup explicit instead of hiding important state behind magic.

## How local state works

YAi separates human-owned workspace files from runtime-generated data.

- The workspace is where editable memory, prompts, regex files, and runtime skills live.
- The data area is where logs, history, daily files, and dream proposals live.

That split is meant to keep user-editable material visible and portable while still giving the runtime somewhere to keep generated state.

## Why the tool behavior feels cautious

YAi does not treat tools as a blank cheque.

The current tool and workflow model is designed around a few ideas:

- typed actions instead of raw shell access,
- workspace boundaries,
- approval before risky local writes,
- structured results instead of prose parsing,
- and audit output when workflows run.

In simple terms, YAi tries to show its work and ask before it does the expensive or risky thing.

## What comes next

The repo already points toward a broader experience, including a more interactive CLI flow and future frontend work.

But the safest description of the project today is still this:

YAi is a careful local-first assistant runtime with a growing tool and workflow system, not yet a giant autonomous platform.

## Where to look next

For the broader project posture, see [MANIFEST.md](../../../MANIFEST.md) and [README.md](../../../README.md).

For the current technical structure, start at [docs/README.md](../../README.md).