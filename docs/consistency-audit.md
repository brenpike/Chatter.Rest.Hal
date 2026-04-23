# Consistency Audit Checklist

Generated from a full repository audit on 2026-04-22. Each item is rated by how far it deviates from the established codebase patterns. Items flagged with **Bug risk: Yes** indicate cases where the inconsistency could cause incorrect runtime behavior or misleading diagnostics. Work items smallest-to-largest, or prioritize bug-risk items first.

---

## Medium

### CONS-01: ArgumentNullException for whitespace strings

- **Rating:** Medium
- **Bug risk:** Yes — misleading exception type; callers catching ArgumentNullException for null-guard purposes won't see whitespace rejections
- **Category:** Exception
- **Files:**
  - `src/Chatter.Rest.Hal/Link.cs` line 24
  - `src/Chatter.Rest.Hal/LinkObject.cs` (constructor guard, similar line)
- **Issue:** `Link` and `LinkObject` throw `ArgumentNullException` when `IsNullOrWhiteSpace` returns true. A whitespace-only string is not null — `ArgumentNullException` is semantically incorrect for that case. `EmbeddedResource.cs:22` correctly throws `ArgumentException` instead.
- **Fix:** In `Link.cs` and `LinkObject.cs`, replace `throw new ArgumentNullException(...)` in the `IsNullOrWhiteSpace` guard with `throw new ArgumentException("Value cannot be null or whitespace.", nameof(paramName))` to match `EmbeddedResource.cs` pattern.
- [x] Done

### CONS-02: NotImplementedException in IBuildHalPart<Resource> explicit interface implementation

- **Rating:** Medium
- **Bug risk:** Yes — LSP violation; polymorphic use via interface causes runtime crash
- **Category:** Builder
- **Files:**
  - `src/Chatter.Rest.Hal/Builders/ResourceCollectionBuilder.cs` line 115
- **Issue:** `Resource IBuildHalPart<Resource>.BuildPart() => throw new NotImplementedException();` — The builder hierarchy forces `ResourceCollectionBuilder` to implement this method but it cannot meaningfully do so. Any caller holding an `IBuildHalPart<Resource>` reference that happens to be a `ResourceCollectionBuilder` will get a runtime exception.
- **Fix:** Requires a design decision — either (a) remove `IBuildHalPart<Resource>` from `ResourceCollectionBuilder`'s interface hierarchy if the hierarchy allows it, or (b) throw `NotSupportedException` (more semantically correct than `NotImplementedException`) with a message explaining why this is unsupported on a collection builder.
- [x] Done

### CONS-03: Dead Parser.cs class in CodeGenerators

- **Rating:** Medium
- **Bug risk:** No
- **Category:** SourceGen
- **Files:**
  - `src/Chatter.Rest.Hal.CodeGenerators/Parser.cs` (entire file)
- **Issue:** `Parser` contains `GetSemanticTargetForGeneration()` and `IsSyntaxTargetForGeneration()` — helpers for the old `ISourceGenerator` pipeline. `HalResponseGenerator` was migrated to `IIncrementalGenerator` / `ForAttributeWithMetadataName` in PERF-09. `Parser.cs` has no callers. Dead code increases maintenance surface.
- **Fix:** Delete `src/Chatter.Rest.Hal.CodeGenerators/Parser.cs`. Confirm no remaining references before deleting (grep for `Parser.` in that project). Note: this file appeared as an unstaged deletion in a prior session (missed from commit e6d73cd) — it may already be deleted on branch `perf/remaining-fixes`.
- [x] Done

### CONS-04: Extension methods bypass O(1) dictionary lookup added by PERF-13

- **Rating:** Medium
- **Bug risk:** No
- **Category:** Collection
- **Files:**
  - `src/Chatter.Rest.Hal/Extensions/LinkCollectionExtensions.cs` line 18 (`GetLinkOrDefault`)
  - `src/Chatter.Rest.Hal/Extensions/EmbeddedResourceCollectionExtensions.cs` line 19 (`GetEmbeddedResource`)
