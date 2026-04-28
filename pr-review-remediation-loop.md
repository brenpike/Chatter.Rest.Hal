# PR Review Remediation Loop

## Purpose

This file is the canonical policy for responding to external pull request review feedback, including Codex GitHub reviews.

Codex is an external GitHub PR reviewer, not a Claude Code subagent.

## Ownership

The orchestrator owns the review-remediation loop.

The orchestrator may delegate remediation work, but remains responsible for:
- requesting Codex review
- checking PR review feedback
- identifying unresolved Codex review threads
- classifying feedback
- routing work to the correct agent
- verifying fixes were committed and pushed
- verifying version-bump requirements after remediation changes
- replying to review threads
- resolving review threads
- requesting follow-up Codex review
- stopping the loop safely

## GitHub API Rule

Resolvable review threads must be handled with GitHub GraphQL through `gh api graphql`.

Do not try to resolve review threads using REST review-comment IDs. Resolvable pull request review threads are GraphQL objects.

Use `.claude/skills/remediate-codex-review/github-pr-review-graphql.md` for the canonical GraphQL query and mutation templates.

## Feedback Classification

Each Codex item must be classified as one of:
- actionable-code-change
- actionable-test-change
- actionable-doc-change
- architecture-or-contract-concern
- design-or-UX-concern
- versioning-or-release-concern
- question-needs-user-input
- non-actionable
- incorrect-or-rejected

The orchestrator must not silently ignore Codex feedback.

## Agent Routing

Because no architect agent is currently included:
- Route implementation, bug, test, packaging, serialization, source-generator, documentation, and version-file fixes to `coder`.
- Route presentational UI/UX/accessibility presentation fixes to `designer`.
- Route multi-step, risky, public API, HAL contract, generated-output, package-behavior, versioning, or release-policy feedback to `planner` first for remediation planning.
- Escalate to the user when feedback requires a product, public API, architecture, SemVer, or package-contract decision.

## Fix Rules

For each actionable item:
1. Identify the exact review thread or comment.
2. Identify the affected files.
3. Delegate the smallest correct fix to the correct agent.
4. Add or update tests when behavior changes.
5. Run relevant validation.
6. Re-check version-bump requirements if any non-markdown files under a packable package's `src/` directory changed.
7. Commit with a clear conventional commit message.
8. Push to the PR branch.
9. Reply to the review thread with the fix summary and commit SHA.
10. Resolve the thread only after the fix is pushed and validated.

## Re-review

After all actionable Codex comments are fixed, pushed, replied to, and resolved, request another Codex review.

Default request:

`@codex review the latest changes and verify the prior findings were addressed. Focus only on remaining regressions, missing tests, public API compatibility, security issues, package behavior, versioning/SemVer issues, and HAL/HATEOAS behavior.`

## Stop Conditions

The loop must stop when any of the following is true:
- no unresolved actionable Codex review threads remain
- Codex approves or posts no new actionable findings
- the maximum loop count is reached
- the same finding appears twice after attempted remediation
- the fix requires user/product decision
- the fix requires architecture, public API, SemVer, or package-contract change beyond the approved plan
- CI fails for reasons unrelated to the review feedback
- resolving the feedback would violate project standards

Default maximum: 3 Codex remediation iterations per PR.

## Thread Resolution Rule

A review thread may only be resolved after the fix has been committed, pushed, and validated, or after the orchestrator has explicitly rejected the feedback with a written rationale.
