# CLAUDE.md

## Project Overview

Chatter.Rest.Hal is a .NET/C# implementation of the HAL (Hypertext Application Language) specification for building and consuming RESTful APIs. It provides a fluent builder API, System.Text.Json serialization/deserialization, and a Roslyn source generator package.

**Repository:** https://github.com/brenpike/Chatter.Rest.Hal  
**HAL Specification:** https://datatracker.ietf.org/doc/html/draft-kelly-json-hal  
**License:** MIT  
**Author:** Brennan Pike

## Documentation Index

| Doc | Contents |
|---|---|
| `docs/architecture.md` | Domain model, builder internals, converters, source generator pipeline |
| `docs/api.md` | Fluent builder API reference and extension method signatures |
| `docs/serialization.md` | Converter wiring, HalJsonOptions, force-array, deserialization internals |
| `docs/usage.md` | Copy-paste examples for building, serializing, deserializing |
| `docs/development.md` | Build/test/pack commands, code style, test conventions, CI/CD |
| `docs/HAL_TEST_PLAN.md` | HAL spec-to-test mapping; consult for spec compliance and tests |
| `docs/aspnetcore/requirements.md` | Package requirements for `Chatter.Rest.Hal.AspNetCore` |
| `docs/aspnetcore/architecture.md` | Architecture, pseudocode, and test strategy for `Chatter.Rest.Hal.AspNetCore` |
| `versioning.md` | SemVer rules, bump triggers, changelog, tag policy |

## Solution Structure

| Project | NuGet Package |
|---|---|
| `src/Chatter.Rest.Hal/` | `Chatter.Rest.Hal` |
| `src/Chatter.Rest.Hal.CodeGenerators/` | `Chatter.Rest.Hal.CodeGenerators` |
| `src/Chatter.Rest.Hal.Core/` | Shared types; no standalone package |
| `test/Chatter.Rest.Hal.Tests/` | — |
| `test/Chatter.Rest.Hal.CodeGenerators.Tests/` | — |

`Chatter.Rest.UriTemplates` is an external NuGet package dependency, not an in-repo project.

## Multi-Agent Governance

This repository uses the [agent-framework](https://github.com/brenpike/agent-framework) Claude Code plugin.

Enable the plugin in `.claude/settings.json`:
```json
{
  "enabledPlugins": {
    "agent-framework@brenpike": true
  }
}
```

The plugin provides:
- Agents: `agent-framework:orchestrator` (default), `agent-framework:planner`, `agent-framework:coder`, `agent-framework:designer`
- Skills: `agent-framework:create-working-branch`, `agent-framework:checkpoint-commit`, `agent-framework:open-plan-pr`, and others
- Governance reference docs in the plugin's `governance/` directory

Project-specific adapter details live in `CLAUDE.md` (this file). External reviewer guidance lives in `AGENTS.md`.

## Build and Test Commands

See `docs/development.md` for canonical build, test, pack, and CI/CD commands.

Use project documentation rather than inventing commands.

## Architecture and Code Style

See:

- `docs/architecture.md` for domain model, builders, converters, and source generator pipeline
- `docs/development.md` for editorconfig rules, style, tests, and fixtures
- `docs/HAL_TEST_PLAN.md` when adding tests or evaluating HAL spec compliance

Observed conventions:

- collection types are `sealed record` implementing `ICollection<T>` and `IHalPart`
- domain types use namespace `Chatter.Rest.Hal`
- converters use namespace `Chatter.Rest.Hal.Converters`
- builders use namespace `Chatter.Rest.Hal.Builders` with stages in sub-namespaces
- internal members are exposed to test assemblies via `InternalsVisibleTo`

## Package Versions

| Package | Version |
|---|---|
| `Chatter.Rest.Hal` | `1.1.0` |
| `Chatter.Rest.Hal.CodeGenerators` | `0.3.0` |

External dependency: `Chatter.Rest.UriTemplates` v0.1.0.

## Versioning Configuration

### Packages

| Package | Bump trigger path | Canonical version source | CHANGELOG | Tag prefix |
|---|---|---|---|---|
| `Chatter.Rest.Hal` | `src/Chatter.Rest.Hal/**` excluding `.md` | `src/Chatter.Rest.Hal/Chatter.Rest.Hal.csproj` | `CHANGELOG.md` | `hal` |
| `Chatter.Rest.Hal.CodeGenerators` | `src/Chatter.Rest.Hal.CodeGenerators/**` excluding `.md` | `src/Chatter.Rest.Hal.CodeGenerators/Chatter.Rest.Hal.CodeGenerators.csproj` | `CHANGELOG-CodeGenerators.md` | `codegen` |

`src/Chatter.Rest.Hal.Core/**` is a shared internal component. Changes there may trigger a bump in dependent packages if the change propagates through public API, runtime behavior, generated output, package contents, or compatibility contracts.

### Atomic Version Bump Files

When bumping version `X.Y.Z` for a package, update these atomically:

1. `src/<package>/<package>.csproj` — canonical `<Version>X.Y.Z</Version>`
2. `CLAUDE.md` — package version table row
3. package CHANGELOG — dated release section above `[Unreleased]` and comparison link update

### Git Tags

CI creates annotated tags after successful deploy to NuGet, post-`main` merge.

| Package | Tag format | Example |
|---|---|---|
| `Chatter.Rest.Hal` | `hal/vX.Y.Z` | `hal/v1.2.0` |
| `Chatter.Rest.Hal.CodeGenerators` | `codegen/vX.Y.Z` | `codegen/v0.4.0` |

Tags serve as version anchors for CI `version-check` validation on future PRs.

## Memory Usage

Use `claude-mem` when prior context, decisions, constraints, risks, or continuity may materially improve accuracy, efficiency, or consistency.

Memory is a continuity/token-efficiency aid, not a substitute for current repo inspection, validation, or required verification.

If memory fails, retry at most once when transient, then continue with normal tools and available context. Memory failure alone must not block execution.

## Codebase Exploration Guidance

Use local repo inspection first.

Preferred tools:

- `Read` for targeted inspection
- `Grep` and `Glob` for discovery
- read-only shell commands for structure/search
- `Context7` when external framework/library/platform/API docs are needed
- `claude-mem` when prior context reduces rediscovery

For code review, debugging, and refactoring:

1. start with the smallest local inspection that can answer the question
2. widen scope only when necessary
3. validate conclusions with the actual files being changed
