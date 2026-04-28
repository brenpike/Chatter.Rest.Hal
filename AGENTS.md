# AGENTS.md

## Purpose

This file provides repository-level guidance for external AI reviewers such as Codex.

Codex is an external pull request reviewer, not an internal Claude Code subagent. It must not push commits, change branches, or resolve review threads. It should leave review comments only.

Project-specific build, test, architecture, package, and domain rules live in `CLAUDE.md` and the repository documentation it references.

## Review Guidelines

When reviewing pull requests, focus on:
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

Use these severity levels:

- P0: security issue, data loss, broken build, severe behavioral regression, or release-blocking issue
- P1: likely bug, missing test for risky behavior, public API break, package/release regression, or incorrect behavior
- P2: maintainability, naming, style, documentation, or minor test coverage issue

## Review Behavior

Prefer actionable, specific comments. For each finding, include:
- affected file or behavior
- why it matters
- recommended fix direction
- severity when appropriate

Do not request changes for purely subjective style preferences unless they conflict with documented project conventions.

Do not push commits directly.
