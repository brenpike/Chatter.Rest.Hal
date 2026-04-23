# Development Guide

Local setup, build commands, test conventions, and CI/CD parity for Chatter.Rest.Hal.

---

## 1. Prerequisites

- **.NET 8.0 SDK** (`8.0.x`) — the version used in CI workflows
- **Language:** C# 10.0 (set per project via `<LangVersion>10.0</LangVersion>`)
- No external tools beyond the .NET SDK are required

There is no `global.json` in this repository. The SDK version is pinned only in CI. Use any `8.0.x` SDK locally.

---

## 2. Solution Structure

```
Chatter.Rest.Hal.sln
├── src/
│   ├── Chatter.Rest.Hal/Chatter.Rest.Hal.csproj
│   ├── Chatter.Rest.Hal.Core/Chatter.Rest.Hal.Core.csproj
│   ├── Chatter.Rest.Hal.CodeGenerators/Chatter.Rest.Hal.CodeGenerators.csproj
│   └── Chatter.Rest.UriTemplates/Chatter.Rest.UriTemplates.csproj
└── test/
    ├── Chatter.Rest.Hal.Tests/Chatter.Rest.Hal.Tests.csproj
    ├── Chatter.Rest.Hal.CodeGenerators.Tests/Chatter.Rest.Hal.CodeGenerators.Tests.csproj
    └── Chatter.Rest.UriTemplates.Tests/Chatter.Rest.UriTemplates.Tests.csproj
```

**Target frameworks:**

- `src/` projects multi-target `net8.0;netstandard2.0`
- `test/` projects target `net8.0` only

---

## 3. Build Commands

```bash
# Restore dependencies
dotnet restore

# Build (Debug)
dotnet build

# Build (Release, skip restore)
dotnet build -c Release --no-restore
```

---

## 4. Test Commands

```bash
# Run core library tests
dotnet test test/Chatter.Rest.Hal.Tests/Chatter.Rest.Hal.Tests.csproj

# Run code generator tests
dotnet test test/Chatter.Rest.Hal.CodeGenerators.Tests/Chatter.Rest.Hal.CodeGenerators.Tests.csproj

# Run URI template tests
dotnet test test/Chatter.Rest.UriTemplates.Tests/Chatter.Rest.UriTemplates.Tests.csproj

# Run all tests
dotnet test
```

CI runs tests with `-c Release --no-build` after a Release build step. To replicate CI exactly:

```bash
dotnet build -c Release --no-restore
dotnet test test/Chatter.Rest.Hal.Tests/Chatter.Rest.Hal.Tests.csproj -c Release --no-build
dotnet test test/Chatter.Rest.Hal.CodeGenerators.Tests/Chatter.Rest.Hal.CodeGenerators.Tests.csproj -c Release --no-build
```

---

## 5. NuGet Packaging

```bash
dotnet pack src/Chatter.Rest.Hal/Chatter.Rest.Hal.csproj -c Release -o publish/nuget
dotnet pack src/Chatter.Rest.Hal.CodeGenerators/Chatter.Rest.Hal.CodeGenerators.csproj -c Release -o publish/nuget
```

Output packages land in `publish/nuget/`.

---

## 6. CI/CD Parity

Two workflows live in `.github/workflows/`:

| Workflow | Scope |
|---|---|
| `hal-cicd.yml` | `src/Chatter.Rest.Hal/`, `test/Chatter.Rest.Hal.Tests/` |
| `codegen-cicd.yml` | `src/Chatter.Rest.Hal.CodeGenerators/`, `test/Chatter.Rest.Hal.CodeGenerators.Tests/` |
| `uritemplate-cicd.yml` | `src/Chatter.Rest.UriTemplates/`, `test/Chatter.Rest.UriTemplates.Tests/` |

**Both workflows:**

- Trigger on pushes to `feature/**` branches (path-scoped to their respective `src/` and `test/` directories) and on pushes to `main`
- Also trigger on pull requests targeting `main` (path-scoped)
- Use `dotnet restore --locked-mode` — requires `packages.lock.json` to be present and current
- Build with `-c Release --no-restore`
- Test with `-c Release --no-build`
- Deploy to NuGet.org only when `github.ref == 'refs/heads/main'` (after a successful PR merge) via the `NUGET_API_KEY_CHATTER_HAL` secret

**Lock file troubleshooting:** If `dotnet restore --locked-mode` fails locally with lock file errors, delete `packages.lock.json` and run `dotnet restore` to regenerate it. Commit the regenerated file.

---

## 7. Source Generator: No Manual Invocation

The Roslyn source generator (`Chatter.Rest.Hal.CodeGenerators`) runs automatically during `dotnet build`. No explicit invocation is required.

To inspect generated output after a build:

```
obj/Debug/net8.0/generated/Chatter.Rest.Hal.CodeGenerators/Chatter.Rest.Hal.CodeGenerators.HalResponseGenerator/
```

---

## 8. Code Style

`.editorconfig` enforces:

- **Line endings:** CRLF
- **Indentation:** tabs
- **Braces:** Allman style (`csharp_new_line_before_open_brace=all`)
- **Namespaces:** file-scoped (silent preference)

All projects have `<Nullable>enable</Nullable>`. The `netstandard2.0` target of converter files produces pre-existing `CS8604`/`CS8603` nullable warnings — these are baseline noise and do not affect the `net8.0` build. Do not treat them as regressions.

---

## 9. Test Conventions

- **Framework:** xunit 2.4.x
- **Assertions:** FluentAssertions 6.x (preferred); xunit `Assert` also used
- **Mocking:** Moq 4.x (core library tests only)
- **Coverage:** coverlet
- **Test naming:** `Method_Scenario_Expected` — e.g., `Curies_Are_Parsed_As_Array_Of_LinkObjects`
- **JSON fixtures:** `test/Chatter.Rest.Hal.Tests/Json/` — loaded via `TestHelpers.LoadResourceFromFixture()`
- **Shared helpers:** `TestHelpers` provides factory methods (`CreateLink`, `CreateLinkObject`, `CreateResourceWithLink`) and JSON assertion utilities

### Test Assertion Style

FluentAssertions (`Should()` syntax) is the preferred assertion library for new tests. Existing tests using xUnit `Assert.*` should not be migrated retroactively.
