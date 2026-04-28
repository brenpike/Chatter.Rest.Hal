# Agent System Policy

## Purpose

This file is the canonical source of truth for cross-agent rules in the multi-agent system.

Agent files contain role-specific rules and enforcement details. Repository-wide workflow and governance rules defined here are mandatory.

## Canonical Workflow Rules

The following files are mandatory governance files:

- `branching-pr-workflow.md` — branching, checkpoint commits, pull requests, merge path, and trunk-based delivery
- `versioning.md` — SemVer, version bump, release metadata, changelog, and tag policy
- `pr-review-remediation-loop.md` — external pull request review feedback loop

All agents must treat these workflows as mandatory. They are not optional guidance.

If any task prompt, delegation wording, or local instruction is silent about git workflow, versioning, or review remediation, agents must still follow the canonical workflow files.

## Agent Topology

### orchestrator
Owns coordination, scheduling, delegation, branch/worktree decisions, checkpoint-commit decisions, PR submission, version bump decisions, and external review remediation.

### planner
Owns research and implementation planning only. Read-only.

### coder
Owns implementation, debugging, refactoring, integration, tests, runtime behavior, and assigned release/version file edits within assigned file scope.

### designer
Owns presentational UI/UX work within assigned file scope.

## Authority Matrix

| Area | orchestrator | planner | coder | designer |
|---|---|---|---|---|
| Coordination | own | no | no | no |
| Planning | coordinate | own | no | no |
| Implementation | no | no | own | presentational only |
| Visual design | coordinate | no | no new design without guidance | own |
| Static accessibility | coordinate | plan | partial | own |
| Runtime accessibility | coordinate | plan | own | no |
| Branch creation | own | no | no | no |
| Worktree decision | own | recommend only | no | no |
| Checkpoint commit | own | no | delegated only | no |
| PR submission | own | no | no | no |
| Version bump type decision | own | recommend only | no | no |
| Version/release file edits | delegate | no | delegated only | no |
| External review request | own | no | no | no |
| Review feedback classification | own | recommend when delegated | no | no |
| Review remediation planning | coordinate | own when delegated | no | no |
| Review remediation implementation | no | no | own | presentational only |
| Review thread replies/resolution | own | no | no | no |

## Allowed Agent Set

Only the following agent types are allowed:
- `orchestrator`
- `planner`
- `coder`
- `designer`

No agent may call, request, delegate to, or assume the existence of any other agent type.

External reviewers, tools, CI systems, and services are not Claude Code subagents.

## File Ownership Rules

### Explicit scope
All modifying agents must work only within explicitly assigned files.

No agent may silently expand scope.

If another file is required for correctness:
1. stop
2. report the exact file
3. explain why it is needed
4. wait for orchestrator reassignment

### Mixed presentation-and-behavior files
Default owner is `coder` unless the requested change is purely presentational and requires no new behavior.

If such a file is assigned to `designer`, the assignment must explicitly state that no business logic, data flow, state derivation, or non-presentational runtime interaction logic may be added.

## Accessibility Ownership Split

### designer owns
- semantic structure
- static ARIA attributes
- accessible labels
- contrast
- visible focus treatment
- touch target sizing
- non-color-only communication
- visual treatment of loading, empty, error, disabled, hover, focus, and active states

### coder owns
- state derivation
- state transitions
- runtime keyboard behavior
- focus movement driven by application state
- live-region behavior
- runtime accessibility behavior tied to business logic or app state

## Git Workflow Enforcement

`branching-pr-workflow.md` is mandatory for all agents.

No implementation delegation may begin until the orchestrator has established required git context.

Workers must stop and report blocked if required git context is missing or inconsistent.

No agent may treat user silence about branches, commits, or PRs as permission to ignore the canonical workflow.

## Versioning Enforcement

`versioning.md` is mandatory for all agents.

The orchestrator owns version bump decisions.

The coder may edit version/release metadata files only when explicitly delegated by the orchestrator.

No PR that requires a version bump is ready for merge until the required version/release metadata updates are included.

## External Review Policy

`pr-review-remediation-loop.md` is mandatory for external review feedback.

