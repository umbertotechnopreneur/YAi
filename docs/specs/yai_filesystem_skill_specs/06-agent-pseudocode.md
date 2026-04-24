# 06 — Agent Pseudocode

This file describes the logic without implementation code.

---

## High-level flow

```text
receive user request
select relevant skill
collect required context
send context + skill rules + user request to frontier model
receive CommandPlan
validate CommandPlan
render approval cards
for each approved card:
    revalidate preconditions
    execute typed operation
    verify result
    update audit trail
    refresh context
    stop if unexpected state
summarize outcome
```

---

## Planner flow

```text
function create_plan(user_request, skill, context_pack):

    if skill is filesystem:
        identify requested filesystem operations

    determine target folder:
        use current_folder from context_pack unless user specified another folder

    normalize all paths:
        convert relative paths to absolute paths
        resolve path traversal
        ensure path is inside workspace root

    inspect existing items:
        detect which targets already exist
        detect conflicts

    for each requested action:
        create a step card
        classify risk
        add mitigation if needed
        add rollback if possible
        add verification criteria

    return CommandPlan
```

---

## Validator flow

```text
function validate_plan(command_plan, context_pack):

    reject if domain is not supported

    reject if workspace root is missing

    for each step:
        reject if typed_operation is missing
        reject if typed_operation type is unsupported
        reject if target path is outside workspace
        reject if write operation does not require approval
        reject if destructive operation is permanent
        reject if overwrite-risk step has no backup mitigation
        reject if verification criteria are missing
        reject if display command and typed operation disagree materially

    return validated plan
```

---

## Approval flow

```text
function process_step_card(step):

    show card to user

    wait for user action:
        Run
        Edit
        Skip
        Cancel Plan

    if Edit:
        update step
        revalidate step
        show card again

    if Skip:
        mark skipped
        continue if safe

    if Cancel:
        cancel remaining plan

    if Run:
        execute approved step
```

---

## Execution flow

```text
function execute_step(step, context_pack):

    revalidate path boundaries

    check preconditions:
        target exists or does not exist as expected
        destination exists or does not exist as expected
        workspace is writable

    if mitigation required:
        execute mitigation first
        verify mitigation result
        stop if mitigation fails

    execute typed operation

    verify expected result

    write audit event

    refresh context

    if verification failed:
        stop plan and ask model for recovery plan
```

---

## Recovery flow

```text
function handle_failure(failed_step, error, updated_context):

    stop automatic continuation

    create failure report:
        failed step
        attempted operation
        error
        current filesystem state
        available rollback operations

    ask frontier model:
        propose recovery plan only
        do not continue original plan blindly

    validate recovery plan

    show recovery cards to user
```

---

## Audit flow

```text
function write_audit_event(event):

    persist:
        plan id
        step id
        timestamp
        user approval
        typed operation
        mitigation operation
        verification result
        error if any
```

---

## Model interaction strategy

Use frontier models for:

```yaml
model_tasks:
  - intent interpretation
  - plan generation
  - conflict explanation
  - recovery plan suggestion
  - user-facing summaries
```

Do not rely on the model for:

```yaml
app_tasks:
  - path security
  - operation execution
  - policy enforcement
  - final risk enforcement
  - workspace boundary validation
  - approval state
```
