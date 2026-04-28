---
name: create-working-branch
description: Create or confirm the compliant working branch for the current approved plan before implementation begins.
disable-model-invocation: true
allowed-tools:
  - Bash(git status *)
  - Bash(git branch *)
  - Bash(git rev-parse *)
  - Bash(git checkout *)
  - Bash(git switch *)
shell: powershell
---

Create or confirm the working branch for the current approved plan.

Requirements:
1. Confirm the current branch.
2. Confirm the base branch exists.
3. Confirm the requested working branch name is compliant with `branching-pr-workflow.md`.
4. Confirm there are no unexpected unstaged or uncommitted changes that would make switching unsafe.
5. Create or switch to the requested working branch from the requested base branch.
6. Return classification, base branch, previous branch, working branch, whether it was created, and warnings.

Do not create or modify product files, commit, push, open a PR, or continue if branch state is unsafe or ambiguous.
