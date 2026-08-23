# Context Map

## Contexts

- [HAL Domain & Serialization](./src/Chatter.Rest.Hal/CONTEXT.md) — in-memory HAL document model, fluent builder API, and JSON serialization/deserialization
- [HAL Shared Kernel](./src/Chatter.Rest.Hal.Core/CONTEXT.md) — declaration of record for HalResponseAttribute, with no standalone package and no consumer references; since CodeGenerators 0.4.0 the generator emits its own copy of the attribute into consuming compilations and discovers it by metadata name
- [HAL Code Generation](./src/Chatter.Rest.Hal.CodeGenerators/CONTEXT.md) — Roslyn source generator that emits HAL-aware partial classes from annotated user types

## Relationships

- **Shared Kernel -> HAL Domain & Serialization**: Core holds the declaration of record for **HalResponseAttribute**; the domain library declares the **Link Collection** and **Embedded Resource Collection** types that generated code references
- **Shared Kernel -> HAL Code Generation**: Code Generation emits its own copy of **HalResponseAttribute** into each consuming compilation (since 0.4.0) and reads it by metadata name at compile time to discover annotated classes; the CodeGenerators package declares a runtime dependency on the **Chatter.Rest.Hal** package for the types generated members use
- **HAL Domain & Serialization <-> HAL Code Generation**: No direct project reference; generated code depends on domain types (**Link Collection**, **Embedded Resource Collection**) at the consuming project's compile time, not at generator compile time
