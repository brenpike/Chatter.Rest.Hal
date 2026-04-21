# CLAUDE.md

## Project Overview

Chatter.Rest.Hal is a .NET/C# implementation of the HAL (Hypertext Application Language) specification for building and consuming RESTful APIs. It provides a fluent builder API, System.Text.Json serialization/deserialization, and a Roslyn source generator package.

**Repository:** https://github.com/brenpike/Chatter.Rest.Hal
**HAL Specification:** https://datatracker.ietf.org/doc/html/draft-kelly-json-hal
**License:** MIT
**Author:** Brennan Pike

## HAL Specification Summary

HAL establishes conventions for expressing hypermedia controls (links) in JSON. Media type: `application/hal+json`.

### Resource Object
The root of every HAL document. Contains:
- Arbitrary JSON properties representing resource state
- `_links` (optional) — object whose keys are link relation types and values are a `LinkObject` or array of `LinkObject`
- `_embedded` (optional) — object whose keys are link relation types and values are a `Resource` or array of `Resource`

### Link Object Properties
| Property | Required | Description |
|---|---|---|
| `href` | Yes | URI or URI Template (RFC 6570) |
| `templated` | No | `true` when `href` is a URI Template |
| `type` | No | Media type hint for the target resource |
| `deprecation` | No | URL providing deprecation info |
| `name` | No | Secondary key when multiple links share the same relation |
| `profile` | No | URI hinting at the target resource's profile |
| `title` | No | Human-readable label |
| `hreflang` | No | Language of the target resource |

### CURIEs
Compact URI relations established via the reserved `curies` link relation — an array of named `LinkObject` entries whose `href` is a URI Template containing the `{rel}` token. Allows shortening `https://docs.acme.com/relations/widgets` to `acme:widgets`.

### Normative Rules
- Root object MUST be a Resource Object
- Each Resource Object SHOULD contain a `self` link
- Servers SHOULD NOT change a relation between a single `LinkObject` and an array across responses
- Custom link relation types SHOULD be URIs that provide documentation when dereferenced

## Solution Structure

```
Chatter.Rest.Hal.sln
├── src/
│   ├── Chatter.Rest.Hal/                     # Core library (NuGet: Chatter.Rest.Hal)
│   ├── Chatter.Rest.Hal.Core/                # Shared types (e.g., HalResponseAttribute)
│   └── Chatter.Rest.Hal.CodeGenerators/      # Roslyn source generator (NuGet: Chatter.Rest.Hal.CodeGenerators)
├── test/
│   ├── Chatter.Rest.Hal.Tests/               # Tests for the core library
│   └── Chatter.Rest.Hal.CodeGenerators.Tests/ # Tests for the source generator
```

## Multi-Agent Governance

This repository uses a constrained multi-agent workflow.

Canonical governance files:
- `agent-system-policy.md` — shared agent roles, authority, tool policy, escalation, and reporting
- `branching-pr-workflow.md` — MANDATORY branching, commit, PR, merge, and validation workflow

These files must ALWAYS be respected unless the user says otherwise.

Role-specific behavior is defined in:
- `orchestrator.md`
- `planner.md`
- `coder.md`
- `designer.md`

## Build and Test Commands

```bash
# Restore dependencies
dotnet restore

# Build the entire solution
dotnet build

# Build in Release mode
dotnet build -c Release --no-restore

# Run core library tests
dotnet test test/Chatter.Rest.Hal.Tests/Chatter.Rest.Hal.Tests.csproj

# Run code generator tests
dotnet test test/Chatter.Rest.Hal.CodeGenerators.Tests/Chatter.Rest.Hal.CodeGenerators.Tests.csproj

# Run all tests
dotnet test

# Create NuGet packages
dotnet pack src/Chatter.Rest.Hal/Chatter.Rest.Hal.csproj -c Release -o publish/nuget
dotnet pack src/Chatter.Rest.Hal.CodeGenerators/Chatter.Rest.Hal.CodeGenerators.csproj -c Release -o publish/nuget
```

## Target Frameworks

- **Core library and CodeGenerators:** Multi-target `net8.0` and `netstandard2.0`
- **Chatter.Rest.Hal.Core:** Multi-target `net8.0` and `netstandard2.0`
- **Test projects:** `net8.0` only
- **Language version:** C# 10.0
- **Nullable reference types:** Enabled across all projects

## Architecture

### Core Domain Types (Chatter.Rest.Hal)

All domain types are `sealed record` types implementing the `IHalPart` marker interface:

| Type | Description |
|---|---|
| `Resource` | Root HAL document; contains state, Links, and Embedded collections |
| `Link` | A link relation (rel) with a collection of `LinkObject` entries |
| `LinkObject` | A single hyperlink with href, templated, type, title, name, etc. |
| `EmbeddedResource` | A named embedded resource containing a `ResourceCollection` |
| `LinkCollection` | Collection of `Link` (serialized as `_links`) |
| `EmbeddedResourceCollection` | Collection of `EmbeddedResource` (serialized as `_embedded`) |
| `LinkObjectCollection` | Collection of `LinkObject` within a single `Link` |
| `ResourceCollection` | Collection of `Resource` within an `EmbeddedResource` |

### Fluent Builder API (Chatter.Rest.Hal/Builders/)

The builder uses a staged interface pattern to guide construction:

- **Entry point:** `ResourceBuilder.WithState(obj)` or `ResourceBuilder.New()`
- **Stages:** Interfaces in `Builders/Stages/` enforce valid builder transitions (e.g., `IResourceCreationStage`, `ILinkCreationStage`, `IAddResourceStage`)
- **Sub-builders:** `LinkCollectionBuilder`, `EmbeddedResourceCollectionBuilder`, `LinkObjectBuilder`
- **Termination:** `.Build()` walks up to the root `HalBuilder<Resource>` and calls `BuildPart()`

