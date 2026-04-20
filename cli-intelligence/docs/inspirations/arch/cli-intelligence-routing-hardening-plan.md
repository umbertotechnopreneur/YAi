# CLI-Intelligence Routing Hardening and Seamless Local→Remote Failover Plan

## Purpose

This document defines a precise implementation plan to harden the AI routing layer in CLI-Intelligence, reduce fragility, and introduce a seamless automatic switch from the local model to the remote provider after a configurable timeout.

Target audience: developers working on the C#/.NET codebase.

---

## Executive Summary

The current codebase already contains three important building blocks:

1. a routing policy (`DefaultAiRoutingPolicy`) that decides local vs remote before the request is sent;
2. a router (`AiRouter`) that resolves the selected backend;
3. a partial failover mechanism inside `AiInteractionService.CallModelAsync()` that retries on the frontier provider only for a limited subset of task kinds when the first backend is local.

This is a good start, but the behavior is still fragile and not yet truly seamless.

The main issues are:

- failover is implemented in the interaction layer, not as a first-class routing strategy;
- failover is limited to only some task kinds;
- timeout behavior is implicit and backend-specific rather than policy-driven;
- observability is incomplete because the request/response log path still uses the original client identity even when fallback occurs;
- local-vs-remote eligibility is split between policy logic and ad hoc flags such as `LocalEnabled`;
- the routing policy is deterministic, but the failure-handling model is not yet explicit.

This plan proposes a controlled evolution toward:

- policy-based pre-routing;
- policy-based failover;
- configurable timeout-driven local→remote retry;
- typed failure classification;
- consistent usage and diagnostic logging;
- clear UX and settings for developers and operators.

---

## Current Observed State

### 1. Initial routing is policy-based

`AiRouter.Resolve()` returns either the local client or the frontier client based only on `IAiRoutingPolicy.Decide()`. There is no multi-step orchestration in the router itself. The router is a single-choice resolver. 

### 2. The default policy is strict and mostly remote-first

`DefaultAiRoutingPolicy` sends requests to the frontier provider when any of the following is true:

- destructive candidate;
- high-confidence requirement;
- estimated prompt tokens greater than 4000.

It only prefers local for `GeneralChat` when `LocalEnabled` is true. It also routes some task kinds such as `IntentClassification`, `Extraction`, `MemoryDraft`, and `ToolPlanning` to local by default, while most other task kinds remain frontier-first.

### 3. A partial fallback already exists, but only inside `AiInteractionService`

`AiInteractionService.CallModelAsync()` currently wraps the call in a `try/catch` and retries with a frontier request only when:

- the original backend name starts with `Llama:`; and
- `ShouldFallbackToFrontier()` returns true.

`ShouldFallbackToFrontier()` currently returns true only for:

- `IntentClassification`
- `Extraction`
- `MemoryDraft`
- `ToolPlanning`

Therefore, a normal local chat timeout or local chat HTTP failure does **not** currently guarantee retry on the remote provider.

### 4. Local timeout is already configurable, but only at the client level

`LlamaAiClient` constructs its own `HttpClient` and sets:

- `BaseAddress` from `Llama.Url`
- `Timeout` from `Llama.TimeoutSeconds`

This means timeout exists today, but it is only a transport timeout. It is not yet a formal routing/failover rule.

### 5. The UI already exposes local timeout settings

`LocalModelSettingsScreen` already allows changing:

- model URL;
- model name;
- context length;
- temperature;
- top-p;
- max tokens;
- request timeout.

So the settings surface already exists and should be extended rather than replaced.

### 6. There is a correctness/observability issue in the current failover path

If the first request fails on local and the code retries remotely, the interaction logger still records request/response metadata against the original `client` variable in some log messages. That produces a mismatch between:

- the backend that actually answered;
- the backend shown in the verbose log entries.

The structured usage result can still reflect the returned provider, but the verbose interaction log becomes ambiguous.

### 7. `LocalEnabled` is inconsistently propagated

The routing policy uses `AiRequestContext.LocalEnabled`, but this field is not obviously populated in every call site. This makes the behavior dependent on each caller rather than being centrally inferred from configuration.

