---
name: remediate-codex-review
description: Fetch Codex PR review feedback, classify it, delegate fixes, reply to threads, resolve fixed threads, and request re-review using GitHub GraphQL.
disable-model-invocation: false
allowed-tools:
  - Read
  - Bash(git status *)
  - Bash(git branch *)
  - Bash(git rev-parse *)
  - Bash(git fetch *)
  - Bash(git log *)
  - Bash(git diff *)
  - Bash(git add *)
  - Bash(git commit *)
  - Bash(git push *)
  - Bash(gh pr view *)
  - Bash(gh pr comment *)
  - Bash(gh api graphql *)
  - Agent(planner, coder, designer)
  - Skill
shell: powershell
---

Remediate Codex review feedback for an existing PR.

Follow:
- `agent-system-policy.md`
- `branching-pr-workflow.md`
- `pr-review-remediation-loop.md`
- `.claude/skills/remediate-codex-review/github-pr-review-graphql.md`
- `versioning.md` when remediation affects packable package source

## Steps

1. Confirm PR state: current branch is PR head, working tree is clean, remote branch is fetched, PR exists.
2. Fetch unresolved review threads, inline comments, top-level comments, and review summaries using `gh api graphql` and `github-pr-review-graphql.md`.
3. Process Codex-authored comments only unless the user explicitly asks for all reviewers.
4. Build a remediation queue containing thread/comment id, file, line/diff hunk, summary, classification, severity, owner, user-input need, and version impact.
5. Delegate:
   - planner for multi-step, risky, public API, HAL contract, package behavior, versioning/SemVer, or sequencing feedback
   - coder for source, test, docs, packaging, serialization, source-generator, validation, and version-file fixes
   - designer only for presentational UI/UX/accessibility presentation fixes
6. Apply the smallest correct fixes, update tests where appropriate, run validation, re-check version bump requirements, commit, and push.
7. Reply to each fixed thread with a concise explanation and commit SHA, then resolve it using GraphQL.
8. Request re-review after all actionable items are handled:

`@codex review the latest changes and verify the prior findings were addressed. Focus only on remaining regressions, missing tests, public API compatibility, security issues, package behavior, versioning/SemVer issues, and HAL/HATEOAS behavior.`

Stop after no actionable unresolved comments remain, user input is required, the same finding repeats after attempted remediation, or 3 iterations are reached.

## Output

Return a concise remediation ledger with PR, branch, iteration, feedback queue, changed files, validation, version impact, commits, replied/resolved/open threads, and next action.
