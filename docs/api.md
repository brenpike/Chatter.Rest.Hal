# API Reference — Chatter.Rest.Hal

- **Package:** `Chatter.Rest.Hal`
- **Media type:** `application/hal+json`
- **Namespace (unless noted):** `Chatter.Rest.Hal`

---

## 1. Entry Points

```csharp
// Chatter.Rest.Hal.Builders namespace
public class ResourceBuilder
{
    // Start building a resource with no state.
    public static IResourceCreationStage New();

    // Start building a resource with an existing state object.
    public static IResourceCreationStage WithState(object state);
}
```

`ResourceBuilder` is never used as a return type in the fluent chain. All builder methods return stage interfaces.

---

## 2. `IResourceCreationStage`

Namespace: `Chatter.Rest.Hal.Builders.Stages.Resource`

The root stage returned by both entry points. Exposes all top-level resource operations.

```csharp
public interface IResourceCreationStage :
    IAddSelfLinkToResourceStage,
    IAddLinkToResourceStage,
    IAddCuriesLinkToResourceStage,
    IAddEmbeddedResourceToResourceStage,
    IBuildResource
{
}

// Inherited members (flattened):
IResourceLinkCreationStage  AddSelf();
IResourceLinkCreationStage  AddLink(string rel);
IResourceCuriesLinkCreationStage AddCuries();
IAddResourceStage           AddEmbedded(string name);
Resource?                   Build();
```

---

## 3. `IResourceLinkCreationStage`

Namespace: `Chatter.Rest.Hal.Builders.Stages.Resource`

Returned by `AddSelf()` and `AddLink()` on the resource stage. Allows adding link objects to the current link relation, or forcing array serialization.

```csharp
public interface IResourceLinkCreationStage
{
    IResourceLinkObjectPropertiesSelectionStage AddLinkObject(string href);

    // Force this link relation to serialize as a JSON array even when it
    // contains only one LinkObject. Call before AddLinkObject().
    IResourceLinkCreationStage AsArray();
}
```

---

## 4. `IResourceLinkObjectPropertiesSelectionStage`

Namespace: `Chatter.Rest.Hal.Builders.Stages.Resource`

Returned by `AddLinkObject()`. Provides all optional HAL link object properties. Also inherits re-entry points to start a new link, embedded block, or terminate the build.

```csharp
public interface IResourceLinkObjectPropertiesSelectionStage :
    IResourceLinkCreationStage,
    IResourceCuriesLinkCreationStage,
    IAddSelfLinkToResourceStage,
    IAddLinkToResourceStage,
    IAddCuriesLinkToResourceStage,
    IAddEmbeddedResourceToResourceStage,
    IBuildResource
{
    // HAL spec §5.2 — set "templated" to true.
    IResourceLinkObjectPropertiesSelectionStage Templated();

    // HAL spec §5.3 — media type hint for the target resource.
    IResourceLinkObjectPropertiesSelectionStage WithType(string type);

    // HAL spec §5.4 — URL providing deprecation info for this link.
    IResourceLinkObjectPropertiesSelectionStage WithDeprecationUrl(string deprecation);

    // HAL spec §5.5 — secondary key when multiple links share the same relation.
    IResourceLinkObjectPropertiesSelectionStage WithName(string name);

    // HAL spec §5.6 — URI hinting at the target resource's profile.
    IResourceLinkObjectPropertiesSelectionStage WithProfileUri(string profile);

    // HAL spec §5.7 — human-readable label for the link.
    IResourceLinkObjectPropertiesSelectionStage WithTitle(string title);

    // HAL spec §5.8 — language of the target resource.
    IResourceLinkObjectPropertiesSelectionStage WithHreflang(string hreflang);

    // Force the current link object's parent link relation to serialize as a
    // JSON array. Shadows IResourceLinkCreationStage.AsArray().
    new IResourceLinkObjectPropertiesSelectionStage AsArray();

    // Re-entry — inherited:
    IResourceLinkCreationStage      AddSelf();
    IResourceLinkCreationStage      AddLink(string rel);
    IResourceCuriesLinkCreationStage AddCuries();
    IAddResourceStage               AddEmbedded(string name);
    Resource?                       Build();
}
```

