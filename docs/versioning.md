# Versioning Policy

## Scope

Two independently versioned NuGet packages:

| Package | Project |
|---|---|
| `Chatter.Rest.Hal` | `src/Chatter.Rest.Hal/` |
| `Chatter.Rest.Hal.CodeGenerators` | `src/Chatter.Rest.Hal.CodeGenerators/` |

`Chatter.Rest.Hal.Core` is an internal shared library — not packaged independently and has no version.

Each package is versioned independently. A change to one package does not require a bump in the other unless the change propagates through a public API dependency.

## SemVer Rules

This repository follows [Semantic Versioning 2.0.0](https://semver.org/spec/v2.0.0.html).

Version format: `MAJOR.MINOR.PATCH`

| Increment | When |
|---|---|
| MAJOR | Breaking change to the public API (removal, rename, signature change, behavior contract change) |
| MINOR | New backward-compatible public API surface (`feat` commits) |
| PATCH | Bug fixes or internal refactors with no public API change (`fix`, `bugfix`, `refactor` commits) |

**Pre-1.0 note:** `Chatter.Rest.Hal.CodeGenerators` is currently pre-1.0 (`0.x.y`). In pre-1.0, MINOR increments may include breaking changes per SemVer spec. Document breaking changes clearly in the CHANGELOG regardless.

Pre-release labels (e.g., `1.2.0-beta.1`) are allowed but must be coordinated with the orchestrator.

## Bump Trigger

A version bump is **required** when a PR merges changes to non-markdown files under the relevant package's `src/` directory:

| Package | Trigger path |
|---|---|
| `Chatter.Rest.Hal` | `src/Chatter.Rest.Hal/**` (excluding `*.md`) |
| `Chatter.Rest.Hal.CodeGenerators` | `src/Chatter.Rest.Hal.CodeGenerators/**` (excluding `*.md`) |

Changes to `src/Chatter.Rest.Hal.Core/**` may trigger a bump in either or both dependent packages if the change affects their public API surface.

**No version bump required for:**
- `docs/**`
- `test/**`
- `.github/workflows/**`
- Governance files (`agent-system-policy.md`, `branching-pr-workflow.md`, `orchestrator.md`, etc.)
- `CLAUDE.md`
- `CHANGELOG.md` / `CHANGELOG-CodeGenerators.md`
- Any `*.md` file

## Bump Type Determination

The **orchestrator** determines the bump type by examining:
1. The conventional commit type(s) on the branch (`feat`, `fix`, `refactor`, etc.)
2. Whether any public API surface was added, changed, or removed
3. Whether any breaking changes are present (indicated by `!` suffix or `BREAKING CHANGE:` footer)

| Commit type | Public API impact | SemVer increment |
|---|---|---|
| `feat` | New API surface | MINOR |
| `feat!` or `BREAKING CHANGE:` footer | Breaking change | MAJOR |
| `fix` / `bugfix` | No API change | PATCH |
| `refactor` | No API change | PATCH |
| `refactor!` | Breaking change | MAJOR |
| `chore` / `docs` / `test` / `ci` | None | **No bump** |

When in doubt, the orchestrator asks the user to confirm the bump type before delegating the version edit.

## Bump Execution

The orchestrator **delegates version file edits to the coder agent**. The version bump is included in the same PR as the feature or fix — not as a follow-up PR.

Files to update atomically when bumping version `X.Y.Z` for a package:

**`Chatter.Rest.Hal`:**
1. `src/Chatter.Rest.Hal/Chatter.Rest.Hal.csproj` — `<Version>X.Y.Z</Version>` (source of truth)
2. `CLAUDE.md` — Package Versions table row for `Chatter.Rest.Hal`
3. `docs/architecture.md` — Solution structure table row for `Chatter.Rest.Hal`
4. `CHANGELOG.md` — Add release section `## [X.Y.Z] - YYYY-MM-DD` above `[Unreleased]`; add link at bottom

**`Chatter.Rest.Hal.CodeGenerators`:**
1. `src/Chatter.Rest.Hal.CodeGenerators/Chatter.Rest.Hal.CodeGenerators.csproj` — `<Version>X.Y.Z</Version>` (source of truth)
2. `CLAUDE.md` — Package Versions table row for `Chatter.Rest.Hal.CodeGenerators`
3. `docs/architecture.md` — Solution structure table row for `Chatter.Rest.Hal.CodeGenerators`
4. `CHANGELOG-CodeGenerators.md` — Add release section `## [X.Y.Z] - YYYY-MM-DD` above `[Unreleased]`; add link at bottom

## CHANGELOG Convention

Each package maintains its own CHANGELOG at the repository root:

| File | Package |
|---|---|
| `CHANGELOG.md` | `Chatter.Rest.Hal` |
| `CHANGELOG-CodeGenerators.md` | `Chatter.Rest.Hal.CodeGenerators` |

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

Each release section uses these subsections as needed: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`.

The `[Unreleased]` section at the top accumulates entries for the next release. When the orchestrator bumps the version, it converts `[Unreleased]` entries into a dated release section and resets `[Unreleased]` to empty.

## Git Tags

A git tag is created automatically by CI after each successful deploy to NuGet.

Tag format:

| Package | Tag format | Example |
|---|---|---|
| `Chatter.Rest.Hal` | `hal/vX.Y.Z` | `hal/v1.2.0` |
| `Chatter.Rest.Hal.CodeGenerators` | `codegen/vX.Y.Z` | `codegen/v0.4.0` |

Tags are annotated (not lightweight). The CI `tag` job runs after `deploy` succeeds on `main`, reads the version from the `.csproj`, and creates + pushes the tag if it does not already exist.

These tags serve as:
- The version anchor for CI version-check validation on future PRs
- GitHub Releases anchors
- NuGet package traceability points

## Version Source of Truth

The `<Version>` element in each `.csproj` is the canonical version. All other references (`CLAUDE.md`, `docs/architecture.md`) are informational mirrors that must be kept in sync during every version bump.

CI reads the `.csproj` version at pack time. The CI version-check job compares the `.csproj` version against the latest matching git tag and fails the PR if they are equal (version not bumped).
