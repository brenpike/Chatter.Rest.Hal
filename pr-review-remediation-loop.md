# PR Review Remediation Loop

## Purpose

Defines how Claude agents respond to external pull request review feedback, including Codex GitHub reviews.

Codex and other external AI reviewers are external reviewers, not Claude Code subagents.

## Ownership

The orchestrator owns the loop:

- request external review
- check feedback
- identify unresolved review threads/comments
- classify and route feedback
- verify fixes are committed and pushed
- reply to review threads/comments
- resolve review threads
- request re-review
- stop safely

Skills may execute loop steps only when invoked by the orchestrator. Ownership remains with the orchestrator.

## Entry Criteria

Start only after:

- a PR exists
- the PR branch has been pushed
- required validation completed or is known to be in progress
- external review was requested or feedback already exists

## Feedback Sources

Check:

- unresolved PR review threads
- inline PR review comments
- top-level PR comments
- requested-changes or commented review summaries
- CI failures when relevant to the review feedback

## Classification

Classify every review item as one of:

- `actionable-code-change`
- `actionable-test-change`
- `actionable-doc-change`
- `architecture-or-contract-concern`
- `design-or-UX-concern`
- `version-or-release-concern`
- `question-needs-user-input`
- `non-actionable`
- `incorrect-or-rejected`

Do not silently ignore review feedback.

## Routing

- `coder`: source, tests, docs, build, packaging, release metadata, serialization, generation, runtime behavior, validation fixes
- `designer`: presentational UI/UX or static accessibility fixes
- `planner`: multi-step, risky, public API, architecture, compatibility, package/release, versioning, generated-output, cross-cutting, or test-strategy feedback
- user: product, public API, architecture, security, compatibility, release, or versioning decisions that cannot be safely inferred

## Fix Rules

For each actionable item:

1. identify the exact thread/comment
2. identify affected files
3. delegate the smallest correct fix
4. update tests when behavior changes
5. update version/release metadata when required
6. run relevant validation
7. commit and push to the PR branch
8. reply with fix summary and commit SHA
9. resolve only after fix is pushed and validated

## Rejected Feedback

If feedback is incorrect or intentionally not applied:

1. reply with concise rationale
2. do not resolve unless policy allows the PR author to resolve rejected feedback
3. escalate before rejecting P0/P1, security, public API, compatibility, architecture, package/release, or versioning feedback

## Re-review

After actionable comments are fixed, pushed, replied to, and resolved, request another external review when appropriate.

Default Codex re-review request:

```text
@codex review the latest changes and verify the prior findings were addressed. Focus only on remaining regressions, missing tests, public API compatibility, security issues, package/release behavior, versioning, and risky behavior changes.
```

Do not repeatedly request review without new commits or clear rationale.

## Stop Conditions

Stop when:

- no unresolved actionable review feedback remains
- reviewer approves or posts no new actionable findings
- max loop count is reached
- the same finding repeats after attempted remediation
- user/product decision is required
- feedback requires out-of-plan architecture, public API, compatibility, release, or versioning change
- unrelated CI failure blocks confidence
- remediation would violate project standards
- unsafe git state is detected
- GitHub API/parser/tool failure cannot be safely recovered

Default maximum: 3 remediation iterations per PR.

After 3 iterations, summarize remaining items, attempted fixes, non-convergence reason, and recommended next action.

## Thread Resolution Rule

Resolve review threads only after:

- fix is committed
- fix is pushed
- relevant validation is complete or explicitly reported
- reply was posted

Do not resolve unresolved questions or unapproved rejected high-severity feedback.

## Remediation Ledger

Maintain a short session-local ledger during each loop:

- PR number/URL
- branch
- iteration
- feedback queue
- classification
- owner
- status
- validation
- pushed commits
- remaining items

Do not commit the ledger unless the user or project policy explicitly requests it.

## Skill Selection

Use the narrowest matching skill:

- `address-pr-feedback` for generic PR comments, human reviewer comments, ambiguous reviewer feedback, or one-off fixes
- `run-codex-review-loop` only for explicit Codex review feedback, Codex threads, Codex re-review, or the bounded Codex loop
- `watch-pr-feedback` only for explicit watch/monitor/wait/poll/loop/continue requests

Ambiguous requests such as `fix PR comment on PR #80` must not trigger the Codex loop by default.

## Monitoring

A remediation skill is not a monitor. A monitor detects new feedback and routes to remediation skills.

Monitoring must be read-only, deterministic, bounded, parser-stable, and truthfully reported.

Use `watch-pr-feedback` for monitor-backed behavior. If Monitor, `/loop`, scheduling support, or the approved parser strategy is unavailable, fall back to manual remediation or return `blocked`.