- **Issue:** Both extension methods use `SingleOrDefault()` LINQ linear scan (O(n)). PERF-13 added `TryGetByRel()` on `LinkCollection` and `TryGetByName()` on `EmbeddedResourceCollection` for O(1) lookup, but the primary consumer-facing extension methods were not updated to use them. The dictionary index exists but is unreachable via the public API.
- **Fix:** In `GetLinkOrDefault`, call `links.TryGetByRel(rel, out var link)` and return `link ?? default` instead of the `SingleOrDefault` predicate. In `GetEmbeddedResource`, call `collection.TryGetByName(name, out var resource)` and return `resource`. Note: the existing `SingleOrDefault` throws `InvalidOperationException` on duplicates — verify whether tests assert this behavior and whether O(1) lookup should preserve or relax that contract. Three tests in `LinkCollectionExtensionsTests` and `EmbeddedResourceCollectionExtensionsTests` assert the duplicate-throw contract; switching to TryGet will change that behavior (TryGet does not throw on duplicates). Design decision needed.
- [x] Done

### CONS-05: Corrupted line ending in Resource.cs As<T>() method

- **Rating:** Medium
- **Bug risk:** No
- **Category:** PerfWork
- **Files:**
  - `src/Chatter.Rest.Hal/Resource.cs` line 166
- **Issue:** `return null;` is followed by `\r\t\t}` instead of `\r\n\t\t}` — the closing brace appears on the same line in editors that display bare `\r`. Introduced during PERF edits. Compiles fine but produces confusing diffs and rendering issues.
- **Fix:** Open `Resource.cs`, find line 166, fix the line ending so `return null;` and the closing `}` are on separate lines with proper `\r\n` termination.
- [x] Done

### CONS-06: Mixed tabs/spaces indentation in converter method bodies

- **Rating:** Medium
- **Bug risk:** No
- **Category:** Indentation
- **Files:**
  - `src/Chatter.Rest.Hal/Converters/EmbeddedResourceCollectionConverter.cs` lines 21-40 (Read), 49-76 (helper), 82-105 (Write)
  - `src/Chatter.Rest.Hal/Converters/EmbeddedResourceConverter.cs` lines 24-57 (Read), 66-71 (Write)
  - `src/Chatter.Rest.Hal/Converters/LinkConverter.cs` lines 37-124 (Read)
  - `src/Chatter.Rest.Hal/Converters/LinkObjectCollectionConverter.cs` lines 30-61 (Read), 68-88 (helpers), 97-149 (Write)
  - `src/Chatter.Rest.Hal/Converters/LinkCollectionConverter.cs` lines 62-101 (helper)
  - `src/Chatter.Rest.Hal.CodeGenerators/Emitter.cs` lines 57-88 (`GenerateCode` method)
- **Issue:** `.editorconfig` mandates `indent_style=tab`. Class-level declarations and XML docs use tabs consistently. Method bodies in these files use 4-space indentation. Likely migrated from an earlier codebase. Makes diffs noisy.
- **Fix:** Run an editor format (Format Document) on each file with editorconfig enforcement, or use `dotnet format` to normalize whitespace across the solution.
- [x] Done

---

## Low

### CONS-07: Dead EnumerableExtensions.cs in CodeGenerators

- **Rating:** Low
- **Bug risk:** No
- **Category:** SourceGen
- **Files:**
  - `src/Chatter.Rest.Hal.CodeGenerators/EnumerableExtensions.cs` (entire file)
- **Issue:** `NotNull<T>()` extension method has no callers after PERF-09/10 migration. Was used by the old Parser-based pipeline. Dead code.
- **Fix:** Delete `src/Chatter.Rest.Hal.CodeGenerators/EnumerableExtensions.cs`. Grep for `NotNull` references first. Note: like Parser.cs, this may already be deleted as an unstaged change on branch `perf/remaining-fixes`.
- [x] Done

### CONS-08: JsonSerializerOptionsExtensions in separate namespace

- **Rating:** Low
- **Bug risk:** No
- **Category:** Namespace
- **Files:**
  - `src/Chatter.Rest.Hal/Extensions/JsonSerializerOptionsExtensions.cs` line 4
