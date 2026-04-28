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

Requirements:
1. Confirm the current branch name.
2. Review the staged and unstaged diff.
3. Stage only the files that belong to the completed phase, approved milestone, version bump, or review remediation item.
4. Create a clear commit message using conventional-style format.
5. Return:
   - branch name
   - commit hash
   - commit message
   - files included
   - any warnings

Do not:
- create a branch
- push
- open a PR
- include unrelated files