---

## Design Goals

The improved system must satisfy the following goals.

### Functional goals

- Use the local backend when appropriate.
- Automatically retry on the remote provider when the local backend fails for allowed scenarios.
- Support configurable local timeout with a default of **60 seconds** for seamless failover.
- Avoid duplicate retry storms.
- Preserve deterministic behavior for high-confidence or destructive operations.

### Reliability goals

- Distinguish routing failures from model failures.
- Distinguish retryable failures from terminal failures.
- Make failover explicit and traceable.
- Prevent silent masking of systematic local backend problems.

### Developer goals

- Keep the design understandable.
- Centralize routing decisions.
- Centralize failure classification.
- Avoid scattering fallback logic across screens and services.

### UX goals

- The end user should not need to know that the request switched providers unless the UI explicitly wants to show it.
- Provider badges and logs must still reflect the actual answering backend.
- Configuration should remain simple.

---

## Target Behavior

### High-level routing contract

For each request, the system should perform up to two stages:

1. **Primary route decision**
   - Decide local vs remote before sending the request.

2. **Failover decision**
   - If the primary backend was local and the failure is retryable, evaluate whether failover to remote is allowed for this request type.

No remote→local retry should be introduced in this phase.

### Default failover rule

For the first implementation, use this default:

- if primary backend is local;
- and local call fails because of timeout, connection failure, HTTP 5xx, invalid/empty response, or explicit local context overflow;
- and request is not explicitly pinned to local-only;
- then retry once on remote.

### Default timeout rule

- `Llama.TimeoutSeconds` should default to **60**.
- Local timeout should be interpreted as **retryable**.
- Timeout should trigger one remote retry if failover is enabled.

### Default non-failover rule

Do **not** fail over when:

- the request was already routed to remote;
- failover is disabled;
- the failure is classified as non-retryable;
- the request is marked local-only;
- the request has already been retried once.

---

## Proposed Architecture Changes

## 1. Introduce explicit failover settings

### New configuration section

Extend `LlamaSection` with the following fields:

```json
"Llama": {
  "Enabled": true,
  "Url": "http://HCMC-SEVER:9090",
  "Model": "gemma-3-4b-it-Q4_K_M.gguf",
  "ContextLength": 8196,
  "Temperature": 0.7,
  "TopP": 0.9,
  "MaxTokens": 2048,
  "TimeoutSeconds": 60,
  "EnableRemoteFailover": true,
  "FailoverOnTimeout": true,
  "FailoverOnHttp5xx": true,
  "FailoverOnConnectionError": true,
  "FailoverOnInvalidResponse": true,
  "FailoverOnContextOverflow": true,
  "MaxFailoverAttempts": 1
}
```

### Required model changes

Add properties to `LlamaSection`:

- `bool EnableRemoteFailover { get; set; } = true;`
- `bool FailoverOnTimeout { get; set; } = true;`
- `bool FailoverOnHttp5xx { get; set; } = true;`
- `bool FailoverOnConnectionError { get; set; } = true;`
- `bool FailoverOnInvalidResponse { get; set; } = true;`
- `bool FailoverOnContextOverflow { get; set; } = true;`
- `int MaxFailoverAttempts { get; set; } = 1;`

### Notes

- Keep `TimeoutSeconds` in the same section because it still belongs to the local client transport configuration.
- The failover flags belong there as operational settings for local-backend behavior.

---

## 2. Extend `AiRequestContext` with explicit routing controls

Today `AiRequestContext` mixes task semantics with local eligibility, but it does not clearly express retry/failover intent.

Add the following properties:

```csharp
public bool PreferLocal { get; init; }
public bool AllowLocal { get; init; } = true;
public bool AllowRemote { get; init; } = true;
public bool AllowFailoverToRemote { get; init; } = true;
public bool LocalOnly { get; init; }
public bool RemoteOnly { get; init; }
public int AttemptNumber { get; init; } = 0;
public string? CorrelationId { get; init; }
```

### Why this matters

This makes intent explicit and removes hidden semantics from `LocalEnabled` alone.

