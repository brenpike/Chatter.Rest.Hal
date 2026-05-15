# HAL Shared Kernel

Shared marker types consumed by both the HAL domain library and the source generator. This context exists to break the circular dependency: the generator needs to know which classes to target, and the domain library defines the types that generated code references.

## Language

**HalResponseAttribute**:
A class-level attribute that marks a user-defined type for HAL source generation; the generator scans for this attribute at compile time to discover target classes.
_Avoid_: HalAttribute, HAL marker, response marker

## Relationships

- **HalResponseAttribute** is declared here and consumed by two downstream contexts: HAL Code Generation reads it to discover annotated classes; HAL Domain & Serialization ships the domain types (**Link Collection**, **Embedded Resource Collection**) that appear in the generated partial-class members

## Example dialogue

> **Dev:** "I decorated my DTO with `[HalResponse]` but nothing happened."
> **Domain expert:** "**HalResponseAttribute** only marks the class for the source generator. You also need the `Chatter.Rest.Hal.CodeGenerators` analyzer package referenced so the generator can find it and emit the partial class."

## Flagged ambiguities

- None at this time.