- **Issue:** Uses namespace `Chatter.Rest.Hal.Extensions`. All other extension classes (`LinkCollectionExtensions`, `EmbeddedResourceCollectionExtensions`, `ResourceCollectionExtensions`, `ResourceExtensions`, `LinkObjectCollectionExtensions`) use namespace `Chatter.Rest.Hal`. Consumers must add `using Chatter.Rest.Hal.Extensions;` to access `AddHalConverters()` but not for any other extension method. Undocumented inconsistency.
- **Fix:** Change namespace to `Chatter.Rest.Hal`. Confirm no external consumers reference the old namespace (search for `using Chatter.Rest.Hal.Extensions`). Update tests if any import that namespace explicitly.
- [x] Done

### CONS-09: Redundant using Chatter.Rest.Hal in 3 converter files

- **Rating:** Low
- **Bug risk:** No
- **Category:** Namespace
- **Files:**
  - `src/Chatter.Rest.Hal/Converters/LinkConverter.cs` line 5
  - `src/Chatter.Rest.Hal/Converters/LinkCollectionConverter.cs` line 5
  - `src/Chatter.Rest.Hal/Converters/LinkObjectCollectionConverter.cs` line 5
- **Issue:** These converters live in `Chatter.Rest.Hal.Converters` namespace. C# resolves parent namespaces (`Chatter.Rest.Hal`) implicitly without a `using` directive. The other 5 converters in the same namespace do not have this import.
- **Fix:** Remove the redundant `using Chatter.Rest.Hal;` from the three listed files.
- [x] Done

### CONS-10: Mixed namespace declaration style in test files

- **Rating:** Low
- **Bug risk:** No
- **Category:** Namespace
- **Files:**
  - Block-scoped (older): `LinkConvertersTests.cs:6`, `HalSerializationRoundTripTests.cs:8`, `TestHelpers.cs:7`, `HalForceArrayTests.cs:7`
  - File-scoped (newer): `ResourceTests.cs:5`, `BuilderTests.cs:10`, `LinkBehaviorTests.cs:5`, `ResourceBehaviorTests.cs:6`
- **Issue:** `.editorconfig` specifies `csharp_style_namespace_declarations=file_scoped:silent`. Older files use block-scoped `namespace X { }`, newer files use file-scoped `namespace X;`. Inconsistent across the test project.
- **Fix:** Convert block-scoped namespace files to file-scoped to match editorconfig preference. Can use `dotnet format` or per-file IDE quick-fix.
- [x] Done

### CONS-11: .First() vs .FirstOrDefault() pattern in converters

- **Rating:** Low
- **Bug risk:** No (guarded)
- **Category:** Converter
- **Files:**
  - `src/Chatter.Rest.Hal/Converters/LinkConverter.cs` line 52
- **Issue:** Uses `.First()` to get first key-value pair from `JsonObject`. Guarded by `Count != 1` check on line 47, so safe. However `EmbeddedResourceConverter.cs:32` uses `.FirstOrDefault()` with null check for the same operation. Inconsistent pattern — both are correct, but the defensive pattern is `.FirstOrDefault()`.
- **Fix:** Change `.First()` to `.FirstOrDefault()` and add a null guard, matching the `EmbeddedResourceConverter` pattern.
- [x] Done

### CONS-12: else if vs standalone if branching in LinkCollectionConverter

- **Rating:** Low
- **Bug risk:** No
- **Category:** Converter
- **Files:**
  - `src/Chatter.Rest.Hal/Converters/LinkCollectionConverter.cs` line 87
- **Issue:** `else if (kvp.Value is JsonObject jo)` followed by a standalone `if (kvp.Value is JsonArray ja)` on line 93. Since a `JsonNode` can only be one subtype, the two checks are mutually exclusive. All other converters (`ResourceCollectionConverter`, `EmbeddedResourceCollectionConverter`, `LinkObjectCollectionConverter`) use two standalone `if` blocks consistently.
- **Fix:** Change `else if (kvp.Value is JsonObject jo)` to `if (kvp.Value is JsonObject jo)` for pattern consistency.
- [x] Done

