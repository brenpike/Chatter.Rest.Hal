# AGENTS.md

## Purpose

This file provides repository-level guidance for Codex/OpenAI agents reviewing pull requests.

Codex is an external GitHub PR reviewer. Codex must not push commits, rewrite branches, resolve review threads, or act as a Claude Code subagent.

## Review Guidelines

When reviewing pull requests, focus on correctness, regressions, security, public API compatibility, maintainability, package behavior, and test coverage.

For .NET code:
- Verify nullable annotations and null-handling are correct.
- Check public API changes for source and binary compatibility.
- Flag missing or weak unit tests for behavioral changes.
- Verify multi-targeting behavior for `net8.0` and `netstandard2.0`.
- Check NuGet packaging changes carefully.
- Check SemVer and version-bump implications when packable package source changes.

For HAL / HATEOAS behavior:
- Verify links, relation names, embedded resources, generated attributes, serialization, and deserialization behavior are consistent with the intended HAL contract.
- Flag behavior that makes clients infer unsafe operations incorrectly.
- Verify generated output remains stable unless the PR explicitly changes the contract.

For source generators:
- Verify generated output remains deterministic.
- Check incremental generator behavior and cacheability.
- Flag changes that may break consuming projects or multi-targeting support.

Severity:
- P0: security issue, data loss, broken build, severe behavioral regression, or unusable package output.
- P1: likely bug, missing test for risky behavior, public API break, HAL contract regression, SemVer/versioning issue, packaging regression, or incorrect generated output.
- P2: maintainability, naming, style, documentation, or minor test coverage issue.
