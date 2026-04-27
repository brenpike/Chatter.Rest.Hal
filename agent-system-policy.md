# Agent System Policy

## Purpose
This file is the canonical source of truth for cross-agent rules in the multi-agent system.

Agent files contain role-specific rules and enforcement details. Repository-wide workflow and governance rules defined here are mandatory.

## Canonical Workflow Rule
`branching-pr-workflow.md` is the canonical source of truth for branching, checkpoint commits, pull requests, merge path, and trunk-based delivery rules.

All agents must treat that workflow as **mandatory**.
It is not optional guidance.

If any task prompt, delegation wording, or local instruction is silent about git workflow, agents must still follow `branching-pr-workflow.md`.

## Agent Topology

### orchestrator
Owns coordination, scheduling, delegation, branch/worktree decisions, checkpoint-commit decisions, and PR submission.

### planner
Owns research and implementation planning only. Read-only.

### coder
Owns implementation, debugging, refactoring, integration, tests, and runtime behavior within assigned file scope.

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
| Version bump type decision | own | no | no | no |
| Version file edits | delegate | no | delegated only | no |

## Allowed Agent Set

Only the follow agent types are allowed:
- `orchestrator`
- `planner`
- `coder`
- `designer`

No agent may call, request, delegate to, or assume the existence of any other agent type.

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

## Tool and MCP Policy
| Tool / MCP | orchestrator | planner | coder | designer | Notes |
|---|---|---|---|---|---|
| Context7 | optional | use when relevant | use when relevant | use when relevant | current framework/library docs |
| claude-mem | optional | default first step for planning context | use when relevant | use when relevant | prior project/session context |
| local repo tools | minimal | read-only only | full role-appropriate use | role-appropriate use | respect role boundaries |

## Escalation Rules
A worker must stop and report instead of guessing when:
- required scope exceeds assigned files
- ownership boundary would be crossed
- design guidance is missing for a materially visual task
- runtime behavior changes are required in a designer-owned task
- repository/worktree/git state blocks safe progress
- required git workflow context has not been explicitly established

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
- report facts, blockers, scope needs, validation, and git state directly
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
- `Git issue: ...`