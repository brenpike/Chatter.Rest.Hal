API Reference — Chatter.Rest.Hal (concise)

This short reference lists the public types and fluent methods used in the examples. It is intended to align with the public API surface used by tests and examples.

ResourceBuilder (fluent)
- WithState(object state)
  - Start building a resource with a state object (anonymous or typed).
- AddSelf()
  - Shortcut to add the `self` relation. Returns a LinkBuilder to attach a Link Object.
- AddCuries()
  - Shortcut to add the `curies` relation for namespaced relations.
- AddLink(string rel)
  - Start a link relation; returns LinkBuilder for the named relation.
- AddEmbedded(string rel)
  - Start an embedded collection under the given relation; returns an EmbeddedBuilder.
- Build()
  - Finalize and return a Resource object representing the HAL document.

LinkBuilder
- AddLinkObject(string href)
  - Add a link object with the specified href and return a LinkObjectBuilder to set metadata.

LinkObjectBuilder
- Templated()
  - Mark the last link object as templated (href contains variables).
- WithTitle(string title)
  - Set a title on the link object.
- WithName(string name)
  - Set the name (useful for curies).

EmbeddedBuilder
- AddResource(object state)
  - Add one embedded resource and returns a ResourceBuilder for that embedded resource so you can add links to it.
- AddResources<T>(IEnumerable<T> items, Action<T, ResourceBuilder> configure)
  - Add a list of embedded resources and configure each via the provided callback.

Resource (runtime)
- As<T>()
  - Convert the Resource document into a strongly-typed DTO (uses runtime mapping).
- State<T>()
  - Read the resource state as a strongly-typed object.
- GetLinkOrDefault(string rel)
  - Get a single Link (or null) by relation.
- GetLinkObjects(string rel)
  - Get zero-or-more link objects for a relation.
- GetEmbeddedResources<T>(string rel)
  - Extract embedded resources as T.
- GetResourceCollection(string rel)
  - Get embedded resources as Resource objects.

Notes
- Serialization and deserialization use System.Text.Json (the examples use JsonSerializer).
- The CodeGenerators package provides [HalResponse] source generator to add HAL properties to your DTOs at compile time.

This page is a compact reference for the methods shown in the usage guide. For more detailed examples, consult docs/usage.md and the project README.
