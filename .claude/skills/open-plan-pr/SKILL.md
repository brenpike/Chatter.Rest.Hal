---
name: open-plan-pr
description: Open a pull request for a successfully completed approved plan after final verification.
disable-model-invocation: true
allowed-tools:
  - Bash(git status *)
  - Bash(git branch *)
  - Bash(git log *)
  - Bash(git diff *)
  - Bash(gh pr create *)
  - Bash(gh pr view *)
  - Bash(gh repo view *)
shell: powershell
---

Open a pull request for the completed approved plan.

Follow `branching-pr-workflow.md` and `versioning.md`.

## Requirements

1. Confirm current branch and default base branch.
2. Confirm current branch is not `main`.
3. Confirm no unexpected unstaged changes.
4. Confirm required validation has passed.
5. Confirm required version/release metadata is included or not required.
6. Create PR with:
   - clear title
   - concise summary
   - validation notes
   - version/release notes when relevant
   - unresolved issues when needed

## Do Not

- open PR for partial plan unless workflow explicitly allows it
- open PR if validation is incomplete
- open PR if required version/release metadata is missing
- invent missing validation
- include generated-content signatures

## Output

```text
Status: complete | blocked
Base:
Head:
PR title:
PR URL:
Warnings:
- [warning]
- None
```
