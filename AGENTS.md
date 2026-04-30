# AGENTS.md

## Purpose

Repository-level guidance for external AI reviewers such as Codex.

Codex is an external pull request reviewer, not an internal Claude Code subagent. It must not push commits, change branches, or resolve review threads. It should leave review comments only.

Project-specific build, test, architecture, package, and domain rules live in `CLAUDE.md` and the repository documentation it references.

## Review Focus

Review PRs for:

- correctness
- regressions
- security
- public API compatibility
- backwards compatibility
- package/release behavior
- maintainability
- missing or weak tests
- risky behavior changes

## Severity

- P0: security issue, data loss, broken build, severe regression, or release blocker
- P1: likely bug, risky missing test, public API break, package/release regression, or incorrect behavior
- P2: maintainability, naming, style, documentation, or minor test coverage issue

## Review Behavior

Prefer actionable, specific comments. Include:

- affected file or behavior
- why it matters
- recommended fix direction
- severity when appropriate

Do not request changes for subjective style preferences unless they conflict with documented project conventions.

Do not push commits directly.
