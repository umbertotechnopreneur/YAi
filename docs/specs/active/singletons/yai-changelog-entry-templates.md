**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> YAi Changelog Entry Templates ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** A lightweight shared guide for writing changelog entries across project-local changelogs, shared-area changelogs, and the top-level docs history without drifting into verbose recap text.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# YAi Changelog Entry Templates

## Goal

Use changelog entries to leave short, factual history that helps the next maintainer answer three questions quickly:

- what changed
- where the change belongs
- why it belongs in this changelog instead of another one

Keep entries short enough to scan, but specific enough to be useful.

## Default shape

For most changelog files, use:

```markdown
## YYYY-MM-DD

- First bullet: what changed in the owned area.
- Second bullet: why the change stays in this changelog, or which shared or local surface it now links to.
```

Two bullets is usually enough.

Use a third bullet only when the change would otherwise be ambiguous.

## Decision table

Use this quick routing table before writing an entry.

| If the change mostly belongs to... | Use this changelog | Typical example |
|---|---|---|
| one project's docs and implementation surface | `src/<Project>/docs/local-changelog.md` | a project index update, local code anchors, or wording narrowed for a thin project |
| one shared docs area | `<area>/changelog.md` | a renamed procedure in `docs/operations/` or a reference-doc rewrite in `docs/specs/reference/` |
| the whole shared docs taxonomy | `docs/history/specs-changelog.md` | governance changes, area migrations, naming rules, or manifest-wide structure work |

When in doubt, choose the narrowest changelog that still matches the true owner of the history.

## Project-local `local-changelog.md` template

Use this when the history belongs to one project docs folder.

```markdown
## YYYY-MM-DD

- Added or updated the local docs surface for `<owned files, folders, or implementation seam>`.
- Linked or narrowed the project docs so `<shared contract>` stays shared while `<project-owned detail>` stays local.
```

Good examples:

- local code anchors added to a project index
- shared spec links updated after a migration
- wording narrowed so a thin or placeholder project is described honestly

Avoid using the project-local changelog for:

- cross-project taxonomy changes
- shared spec migration summaries
- operator procedures owned by `docs/operations/`

## Shared-area `changelog.md` template

Use this when the history belongs to one shared docs area such as `docs/operations/`, `docs/specs/reference/`, or `docs/specs/active/`.

```markdown
## YYYY-MM-DD

- Added or updated `<document or group of documents>` in this area.
- Kept the history here because the change affects the shared area, not one project and not the whole docs taxonomy.
```

Good examples:

- a new area-level changelog
- a renamed procedure within one shared area
- a group of reference docs normalized within the same area

## Top-level `docs/history/specs-changelog.md` template

Use the top-level shared changelog only for broader taxonomy or cross-project documentation changes.

```markdown
## YYYY-MM-DD

### Added

- New shared structures, governed areas, or migration records.

### Changed

- Cross-project documentation rules, naming, or placement changes.
```

If a change is mostly local to one project or one shared area, prefer that narrower changelog first.

## Writing rules

- Prefer plain past-tense statements.
- Name the owned area directly.
- Mention one or two concrete files or surfaces when that clarifies the entry.
- Avoid motivational language, long recap paragraphs, and copy-pasting the same migration summary into several changelogs.
- Do not turn a changelog entry into a design document.

## Decision rule

When a change could fit in more than one changelog, choose the narrowest changelog that still matches the true owner of the history.