---

## 5. `IResourceCuriesLinkCreationStage`

Namespace: `Chatter.Rest.Hal.Builders.Stages.Resource`

Returned by `AddCuries()` on the resource stage. CURIEs require both an `href` URI Template and a `name`.

```csharp
public interface IResourceCuriesLinkCreationStage
{
    // href must be a URI Template containing the {rel} token.
    IResourceLinkObjectPropertiesSelectionStage AddLinkObject(string href, string name);

    // Force the "curies" relation to serialize as a JSON array.
    IResourceCuriesLinkCreationStage AsArray();
}
```

---

## 6. `IAddResourceStage`

Namespace: `Chatter.Rest.Hal.Builders.Stages`

Returned by `AddEmbedded(string name)`. Adds individual resources or a batch of resources to the named embedded slot.

```csharp
public interface IAddResourceStage
{
    // Add an empty embedded resource.
    IEmbeddedResourceCreationStage AddResource();

    // Add an embedded resource with a state object.
    IEmbeddedResourceCreationStage AddResource(object? state);

    // Add multiple resources from a collection, optionally building links/embedded
    // for each via the builder callback.
    IEmbeddedResourceCreationStage AddResources<T>(
        IEnumerable<T> resources,
        Action<T, IEmbeddedResourceCreationStage>? builder = null);
}
```

---

## 7. `IEmbeddedResourceCreationStage`

Namespace: `Chatter.Rest.Hal.Builders.Stages.Embedded`

The embedded-context mirror of `IResourceCreationStage`. Returned by `AddResource()` / `AddResources()`. Supports the same link, curies, and embedded operations as the root resource stage, plus `Build()` which terminates the entire chain back to the root `Resource`.

```csharp
public interface IEmbeddedResourceCreationStage :
    IBuildHalPart<Resource>,
    IAddSelfLinkToEmbeddedStage,
    IAddLinkToEmbeddedStage,
    IAddCuriesLinkToEmbeddedStage,
    IAddResourceStage,
    IAddEmbeddedResourceToResourceStage,
    IBuildResource
{
}

// Inherited members (flattened):
IEmbeddedLinkCreationStage      AddSelf();
IEmbeddedLinkCreationStage      AddLink(string rel);
IEmbeddedCuriesLinkCreationStage AddCuries();
IAddResourceStage               AddEmbedded(string name);
IEmbeddedResourceCreationStage  AddResource();
IEmbeddedResourceCreationStage  AddResource(object? state);
IEmbeddedResourceCreationStage  AddResources<T>(
                                    IEnumerable<T> resources,
                                    Action<T, IEmbeddedResourceCreationStage>? builder = null);
Resource?                       Build();
```

---

## 8. `IEmbeddedLinkCreationStage` and `IEmbeddedLinkObjectPropertiesSelectionStage`

Namespace: `Chatter.Rest.Hal.Builders.Stages.Embedded`

Embedded-context mirrors of Sections 3 and 4. All property methods are identical in shape; return types are the embedded-namespace equivalents.

### `IEmbeddedLinkCreationStage`

```csharp
public interface IEmbeddedLinkCreationStage
{
    IEmbeddedLinkObjectPropertiesSelectionStage AddLinkObject(string href);

    // Force this link relation to serialize as a JSON array.
    IEmbeddedLinkCreationStage AsArray();
}
```

### `IEmbeddedCuriesLinkCreationStage`

```csharp
public interface IEmbeddedCuriesLinkCreationStage
{
    IEmbeddedLinkObjectPropertiesSelectionStage AddLinkObject(string href, string name);

    // Force the "curies" relation to serialize as a JSON array.
    IEmbeddedCuriesLinkCreationStage AsArray();
}
```