### Compatibility recommendation

Keep `LocalEnabled` temporarily for backward compatibility, but deprecate it internally. The policy should migrate to `AllowLocal` + `PreferLocal`.

---

## 3. Introduce a typed failure classifier

Create a new component, for example:

- `AiFailureKind`
- `AiFailureClassifier`

### Suggested enum

```csharp
enum AiFailureKind
{
    Unknown,
    Timeout,
    ConnectionError,
    Http4xx,
    Http5xx,
    InvalidResponse,
    EmptyResponse,
    ContextOverflow,
    Cancelled,
    Unauthorized,
    DisabledBackend,
    NonRetryable
}
```

### Suggested classifier contract

```csharp
interface IAiFailureClassifier
{
    AiFailureKind Classify(Exception ex, string backendName, string? responseBody = null);
    bool IsRetryableForRemoteFailover(AiFailureKind kind, LlamaSection config);
}
```

### Classification guidance

#### Timeout
Classify as `Timeout` when the exception chain contains:

- `TaskCanceledException` caused by `HttpClient.Timeout`
- `TimeoutException`

Important: distinguish transport timeout from user cancellation where possible.

#### Connection error
Classify as `ConnectionError` when the chain contains:

- `HttpRequestException`
- socket-level connect failures
- DNS resolution failures
- refused connection

#### HTTP status
When `EnsureSuccessStatusCode()` fails, classify by status class:

- 4xx → `Http4xx`
- 5xx → `Http5xx`

#### Invalid response
Classify as `InvalidResponse` or `EmptyResponse` when:

- JSON parsing fails;
- expected `choices[0].message.content` path is missing;
- content is null or whitespace.

#### Context overflow
For local servers that expose overflow errors as message text instead of status codes, inspect the message for patterns such as:

- `context`
- `n_ctx`
- `prompt too long`
- `maximum context`
- `token limit`

This should be a configurable regex-based detection if needed later.

---

## 4. Move failover orchestration into a dedicated service

The current failover logic in `AiInteractionService` works, but it is too embedded and too limited.

Create a new service such as:

- `AiExecutionService`
- or `ResilientAiInvoker`

### Proposed responsibilities

This new service should:

- resolve the primary backend;
- execute the request;
- classify failures;
- decide whether failover is permitted;
- execute the fallback request;
- return the final result plus execution metadata.

### Suggested result object

```csharp
sealed class AiExecutionOutcome
{
    public required string FinalBackendName { get; init; }
    public required bool UsedFailover { get; init; }
    public string? InitialBackendName { get; init; }
    public AiFailureKind? InitialFailureKind { get; init; }
    public string? InitialFailureMessage { get; init; }
    public required AiClientResult Result { get; init; }
}
```

### Why this is better

This separates:

- routing;
- execution;
- resilience;
- logging.

`AiInteractionService` should become a logging/orchestration wrapper, not the place where backend resilience rules live.

---

## 5. Keep `AiRouter` simple, but make its role explicit

`AiRouter` should remain a selector, not an execution engine.

That is good.

However, the new resilient execution flow should use the router twice when necessary:

- once for primary route resolution;
- once for fallback route resolution.

### Example flow

1. Build primary context.
2. Resolve primary backend.
3. Execute.
4. On retryable local failure, build fallback context with:
   - `RemoteOnly = true`
   - `RequiresHighConfidence = true`
   - `AttemptNumber = 1`
5. Resolve again.
6. Execute remote.

This preserves the routing policy as the authoritative selector.

---

## 6. Fix the policy semantics

### Problem

`DefaultAiRoutingPolicy` currently mixes:

- pre-routing safety rules;
- task-kind defaults;
- a one-off `LocalEnabled && TaskKind == GeneralChat` preference.

### Required correction

Refactor the policy to work on explicit routing intent.

### Suggested policy order

1. `RemoteOnly` → frontier
2. `LocalOnly` → local
3. destructive candidate → frontier
4. requires high confidence → frontier
5. prompt too large for local → frontier
6. `PreferLocal && AllowLocal` → local
7. task-kind defaults
8. final default → frontier

