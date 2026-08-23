# Changelog — Chatter.Rest.Hal.CodeGenerators

All notable changes to `Chatter.Rest.Hal.CodeGenerators` will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.4.0] - 2026-08-22

### Added

- `HalResponseAttribute` is now emitted by the generator itself, so installing
  `Chatter.Rest.Hal.CodeGenerators` is sufficient for `[HalResponse]` to resolve. Previously no
  published package shipped the attribute and the generator never fired for a package consumer
  ([#109](https://github.com/brenpike/Chatter.Rest.Hal/issues/109)).
- The package now declares its dependency on `Chatter.Rest.Hal`, which supplies the `LinkCollection`
  and `EmbeddedResourceCollection` types the generated members are typed as
  ([#109](https://github.com/brenpike/Chatter.Rest.Hal/issues/109)).
- Documented that packable class libraries must pair the `PrivateAssets="all"` generator reference
  with a direct `Chatter.Rest.Hal` reference: `PrivateAssets="all"` keeps the transitive runtime
  dependency out of the packed nuspec, so without the direct reference downstream consumers would
  not restore the types the generated members expose
  ([#109](https://github.com/brenpike/Chatter.Rest.Hal/issues/109)).
- `CHATTER_REST_HAL_CODEGEN_EXCLUDE_ATTRIBUTE`: projects that declared
  `Chatter.Rest.Hal.HalResponseAttribute` in their own source under 0.3.x (the workaround for the
  missing attribute) add this symbol to `DefineConstants` to suppress the generated copy and avoid
  a same-assembly `CS0101` collision when upgrading
  ([#109](https://github.com/brenpike/Chatter.Rest.Hal/issues/109)).
- Diagnostics for annotated declarations that cannot receive the HAL members
  ([#107](https://github.com/brenpike/Chatter.Rest.Hal/issues/107)):
  - `HAL0001` (error) — the target is not declared `partial`.
  - `HAL0002` (error) — a type containing the target is not declared `partial`.
  - `HAL0003` (warning) — the target is a record; only class declarations are supported.
  - `HAL0004` (error) — the target already declares a member named `Links` or `Embedded`.

  Generation is skipped for the offending target so the diagnostic is what the user sees, rather
  than an unrelated `CS0260` or `CS0102`.

### Fixed

- Generic targets are generated correctly. `[HalResponse] partial class Foo<T>` previously produced
  an arity-0 `Foo`, leaving the generic type without `Links` and `Embedded` and adding an unintended
  empty type to the namespace ([#106](https://github.com/brenpike/Chatter.Rest.Hal/issues/106)).
- Nested targets are generated inside their containing types. The generator previously discarded the
  containing-type chain and emitted a stray top-level type
  ([#106](https://github.com/brenpike/Chatter.Rest.Hal/issues/106)).
- Deduplication is keyed on the fully qualified metadata name. Two distinct same-named types — for
  example an `Inner` in two different containing types — are no longer collapsed into one
  ([#106](https://github.com/brenpike/Chatter.Rest.Hal/issues/106)).
- Incremental caching works. The projection onto the equatable model now happens inside the
  `ForAttributeWithMetadataName` transform instead of after `Collect`, so an unrelated edit no longer
  re-runs the pipeline and re-emits every source
  ([#108](https://github.com/brenpike/Chatter.Rest.Hal/issues/108)).

### Changed

- The generated members are now emitted with fully qualified type names
  (`global::Chatter.Rest.Hal.LinkCollection`) instead of relying on a `using` directive, so a
  same-named type in the consumer's namespace cannot change what the generated source binds to.
- The package references `Microsoft.CodeAnalysis.CSharp` instead of
  `Microsoft.CodeAnalysis.CSharp.Workspaces`; the Workspaces layer was never used
  ([#109](https://github.com/brenpike/Chatter.Rest.Hal/issues/109)).

### Upgrade notes

- A project that also references a package or project declaring
  `Chatter.Rest.Hal.HalResponseAttribute` (for example the unpublished `Chatter.Rest.Hal.Core`
  project) will see `CS0436` because the generated attribute takes precedence in the consuming
  assembly. Drop that reference; the generator supplies the attribute.
- A `record` annotated with `[HalResponse]` previously generated nothing silently and now reports
  `HAL0003`. Declare the type as a `partial class` to receive the HAL members.

[Unreleased]: https://github.com/brenpike/Chatter.Rest.Hal/compare/codegen/v0.4.0...HEAD
[0.4.0]: https://github.com/brenpike/Chatter.Rest.Hal/compare/codegen/v0.3.0...codegen/v0.4.0