### `IEmbeddedLinkObjectPropertiesSelectionStage`

```csharp
public interface IEmbeddedLinkObjectPropertiesSelectionStage :
    IEmbeddedLinkCreationStage,
    IEmbeddedCuriesLinkCreationStage,
    IAddSelfLinkToEmbeddedStage,
    IAddLinkToEmbeddedStage,
    IAddCuriesLinkToEmbeddedStage,
    IAddResourceStage,
    IAddEmbeddedResourceToResourceStage,
    IBuildResource
{
    IEmbeddedLinkObjectPropertiesSelectionStage Templated();
    IEmbeddedLinkObjectPropertiesSelectionStage WithType(string type);
    IEmbeddedLinkObjectPropertiesSelectionStage WithDeprecationUrl(string deprecation);
    IEmbeddedLinkObjectPropertiesSelectionStage WithName(string name);
    IEmbeddedLinkObjectPropertiesSelectionStage WithProfileUri(string profile);
    IEmbeddedLinkObjectPropertiesSelectionStage WithTitle(string title);
    IEmbeddedLinkObjectPropertiesSelectionStage WithHreflang(string hreflang);

    // Shadows IEmbeddedLinkCreationStage.AsArray().
    new IEmbeddedLinkObjectPropertiesSelectionStage AsArray();

    // Re-entry — inherited:
    IEmbeddedLinkCreationStage      AddSelf();
    IEmbeddedLinkCreationStage      AddLink(string rel);
    IEmbeddedCuriesLinkCreationStage AddCuries();
    IAddResourceStage               AddEmbedded(string name);
    IEmbeddedResourceCreationStage  AddResource();
    IEmbeddedResourceCreationStage  AddResource(object? state);
    IEmbeddedResourceCreationStage  AddResources<T>(
                                        IEnumerable<T> resources,
                                        Action<T, IEmbeddedResourceCreationStage>? builder = null);
    Resource?                       Build();
}
```

---

## 9. `LinkObject` Methods

Namespace: `Chatter.Rest.Hal`

`LinkObject` is a sealed record representing a single HAL link. In addition to its
[properties](#4-iresourcelinkobjectpropertiesselectionstage) (`Href`, `Templated`,
`Type`, etc.), it exposes two methods for working with RFC 6570 URI Templates.

### `GetTemplateVariables()`

```csharp
public IReadOnlyList<string> GetTemplateVariables();
```

Parses RFC 6570 **Level 1** `{variable}` tokens from `Href` and returns an ordered,
distinct list of variable names.

- Returns an empty list when `Templated` is not `true`.
- Returns an empty list when `Href` contains no Level 1 variables.
- Operator-prefixed expressions (`{+var}`, `{#var}`, `{?query}`, etc.) are **not**
  matched — only simple `{name}` tokens.

### `Expand(IDictionary<string, string> variables)`

```csharp
public string Expand(IDictionary<string, string> variables);
```

Performs RFC 6570 Level 1 simple string expansion on the `Href` URI template.

- Substitutes each `{variable}` whose key exists in the dictionary.
- **Tolerant reader:** unresolved variables (present in the template but absent from
  the dictionary) are left as-is, allowing partial expansion.
- Returns `Href` unchanged when `Templated` is not `true`.
- Throws `ArgumentNullException` when `variables` is `null`.

### Usage example

```csharp
var link = new LinkObject("/orders/{id}") { Templated = true };

// Get variable names
IReadOnlyList<string> vars = link.GetTemplateVariables(); // ["id"]

// Expand to a resolved URI
string uri = link.Expand(new Dictionary<string, string> { ["id"] = "42" });
// uri == "/orders/42"

// Partial expansion — unresolved variables are preserved
var partial = new LinkObject("/search/{term}/page/{page}") { Templated = true };
string result = partial.Expand(new Dictionary<string, string> { ["term"] = "hal" });
// result == "/search/hal/page/{page}"

// Non-templated links return Href unchanged
var plain = new LinkObject("/about");
string href = plain.Expand(new Dictionary<string, string> { ["id"] = "99" });
// href == "/about"
```

