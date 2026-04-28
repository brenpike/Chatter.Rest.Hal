---
name: run-codex-review-loop
description: Run the bounded Codex GitHub PR review remediation loop only when the user explicitly asks to handle Codex review feedback, Codex review threads, or Codex re-review.
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

## Invocation Boundary

Use this skill only when the request explicitly refers to:
- Codex
- Codex review
- Codex PR feedback
- Codex review threads
- Codex re-review
- the Codex review remediation loop

Do not use this skill for generic PR comments, generic reviewer comments, human review comments, issue comments, or ambiguous requests such as:
- "fix PR comment on PR #80"
- "address review feedback"
- "fix the reviewer comment"

For generic or ambiguous PR feedback, use `address-pr-feedback` if available.

If this skill was invoked for a generic or ambiguous PR feedback request, stop and return:

```text
Status: blocked
Stage: skill selection
Blocker: Request is generic PR feedback, not explicitly Codex review feedback.
Retry status: not attempted
Fallback used: none
Impact: Cannot safely run the Codex review loop for a non-Codex-specific request.
Next action:
- Use `address-pr-feedback`
- Or ask the user whether they meant Codex review feedback
```

## Failure Contract

This skill must not crash or silently stop.

If any step fails:
1. Retry once only if the failure appears transient.
2. If retry fails, return `blocked`.
3. Do not continue to thread resolution, commits, pushes, or re-review requests after a failed feedback fetch or ambiguous classification.

Blocked format:

```text
Status: blocked
Stage: [pr lookup | feedback fetch | classification | delegation | validation | git | reply | resolve | rereview]
Blocker: [one-line reason]
Retry status: [not attempted | retried once | exhausted]
Fallback used: [none | description]
Impact: [what cannot proceed]
Next action:
- [specific next step]
```

Follow:
- `agent-system-policy.md`
- `branching-pr-workflow.md`
- `versioning.md`
- `pr-review-remediation-loop.md`
- `github-pr-review-graphql.md` in this skill folder

## Required Inputs

- PR number or PR URL
- Current branch name
- Repository owner/name

## Steps

### 1. Confirm PR State

- Confirm current branch matches the PR head branch.
- Confirm working tree is clean before starting.
- Confirm latest remote branch is fetched.
- Confirm the PR exists.

### 2. Fetch Feedback

Use `gh api graphql` and the GraphQL reference in `github-pr-review-graphql.md`.

Fetch:
- unresolved review threads
- inline review comments
- top-level PR comments
- latest Codex review summary

Only process comments authored by Codex unless the user explicitly asks to process all reviewers.

### 3. Build Remediation Queue

For each Codex item, record:
- thread id
- comment id
- file path
- line or diff hunk
- summary
- classification
- severity
- owner agent
- whether user input is required
- version/release impact if any

### 4. Delegate Work

- Use planner if feedback requires multiple dependent fixes, test strategy, public API analysis, architecture/contract analysis, package/release behavior review, versioning analysis, or sequencing.
- Use coder for source, test, docs, build, packaging, release metadata, serialization, generation, and validation fixes.
- Use designer only for presentational UI/UX/accessibility presentation fixes.

### 5. Apply Fixes

For each actionable item:
- make the smallest correct fix
- update tests where appropriate
- update version/release metadata when required
- run relevant validation
- commit changes
- push branch

### 6. Reply and Resolve

For each fixed thread:
- reply with a concise explanation
- include the commit SHA
- resolve the thread

Do not resolve:
- unresolved questions
- rejected feedback requiring reviewer/user agreement
- items not actually fixed
- items not yet pushed
- items not validated when validation is required

### 7. Request Re-review

After fixes are pushed and threads are resolved, comment:

`@codex review the latest changes and verify the prior findings were addressed. Focus only on remaining regressions, missing tests, public API compatibility, security issues, package/release behavior, versioning, and risky behavior changes.`

### 8. Repeat Safely

Repeat until:
- no actionable unresolved Codex comments remain
- max loop count is reached
- user input is required
- repeated findings indicate non-convergence

Default maximum: 3 remediation iterations.

## Output

Return:
- PR number/URL
- iterations completed
- threads fixed and resolved
- commits pushed
- validation performed
- version/release metadata changes, if any
- remaining unresolved items
- whether re-review was requested