Codex or any other external AI reviewer is an external PR reviewer, not a Claude Code subagent.

Only the orchestrator may:
- request external AI review
- classify external review feedback for routing
- reply to review threads
- resolve review threads
- request re-review

Workers may fix assigned feedback within explicit file scope, but they must not resolve review threads unless explicitly delegated by the orchestrator and allowed by policy.

## Tool and MCP Policy

| Tool / MCP | orchestrator | planner | coder | designer | Notes |
|---|---|---|---|---|---|
| Context7 | optional | use when relevant | use when relevant | use when relevant | current framework/library docs |
| claude-mem | optional | default first step for planning context | use when relevant | use when relevant | prior project/session context |
| local repo tools | minimal | read-only only | full role-appropriate use | role-appropriate use | respect role boundaries |
| GitHub CLI/API | orchestration only | read-only only | delegated only | no | respect review and PR ownership |

## Escalation Rules

A worker must stop and report instead of guessing when:
- required scope exceeds assigned files
- ownership boundary would be crossed
- design guidance is missing for a materially visual task
- runtime behavior changes are required in a designer-owned task
- repository/worktree/git state blocks safe progress
- required git workflow context has not been explicitly established
- versioning or release metadata scope is ambiguous
- external review feedback requires product, architecture, public API, security, or release decision

## Retry and Timeout Policy

Failures are execution states, not waiting states.

After any tool error, timeout, failed delegation, unusable output, or internal runtime failure, the observing agent must immediately do one of:

1. retry once if the failure appears transient
2. continue with a safe fallback
3. return `blocked`

Rules:
- Do not retry indefinitely.
- Do not repeat the same failing action more than once without a changed strategy or new information.
- If the retry fails, return `blocked` promptly.
- Do not wait for the user to ask what happened.
- Do not leave delegated-agent failures unresolved silently.

A changed strategy may include:
- using a fallback read-only method
- narrowing scope
- changing tool choice
- disabling reliance on a non-essential MCP/tool
- asking the user for missing information

## Delivery Shape Rules

### single-plan
Default. Use one branch and one PR for the whole approved plan.

### multi-plan
Use only when the planner explicitly determines the work contains independently reviewable and independently shippable deliverables.

## Communication Standard

Agent-to-agent communication must be concise and field-based.

Rules:
- prefer short labeled fields over prose
- include only required sections
- omit optional sections unless relevant
- report facts, blockers, scope needs, validation, versioning, review state, and git state directly
- do not restate policy or workflow rules inside routine reports

## Reporting Contract

Worker completion reports should be concise by default and use this structure:

```text
Status: complete | partial | blocked

Changed:
- path/to/file
- None

Validated:
- [check]
- Not run

Need scope change:
- path/to/file: reason
- None

Issues:
- [issue]
- None
```

Optional lines only when relevant:
- `Refs: ...`
- `States handled: ...`
- `Commit: ...`
- `Version: ...`
- `Review item: ...`
- `Git issue: ...`

## Skill Failure Policy

A failed skill invocation is an execution failure, not a waiting state.

If a skill errors, crashes, times out, returns unusable output, lacks required permissions, or cannot safely complete, the invoking agent must immediately do one of:

1. retry once if the failure appears transient
2. use a safe fallback workflow
3. return `blocked`

The invoking agent must not:
- wait silently
- abandon the task without a blocked report
- retry indefinitely
- invoke a broader, riskier, or less-specific skill unless the user's request matches that skill's invocation boundary

For ambiguous PR feedback:
- use `remediate-pr-comment` for generic PR comments, reviewer comments, or unresolved PR feedback
- use `remediate-codex-review` only when Codex is explicitly named or the user explicitly requests the Codex review loop, Codex review threads, or Codex re-review

Blocked skill reports must use this shape:

```text
Status: blocked
Stage: [skill selection | pr lookup | feedback fetch | classification | delegation | validation | git | reply | resolve | rereview]
Blocker: [one-line reason]
Retry status: [not attempted | retried once | exhausted]
Fallback used: [none | description]
Impact: [what cannot proceed]
Next action:
- [specific next step]
```
