---
name: request-codex-review
description: Request Codex review on the current pull request.
disable-model-invocation: true
allowed-tools:
  - Bash(gh pr view *)
  - Bash(gh pr comment *)
shell: powershell
---

Request Codex review on the current pull request.

Codex is an external reviewer, not a Claude Code subagent.

## Requirements

1. Confirm the PR exists.
2. Confirm the current branch is the PR head branch.
3. Confirm the PR branch has been pushed.
4. Post this PR comment:

```text
@codex review for regressions, missing tests, public API compatibility issues, security issues, package/release behavior, versioning issues, and risky behavior changes.
```

## Do Not

- modify files
- commit
- push
- resolve review threads
- request review if no PR exists

## Output

```text
Status: complete | blocked
PR number:
PR URL:
Branch:
Review request posted: yes | no
Warnings:
- [warning]
- None
```
