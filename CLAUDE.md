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
| [docs/architecture.md](docs/architecture.md) | Domain model, builder internals, converters, source generator pipeline |
| [docs/api.md](docs/api.md) | Full fluent builder API reference and extension method signatures |
| [docs/serialization.md](docs/serialization.md) | Converter wiring, HalJsonOptions, force-array, deserialization internals |
| [docs/usage.md](docs/usage.md) | Copy-paste examples for building, serializing, deserializing |
| [docs/development.md](docs/development.md) | Build/test/pack commands, code style, test conventions, CI/CD |
| [docs/HAL_TEST_PLAN.md](docs/HAL_TEST_PLAN.md) | Spec-to-test mapping; consult when adding tests or evaluating spec compliance |
| [docs/aspnetcore/requirements.md](docs/aspnetcore/requirements.md) | Package requirements (REQ-01–REQ-37) for `Chatter.Rest.Hal.AspNetCore` |
| [docs/aspnetcore/architecture.md](docs/aspnetcore/architecture.md) | Architecture, type pseudocode, and test strategy for `Chatter.Rest.Hal.AspNetCore` |
| [versioning.md](versioning.md) | SemVer rules, bump triggers, CHANGELOG convention, git tag policy |

## Solution Structure

| Project | NuGet Package |
|---|---|
| `src/Chatter.Rest.Hal/` | `Chatter.Rest.Hal` |
| `src/Chatter.Rest.Hal.CodeGenerators/` | `Chatter.Rest.Hal.CodeGenerators` |
| `src/Chatter.Rest.Hal.Core/` | Shared types; no standalone package |
| `test/Chatter.Rest.Hal.Tests/` | — |
| `test/Chatter.Rest.Hal.CodeGenerators.Tests/` | — |

`Chatter.Rest.UriTemplates` is an external NuGet package dependency (not an in-repo project). See [nuget.org](https://www.nuget.org/packages/Chatter.Rest.UriTemplates/).

## Multi-Agent Governance

This repository uses a constrained multi-agent workflow.

Canonical governance files:
- `agent-system-policy.md` — shared agent roles, authority, tool policy, escalation, and reporting
- `branching-pr-workflow.md` — mandatory branching, commit, PR, merge, and validation workflow
- `versioning.md` — mandatory SemVer, release metadata, CHANGELOG, and tag workflow
- `pr-review-remediation-loop.md` — mandatory external PR review feedback loop

These files must ALWAYS be respected unless the user says otherwise.

Role-specific behavior is defined in:
- `.claude/agents/orchestrator.md`
- `.claude/agents/planner.md`
- `.claude/agents/coder.md`
- `.claude/agents/designer.md`

Reusable Claude Code workflows are defined in `.claude/skills/`.

## Build and Test Commands

See [docs/development.md](docs/development.md) for all build, test, and pack commands.

## Architecture

See [docs/architecture.md](docs/architecture.md) for domain model, builder internals, converters, and source generator pipeline.

## Testing Conventions

See [docs/development.md](docs/development.md) for test framework, assertions, naming conventions, and fixture patterns.

## Code Style and Conventions

See [docs/development.md](docs/development.md) for editorconfig rules (line endings, indentation, braces).

Additional conventions observed in the codebase:

- All collection types are `sealed record` implementing `ICollection<T>` and `IHalPart`
- Domain types use the `Chatter.Rest.Hal` namespace
- Converters use the `Chatter.Rest.Hal.Converters` namespace
- Builders use the `Chatter.Rest.Hal.Builders` namespace with stages in sub-namespaces
- Internal members are exposed to test assemblies via `InternalsVisibleTo`

## Test Plan

`docs/HAL_TEST_PLAN.md` maps every normative and behavioral HAL spec requirement to testable scenarios, cross-referenced against the existing test suite. Consult it when:
- Answering questions about expected behavior
- Adding new tests for spec compliance
- Evaluating whether a bug is a spec violation or implementation choice

## CI/CD

See [docs/development.md](docs/development.md) for CI/CD workflow details.

## Package Versions

- `Chatter.Rest.Hal` — v1.1.0
- `Chatter.Rest.Hal.CodeGenerators` — v0.3.0

External dependency: `Chatter.Rest.UriTemplates` v0.1.0 ([NuGet](https://www.nuget.org/packages/Chatter.Rest.UriTemplates/))

See [versioning.md](versioning.md) for full versioning policy, bump rules, and CHANGELOG convention.

## Memory Usage

- Use `claude-mem` first when prior context, earlier decisions, constraints, risks, or continuity may materially improve accuracy, efficiency, or consistency.
- Treat memory as a continuity and token-efficiency aid, not as a substitute for current repo inspection, validation, or other required verification.
- Reuse still-valid prior context when helpful, but continue normally if no relevant memory is found.
- If `mem-search` or another memory tool fails, retry at most once if the failure appears transient, then fall back to normal tools and available context.
- Memory-tool failure alone must not block execution.

## Codebase Exploration Guidance

Use local repo inspection first for codebase exploration and change understanding.

Preferred tools:
- `Read` for targeted file inspection
- `Grep` and `Glob` for discovery
- read-only shell commands for repository structure and search
- `Context7` only when external framework, library, platform, or API documentation is needed
- `claude-mem` when prior project or session context can reduce rediscovery

For code review, debugging, and refactoring:
1. start with the smallest local inspection that can answer the question
2. widen scope only when necessary
3. validate conclusions with the actual files being changed

## PR Feedback Skill Selection

- Use `remediate-pr-comment` for generic PR comments or ambiguous reviewer feedback.
- Use `remediate-codex-review` only for explicit Codex review remediation or Codex re-review loops.


## PR Feedback Monitoring

- Use `watch-pr-feedback` only when explicitly asked to watch, monitor, wait for, poll, loop on, or continue handling PR feedback as it appears.
- Prefer dynamic `/loop` invocation so Monitor can be used when available.
- Use `remediate-pr-comment` for one-time generic PR comments.
- Use `remediate-codex-review` only for explicit Codex review remediation or Codex re-review loops.