---

## 10. `Resource` Runtime API

Namespace: `Chatter.Rest.Hal`

```csharp
[JsonConverter(typeof(ResourceConverter))]
public sealed record Resource : IHalPart
{
    // The normative HAL media type (HAL spec §4).
    public const string MediaType = "application/hal+json";

    // Lazily initialized; never null after first access.
    public LinkCollection Links { get; set; }

    // Lazily initialized; never null after first access.
    public EmbeddedResourceCollection Embedded { get; set; }

    // Deserialize the resource's state properties into T.
    // Result is cached after the first successful call.
    // Returns null if the state is absent or deserialization fails.
    public T? State<T>() where T : class;

    // Re-serialize the full Resource to a JsonNode, then deserialize
    // the node as T. Useful for projecting a Resource into a DTO that
    // includes _links/_embedded alongside state properties.
    // Returns null if conversion fails.
    public T? As<T>() where T : class;
}
```

---

## 11. `ResourceExtensions`

Namespace: `Chatter.Rest.Hal`

Extension methods on `Resource` for navigating links and embedded resources.

```csharp
public static class ResourceExtensions
{
    // Find a link by relation. Returns null if not found.
    public static Link? GetLinkOrDefault(this Resource resource, string relation);

    // Get all link objects for a relation. Returns null if the relation is not found.
    public static LinkObjectCollection? GetLinkObjects(this Resource resource, string relation);

    // Get the first link object for a relation. Returns null if not found.
    public static LinkObject? GetLinkObjectOrDefault(this Resource resource, string linkRelation);

    // Get a named link object within a relation. Returns null if not found.
    public static LinkObject? GetLinkObjectOrDefault(this Resource resource, string linkRelation, string linkObjectName);

    // Get embedded resources by name, cast to T. Returns null if the name is not found.
    public static IEnumerable<T?>? GetEmbeddedResources<T>(this Resource resource, string name)
        where T : class;

    // Get the raw ResourceCollection for an embedded name. Returns null if not found.
    public static ResourceCollection? GetResourceCollection(this Resource resource, string name);
}
```

---

## 12. `LinkCollectionExtensions`

Namespace: `Chatter.Rest.Hal`

Extension methods on `LinkCollection`.

```csharp
public static class LinkCollectionExtensions
{
    // Find a link by relation. Returns null if not found.
    public static Link? GetLinkOrDefault(this LinkCollection links, string relation);

    // Get all link objects for a relation. Returns null if the relation is not found.
    public static LinkObjectCollection? GetLinkObjects(this LinkCollection links, string relation);

    // Get the first link object for a relation. Returns null if not found.
    public static LinkObject? GetLinkObjectOrDefault(this LinkCollection links, string linkRelation);

    // Get a named link object within a relation. Returns null if not found.
    public static LinkObject? GetLinkObjectOrDefault(this LinkCollection links, string linkRelation, string linkObjectName);

    // Expand a CURIE relation (e.g. "acme:widgets") to its full URI using the
    // "curies" link relation defined in this collection. Returns the original
    // relation unchanged if no matching CURIE definition is found, the relation
    // contains no colon, or the CURIE template lacks the {rel} token.
    // Returns an empty string when relation is null or empty.
    public static string ExpandCurieRelation(this LinkCollection links, string relation);
}
```

---

## 13. `LinkObjectCollectionExtensions` and `ResourceCollectionExtensions`

Namespace: `Chatter.Rest.Hal`

