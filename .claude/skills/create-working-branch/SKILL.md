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

Follow `branching-pr-workflow.md`.

## Requirements

1. Confirm current branch.
2. Confirm base branch exists.
3. Confirm requested working branch name follows the branch taxonomy and naming rules.
4. Confirm there are no unexpected unstaged/uncommitted changes that make switching unsafe.
5. Create or switch to the requested working branch from the requested base branch.

## Do Not

- create or modify product files
- commit
- push
- open a PR
- continue when branch state is unsafe or ambiguous

## Output

```text
Status: complete | blocked
Classification:
Base branch:
Previous branch:
Working branch:
Created: yes | no
Warnings:
- [warning]
- None
```