### Important improvement

Use the actual configured local context threshold rather than hard-coding `4000`.

For example:

```csharp
var safeLocalPromptLimit = Math.Min(configuredLocalContext * 70 / 100, 6000);
```

This is safer than a fixed threshold and scales with the configured local model.

If you do not want to pass config into the policy yet, introduce a dedicated routing settings object.

---

## 7. Make local context overflow a pre-routing condition and a failover condition

### Pre-routing

If `ApproxPromptTokens` already exceeds a safe local threshold, go remote immediately.

### Runtime

If the local server still rejects the request because of context overflow, classify it as `ContextOverflow` and fail over to remote if enabled.

This two-layer approach is important because:

- prompt estimates are approximate;
- server-side tokenization may differ;
- system prompt growth may push a borderline request over the limit.

---

## 8. Improve observability and correctness of logs

### Current issue

When fallback occurs, the code logs request/response against the original backend variable in some paths.

### Required change

Every execution attempt must be logged separately.

### Suggested log model

For each attempt, capture:

- correlation id;
- request id or attempt id;
- attempt number;
- backend name;
- task kind;
- request size;
- elapsed ms;
- success/failure;
- failure kind;
- whether this was primary or fallback.

### Example verbose log sequence

```text
REQUEST attempt=0 backend=Llama:gemma-3-4b-it-Q4_K_M.gguf task=GeneralChat ...
ERROR   attempt=0 backend=Llama:gemma-3-4b-it-Q4_K_M.gguf failure=Timeout ...
REQUEST attempt=1 backend=OpenRouter:anthropic/claude-3-haiku task=GeneralChat reason=LocalTimeoutFailover ...
RESPONSE attempt=1 backend=OpenRouter:anthropic/claude-3-haiku success=true ...
```

### Structured usage logging

Extend usage or a sibling log row to include:

- `attempt_number`
- `initial_backend`
- `final_backend`
- `used_failover`
- `failure_kind`
- `failure_stage`

Do not overwrite the original provider with the wrong backend label.

---

## 9. Improve `LlamaAiClient` error surfaces

### Current state

`LlamaAiClient` uses `EnsureSuccessStatusCode()` and throws generic exceptions.

### Required improvement

Wrap transport and payload errors with richer context.

### Recommended changes

- catch `HttpRequestException` and preserve `StatusCode` when present;
- catch `TaskCanceledException` and detect timeout vs external cancellation;
- capture a bounded copy of response body on HTTP failures when safe;
- throw typed exceptions or at least exception messages with stable prefixes.

### Example

```csharp
throw new LocalAiException(
    kind: AiFailureKind.Http5xx,
    message: $"Local model returned HTTP {(int)statusCode}.",
    statusCode: statusCode,
    responsePreview: preview);
```

If you prefer not to create custom exception types yet, the classifier can still work on wrapped exceptions, but typed exceptions are cleaner.

---

## 10. Make the fallback timeout configurable and default to 60 seconds

### Required change

Change the default in:

- `AppConfig.Llama.TimeoutSeconds`
- default `appsettings.json`
- UI documentation/help text if present

from `120` to `60`.

### Reasoning

For seamless failover, a user-facing wait of 120 seconds before switching is too long for interactive chat. A 60-second local timeout is still generous while allowing the system to recover in a reasonable time.

### Recommendation

Use these defaults for now:

- `TimeoutSeconds = 60`
- `EnableRemoteFailover = true`
- `MaxFailoverAttempts = 1`

---

## 11. Extend the settings UI

`LocalModelSettingsScreen` already supports changing request timeout. Extend it with failover controls.

### Add new menu actions

- Toggle remote failover enable/disable
- Toggle failover on timeout
- Toggle failover on HTTP 5xx
- Toggle failover on connection error
- Toggle failover on invalid response
- Toggle failover on context overflow
- Change max failover attempts

### Rendered settings table should show

- local enabled
- timeout seconds
- remote failover enabled
- each failover condition flag
- max failover attempts

### UX recommendation

Keep the wording operational and explicit.

Good labels:

- `Remote failover enabled`
- `Fail over when local request times out`
- `Fail over when local server returns 5xx`
- `Fail over when local response is invalid or empty`

---

## 12. Standardize caller behavior

### Problem

Some call sites populate `AiRequestContext.LocalEnabled`, some may not.

### Required change

Create helper builders for request contexts so screens do not manually assemble routing-critical fields inconsistently.

### Suggested helper

```csharp
sealed class AiRequestContextFactory
{
    public AiRequestContext CreateGeneralChat(...)
    public AiRequestContext CreateExplanation(...)
    public AiRequestContext CreateExtraction(...)
    public AiRequestContext CreateToolPlanning(...)
}
```

### Minimum rule

Every call site must explicitly set:

- task kind;
- approximate prompt tokens;
- confidence requirement;
- local/remote permissions;
- failover permission.

Do not rely on partially populated context.

---

## Suggested Default Routing Matrix

This should be the baseline behavior after refactor.

| Scenario | Primary backend | Failover allowed | Notes |
|---|---|---:|---|
| Interactive general chat, small prompt | Local preferred | Yes | best UX/cost mix |
| Interactive general chat, huge prompt | Remote | No | pre-route remote |
| Tool planning | Local preferred | Yes | current behavior kept |
| Extraction | Local preferred or config-driven | Yes | if local extraction enabled |
| Explanation | Remote | Not needed | high-confidence by default |
| Final answer / one-shot query | Remote | Not needed | current semantics preserved |
| Deep reasoning | Remote | Not needed | remote-first |
| Destructive candidate | Remote | No | safety-first |
| Local-only diagnostic test | Local | No | explicit local-only mode |

---

## Implementation Steps

## Phase 1 — Safe hardening

1. Change `TimeoutSeconds` default from 120 to 60.
2. Add failover settings to `LlamaSection`.
3. Update `appsettings.json` defaults.
4. Update `LocalModelSettingsScreen` to expose the new flags.
5. Fix verbose logging so fallback attempts record the actual backend used.
6. Ensure `AiRequestContext` always receives explicit routing flags from all call sites.

### Acceptance criteria

- user can set timeout to 60 from settings;
- fallback attempts are visible in logs;
- backend labels in logs are correct after fallback;
- no behavior regression in existing chat flow.

---

## Phase 2 — Failure classification

1. Add `AiFailureKind`.
2. Add a failure classifier.
3. Update `LlamaAiClient` to surface richer failure information.
4. Replace generic fallback conditions with classifier-based retry decisions.

### Acceptance criteria

- timeout is classified reliably;
- invalid response is classified reliably;
- local HTTP 5xx is classified reliably;
- non-retryable failures do not trigger fallback.

---

## Phase 3 — Dedicated resilient execution layer

1. Introduce `AiExecutionService` or equivalent.
2. Move retry/failover orchestration out of `AiInteractionService`.
3. Let `AiInteractionService` focus on logging, usage capture, and extraction trigger.
4. Return execution metadata with initial backend, final backend, and failover state.

### Acceptance criteria

- single place owns primary execution + fallback;
- `AiInteractionService` no longer contains task-kind-specific failover lists;
- final backend is always known.

---

## Phase 4 — Policy cleanup

1. Refactor `DefaultAiRoutingPolicy` to use explicit local/remote intent flags.
2. Remove dependence on scattered `LocalEnabled` semantics.
3. Replace fixed `4000` prompt threshold with configurable or derived local-safe threshold.
4. Keep routing deterministic and easy to reason about.

### Acceptance criteria

- routing decision can be explained from context + policy only;
- local-safe threshold is not magic-number driven;
- screens do not embed routing policy manually.

---

## Recommended Code-Level Changes by File

### `Models/AppConfig.cs`

Add:

- `EnableRemoteFailover`
- `FailoverOnTimeout`
- `FailoverOnHttp5xx`
- `FailoverOnConnectionError`
- `FailoverOnInvalidResponse`
- `FailoverOnContextOverflow`
- `MaxFailoverAttempts`

Also change default `TimeoutSeconds` to 60.

### `appsettings.json`

Update the `Llama` section accordingly.

