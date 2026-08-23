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
A value type capturing everything needed to re-declare a single annotated class -- namespace, name with type parameters, the chain of containing types, and the fully qualified metadata name -- used as the pipeline's intermediate representation between discovery and emission. It is equatable by value, which is what lets the incremental pipeline reuse cached results.
_Avoid_: class metadata, class descriptor, target info

**Parser**:
The component that projects one annotated declaration onto a **HalClassInfo** and reports the diagnostics explaining why a declaration cannot receive the HAL members.
_Avoid_: analyzer, validator, scanner

**Generated Marker Attribute**:
The copy of **HalResponseAttribute** that **HalResponseGenerator** emits into every compilation it runs in, so installing the generator package alone is enough for the annotation to resolve.
_Avoid_: shipped attribute, built-in attribute

**Generated Partial Class**:
The source file emitted per annotated class, adding `Links` (typed as **Link Collection**) and `Embedded` (typed as **Embedded Resource Collection**) properties so the user's DTO can participate in HAL serialization without inheriting from **Resource**.
_Avoid_: generated file, code-gen output, scaffolded class

## Relationships

- **HalResponseGenerator** emits the **Generated Marker Attribute** and then discovers classes annotated with it
- The **Parser** projects each discovered class onto a **HalClassInfo**, or reports a diagnostic (HAL0001-HAL0004) when the class cannot receive the HAL members
- **HalResponseGenerator** deduplicates the models by fully qualified metadata name and passes the set to the **Emitter**
- The **Emitter** produces one **Generated Partial Class** per **HalClassInfo**, re-declaring the containing-type chain and the target's type parameters
- Each **Generated Partial Class** references **Link Collection** and **Embedded Resource Collection** (defined in HAL Domain & Serialization) via `using Chatter.Rest.Hal`

## Example dialogue

> **Dev:** "My `[HalResponse]` class compiles but the generated `Links` property doesn't appear."
> **Domain expert:** "The class must be declared `partial`, and so must every type containing it. **HalResponseGenerator** emits a **Generated Partial Class** that adds the property -- if the class isn't partial, the compiler can't merge them, and you'll see HAL0001 or HAL0002 saying so."

> **Dev:** "Can I customize what the **Emitter** generates for my class?"
> **Domain expert:** "No. The **Emitter** produces a fixed shape: a `Links` property of type **Link Collection** and an `Embedded` property of type **Embedded Resource Collection**. Customization happens at the domain level, not at generation time."

## Flagged ambiguities

- "generator" is used both for the Roslyn concept (IIncrementalGenerator) and for the project-specific **HalResponseGenerator** class -- resolved: use **HalResponseGenerator** when referring to this project's generator; use "Roslyn source generator" when referring to the platform concept.
