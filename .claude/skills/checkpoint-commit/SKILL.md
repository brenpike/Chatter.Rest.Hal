---
name: checkpoint-commit
description: Create a checkpoint commit for the current approved plan after a completed phase or milestone.
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

Requirements:
1. Confirm the current branch name.
2. Review the staged and unstaged diff.
3. Stage only files belonging to the completed phase, approved milestone, version bump, or review remediation.
4. Create a clear conventional-style commit message.
5. Return branch name, commit hash, message, files included, and warnings.

Do not create a branch, push, open a PR, or include unrelated files.
