<div align="center">

# YAi! Manifesto 🛡️  
## Trust-first AI agents for builders who do not trust magic.

**OpenClaw optimizes for autonomy.**  
**YAi! optimizes for trust.**

YAi! is an OpenClaw-inspired local agent runtime for developers, founders, consultants, and technical operators who want AI assistance without surrendering control of their machine, their memory, or their workflows.

We believe AI agents should be powerful, but also inspectable.  
Fast, but not reckless.  
Helpful, but never silently dangerous.

---

## Why YAi! exists

We already built **[CrossModel Labs](https://crossmodellabs.com)** to restore trust in AI outputs by comparing multiple models, surfacing hallucinations, exposing disagreements, and helping users understand when a confident answer may not be reliable.

That same philosophy now moves from **AI answers** to **AI actions**.

If AI can hallucinate facts, it can also hallucinate plans.  
If models can disagree on truth, they can also disagree on what is safe.  
If a chatbot can fabricate a citation, an agent can propose a risky command.

So YAi! starts from one principle:

> AI should propose.  
> The runtime should verify.  
> The human should approve.

CrossModel Labs is backed by **Microsoft for Startups**, and YAi! follows the same core mission: make AI more useful by making it more trustworthy.

---

## About the founder

**Umberto Giacobbi** is a senior technology advisor, founder, and software architect with 25+ years of experience building systems across Europe, the US, and Southeast Asia. He works across AI, legaltech, cloud architecture, backend platforms, and product strategy, with a strong focus on practical execution and operational trust.

More about Umberto: **[umbertogiacobbi.biz](https://umbertogiacobbi.biz)**

---

# The 10 problems we want to fix

</div>

---

<div align="center">

## 1. Tool execution breaks after updates ⚙️

Agent runtimes often move fast, but fast updates can break tools, workflows, filesystem access, or command execution.

### Our promise

YAi! will prioritize stable runtime contracts over feature noise. Core execution paths will be covered by deterministic tests, versioned contracts, and fail-fast behavior. If a tool cannot execute safely, YAi! will say so clearly instead of pretending the job was completed.

</div>

---

<div align="center">

## 2. The agent becomes only a chatbot 💬

When tools are unavailable or broken, an agent can quietly degrade into a normal chatbot while still sounding operational.

### Our promise

YAi! separates chat, planning, and execution. A workflow is only real when a validated execution path exists. If YAi! cannot execute something, it will not simulate execution in prose.

</div>

---

<div align="center">

## 3. Fragile authentication and setup 🔑

API keys, missing headers, invalid models, broken configuration, and incomplete bootstrap states waste time and destroy confidence.

### Our promise

YAi! will include explicit preflight checks for API keys, provider access, model availability, workspace paths, permissions, configuration, and runtime state. Setup problems should be discovered early, locally, and with actionable diagnostics.

</div>

---

<div align="center">

## 4. Weak security boundaries 🛡️

An AI agent with access to local files, terminal commands, browser state, and third-party skills is not just a productivity tool. It is a trust boundary.

### Our promise

YAi! treats the local machine as a protected environment. The model may propose actions, but the runtime enforces policy. Risky actions require approval, workspace boundaries are enforced, and raw shell execution is never the default trust path.

</div>

---

<div align="center">

## 5. Untrusted third-party skills 🧩

Community skills are powerful, but they also create supply-chain risk: hidden instructions, unsafe dependencies, dangerous commands, or unexpected side effects.

### Our promise

YAi! will move toward signed and verifiable skills. Skills should be inspectable, versioned, risk-classified, and optionally digitally signed. The runtime should support allowlists, requirements checks, and workspace-scoped loading before a skill is trusted for execution.

</div>

---

<div align="center">

## 6. Opaque automation 🔍

Users should not have to guess what the agent planned, what it executed, what failed, or what changed on disk.

### Our promise

YAi! makes auditability a product feature. Every meaningful workflow should record the plan, resolved inputs, approval decisions, execution results, artifacts, warnings, errors, and final summary. The user must be able to reconstruct what happened.

</div>

---

<div align="center">

## 7. False confidence and silent failure 🚨

The most dangerous agent is not the one that fails. It is the one that fails and still sounds confident.

### Our promise

YAi! uses structured execution results, not vague success messages. Workflows stop on failed validation, denied approval, unresolved variables, failed execution, failed verification, or audit problems. No silent fallback. No fake success.

</div>

---

<div align="center">

## 8. Operational complexity gets out of control 🧠

Sub-agents, background tasks, hidden state, parallel execution, and complex routing can make systems powerful but hard to reason about.

### Our promise

YAi! starts simple on purpose. The first execution model is linear and inspectable: planner proposes, runtime validates, user approves, typed tool executes, verifier checks, audit records. More autonomy can come later, but not before the trust model is solid.

</div>

---

<div align="center">

## 9. Local model and provider instability 🌐

Local models and remote providers fail in different ways: timeouts, hanging calls, missing credentials, unavailable models, malformed output, or inconsistent latency.

### Our promise

YAi! isolates providers behind explicit health checks, timeouts, structured errors, and replaceable interfaces. Unit tests must not depend on live LLM calls. AI smoke tests must be opt-in, budget-limited, and clearly separated from deterministic runtime tests.

</div>

---

<div align="center">

## 10. Hype exceeds practical value 🚀

Many AI agent tools promise autonomy, but real developer workflows need reliability, control, and clear boundaries more than demos.

### Our promise

YAi! will focus on narrow, useful, verifiable workflows before expanding scope. The first goal is not a magical autonomous assistant. The first goal is a safe local runtime that can execute approved workflows, maintain transparent memory, and produce auditable artifacts.

</div>

---

<div align="center">

# What YAi! stands for

## Local-first by default

Workspace files, memory, configuration, skills, and audit trails should live where the user can inspect and control them.

## Human-readable memory

Memory should be Markdown-first, editable, backup-friendly, and versionable. Hidden memory is not the source of truth.

## Deterministic before AI

Use deterministic code for validation, extraction, path checks, workflow resolution, safety gates, and policy enforcement. Use AI where judgment is useful, not where enforcement is required.

## Explicit approval before mutation

Write-capable and risky operations must cross an approval boundary before execution.

## Typed execution over raw shell

Filesystem operations should execute through typed runtime operations, not arbitrary shell commands generated by a model.

## Workspace boundaries

Operations must stay inside the approved workspace unless a future version introduces explicit, high-friction approval for outside-workspace access.

## Audit trail by design

Every important action should leave a structured trail.

## Fail fast

If something is missing, unsafe, invalid, ambiguous, or unverifiable, YAi! should stop and explain the problem.

## Skill governance

Skills should become inspectable, signed, versioned, risk-classified, and checked before execution.

## Trust over autonomy

YAi! does not optimize for doing the most things automatically.  
YAi! optimizes for doing useful things safely, visibly, and correctly.

---

# Short positioning statement

**YAi! is an OpenClaw-inspired local agent runtime for high-control environments.**

It preserves the useful ideas: skills, workflows, local memory, and assistant-driven automation.

It hardens the dangerous parts: execution, approval, audit, memory mutation, skill trust, and workspace boundaries.

> **YAi! is for builders who want AI assistance without surrendering operational control.**

---

## Learn more

**Repository**

[https://github.com/umbertotechnopreneur/YAi](https://github.com/umbertotechnopreneur/YAi)

**CrossModel Labs**  
[https://crossmodellabs.com](https://crossmodellabs.com)

**Umberto Giacobbi**  
[https://umbertogiacobbi.biz](https://umbertogiacobbi.biz)

</div>