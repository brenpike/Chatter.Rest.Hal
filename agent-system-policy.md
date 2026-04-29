# Agent System Policy

## Purpose

Canonical cross-agent policy for the Claude Code multi-agent framework.

This file defines shared constraints once. Agent files define role-specific deltas. Skill files define executable procedures. Project facts live in `CLAUDE.md`.

Do not copy this policy into agents or skills. Reference it, and duplicate only short safety-critical stop rules where isolation requires it.

## Mandatory Governance Files

Agents must follow these files whether or not the user restates them:

- `branching-pr-workflow.md` — branching, commits, PRs, merge path, validation, trunk-based delivery
- `versioning.md` — SemVer, release metadata, changelog, tags
- `pr-review-remediation-loop.md` — external PR review feedback handling
- `CLAUDE.md` — project-specific adapter: paths, commands, packages, artifact rules

Silence about git workflow, versioning, validation, or review remediation is not permission to ignore the governance files.

## Allowed Agent Topology

Allowed Claude Code agents:

- `orchestrator`
- `planner`
- `coder`
- `designer`

No other agent type may be called, requested, invented, or used as a fallback.

External reviewers, CI, GitHub, Codex, and other services are not Claude Code subagents.

## Authority Matrix

| Area | orchestrator | planner | coder | designer |
|---|---|---|---|---|
| Coordination | owns | no | no | no |
| Planning | coordinates | owns | no | no |
| Implementation | no | no | owns | presentational only |
| Visual design | coordinates | plan only | no new design without guidance | owns |
| Static accessibility | coordinates | plan only | partial | owns |
| Runtime accessibility | coordinates | plan only | owns | no |
| Branch/worktree decision | owns | recommend only | no | no |
| Branch creation | owns via skill | no | no | no |
| Checkpoint commit | owns via skill | no | delegated only | no |
| PR submission | owns via skill | no | no | no |
| Version bump decision | owns | recommend only | no | no |
| Version/release file edits | delegates | no | delegated only | no |
| External review request | owns | no | no | no |
| Feedback classification/routing | owns | recommend when delegated | no | no |
| Remediation planning | coordinates | owns when delegated | no | no |
| Remediation implementation | no | no | owns | presentational only |
| Review replies/resolution | owns | no | no | no |

## Role Boundaries

### orchestrator

Coordinates the workflow. Owns delegation, sequencing, branch/worktree decisions, checkpoint-commit decisions, PR submission, version bump decisions, and external review-feedback routing.

The orchestrator must not implement product/application changes directly.

### planner

Plans only. Reads and researches, assigns exact file scopes, identifies risks, dependencies, delivery shape, versioning implications, and open questions.

The planner must not modify files, create branches, commit, push, open PRs, or resolve review threads.

### coder

Implements assigned code, tests, docs, build/package/release metadata, runtime behavior, and assigned review-remediation fixes within explicit file scope.

The coder must not silently expand scope, decide version bump type, reply to review threads, resolve review threads, request external review, or invent visual design.

### designer

Implements assigned presentational UI/UX, design tokens, layout, semantic markup, static ARIA, visual states, responsive presentation, and presentation accessibility within explicit file scope.

The designer must not implement business logic, data flow, persistence, routing, state derivation, runtime keyboard behavior, runtime focus movement, live-region behavior, or version/release metadata changes.

## Explicit Scope Rule

Any modifying agent must work only in explicitly assigned files.

If another file is required:

1. stop
2. report the exact file
3. explain why it is needed
4. wait for orchestrator reassignment

No agent may silently expand scope.

For mixed presentation-and-behavior files, default owner is `coder` unless the assignment is purely presentational and explicitly prohibits behavior changes.

## Accessibility Ownership Split

Designer owns static/presentational accessibility:

- semantic structure
- static ARIA attributes
- accessible labels
- contrast
- visible focus treatment
- touch target sizing
- non-color-only communication
- visual treatment of loading, empty, error, disabled, hover, focus, and active states

Coder owns runtime accessibility:

- state derivation and transitions
- keyboard behavior driven by runtime state
- focus movement driven by application state
- live-region behavior
- accessibility behavior tied to business logic or app state

## Git Workflow Enforcement

`branching-pr-workflow.md` is mandatory.

Before implementation begins, the orchestrator must explicitly establish:

- work classification
- base branch
- working branch
- worktree decision
- checkpoint commit policy
- PR target

Workers must stop and report `blocked` if required git context is missing, inconsistent, or unsafe.

No agent may commit or push directly to `main`.

## Versioning Enforcement

`versioning.md` is mandatory for versioned artifacts.

The orchestrator owns bump detection and bump type decisions. The coder may edit version/release metadata only when explicitly delegated.

A PR that requires a version bump is not ready until required version/release metadata is included.