```csharp
public static class LinkObjectCollectionExtensions
{
    // Get a link object by its name property. Returns null if not found.
    public static LinkObject? GetLinkObjectOrDefault(this LinkObjectCollection linkObjects, string name);
}

public static class ResourceCollectionExtensions
{
    // Project each Resource in the collection to TResource via Resource.As<TResource>().
    public static IEnumerable<TResource?> As<TResource>(this ResourceCollection rc)
        where TResource : class;
}
```

---

## 14. `EmbeddedResourceCollectionExtensions`

Namespace: `Chatter.Rest.Hal`

Extension methods on `EmbeddedResourceCollection`.

```csharp
public static class EmbeddedResourceCollectionExtensions
{
    // Get the EmbeddedResource entry for a given name. Returns null if not found.
    public static EmbeddedResource? GetEmbeddedResource(this EmbeddedResourceCollection erc, string name);

    // Get the ResourceCollection for a given embedded name. Returns null if not found.
    public static ResourceCollection? GetResourceCollection(this EmbeddedResourceCollection erc, string name);

    // Get embedded resources by name, cast to T. Returns null if not found.
    public static IEnumerable<T?>? GetResources<T>(this EmbeddedResourceCollection erc, string name)
        where T : class;
}
```

---

## 15. Serialization API

### `HalJsonOptions`

Namespace: `Chatter.Rest.Hal`

```csharp
public sealed class HalJsonOptions
{
    // Process-global singleton consumed by attribute-wired converters.
    // Mutate only at application startup, before any serialization occurs.
    public static readonly HalJsonOptions Default;

    // When true, all link relations serialize as JSON arrays regardless of
    // how many LinkObjects they contain. Aligns with HAL spec guidance:
    // "If you're unsure whether the link should be singular, assume multiple."
    // Default: false (preserves existing library behavior).
    public bool AlwaysUseArrayForLinks { get; set; }
}
```

### `JsonSerializerOptionsExtensions`

Namespace: `Chatter.Rest.Hal.Extensions`

```csharp
public static class JsonSerializerOptionsExtensions
{
    // Register all HAL JSON converters with the given JsonSerializerOptions.
    // Options-registered converters take precedence over [JsonConverter] attribute
    // converters when the options instance is supplied to JsonSerializer.
    // Safe to call multiple times — duplicate registration is suppressed.
    public static JsonSerializerOptions AddHalConverters(
        this JsonSerializerOptions options,
        HalJsonOptions? halOptions = null);
}
```

**Usage:**

```csharp
var options = new JsonSerializerOptions();
options.AddHalConverters();                             // default options
options.AddHalConverters(new HalJsonOptions { AlwaysUseArrayForLinks = true });
```

To use `AlwaysUseArrayForLinks` globally via attribute-wired converters (without passing options to every serializer call):

```csharp
// At application startup only:
HalJsonOptions.Default.AlwaysUseArrayForLinks = true;
```

---

## 16. Source Generator: `[HalResponse]`

### Attribute

Package: `Chatter.Rest.Hal.Core`
Namespace: `Chatter.Rest.Hal`

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class HalResponseAttribute : Attribute { }
```

### Generator

Package: `Chatter.Rest.Hal.CodeGenerators` (analyzer; referenced as `<PackageReference>` with `OutputItemType="Analyzer"`)

### Constraints on the decorated class

- Must be `partial`
- Must be non-generic
- Must be non-nested

### Generated output

The generator adds the following members to the partial class at compile time:

```csharp
[JsonPropertyName("_links")]
public LinkCollection? Links { get; set; }

[JsonPropertyName("_embedded")]
public EmbeddedResourceCollection? Embedded { get; set; }
```

### Example

```csharp
// Declaration (consumer code):
[HalResponse]
public partial class OrderResponse
{
    public int Id { get; set; }
    public string Status { get; set; }
}

// Generated (compile-time, not written to disk):
public partial class OrderResponse
{
    [JsonPropertyName("_links")]
    public LinkCollection? Links { get; set; }

    [JsonPropertyName("_embedded")]
    public EmbeddedResourceCollection? Embedded { get; set; }
}
```