### `Services/AI/AiRequestContext.cs`

Add explicit routing/failover fields.

### `Services/AI/DefaultAiRoutingPolicy.cs`

Refactor route order and remove hidden dependence on one-off flags where possible.

### `Services/AI/LlamaAiClient.cs`

Improve exception surfaces and preserve context for the failure classifier.

### `Services/AiInteractionService.cs`

Short term:

- fix logging mismatch during fallback;
- stop using task-kind hardcoded fallback list directly in this service.

Long term:

- delegate execution and fallback to a dedicated resilient executor.

### `Screens/LocalModelSettingsScreen.cs`

Expose failover flags and max failover attempts.

### `Program.cs`

Register any new services such as:

- `IAiFailureClassifier`
- `AiExecutionService`
- `AiRequestContextFactory`

---

## Test Plan

## Unit tests

### Routing policy tests

- destructive candidate routes remote
- high-confidence routes remote
- prompt above local-safe threshold routes remote
- general chat with `PreferLocal=true` routes local
- local-only routes local
- remote-only routes remote

### Failure classification tests

- timeout exception -> `Timeout`
- refused connection -> `ConnectionError`
- HTTP 503 -> `Http5xx`
- malformed JSON -> `InvalidResponse`
- empty content -> `EmptyResponse`
- context overflow text -> `ContextOverflow`

### Failover decision tests

- timeout with failover enabled -> retry remote
- timeout with failover disabled -> throw
- HTTP 5xx with failover enabled -> retry remote
- invalid response with failover enabled -> retry remote
- local-only context -> do not fail over
- second failure after one retry -> stop

## Integration tests

### Simulated local timeout

- configure local endpoint to hang longer than timeout
- verify fallback executes once on remote
- verify final response returns successfully
- verify logs show two attempts

### Simulated local 500

- local server returns 500
- verify remote retry
- verify final provider badge reflects remote

### Simulated invalid local payload

- local server returns malformed JSON or missing message content
- verify retry to remote

### No-fallback scenario

- explanation request should route remote first
- no local attempt should be performed

### Logging correctness

- verify initial and final backend names are correctly recorded
- verify `used_failover=true` is present when applicable

---

## Risks and Mitigations

## Risk 1 — Silent masking of a broken local backend

If failover always succeeds remotely, developers may not notice that the local server is unhealthy.

### Mitigation

- log warnings on every failover;
- add counters for failover frequency;
- optionally show a non-blocking status indicator in the local settings screen.

## Risk 2 — Duplicate cost increase

Automatic remote retry may increase spend if the local backend fails often.

### Mitigation

- retry at most once;
- make failover configurable;
- log all failovers for later review.

## Risk 3 — Wrong backend telemetry

If logging still uses stale backend names, operations become misleading.

### Mitigation

- log each attempt independently;
- store initial and final backend names separately.

## Risk 4 — Over-coupled policy and UI

If screens keep constructing routing flags manually, behavior will drift.

### Mitigation

- centralize context creation.

---

## Recommended Immediate Actions

These are the first concrete changes I recommend implementing now, in order.

1. Change local timeout default from 120s to 60s.
2. Fix fallback logging so the actual answering backend is recorded correctly.
3. Extend settings with `EnableRemoteFailover` and `MaxFailoverAttempts`.
4. Expand failover beyond the current narrow task list to cover general local chat failures too.
5. Introduce typed failure classification.
6. Move failover orchestration into a dedicated execution service.
7. Refactor routing policy to remove magic thresholds and implicit local eligibility.

---

## Final Recommendation

The current system is close to being solid, but it is still half-way between:

- a simple single-route selector; and
- a resilient multi-backend execution pipeline.

The right move is **not** to add more ad hoc `try/catch` logic in screens or services.

The right move is to formalize:

- routing intent;
- failure classification;
- retry/failover policy;
- attempt-level observability.

With the changes above, CLI-Intelligence will gain:

- better UX;
- cleaner architecture;
- lower fragility;
- reliable seamless local→remote fallback after a 60-second configurable timeout;
- more trustworthy diagnostics for developers.

