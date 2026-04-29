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

Fix one-time generic, human, non-Codex, or ambiguous PR feedback.

Follow:

- `agent-system-policy.md`
- `branching-pr-workflow.md`
- `versioning.md`
- `pr-review-remediation-loop.md`
- `.claude/references/github-pr-review-graphql.md` for PR review threads, thread replies, and GraphQL review data

## Invocation Boundary

Use for:

- `fix PR comment on PR #N`
- `address reviewer feedback`
- `fix the unresolved comment`
- ambiguous PR feedback requests

Do not use for explicit Codex review loops. Use `run-codex-review-loop` only when Codex is explicitly requested.

## Required Inputs

At minimum:

- PR number or PR URL

Optional:

- comment URL
- comment author
- file path
- quoted comment text
- whether to reply after fixing

## Procedure

1. Confirm PR exists, target branch, head branch, current branch, and safe working tree.
2. Fetch top-level PR comments, inline review comments, unresolved review threads, and review summaries using `.claude/references/github-pr-review-graphql.md` where GraphQL review-thread data is required.
3. Identify the target comment.
   - If exactly one unresolved/actionable candidate exists, process it.
   - If multiple unrelated candidates exist and the user did not identify one, return blocked with candidates.
4. Classify feedback using `pr-review-remediation-loop.md`.
5. Route to planner/coder/designer according to policy.
6. Apply the smallest correct fix.
7. Run relevant validation when feasible.
8. Commit and push when a change was made and policy allows.
9. Reply with concise fix summary, validation, and commit SHA when appropriate.

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

Use the blocked report contract from `agent-system-policy.md` for blocked states.
