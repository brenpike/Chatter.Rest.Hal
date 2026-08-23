# HAL Shared Kernel

Shared marker types. The source generator discovers HalResponseAttribute by metadata name at compile time and does not require a direct project reference to Core. Since Chatter.Rest.Hal.CodeGenerators 0.4.0 the generator emits its own copy of the attribute into every compilation it runs in, so consumers do not reference this project; it remains as the declaration of record. Generated code references the domain package (Chatter.Rest.Hal) at the consuming project's compile time.

## Language

**HalResponseAttribute**:
A class-level attribute that marks a user-defined type for HAL source generation; the generator scans for this attribute at compile time to discover target classes.
_Avoid_: HalAttribute, HAL marker, response marker

## Relationships

- **HalResponseAttribute** is declared here as the declaration of record only; the generator emits the consumer-facing copy of the attribute into each consuming compilation (since CodeGenerators 0.4.0) and discovers it by metadata name at compile time to find annotated classes; generated code references HAL Domain & Serialization types (**Link Collection**, **Embedded Resource Collection**) at the consuming project's compile time

## Example dialogue

> **Dev:** "I decorated my DTO with `[HalResponse]` but nothing happened."
> **Domain expert:** "**HalResponseAttribute** only marks the class for the source generator. You also need the `Chatter.Rest.Hal.CodeGenerators` analyzer package referenced so the generator can find it and emit the partial class -- that package now supplies the attribute too."

## Flagged ambiguities

- None at this time.
