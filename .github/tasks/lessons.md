# Documentation Lessons

- Shared docs and project-local docs need different jobs. Shared docs define contracts and cross-project behavior; project-local docs explain one implementation surface.
- History needs its own layer. Active specs should not carry the full burden of change tracking, rationale, and git traceability.
- A visible governance document under `docs/` is useful for humans, while a short always-on instructions file under `.github/instructions/` is useful for agents. Both are needed.
- The services area still needs careful wording because `YAi.Services` and `YAi.Services.Core` are currently much smaller than some older documentation implies.
- Grounding a doc against code often reveals a third state between "implemented" and "planned": services or UI pieces may exist but still not be wired into the active user flow. The docs should say that plainly.
- When a doc is reclassified or renamed, update the shared manifest, history files, and project-local indexes in the same patch. If those move separately, stale references linger.
