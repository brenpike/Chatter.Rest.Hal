# HAL Shared Kernel

Shared marker types referenced by consumer projects and test projects. The source generator discovers HalResponseAttribute by metadata name at compile time and does not require a direct project reference to Core. Generated code references the domain package (Chatter.Rest.Hal) at the consuming project's compile time.

## Language

**HalResponseAttribute**:
A class-level attribute that marks a user-defined type for HAL source generation; the generator scans for this attribute at compile time to discover target classes.
_Avoid_: HalAttribute, HAL marker, response marker

## Relationships

- **HalResponseAttribute** is declared here and consumed by consumer projects and test projects; HAL Code Generation discovers it by metadata name at compile time to find annotated classes; generated code references HAL Domain & Serialization types (**Link Collection**, **Embedded Resource Collection**) at the consuming project's compile time

## Example dialogue

> **Dev:** "I decorated my DTO with `[HalResponse]` but nothing happened."
> **Domain expert:** "**HalResponseAttribute** only marks the class for the source generator. You also need the `Chatter.Rest.Hal.CodeGenerators` analyzer package referenced so the generator can find it and emit the partial class."

## Flagged ambiguities

- None at this time.
