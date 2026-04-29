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

# Run Codex Review Loop

Run the bounded Codex-specific remediation and re-review loop for an existing PR.

Follow:

- `agent-system-policy.md`
- `branching-pr-workflow.md`
- `versioning.md`
- `pr-review-remediation-loop.md`
- `.claude/references/github-pr-review-graphql.md`

## Invocation Boundary

Use only when the request explicitly refers to:

- Codex
- Codex review
- Codex PR feedback
- Codex review threads
- Codex re-review
- Codex review remediation loop

If invoked for generic or ambiguous feedback, return blocked and direct the orchestrator to `address-pr-feedback`.

## Required Inputs

- PR number or PR URL
- current branch name
- repository owner/name

## Procedure

1. Confirm PR exists, current branch is the PR head branch, latest remote state is fetched, and working tree is clean.
2. Fetch Codex-authored feedback using `.claude/references/github-pr-review-graphql.md`: review threads, inline thread comments, top-level PR comments, and review summaries (reviews with `CHANGES_REQUESTED` or `COMMENTED` state whose body contains actionable feedback not captured in inline threads).
3. Build a remediation queue with thread/comment id, file, line/diff context, summary, classification, severity, owner, user-input requirement, and version/release impact.
4. Route per `pr-review-remediation-loop.md`.
5. Apply smallest correct fixes through delegated agents.
6. Run relevant validation.
7. Commit and push remediation changes.
8. Reply to fixed items with concise summary and commit SHA.
9. Resolve fixed review threads only after push and validation.
10. Request Codex re-review when all actionable items are handled.
11. Repeat until clean, blocked, user input is required, repeated finding appears, or max loop count is reached.

Default maximum: 3 remediation iterations.

Do not process non-Codex feedback unless the user explicitly asks to include all reviewers.

## Output

```text
Status: complete | partial | blocked

PR:
- Number:
- URL:
- Branch:

Loop:
- Iterations completed:
- Max iterations:
- Re-review requested: yes | no

Fixed:
- [thread/comment id]: [summary]
- None

Resolved:
- [thread id]
- None

Commits pushed:
- [sha]
- None

Validated:
- [check]
- Not run

Version/release:
- [change]
- None

Remaining:
- [item]
- None

Issues:
- [issue]
- None
```

Use the blocked report contract from `agent-system-policy.md` for blocked states.
