---
name: open-plan-pr
description: Open a pull request for a successfully completed approved plan after final verification. Use only when the orchestrator has confirmed that the plan is complete and validation has passed.
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
3. Summarize the completed plan using:
   - planner summary
   - completed phases
   - key files changed
   - validation performed
   - unresolved issues, if any
4. Open the PR with:
   - clear title
   - concise summary
   - validation notes
   - unresolved issues section when needed
5. Return:
   - base branch
   - head branch
   - PR title
   - PR URL
   - any warnings

Do not:
- open a PR for a partial plan unless the workflow explicitly allows it
- open a PR if validation is incomplete
- invent missing validation