---
name: checkpoint-commit
description: Create a checkpoint commit for the current approved plan after a completed phase, milestone, version bump, or review remediation item.
disable-model-invocation: true
allowed-tools:
  - Bash(git status *)
  - Bash(git diff *)
  - Bash(git add *)
  - Bash(git commit *)
  - Bash(git rev-parse *)
  - Bash(git log *)
shell: powershell
---

Create a checkpoint commit for the current approved plan.

Follow `branching-pr-workflow.md`.

## Requirements

1. Confirm current branch is not `main`.
2. Review staged and unstaged diff.
3. Stage only files that belong to the completed phase, milestone, version bump, or review-remediation item.
4. Create a clear conventional-style commit message.

## Do Not

- create a branch
- push
- open a PR
- include unrelated files
- commit on `main`

## Output

```text
Status: complete | blocked
Branch:
Commit:
Message:
Files included:
- [file]
Warnings:
- [warning]
- None
```
