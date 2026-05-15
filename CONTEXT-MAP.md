# Context Map

## Contexts

- [HAL Domain & Serialization](./src/Chatter.Rest.Hal/CONTEXT.md) — in-memory HAL document model, fluent builder API, and JSON serialization/deserialization
- [HAL Shared Kernel](./src/Chatter.Rest.Hal.Core/CONTEXT.md) — shared marker types consumed by both the domain library and the source generator
- [HAL Code Generation](./src/Chatter.Rest.Hal.CodeGenerators/CONTEXT.md) — Roslyn source generator that emits HAL-aware partial classes from annotated user types

## Relationships

- **Shared Kernel -> HAL Domain & Serialization**: Core defines **HalResponseAttribute**; the domain library declares the **Link Collection** and **Embedded Resource Collection** types that generated code references
- **Shared Kernel -> HAL Code Generation**: Code Generation reads **HalResponseAttribute** at compile time to discover annotated classes and emit partial-class source
- **HAL Domain & Serialization <-> HAL Code Generation**: No direct project reference; generated code depends on domain types (**Link Collection**, **Embedded Resource Collection**) at the consuming project's compile time, not at generator compile time