### CONS-13: Duplicated IsJsonNull() helper in two converters

- **Rating:** Low
- **Bug risk:** No
- **Category:** Converter
- **Files:**
  - `src/Chatter.Rest.Hal/Converters/LinkConverter.cs` lines 153-161
  - `src/Chatter.Rest.Hal/Converters/LinkCollectionConverter.cs` lines 134-142
- **Issue:** Identical `private static bool IsJsonNull(JsonNode? node)` helper with same `#if NET8_0_OR_GREATER` conditional exists in both files. DRY violation — if the logic changes, both must be updated.
- **Fix:** Extract to an `internal static class JsonNodeExtensions` (or similar) in the Converters namespace, or move to a `ConverterHelpers.cs` internal static class. Both converter files import it. Alternatively move to the `Chatter.Rest.Hal.Core` project if it belongs at shared-types level.
- [x] Done

### CONS-14: Dictionary index only on 2 of 4 collection types

- **Rating:** Low
- **Bug risk:** No
- **Category:** Collection
- **Files:**
  - Has index: `src/Chatter.Rest.Hal/LinkCollection.cs`, `src/Chatter.Rest.Hal/EmbeddedResourceCollection.cs`
  - No index: `src/Chatter.Rest.Hal/ResourceCollection.cs`, `src/Chatter.Rest.Hal/LinkObjectCollection.cs`
- **Issue:** PERF-13 added `Dictionary<string, T>` O(1) index with `TryGetByRel`/`TryGetByName` to `LinkCollection` and `EmbeddedResourceCollection`. `ResourceCollection` and `LinkObjectCollection` have no such index. This is likely intentional (no natural string key for those types) but creates asymmetric API surface. `ResourceCollection` items are keyed by what? `LinkObjectCollection` has no relation key at the item level.
- **Fix:** Document the intentional asymmetry (add XML comment to `ResourceCollection` and `LinkObjectCollection` explaining no O(1) index exists and why), OR if a natural key exists, add it. No code change required if intentional.
- [x] Done

### CONS-15: Non-descriptive test method names

- **Rating:** Low
- **Bug risk:** No
- **Category:** Test
- **Files:**
  - `test/Chatter.Rest.Hal.Tests/Tests.cs` line 108 — method named `Link()`
  - `test/Chatter.Rest.Hal.Tests/BuilderTests.cs` line 15 — method named `test()`
- **Issue:** All other tests follow descriptive `Should_X_When_Y` or `X_Should_Y` naming. These two appear to be early exploratory tests never renamed.
- **Fix:** Rename to descriptive names following repo naming convention. Read the test body to determine appropriate names.
- [x] Done

### CONS-16: Test method name typos

- **Rating:** Low
- **Bug risk:** No
- **Category:** Test
- **Files:**
  - `test/Chatter.Rest.Hal.Tests/ResourceTests.cs` line 91 — `"Striongly"` should be `"Strongly"`
  - `test/Chatter.Rest.Hal.Tests/ResourceTests.cs` line 231 — `"EmbeddedResourceollection"` should be `"EmbeddedResourceCollection"`
- **Issue:** Typos in test method names reduce searchability and look unprofessional.
- **Fix:** Rename both methods. Check if any test filter strings in CI reference these names exactly.
- [x] Done

### CONS-17: Mixed assertion libraries in test project

- **Rating:** Low
- **Bug risk:** No
- **Category:** Test
- **Files:**
  - xUnit Assert.*: `Tests.cs`, `ResourceTests.cs`, `ResourceExtensionsTests.cs`, `LinkConvertersTests.cs`, `HalLinksCollectionTests.cs`, `HalDeserializationRobustnessTests.cs`
  - FluentAssertions: `BuilderTests.cs`, `HalForceArrayTests.cs`, `HalCuriesAndTemplatedTests.cs`, `HalEmbeddedTests.cs`, `HalLinkObjectTests.cs`, `HalSerializationRoundTripTests.cs`, `LinkCollectionExtensionsTests.cs`, `HalMediaTypeTests.cs`, `HalLinkAttributesValidationTests.cs`
