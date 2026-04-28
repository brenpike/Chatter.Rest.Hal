# Versioning Policy

## Purpose

This file defines the generic SemVer workflow for repositories that publish versioned artifacts.

Project-specific package names, artifact paths, version-file locations, changelog locations, tag prefixes, and validation commands must be defined in `CLAUDE.md` or project documentation referenced by `CLAUDE.md`.

## Scope

This policy applies to every independently versioned artifact defined by the project. Examples include packages, libraries, applications, plugins, containers, or distributable binaries.

Each artifact is versioned independently unless the project documentation says otherwise. A change to one artifact does not require a bump in another unless the change propagates through a public API, compatibility contract, runtime contract, or distribution dependency.

Internal shared components that are not published independently carry no version unless the project defines one.

## SemVer Rules

This repository follows Semantic Versioning 2.0.0.

Version format: `MAJOR.MINOR.PATCH`

| Increment | When |
|---|---|
| MAJOR | Breaking change to public API, compatibility contract, data format, runtime behavior contract, or documented consumer expectation |
| MINOR | New backward-compatible public API, capability, option, behavior, or artifact surface |
| PATCH | Bug fix, internal refactor, or implementation change with no public compatibility impact |

Pre-release labels such as `1.2.0-beta.1` are allowed only when coordinated by the orchestrator and supported by the project release workflow.

For artifacts at `0.x.y`, SemVer allows minor increments to include breaking changes. Breaking changes must still be documented clearly in release notes or changelog entries.

## Bump Trigger

A version bump is required whenever a PR changes any non-markdown file under a packable package's `src/` directory. The CI version-check gate enforces this rule mechanically and is the authoritative trigger — behavioral judgment does not override it.

Behavioral analysis (public API change, runtime behavior change, compatibility impact) determines the *type* of bump (major/minor/patch), not whether a bump is required.

The exact `src/` paths and tag prefixes for each packable artifact are project-specific and must be defined in `CLAUDE.md` or project documentation referenced by `CLAUDE.md`.

No version bump is required by default for:
- documentation-only changes (markdown and non-src docs)
- test-only changes
- CI-only changes
- agent framework/governance changes
- changelog-only maintenance

Project-specific documentation may define additional no-bump or bump-required paths.

## Bump Type Determination

The orchestrator determines the bump type by examining:
1. conventional commit type(s) on the branch
2. whether public API, compatibility contract, runtime behavior, data format, generated output, package contents, or documented behavior changed
3. whether breaking changes are present through `!` suffix, `BREAKING CHANGE:` footer, or actual compatibility impact

| Commit type | Compatibility impact | SemVer increment |
|---|---|---|
| `feat` | Backward-compatible new public capability | MINOR |
| `feat!` or `BREAKING CHANGE:` | Breaking change | MAJOR |
| `fix` / `bugfix` | No breaking change | PATCH |
| `refactor` | No public compatibility impact | PATCH |
| `refactor!` | Breaking change | MAJOR |
| `chore` / `docs` / `test` / `ci` | None | No bump |

When in doubt, the orchestrator asks the user to confirm the bump type before delegating version edits.

## Bump Execution

The orchestrator delegates version/release file edits to the coder agent.

The version bump is included in the same PR as the feature or fix — not as a follow-up PR — unless the user explicitly directs otherwise.

Project-specific documentation must define the exact files to update atomically for each artifact. Common examples include:
- artifact manifest or project file containing the canonical version
- root project instruction file if it mirrors package versions
- package or release changelog
- release notes
- artifact metadata file
- documentation tables that mirror current versions

The canonical version source must be defined by the project. If undefined, the orchestrator must inspect the repository and ask for user confirmation before editing version metadata.

## CHANGELOG / Release Notes Convention

Each versioned artifact should maintain release notes or a changelog unless the project explicitly uses another release documentation mechanism.

Recommended format follows Keep a Changelog:
- `Added`
- `Changed`
- `Deprecated`
- `Removed`
- `Fixed`
- `Security`

When bumping a version, convert pending unreleased entries into a dated release section and reset the unreleased section according to project convention.

## Git Tags

Tags are created according to the project release workflow.

Project-specific documentation must define:
- whether tags are created manually or by CI
- tag format
- tag prefix per artifact when multiple artifacts exist
- whether tags are annotated or lightweight
- whether tags are created before or after publish/deploy

Recommended generic format:
- single artifact: `vX.Y.Z`
- multiple artifacts: `<artifact-prefix>/vX.Y.Z`

## Version Source of Truth

Each versioned artifact must have one canonical version source.

All mirrored references are informational and must be kept in sync during every version bump.

CI or release validation should fail when a version-required PR does not include the necessary version bump.

## Agent Rules

- The orchestrator owns version bump detection and bump type decisions.
- The planner may recommend versioning implications but must not edit files.
- The coder may edit version/release files only when explicitly delegated.
- The designer never edits version/release files unless the file is purely presentational documentation explicitly assigned by the orchestrator.
- If project-specific version paths or canonical version source are unclear, stop and ask the user.