### JSON Serialization (Chatter.Rest.Hal/Converters/)

Custom `System.Text.Json` converters handle HAL-specific serialization:

- `ResourceConverter` -- Reads/writes the top-level resource, separating state from `_links`/`_embedded`
- `LinkCollectionConverter`, `LinkConverter`, `LinkObjectConverter`, `LinkObjectCollectionConverter`
- `EmbeddedResourceCollectionConverter`, `EmbeddedResourceConverter`
- `ResourceCollectionConverter`

Each domain type is annotated with `[JsonConverter(typeof(...))]` to wire up its converter.

### Extension Methods (Chatter.Rest.Hal/Extensions/)

Query helpers for navigating deserialized HAL resources:

- `ResourceExtensions` -- `GetEmbeddedResources<T>()`, `GetLinkOrDefault()`, `GetLinkObjectOrDefault()`, etc.
- `LinkCollectionExtensions` -- Link lookup by relation
- `EmbeddedResourceCollectionExtensions` -- Embedded resource lookup by name
- `LinkObjectCollectionExtensions` -- LinkObject lookup by name
- `ResourceCollectionExtensions` -- Cast resource collections via `.As<T>()`

### Source Generator (Chatter.Rest.Hal.CodeGenerators)

An incremental Roslyn source generator that processes classes decorated with `[HalResponse]`:

- `HalResponseGenerator` -- The `IIncrementalGenerator` entry point
- `Parser` -- Identifies syntax/semantic targets (classes with `[HalResponse]` attribute)
- `Emitter` -- Generates a partial class adding `Links` and `Embedded` properties with `[JsonPropertyName]` attributes

### Shared Types (Chatter.Rest.Hal.Core)

- `HalResponseAttribute` -- The attribute consumed by the source generator; lives in a separate package so consumer projects only need a reference to this (not the generator assembly at runtime)

## Testing Conventions

- **Framework:** xunit 2.4.x
- **Assertions:** FluentAssertions 6.x (preferred) and xunit `Assert` (both are used)
- **Mocking:** Moq 4.x (available in core tests)
- **Coverage:** coverlet (msbuild in core tests, collector in codegen tests)
- **Test naming:** Descriptive `Method_Scenario_Expected` style (e.g., `Curies_Are_Parsed_As_Array_Of_LinkObjects`)
- **JSON fixtures:** Test JSON files live in `test/Chatter.Rest.Hal.Tests/Json/` and are loaded via `TestHelpers.LoadResourceFromFixture()`
- **Shared helpers:** `TestHelpers` class provides factory methods (`CreateLink`, `CreateLinkObject`, `CreateResourceWithLink`) and JSON assertion utilities

## Code Style and Conventions

Defined in `.editorconfig`:

- **Line endings:** CRLF
- **Indentation:** Tabs
- **Namespaces:** File-scoped (silent preference)
- **Expression-bodied members:** Allowed on single line (operators, constructors, methods)
- **Braces:** Allman style (`csharp_new_line_before_open_brace=all`)

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

Two GitHub Actions workflows in `.github/workflows/`:

1. **hal-cicd.yml** -- Builds, tests, and publishes the core `Chatter.Rest.Hal` NuGet package
2. **codegen-cicd.yml** -- Builds, tests, and publishes the `Chatter.Rest.Hal.CodeGenerators` NuGet package

Both workflows:
- Trigger on pushes to `feature/**` branches (scoped to their respective `src/` paths) and merged PRs to `main`
- Use .NET 8.0.x SDK
- Deploy to NuGet.org on merged PRs using the `NUGET_API_KEY_CHATTER_HAL` secret

## Package Versions

- `Chatter.Rest.Hal` -- v0.9.2
- `Chatter.Rest.Hal.CodeGenerators` -- v0.2.5

<!-- code-review-graph MCP tools -->
## MCP Tools: code-review-graph

**IMPORTANT: This project has a knowledge graph. ALWAYS use the
code-review-graph MCP tools BEFORE using Grep/Glob/Read to explore
the codebase.** The graph is faster, cheaper (fewer tokens), and gives
you structural context (callers, dependents, test coverage) that file
scanning cannot.

### When to use graph tools FIRST

- **Exploring code**: `semantic_search_nodes` or `query_graph` instead of Grep
- **Understanding impact**: `get_impact_radius` instead of manually tracing imports
- **Code review**: `detect_changes` + `get_review_context` instead of reading entire files
- **Finding relationships**: `query_graph` with callers_of/callees_of/imports_of/tests_for
- **Architecture questions**: `get_architecture_overview` + `list_communities`

Fall back to Grep/Glob/Read **only** when the graph doesn't cover what you need.

### Key Tools

| Tool | Use when |
|------|----------|
| `detect_changes` | Reviewing code changes — gives risk-scored analysis |
| `get_review_context` | Need source snippets for review — token-efficient |
| `get_impact_radius` | Understanding blast radius of a change |
| `get_affected_flows` | Finding which execution paths are impacted |
| `query_graph` | Tracing callers, callees, imports, tests, dependencies |
| `semantic_search_nodes` | Finding functions/classes by name or keyword |
| `get_architecture_overview` | Understanding high-level codebase structure |
| `refactor_tool` | Planning renames, finding dead code |

### Workflow

1. The graph auto-updates on file changes (via hooks).
2. Use `detect_changes` for code review.
3. Use `get_affected_flows` to understand impact.
4. Use `query_graph` pattern="tests_for" to check coverage.