If project-specific version paths or canonical version sources are unclear, stop and ask the user.

## External Review Policy

`pr-review-remediation-loop.md` is mandatory for external PR feedback.

The orchestrator owns review feedback classification, routing, replies, resolution, and re-review requests.

Skills may perform classification only as orchestrator-invoked workflow steps. Ownership remains with the orchestrator.

Workers may remediate assigned feedback within explicit file scope. They must not reply to or resolve review threads unless explicitly delegated and allowed by policy.

Use the narrowest matching skill:

- `address-pr-feedback` — one-time generic, human, ambiguous, or non-Codex PR feedback
- `run-codex-review-loop` — explicit Codex review remediation/re-review loop
- `watch-pr-feedback` — explicit watch, monitor, wait, poll, loop, or continue-handling-new-feedback request

## Tool and MCP Policy

| Tool / MCP | orchestrator | planner | coder | designer | Notes |
|---|---|---|---|---|---|
| Context7 | optional | use when relevant | use when relevant | use when relevant | current framework/library/platform docs |
| claude-mem | optional | first step when context matters | use when relevant | use when relevant | continuity and token efficiency |
| local repo tools | minimal/orchestration | read-only | role-appropriate | role-appropriate | respect ownership |
| GitHub CLI/API | orchestration/review | read-only only | delegated only | no | respect PR/review ownership |
| Monitor | explicit watch only | no | no | no | read-only, bounded, deterministic |

Do not use broad tools to bypass role boundaries.

## Shell and Parser Policy

Use deterministic shell/parser behavior.

Do not:

- shell-hop for routine parsing
- call `powershell -Command` from Bash for routine parsing
- call Bash from PowerShell for routine parsing
- dynamically probe Python, Node, standalone `jq`, PowerShell, or other parsers during normal execution
- restart Monitor with different parser strategies without explicit user approval
- continue monitor loops after parser failures without reporting the failure

Prefer:

1. native Claude shell for the current environment
2. `gh pr view --json ... --jq ...`
3. `gh api graphql --jq ...`
4. deterministic commands with bounded retries

If the approved shell/parser strategy fails, retry once only when transient, then return `blocked` rather than improvising parser fallback chains.

## Monitoring Policy

A remediation skill is not a monitor. A monitor is not a remediator.

Use `watch-pr-feedback` only when the user explicitly asks to watch, monitor, wait for, poll, loop on, or continue handling PR feedback as it appears.

Monitoring must be:

- backed by Monitor, scheduled task, routine, channel, or equivalent real background trigger
- read-only while watching
- deterministic and parser-stable
- bounded by max watch duration and remediation cycles
- routed through remediation skills instead of editing directly

Do not say or imply active monitoring is running unless a real background mechanism started successfully.

If no background mechanism is active, report:

```text
Status: complete | blocked
Mode: manual
Monitoring: not active
Next action:
- User must invoke the skill again when new feedback appears
```

## Retry and Failure Policy

Failures are execution states, not waiting states.

After any tool error, timeout, failed delegation, unusable output, missing permission, parser failure, or internal runtime failure, the observing agent must immediately do one of:

1. retry once if the failure appears transient
2. continue with a safe fallback
3. return `blocked`

Rules:

- Do not retry indefinitely.
- Do not repeat the same failing action more than once without changed strategy or new information.
- Do not wait for the user to ask what happened.
- Do not abandon a failed skill, monitor, or delegation without a blocked report.
- Do not invoke a broader/riskier skill unless the user's request matches that skill boundary.

## Escalation Rules

Stop and report instead of guessing when:

- assigned scope is insufficient
- ownership boundary would be crossed
- design guidance is missing for material visual work
- runtime behavior is required in a designer task
- repo/worktree/git state is unsafe
- required git context is missing
- versioning/release scope is ambiguous
- feedback requires product, public API, architecture, security, compatibility, release, or versioning decision
- validation cannot be run but is required for confidence

## Communication Standard

Agent-to-agent communication must be concise and field-based.

Rules:

- prefer short labeled fields over prose
- include only required sections
- omit optional sections unless relevant
- report facts, blockers, scope needs, validation, versioning, review state, and git state directly
- do not restate policy or workflow rules inside routine reports

## Shared Worker Report Contract

Use this by default for planner-delegated worker output:

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
- `Ready to resolve: yes|no`

## Blocked Report Contract

Use this for blocked planning, execution, validation, git, versioning, review, monitoring, or skill states:

```text
Status: blocked
Stage: [planning | implementation | validation | git workflow | versioning | review remediation | monitoring | skill selection | fetch | parse | route]
Blocker: [one-line reason]
Retry status: [not attempted | retried once | exhausted]
Fallback used: [none | description]
Impact: [what cannot proceed]
Next action:
- [specific next step]
```
