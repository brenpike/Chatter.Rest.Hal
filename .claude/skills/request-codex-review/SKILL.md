---
name: request-codex-review
description: Request Codex review on the current pull request after the orchestrator has opened or confirmed the PR.
disable-model-invocation: true
allowed-tools:
  - Bash(git status *)
  - Bash(git branch *)
  - Bash(gh pr view *)
  - Bash(gh pr comment *)
shell: powershell
---

Request Codex review on the current pull request.

Requirements:
1. Confirm the PR exists.
2. Confirm the current branch is the PR head branch.
3. Confirm the PR branch has been pushed.
4. Confirm no required version bump is obviously missing from the PR summary or changed-file context.
5. Post this PR comment:

`@codex review for regressions, missing tests, public API compatibility issues, security issues, package behavior, versioning/SemVer issues, and HAL/HATEOAS behavior.`

Return PR number, PR URL, branch, review request status, and warnings.

Do not modify files, commit, push, resolve review threads, or request review if no PR exists.
