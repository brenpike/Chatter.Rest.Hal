# PR Review Remediation Loop

## Purpose

This policy defines how Claude agents respond to external pull request review feedback, including Codex GitHub reviews.

Codex and other external AI reviewers are external reviewers, not Claude Code subagents.

## Ownership

The orchestrator owns the review-remediation loop.

The orchestrator may delegate remediation work, but remains responsible for:
- requesting external review
- checking PR review feedback
- identifying unresolved review threads
- classifying feedback
- routing work to the correct agent
- verifying fixes were committed and pushed
- replying to review threads
- resolving review threads
- requesting follow-up review
- stopping the loop safely

## Loop Entry Criteria

The loop may start only after:
- a pull request exists
- the PR branch has been pushed
- required validation has completed or is known to be in progress
- external review has been requested or external review feedback already exists

## Feedback Sources

The orchestrator must check:
- unresolved pull request review threads
- inline pull request review comments
- top-level PR comments
- review summaries with state `COMMENTED` or `CHANGES_REQUESTED` (exclude `APPROVED`, `DISMISSED`)
- CI failures when relevant to the review feedback

## Feedback Classification

Each review item must be classified as one of:

- `actionable-code-change`
- `actionable-test-change`
- `actionable-doc-change`
- `architecture-or-contract-concern`
- `design-or-UX-concern`
- `version-or-release-concern`
- `question-needs-user-input`
- `non-actionable`
- `incorrect-or-rejected`

The orchestrator must not silently ignore review feedback.

## Agent Routing

- Route implementation, bug, runtime behavior, test, packaging, release metadata, serialization, generation, build, and documentation fixes to `coder`.
- Route presentational UI/UX/accessibility presentation fixes to `designer`.
- Route multi-step, risky, public API, architecture, compatibility, package/release behavior, versioning, generated-output, or cross-cutting feedback to `planner` first for remediation planning.
- Escalate to the user when feedback requires a product, public API, architecture, security, release, versioning, or compatibility decision.

## Fix Rules

For each actionable item:
1. Identify the exact review thread or comment.
2. Identify the affected files.
3. Delegate the smallest correct fix to the correct agent.
4. Add or update tests when behavior changes.
5. Update version/release metadata if the fix changes version-triggering files or artifact behavior.
6. Run relevant validation.
7. Commit with a clear conventional commit message.
8. Push to the PR branch.
9. Reply to the review thread with the fix summary and commit SHA.
10. Resolve the thread only after the fix is pushed and validated.

## Rejected Feedback

If feedback is incorrect or intentionally not applied:
1. Reply to the thread with a concise rationale.
2. Do not resolve the thread unless policy allows rejected feedback to be resolved by the PR author.
3. Escalate to the user before rejecting P0/P1 feedback, public API feedback, compatibility feedback, security feedback, architecture feedback, package/release feedback, or versioning feedback.

## Re-review

After all actionable comments are fixed, pushed, replied to, and resolved, request another external review.

Default Codex request:

`@codex review the latest changes and verify the prior findings were addressed. Focus only on remaining regressions, missing tests, public API compatibility, security issues, package/release behavior, versioning, and risky behavior changes.`

## Stop Conditions

The loop must stop when any of the following is true:

- no unresolved actionable review threads remain
- the reviewer approves or posts no new actionable findings
- the maximum loop count is reached
- the same finding appears twice after attempted remediation
- the fix requires user/product decision
- the fix requires architecture, public API, compatibility, release, or versioning change beyond the approved plan
- CI fails for reasons unrelated to the review feedback
- resolving the feedback would violate project standards

## Maximum Loop Count

Default maximum: 3 review-remediation iterations per PR.

After 3 iterations, stop and summarize:
- remaining unresolved items
- attempted fixes
- suspected reason the loop is not converging
- recommended next action

## Anti-loop Rule

The orchestrator must not repeatedly request review without new commits or a clear rationale.

The orchestrator must not repeatedly apply speculative fixes for the same comment.

## Thread Resolution Rule

A review thread may only be resolved after the fix has been committed, pushed, and validated, or after the orchestrator has explicitly rejected the feedback with a written rationale.

## Remediation Ledger

The orchestrator should maintain a short remediation ledger during each loop.

The ledger should include:
- PR number/URL
- branch
- iteration number
- feedback queue
- classification
- owner
- status
- validation
- pushed commits
- remaining items

The ledger is a session artifact by default. Do not commit it unless the user or project policy explicitly requests that.

## Skill Selection

Use the narrowest matching skill for the user's request.

- Use `remediate-pr-comment` for generic PR comments, human reviewer comments, ambiguous reviewer feedback, or one-off PR comment fixes.
- Use `remediate-codex-review` only for explicit Codex review feedback, Codex review threads, Codex re-review, or the bounded Codex review remediation loop.

Ambiguous requests such as `fix PR comment on PR #80` must not trigger the Codex loop by default.