- **Issue:** Both FluentAssertions (`.Should()`) and xUnit `Assert.*` are used across the test project. No single house style.
- **Fix:** Pick one (FluentAssertions is preferred by newer tests) and migrate the older files. Or document the accepted style in `docs/development.md` to prevent further mixing.
- [x] Done

### CONS-18: Stale TODO comment in ResourceTests.cs

- **Rating:** Low
- **Bug risk:** No
- **Category:** Test
- **Files:**
  - `test/Chatter.Rest.Hal.Tests/ResourceTests.cs` line 243
- **Issue:** `//TODO: As<T> tests` — `As<T>` tests already exist in `ResourceBehaviorTests.cs` and `ResourceConvertersTests.cs`. The TODO was never removed.
- **Fix:** Delete the comment.
- [x] Done

### CONS-19: Non-sealed builder classes with no subclasses

- **Rating:** Low
- **Bug risk:** No
- **Category:** Builder
- **Files:**
  - `src/Chatter.Rest.Hal/Builders/EmbeddedResourceBuilder.cs` line 11
  - `src/Chatter.Rest.Hal/Builders/EmbeddedResourceCollectionBuilder.cs` line 10
- **Issue:** These two builders are not `sealed`. All peer builders (`LinkBuilder`, `LinkCollectionBuilder`, `LinkObjectCollectionBuilder`, `LinkObjectBuilder`, `ResourceCollectionBuilder`, `ResourceCollectionResourceBuilder`) are `sealed`. `ResourceBuilder` is intentionally non-sealed (has a subclass `ResourceCollectionResourceBuilder`). `EmbeddedResourceBuilder` and `EmbeddedResourceCollectionBuilder` have no subclasses.
- **Fix:** Add `sealed` to both `EmbeddedResourceBuilder` and `EmbeddedResourceCollectionBuilder`.
- [x] Done

### CONS-20: Converter classes not sealed

- **Rating:** Low
- **Bug risk:** No
- **Category:** Access
- **Files:**
  - All 8 converters in `src/Chatter.Rest.Hal/Converters/`
- **Issue:** All converters are `public class` without `sealed`. JsonConverters are not designed for subclassing. Making them sealed prevents unintended inheritance and allows JIT devirtualization.
- **Fix:** Add `sealed` to all 8 converter class declarations.
- [x] Done

### CONS-21: Bare catch blocks vs catch (Exception)

- **Rating:** Low
- **Bug risk:** No
- **Category:** Exception
- **Files:**
  - `src/Chatter.Rest.Hal/Converters/LinkObjectCollectionConverter.cs` line 76
  - `src/Chatter.Rest.Hal/Converters/LinkConverter.cs` line 116
- **Issue:** Bare `catch` (no exception type) vs `catch (Exception)` in `Resource.cs`. Both are identical in practice for CLS-compliant code but are inconsistent in style.
- **Fix:** Change bare `catch` to `catch (Exception)` in the two converter files for consistency.
- [x] Done

### CONS-22: InvalidOperationException thrown with no message

- **Rating:** Low
- **Bug risk:** No
- **Category:** Exception
- **Files:**
  - `src/Chatter.Rest.Hal/Builders/LinkObjectBuilder.cs` line 205
  - `src/Chatter.Rest.Hal/Builders/ResourceCollectionBuilder.cs` line 86
- **Issue:** `throw new InvalidOperationException()` with no message string. When thrown, callers get no context about what state caused the error.
- **Fix:** Add descriptive messages, e.g. `throw new InvalidOperationException("Cannot call BuildPart when builder is in an invalid state.")`.
- [x] Done

### CONS-23: Redundant inheritdoc + summary XML docs on same member

- **Rating:** Low
- **Bug risk:** No
- **Category:** XmlDocs
- **Files:**
  - `src/Chatter.Rest.Hal/Builders/LinkObjectBuilder.cs` lines 208-212
