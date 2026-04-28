---
name: address-pr-feedback
description: Fix a specific generic GitHub PR comment or reviewer comment on an existing pull request. Use for non-Codex or ambiguous PR feedback requests.
disable-model-invocation: false
allowed-tools:
  - Read
  - Bash(git status *)
  - Bash(git branch *)
  - Bash(git rev-parse *)
  - Bash(git fetch *)
  - Bash(git diff *)
  - Bash(git log *)
  - Bash(git add *)
  - Bash(git commit *)
  - Bash(git push *)
  - Bash(gh pr view *)
  - Bash(gh pr comment *)
  - Bash(gh api *)
  - Agent(planner, coder, designer)
  - Skill
shell: powershell
---

# Address PR Feedback

Fix a specific GitHub PR comment or reviewer comment.

Use this skill for requests like:
- "fix PR comment on PR #80"
- "address the reviewer comment on PR #80"
- "fix the unresolved comment on my PR"
- "handle the comment from reviewer X"

Do not use this skill for the full Codex review loop. Use `run-codex-review-loop` only when the user explicitly asks for Codex review remediation.

Follow:
- `agent-system-policy.md`
- `branching-pr-workflow.md`
- `versioning.md`
- `pr-review-remediation-loop.md`

## Required Inputs

At minimum:
- PR number or PR URL

Optional:
- comment URL
- comment author
- file path
- quoted comment text
- whether to reply after fixing

## Failure Contract

This skill must not crash or silently stop.

On any command failure, API error, missing permission, missing PR, ambiguous comment selection, or unsafe git state:
1. Retry once if the failure appears transient.
2. Use a safe fallback if available.
3. Otherwise return `blocked`.

Use this format:

```text
Status: blocked
Stage: [pr lookup | feedback fetch | classification | delegation | validation | git | reply]
Blocker: [one-line reason]
Retry status: [not attempted | retried once | exhausted]
Fallback used: [none | description]
Impact: [what cannot proceed]
Next action:
- [specific next step]
```

## Steps

### 1. Confirm PR State

- Confirm the PR exists.
- Confirm the PR head branch.
- Confirm the current branch matches the PR head branch, or safely switch only if allowed by workflow.
- Confirm the working tree is clean or only contains expected changes.
- Confirm the PR target branch.

If unsafe or ambiguous, return `blocked`.

### 2. Fetch PR Feedback

Fetch:
- top-level PR comments
- inline review comments
- unresolved review threads when available
- review summaries

Use GitHub CLI. Use GraphQL for resolvable review threads. Use REST only where GitHub exposes the feedback type as REST objects, such as top-level issue comments.

For generic PR comments, do not filter only to Codex.

### 3. Identify the Target Comment

If exactly one unresolved/actionable comment exists, process it.

If multiple possible comments exist and the user did not identify which one, return `blocked` with a concise list of candidates.

Do not guess between multiple unrelated comments.

### 4. Classify Feedback

Classify as one of:
- `actionable-code-change`
- `actionable-test-change`
- `actionable-doc-change`
- `version-or-release-concern`
- `architecture-or-contract-concern`
- `design-or-UX-concern`
- `question-needs-user-input`
- `non-actionable`
- `incorrect-or-rejected`

### 5. Route Work

- Use planner for multi-step, risky, versioning, architecture, API, contract, compatibility, or sequencing concerns.
- Use coder for code, test, docs, versioning, build, package, release metadata, and validation changes.
- Use designer for presentational UI/UX/static accessibility changes only.

### 6. Apply Fix

For actionable feedback:
- make the smallest correct change
- update tests/docs/versioning when required
- run relevant validation
- keep changes on the PR branch
- commit and push according to `branching-pr-workflow.md`

### 7. Reply

After the fix is committed and pushed, post a reply appropriate to the feedback source type:

**Review thread (inline or conversation thread):**
- Reply directly to the review thread with concise fix summary, validation run, and commit SHA.
- Do not resolve the thread unless policy allows it and the fix is actually pushed and validated.

**Top-level PR comment (issue comment):**
- Post a reply using `gh pr comment` with concise fix summary, validation run, and commit SHA.

**Review summary (`PullRequestReview` with `CHANGES_REQUESTED` or `COMMENTED` state, no inline thread):**
- Post a PR-level acknowledgement comment using `gh pr comment` referencing the review author and review state, with concise fix summary, validation run, and commit SHA. Review summaries cannot be resolved as threads.

Do not request Codex re-review from this skill unless the user explicitly asks.

## Output

```text
Status: complete | partial | blocked

PR:
- Number:
- Branch:
- Target:

Feedback:
- Source:
- Author:
- URL:
- Classification:

Changed:
- path/to/file
- None

Validated:
- [check]
- Not run

Git:
- Commit:
- Pushed: yes | no

Reply:
- Posted: yes | no
- URL:
- Not posted because:

Issues:
- [issue]
- None
```
