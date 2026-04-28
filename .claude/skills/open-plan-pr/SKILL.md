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

Open a pull request for the current approved plan.

Requirements:
1. Confirm the current branch name and default base branch.
2. Confirm there are no unexpected unstaged changes.
3. Confirm required version bump is included when non-markdown packable package source changed.
4. Summarize completed plan, key files, validation, version bump status, and unresolved issues.
5. Open the PR with clear title, concise summary, validation notes, version bump notes when applicable, and unresolved issues when needed.
6. Return base branch, head branch, PR title, PR URL, and warnings.

Do not open a PR for a partial plan unless explicitly allowed. Do not open a PR if validation is incomplete or a required version bump is missing.