- **Issue:** Method has both `///<inheritdoc/>` AND a `/// <summary>` block. `inheritdoc` pulls docs from the interface; the `summary` is redundant and may produce duplicate IntelliSense output.
- **Fix:** Remove the `/// <summary>` block; keep only `///<inheritdoc/>`.
- [x] Done

### CONS-24: Emitter.cs mixed indentation in GenerateCode method

- **Rating:** Low
- **Bug risk:** No
- **Category:** SourceGen
- **Files:**
  - `src/Chatter.Rest.Hal.CodeGenerators/Emitter.cs` lines 57-88
- **Issue:** `GenerateCode()` method body uses 4-space indent. Rest of file and all other methods use tabs (matching editorconfig). Same pattern as converter files.
- **Fix:** Reformat `GenerateCode()` to use tabs. Or use `dotnet format` across the solution.
- [x] Done

### CONS-25: CachedState vs StateObject semantic divergence in ResourceConverter

- **Rating:** Low
- **Bug risk:** No (benign for Write path)
- **Category:** PerfWork
- **Files:**
  - `src/Chatter.Rest.Hal/Converters/ResourceConverter.cs` line 59
  - `src/Chatter.Rest.Hal/Resource.cs` lines 53-67, 121-127
- **Issue:** `ResourceConverter.Write()` accesses `value.CachedState` (introduced in PERF-11) which bypasses the Link-guard logic in `State<T>()` (Resource.cs lines 121-127). The guard prevents generic objects from being misinterpreted as Links. For the Write path, this divergence is benign (the Link-guard is a deserialization concern). Documented here for awareness.
- **Fix:** Add an XML comment to `CachedState` property explaining it bypasses the Link-guard and is only safe for the Write (serialization) path. No behavior change needed.
- [x] Done

### CONS-26: HalBuilder constructor accessibility

- **Rating:** Low
- **Bug risk:** No
- **Category:** Access
- **Files:**
  - `src/Chatter.Rest.Hal/Builders/HalBuilder.cs` line 7
- **Issue:** `HalBuilder<THalPart>` is `public abstract class` with a public constructor. External consumers can subclass `HalBuilder` and create custom builders outside the library. May or may not be intended. If external extension is not a design goal, the constructor should be `internal` or `protected internal`.
- **Fix:** If external subclassing is not intended: change constructor from `public` to `protected internal`. If it is intended: add XML doc noting the extension point is public by design.
- [x] Done

### CONS-27: Null-parent builder paths in tests bypass production hierarchy

- **Rating:** Low
- **Bug risk:** No
- **Category:** Test
- **Files:**
  - `test/Chatter.Rest.Hal.Tests/Tests.cs` lines 143-144, 171
- **Issue:** Tests call `LinkObjectBuilder.WithHref(null, "/foo/2").BuildPart()` and `EmbeddedResourceBuilder.WithName(null, "num1")` — passing `null` as the parent builder. This works because `WithHref`/`WithName` are `internal static` factory methods (accessed via `InternalsVisibleTo`). In production, these factory methods are always called with a non-null parent. This exercises a code path that cannot occur in production use.
- **Fix:** Document with a comment explaining the null parent is a test-only pattern, OR refactor test to use the real builder hierarchy to eliminate the special case. No runtime impact.
- [x] Done

---

## Remediation Summary

All 27 items remediated on 2026-04-22 on branch `chore/consistency-fixes`.

Validation: 183 tests passed (177 HAL + 6 CodeGenerators) on net8.0. Both TFMs built clean.

Commits:
- `c365e42` — CONS-01, 02, 11, 12, 21, 22: exception types, logic, catch style
- `02af838` — CONS-03, 07, 13: delete dead Parser/EnumerableExtensions, extract ConverterHelpers
- `1fb95d8` — CONS-05, 06, 08, 09, 19, 20, 23, 24: namespaces, sealing, formatting, line endings
- `029746d` — CONS-10, 15, 16, 18, 27: test file hygiene
- `3d95cbd` — CONS-04, 14, 17, 25, 26: XML docs and assertion style guidance
