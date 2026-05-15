# HAL Code Generation

A Roslyn incremental source generator that discovers classes annotated with **HalResponseAttribute** and emits partial-class source containing HAL navigation properties (`Links`, `Embedded`).

## Language

**HalResponseGenerator**:
The Roslyn incremental source generator entry point that discovers **HalResponseAttribute**-annotated classes and feeds them to the **Emitter**.
_Avoid_: source generator, code generator (too generic), analyzer

**Emitter**:
The component that produces the generated C# source text for each discovered class, adding `Links` and `Embedded` properties with the correct `[JsonPropertyName]` attributes.
_Avoid_: code writer, template, renderer

**HalClassInfo**:
A value type capturing the name and namespace of a single annotated class, used as the pipeline's intermediate representation between discovery and emission.
_Avoid_: class metadata, class descriptor, target info

**Generated Partial Class**:
The source file emitted per annotated class, adding `Links` (typed as **Link Collection**) and `Embedded` (typed as **Embedded Resource Collection**) properties so the user's DTO can participate in HAL serialization without inheriting from **Resource**.
_Avoid_: generated file, code-gen output, scaffolded class

## Relationships

- **HalResponseGenerator** discovers classes annotated with **HalResponseAttribute** (defined in HAL Shared Kernel)
- **HalResponseGenerator** collects each discovered class as a **HalClassInfo** and passes the set to the **Emitter**
- The **Emitter** produces one **Generated Partial Class** per **HalClassInfo**
- Each **Generated Partial Class** references **Link Collection** and **Embedded Resource Collection** (defined in HAL Domain & Serialization) via `using Chatter.Rest.Hal`

## Example dialogue

> **Dev:** "My `[HalResponse]` class compiles but the generated `Links` property doesn't appear."
> **Domain expert:** "The class must be declared `partial`. **HalResponseGenerator** emits a **Generated Partial Class** that adds the property -- if the class isn't partial, the compiler can't merge them."

> **Dev:** "Can I customize what the **Emitter** generates for my class?"
> **Domain expert:** "No. The **Emitter** produces a fixed shape: a `Links` property of type **Link Collection** and an `Embedded` property of type **Embedded Resource Collection**. Customization happens at the domain level, not at generation time."

## Flagged ambiguities

- "generator" is used both for the Roslyn concept (IIncrementalGenerator) and for the project-specific **HalResponseGenerator** class -- resolved: use **HalResponseGenerator** when referring to this project's generator; use "Roslyn source generator" when referring to the platform concept.
