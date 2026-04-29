# Versioning Policy

## Purpose

Generic SemVer workflow for repositories that publish versioned artifacts.

Project-specific package names, artifact paths, version-file locations, changelog locations, tag prefixes, and validation commands live in `CLAUDE.md` or project docs referenced by `CLAUDE.md`.

## Scope

Applies to every independently versioned artifact defined by the project: packages, libraries, applications, plugins, containers, distributable binaries, or similar artifacts.

Each artifact is versioned independently unless project documentation says otherwise.

Internal shared components with no standalone distribution carry no version unless the project defines one. Changes to shared components may require bumps in dependent artifacts if public API, runtime behavior, generated output, package contents, or compatibility contracts change.

## SemVer Rules

This repository follows Semantic Versioning 2.0.0.

Format: `MAJOR.MINOR.PATCH`

| Increment | Trigger |
|---|---|
| MAJOR | Breaking change to public API, compatibility contract, data format, runtime behavior contract, or documented consumer expectation |
| MINOR | Backward-compatible public API, capability, option, behavior, or artifact surface |
| PATCH | Bug fix, internal refactor, or implementation change with no public compatibility impact |

For `0.x.y` artifacts, SemVer permits minor increments for breaking changes. Breaking changes must still be documented clearly.

Pre-release labels such as `1.2.0-beta.1` require orchestrator coordination and project release-workflow support.

## Bump Trigger

A version bump is required when a PR changes files that affect a published artifact's:

- runtime behavior
- public API
- compatibility contract
- generated output
- packaged output
- distribution metadata
- documented consumer expectation

Exact bump-trigger paths are project-specific and must be defined in `CLAUDE.md` or referenced project documentation.

No bump is required by default for:

- documentation-only changes
- test-only changes
- CI-only changes
- agent framework/governance changes
- changelog-only maintenance
- markdown-only changes

Project documentation may define additional required or excluded paths.

## Bump Type Determination

The orchestrator determines bump type from:

1. conventional commit type(s)
2. public API, compatibility, runtime, data format, generated output, package, or documented behavior impact
3. breaking-change markers such as `!`, `BREAKING CHANGE:`, or actual compatibility impact

| Commit / impact | Increment |
|---|---|
| `feat` with backward-compatible public capability | MINOR |
| `feat!` or `BREAKING CHANGE:` | MAJOR |
| `fix` / `bugfix` without breaking change | PATCH |
| `refactor` without public compatibility impact | PATCH |
| `refactor!` | MAJOR |
| `chore` / `docs` / `test` / `ci` without artifact impact | No bump |

When uncertain, ask the user before delegating version edits.

## Bump Execution

The orchestrator delegates version/release file edits to coder.

A bump is included in the same PR as the triggering change unless the user explicitly directs otherwise.

Project-specific documentation must define the exact files to update atomically, such as:

- canonical version file
- changelog/release notes
- package/artifact metadata
- documentation mirrors
- release validation files

Every artifact must have one canonical version source. Mirrors are informational and must be kept in sync.

If the canonical source is undefined, inspect the repo and ask for user confirmation before editing version metadata.

## CHANGELOG / Release Notes

Each versioned artifact should maintain release notes or a changelog unless the project defines another release documentation mechanism.

Recommended sections follow Keep a Changelog:

- Added
- Changed
- Deprecated
- Removed
- Fixed
- Security

When bumping, convert pending unreleased entries into a dated release section and reset the unreleased section according to project convention.

## Tags

Tags are created according to the project release workflow.

Project documentation must define:

- manual vs CI-created tags
- tag format
- prefix per artifact when multiple artifacts exist
- annotated vs lightweight
- timing relative to publish/deploy

Recommended generic formats:

- single artifact: `vX.Y.Z`
- multiple artifacts: `<artifact-prefix>/vX.Y.Z`

## Agent Rules

- Orchestrator owns bump detection and bump type decisions.
- Planner may recommend versioning implications but must not edit files.
- Coder may edit version/release files only when explicitly delegated.
- Designer never edits version/release files unless a purely presentational documentation file is explicitly assigned.
- If project-specific version paths or canonical version source are unclear, stop and ask the user.
