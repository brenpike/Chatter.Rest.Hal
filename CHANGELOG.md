# Changelog — Chatter.Rest.Hal

All notable changes to `Chatter.Rest.Hal` will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.1.1] - 2026-08-23

### Documentation

- Added XML documentation comments to the 20 public fluent builder stage interfaces under
  `Builders/Stages/`, which were previously undocumented. Documentation only: no API or
  behavior change.

## [2.1.0] - 2026-08-23

### Fixed

- A Link Object whose `href` is the empty string is no longer silently dropped on
  deserialization. The empty string is a valid RFC 3986 section 4.4 same-document reference,
  which HAL section 5.1 permits by reference; it now materializes (preserving sibling
  properties such as `title`) and re-serializes as `"href": ""`, making the round-trip
  lossless. Whitespace-only, JSON-null, and absent `href` still drop the Link Object; a
  non-string `href` still throws `JsonException`; the public `LinkObject(string href)`
  constructor and the fluent builder path still reject null and whitespace hrefs.
  ([#120](https://github.com/brenpike/Chatter.Rest.Hal/issues/120))

## [2.0.0] - 2026-08-23

A hardening and correctness release: a full-repository review produced 22 verified fixes across
converters, domain types, builders, and extensions, followed by a clause-by-clause conformance
audit against draft-kelly-json-hal-11. All changes were coordinated under the design decisions
recorded in [#86](https://github.com/brenpike/Chatter.Rest.Hal/issues/86): HAL-spec conformance is
a hard rule, malformed input always fails as `JsonException`, and duplicate link relations are
rejected.

### Breaking changes

- **Reserved names are literal and case-sensitive.** Converters no longer force
  case-insensitive parsing: `_links`/`_embedded` match ordinally per the HAL spec, and the caller's
  `JsonSerializerOptions.PropertyNameCaseInsensitive` is honored for ordinary properties (last
  case-variant wins, matching duplicate-key normalization). Case-variant state properties such as
  `{"Name":..,"name":..}` no longer collide. ([#93](https://github.com/brenpike/Chatter.Rest.Hal/issues/93))
- **Malformed input throws `JsonException`.** Structurally invalid HAL (non-object resources,
  non-string hrefs, invalid `_links`/`_embedded` shapes) fails at the deserialization call with
  `JsonException` — never `InvalidOperationException`/`ArgumentException`, and never deferred to
  property access. (Duplicate JSON keys are not an error: they normalize last-wins, see below.) ([#94](https://github.com/brenpike/Chatter.Rest.Hal/issues/94),
  [#95](https://github.com/brenpike/Chatter.Rest.Hal/issues/95))
- **Duplicate link relations are rejected.** `LinkCollection.Add` (and
  `EmbeddedResourceCollection.Add` for duplicate names) throws `ArgumentException` on a duplicate
  key; the fluent builders instead merge repeated `AddLink(rel)`/`AddSelf()`/`AddCuries()` into the
  existing relation. Duplicate keys in *input JSON* are normalized last-wins.
  ([#99](https://github.com/brenpike/Chatter.Rest.Hal/issues/99),
  [#104](https://github.com/brenpike/Chatter.Rest.Hal/issues/104))
- **`curies` serializes as an array by default**, per HAL §8.3, on every builder path — a single
  definition emits `[{...}]`. `AsArray()` is now a no-op.
  ([#119](https://github.com/brenpike/Chatter.Rest.Hal/issues/119))
- **Single-element embedded collections keep their array shape** on round-trip, matching how links
  preserve single-vs-array form. ([#97](https://github.com/brenpike/Chatter.Rest.Hal/issues/97))
- **Equality is HAL-content equality.** `Resource`, `Link`, `LinkObject`, and the collections
  compare by the HAL document they produce: reading properties never changes a hash code, the state
  key derives from the actual writer (single source of truth), and shape flags count only where
  they affect serialization. ([#100](https://github.com/brenpike/Chatter.Rest.Hal/issues/100))
- **`State<T>()` returns detached projections.** Each call materializes a fresh snapshot from the
  state of record; mutating the result no longer affects serialization or equality (a state
  supplied to the constructor directly as `T` remains by-reference). `As<T>()` no longer caches for
  in-memory resources, so post-call mutations are reflected.
  ([#101](https://github.com/brenpike/Chatter.Rest.Hal/issues/101))
- **`GetLinkObjectOrDefault` returns the first matching link object** instead of throwing when a
  relation carries several. ([#105](https://github.com/brenpike/Chatter.Rest.Hal/issues/105))

### Fixed

- Builder crashes and misplacement: `AddLink`/`AddSelf`/`AddCuries` after `AddResources(...)` no
  longer throw `NullReferenceException`; `AddEmbedded` after configuring a link attaches to the
  correct resource; `FindParent` no longer self-matches.
  ([#102](https://github.com/brenpike/Chatter.Rest.Hal/issues/102),
  [#103](https://github.com/brenpike/Chatter.Rest.Hal/issues/103))
- `ResourceConverter.Write` no longer drops state fields whose CLR names are `Links`/`Embedded`;
  a state property literally named `_links`/`_embedded` is suppressed only when it would duplicate
  the resource's own emitted collection. ([#96](https://github.com/brenpike/Chatter.Rest.Hal/issues/96))
- CURIE expansion percent-encodes per RFC 6570, rejects empty suffixes, and searches all curie
  definitions; eager null/whitespace validation in builders.
  ([#105](https://github.com/brenpike/Chatter.Rest.Hal/issues/105))
- Primitive or array resource state fails serialization with a descriptive `JsonException` instead
  of producing invalid HAL; `AddHalConverters` registers correctly when a subset of converters was
  already present. ([#98](https://github.com/brenpike/Chatter.Rest.Hal/issues/98))
- Deeply nested `_embedded` chains parse in O(size) (no per-ancestor re-serialization), the
  duplicate-key pre-scan and rebuild are iterative (no stack overflow at raised `MaxDepth`), and
  options-registered custom converters take precedence over the built-in fast paths.

### Notes

- Full HAL spec-conformance audit: 28/30 normative requirements conform; the two deviations found
  were fixed ([#119](https://github.com/brenpike/Chatter.Rest.Hal/issues/119)) or documented
  ([#120](https://github.com/brenpike/Chatter.Rest.Hal/issues/120), open: empty-string href
  tolerance).

[Unreleased]: https://github.com/brenpike/Chatter.Rest.Hal/compare/hal/v2.1.1...HEAD
[2.1.1]: https://github.com/brenpike/Chatter.Rest.Hal/compare/hal/v2.1.0...hal/v2.1.1
[2.1.0]: https://github.com/brenpike/Chatter.Rest.Hal/compare/hal/v2.0.0...hal/v2.1.0
[2.0.0]: https://github.com/brenpike/Chatter.Rest.Hal/compare/hal/v1.1.0...hal/v2.0.0
