# Chatter.Rest.Hal — Technical Architecture Reference

This document is a technical reference for contributors and AI agents. It describes the internal structure, contracts, and behavioral invariants of the library. For the HAL specification itself, see [CLAUDE.md](../CLAUDE.md) and the [HAL draft RFC](https://datatracker.ietf.org/doc/html/draft-kelly-json-hal).

---

## Section 1 — Domain Model

All eight domain types are `sealed record` implementing the `IHalPart` marker interface. Every type is annotated with `[JsonConverter(typeof(XConverter))]` to wire up its custom serializer.

### `Resource`

```csharp
public sealed record Resource : IHalPart
{
    public const string MediaType = "application/hal+json";

    public Resource() { }
    public Resource(object? state);

    public LinkCollection Links { get; set; }
    public EmbeddedResourceCollection Embedded { get; set; }

    public T? State<T>() where T : class;
    public T? As<T>() where T : class;
}
```

- `State<T>()` — returns the resource state as `T`. If the underlying state is a `JsonElement`, it deserializes it on first call and caches the result. If state is a `JsonObject` (from the deserialization path), it calls the lazy `_stateCreator` delegate. Returns `null` on failure rather than throwing.
- `As<T>()` — serializes the entire `Resource` to a `JsonNode` (if not already cached in `_resourceNode`) then deserializes that node to `T`. Use this to round-trip a HAL response into a typed DTO that includes `_links`/`_embedded` properties.
- **Lazy init via `Func<T>` delegates** — the internal deserialization constructor (`internal Resource(JsonNode?, Func<JsonObject?>, Func<LinkCollection?>, Func<EmbeddedResourceCollection?>)`) stores the three factory delegates. `Links` and `Embedded` property getters invoke these delegates on first access and cache the result, making deserialization allocation-lazy.
- `StateObject` — internal property that drives `ResourceConverter.Write`. Its getter calls `State<object>()`.

### `Link`

```csharp
public sealed record Link : IHalPart
{
    public Link(string rel);      // throws ArgumentNullException if null/whitespace

    public string Rel { get; }
    public LinkObjectCollection LinkObjects { get; set; }
    public bool IsArray { get; set; }
}
```

- `IsArray` — when `true`, this relation serializes as a JSON array regardless of how many `LinkObject` entries it contains. Set by `LinkBuilder.SetIsArray()` via the builder API, or set to `true` by `LinkCollectionConverter.Read` when the JSON value is a JSON array.

### `LinkObject`

```csharp
public sealed record LinkObject : IHalPart
{
    public LinkObject(string href);   // throws ArgumentNullException if null/whitespace

    public string Href { get; }           // REQUIRED
    public bool? Templated { get; set; }  // OPTIONAL
    public string? Type { get; set; }     // OPTIONAL
    public string? Deprecation { get; set; } // OPTIONAL
    public string? Name { get; set; }     // OPTIONAL
    public string? Profile { get; set; }  // OPTIONAL
    public string? Title { get; set; }    // OPTIONAL
    public string? Hreflang { get; set; } // OPTIONAL
}
```

All properties except `Href` are optional per the HAL specification.

### `EmbeddedResource`

```csharp
public sealed record EmbeddedResource : IHalPart
{
    public EmbeddedResource(string name);  // throws ArgumentException if null/whitespace

    public string Name { get; }
    public ResourceCollection Resources { get; set; }
    public bool ForceWriteAsCollection { get; set; }
}
```

- `ForceWriteAsCollection` — overrides the default count-based write behavior. When `true`, the converter always writes an array even if `Resources` contains only one element.

### Collection Types

| Type | Implements | Serialized as |
|---|---|---|
| `LinkCollection` | `ICollection<Link>, IHalPart` | `_links` object |
| `LinkObjectCollection` | `ICollection<LinkObject>, IHalPart` | array or object within a link relation |
| `EmbeddedResourceCollection` | `ICollection<EmbeddedResource>, IHalPart` | `_embedded` object |
| `ResourceCollection` | `ICollection<Resource>, IHalPart` | array of resource objects |

Each collection wraps an internal `Collection<T>` and delegates all `ICollection<T>` operations to it. Each carries a `[JsonConverter]` attribute wiring to its own converter.

### Containment Diagram

```
Resource
├── LinkCollection (_links)
│   └── Link[]
│       ├── Rel: string
│       ├── IsArray: bool
│       └── LinkObjectCollection
│           └── LinkObject[]
│               ├── Href (required)
│               └── Templated, Type, Deprecation, Name, Profile, Title, Hreflang (optional)
└── EmbeddedResourceCollection (_embedded)
    └── EmbeddedResource[]
        ├── Name: string
        ├── ForceWriteAsCollection: bool
        └── ResourceCollection
            └── Resource[]  (recursive)
```

---

## Section 2 — Staged Builder Pattern

The builder uses a staged interface pattern: each method returns an interface exposing only the operations valid at that point. This enforces correct construction order at compile time.

### Entry Points

```csharp
ResourceBuilder.New() -> IResourceCreationStage
ResourceBuilder.WithState(object state) -> IResourceCreationStage
```

Both create a `ResourceBuilder` with `parent = null`, making it the root of the chain.

### Abstract Base: `HalBuilder<THalPart>`

```csharp
public abstract class HalBuilder<THalPart> : IBuildResource, IBuildHalPart<THalPart>
    where THalPart : class, IHalPart
{
    public HalBuilder(IBuildHalPart<IHalPart>? parent);
    public IBuildHalPart<IHalPart>? Parent { get; }

    public IBuildHalPart<TParent>? FindParent<TParent>() where TParent : class, IHalPart;
    public IBuildHalPart<IHalPart> FindRoot();
    protected bool IsRoot();

    public abstract THalPart BuildPart();
    public Resource? Build();
}
```

- `FindParent<TParent>()` — walks the `Parent` chain upward, returning the first builder that implements `IBuildHalPart<TParent>`. Used by `LinkObjectBuilder.AsArray()` to find the enclosing `LinkBuilder` without coupling tightly to the chain shape.
- `FindRoot()` — walks to the top of the chain (where `Parent == null`).
- `Build()` — calls `FindRoot()`, casts to `IBuildHalPart<Resource>`, and calls `BuildPart()`. Available at every stage via `IBuildResource`.

### Builder Class Map

| Builder Class | Produces | Key notes |
|---|---|---|
| `ResourceBuilder` | `Resource` | Root builder; holds `LinkCollectionBuilder` and `EmbeddedResourceCollectionBuilder` |
| `LinkCollectionBuilder` | `LinkCollection` | Creates `LinkBuilder` instances for each relation |
| `LinkBuilder` | `Link` | Holds `_isArray`; exposes `SetIsArray()` (called by `LinkObjectBuilder.AsArray()`) |
| `LinkObjectCollectionBuilder` | `LinkObjectCollection` | Creates `LinkObjectBuilder` instances |
| `LinkObjectBuilder` | `LinkObject` | `AsArray()` navigates to enclosing `LinkBuilder` via `FindParent<Link>()` |
| `EmbeddedResourceCollectionBuilder` | `EmbeddedResourceCollection` | Creates `EmbeddedResourceBuilder` instances |
| `EmbeddedResourceBuilder` | `EmbeddedResource` | Delegates to `ResourceCollectionBuilder` |
| `ResourceCollectionBuilder` | `ResourceCollection` | Creates nested `ResourceBuilder` instances |

### Stage Interface Hierarchy

Stages live in two parallel namespaces: `Builders.Stages.Resource` and `Builders.Stages.Embedded`. This dual-namespace pattern means the same logical operation (e.g., "add a link object") returns a different interface type depending on whether you are building a top-level resource or an embedded resource — preserving the correct stage context for IntelliSense and compile-time safety.

**Resource namespace (`Builders.Stages.Resource`)**

| Interface | Key members |
|---|---|
| `IResourceCreationStage` | Extends `IAddLinkToResourceStage`, `IAddSelfLinkToResourceStage`, `IAddCuriesLinkToResourceStage`, `IAddEmbeddedResourceToResourceStage`, `IBuildResource` |
| `IResourceLinkCreationStage` | `AddLinkObject(string href)`, `AsArray()` |
| `IResourceLinkObjectPropertiesSelectionStage` | `AsArray()`, `Templated()`, `WithType()`, `WithDeprecationUrl()`, `WithName()`, `WithProfileUri()`, `WithTitle()`, `WithHreflang()` |
| `IResourceCuriesLinkCreationStage` | `AddLinkObject(string href, string name)`, `AsArray()` |
| `IAddLinkToResourceStage` | `AddLink(string rel) -> IResourceLinkCreationStage` |
| `IAddSelfLinkToResourceStage` | `AddSelf() -> IResourceLinkCreationStage` |
| `IAddCuriesLinkToResourceStage` | `AddCuries() -> IResourceCuriesLinkCreationStage` |

**Embedded namespace (`Builders.Stages.Embedded`)**

Mirrors the Resource namespace exactly, but each operation returns the `IEmbedded*` variant of the stage interface.

**Shared stages**

| Interface | Key members |
|---|---|
| `IAddResourceStage` | `AddResource()`, `AddResource(object? state)`, `AddResources<T>(IEnumerable<T>, Action<T, IEmbeddedResourceCreationStage>?)` |
| `IBuildResource` | `Build() -> Resource?` — available at every leaf stage |

### `AsArray()` Propagation

`AsArray()` is available in two contexts:

1. **After `AddLink`/`AddSelf`/`AddCuries`** — on the link creation stage (e.g., `IResourceLinkCreationStage`). Calls `LinkBuilder.AsArray()` directly, setting `_isArray = true` on that builder.
2. **After `AddLinkObject`** — on the link object properties stage (e.g., `IResourceLinkObjectPropertiesSelectionStage`). `LinkObjectBuilder.AsArray()` calls `FindParent<Link>()` which returns the enclosing `LinkBuilder`, then calls `LinkBuilder.SetIsArray()`.

Both paths ultimately set `Link.IsArray = true` when `BuildPart()` is called on the `LinkBuilder`.

### Call-Chain Flow Example

```
ResourceBuilder.WithState(myDto)         // returns IResourceCreationStage
  .AddSelf()                             // returns IResourceLinkCreationStage
  .AddLinkObject("/api/orders/123")      // returns IResourceLinkObjectPropertiesSelectionStage
  .Build()                               // walks to root ResourceBuilder, calls BuildPart()
                                         // -> Resource { State=myDto, Links=[self->/api/orders/123] }
```

Each step returns a narrower interface. `Build()` is always available because every stage extends `IBuildResource`.

---

## Section 3 — Converter Architecture

**Namespace:** `Chatter.Rest.Hal.Converters`

### Wire-Up

Every domain type carries `[JsonConverter(typeof(XConverter))]`. These attribute-wired converters are used automatically when `JsonSerializer` operates without custom `JsonSerializerOptions`.

**Alternative explicit registration** via `AddHalConverters`:

```csharp
// JsonSerializerOptionsExtensions
public static JsonSerializerOptions AddHalConverters(
    this JsonSerializerOptions options,
    HalJsonOptions? halOptions = null)
```

This registers all 8 converters on the `JsonSerializerOptions` instance. A duplicate-guard checks for an existing `LinkCollectionConverter` before registering; calling `AddHalConverters` multiple times on the same instance is safe. Options-registered converters take precedence over attribute-wired converters when those options are supplied to the serializer.

### `HalJsonOptions`

```csharp
public sealed class HalJsonOptions
{
    public static readonly HalJsonOptions Default = new();
    public bool AlwaysUseArrayForLinks { get; set; } = false;
}
```

`HalJsonOptions.Default` is a process-global singleton. Mutate only at application startup before any serialization occurs.

### `HalJsonOptions`-Aware Converters

Three converters accept an optional `HalJsonOptions` constructor parameter and fall back to `HalJsonOptions.Default` when none is provided:

| Converter | Options-aware |
|---|---|
| `LinkCollectionConverter` | Yes — `(HalJsonOptions? halJsonOptions)` ctor |
| `LinkConverter` | Yes — `(HalJsonOptions? halJsonOptions)` ctor |
| `LinkObjectCollectionConverter` | Yes — `(HalJsonOptions? halJsonOptions)` ctor |

### Non-Options Converters

| Converter | Notes |
|---|---|
| `LinkObjectConverter` | Standard property-by-property read/write |
| `ResourceConverter` | Separates state from `_links`/`_embedded` |
| `EmbeddedResourceCollectionConverter` | Dispatches on object vs. array per entry |
| `EmbeddedResourceConverter` | Wraps read/write of a single embedded entry |
| `ResourceCollectionConverter` | Reads/writes arrays of `Resource` |

### Write Behavior

**`LinkCollectionConverter.Write`**

Iterates links. For each link, evaluates:

```csharp
bool forceArray = (_halJsonOptions ?? HalJsonOptions.Default).AlwaysUseArrayForLinks || link.IsArray;
```

- If `forceArray` is `false` and `link.LinkObjects.Count == 1`, writes the single `LinkObject` as a JSON object.
- Otherwise writes a JSON array of all `LinkObject` entries.

**`LinkObjectCollectionConverter.Write`**

Evaluates only `HalJsonOptions.AlwaysUseArrayForLinks` (no access to `Link.IsArray` at this level). This converter is invoked when `LinkObjectCollection` is serialized directly rather than through `LinkCollectionConverter`.

**`EmbeddedResourceCollectionConverter.Write`**

For each `EmbeddedResource`, evaluates:

```csharp
if (embeddedvalue.Resources.Count == 1 && !embeddedvalue.ForceWriteAsCollection)
```

- `true` → writes the single `Resource` as a JSON object.
- `false` → writes all resources as a JSON array.

**`ResourceConverter.Write`**

1. Serializes `StateObject` to a `JsonNode`.
2. Iterates the node's properties, skipping any named `Links` or `Embedded`.
3. Writes each remaining state property directly to the JSON writer.
4. If `Links` is non-null and non-empty, writes `"_links"` followed by the serialized `LinkCollection`.
5. If `Embedded` is non-null and non-empty, writes `"_embedded"` followed by the serialized `EmbeddedResourceCollection`.

State properties are always emitted before `_links` and `_embedded`.

### Read Behavior

**`ResourceConverter.Read`**

Parses the entire JSON token into a `JsonNode`. Constructs three lazy `Func<T>` delegates:

- `stateCreator` — clones the node, removes `_links` and `_embedded`, returns the remaining `JsonObject`.
- `linksCreator` — deserializes `node["_links"]` as `LinkCollection`.
- `embeddedCreator` — deserializes `node["_embedded"]` as `EmbeddedResourceCollection`.

These delegates are passed to the internal `Resource` constructor and invoked only when the corresponding property is first accessed.

**`LinkCollectionConverter.Read`**

Handles three JSON shapes per link relation:

- **Object** (`"rel": { "href": "..." }`) — deserializes as a single `LinkObject`.
- **Array** (`"rel": [{ "href": "..." }]`) — deserializes as `LinkObjectCollection` and sets `link.IsArray = true`.
- **String shorthand** (`"rel": "/path"`) — constructs a `LinkObject` directly from the string value.
- **Null** (`"rel": null`) — adds the `Link` with an empty `LinkObjectCollection`.

**`LinkConverter.Read`**

Expects a single-property JSON object where the key is the relation name. Returns `null` (tolerant) if:
- Input is not a `JsonObject`.
- The object does not have exactly one property.
- The relation key is null or whitespace.
- The value is an object without a valid `href`.

**`EmbeddedResourceCollectionConverter.Read`**

For each key in the `_embedded` object, dispatches:
- **JsonObject** → deserializes as a single `Resource`, adds to `embedded.Resources`.
- **JsonArray** → deserializes as `ResourceCollection`, assigns to `embedded.Resources`.
- Other/null → creates `EmbeddedResource` with empty `Resources`.

---

## Section 4 — Source Generator Pipeline

The generator lives in `Chatter.Rest.Hal.CodeGenerators` and consists of three files.

### `HalResponseGenerator` — Entry Point

```csharp
[Generator]
public class HalResponseGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context);
}
```

Registers a `SyntaxProvider.CreateSyntaxProvider` pipeline:

1. **Syntax filter**: `Parser.IsSyntaxTargetForGeneration` — fast check, runs on every syntax node.
2. **Semantic filter**: `Parser.GetSemanticTargetForGeneration` — resolves the attribute symbol.
3. Results are collected via `.Collect()` and forwarded to `Emitter.Emit`.

### `Parser` — Filter Stage

**Syntax filter** (`IsSyntaxTargetForGeneration`): matches any `AttributeSyntax` whose name resolves to `"HalResponse"` or `"HalResponseAttribute"` (unqualified, to handle both forms).

**Semantic filter** (`GetSemanticTargetForGeneration`): resolves the attribute's constructor symbol, checks `ContainingType.ToDisplayString() == "Chatter.Rest.Hal.HalResponseAttribute"`. On match, returns the nearest enclosing `ClassDeclarationSyntax` by walking `attributeSyntax.Ancestors()`.

### `Emitter` — Code Generation Stage

Deduplicates by `(Namespace, Name)` pair (handles partial classes and multiple attributes on the same class). For each unique class, generates a `partial class` with two properties:

```csharp
// Generated output shape
namespace MyApp
{
    partial class MyResponse
    {
        [JsonPropertyName("_links")]
        public LinkCollection? Links { get; set; }

        [JsonPropertyName("_embedded")]
        public EmbeddedResourceCollection? Embedded { get; set; }
    }
}
```

Output file name: `{Namespace}.{ClassName}.g.cs`.

### Known Limitations

- **Non-generic classes only** — `ClassDeclarationSyntax` is returned without generic parameter handling; generic classes are not supported.
- **Non-nested classes only** — `GetNamespaceFrom` walks ancestors looking for `NamespaceDeclarationSyntax` or `FileScopedNamespaceDeclarationSyntax`. Classes nested inside other classes produce incorrect namespace resolution.
- **Attribute location** — `[HalResponse]` is defined in `Chatter.Rest.Hal.Core`, not in the generator assembly. Consumer projects reference `Chatter.Rest.Hal.Core` at runtime and the generator assembly with `PrivateAssets="all"`.

---

## Section 5 — Package and Project Relationships

```
Chatter.Rest.Hal.sln
├── src/
│   ├── Chatter.Rest.Hal/            # Core library
│   │   ├── depends on: System.Text.Json (inbox on net8.0, NuGet on netstandard2.0)
│   │   └── NuGet: Chatter.Rest.Hal v0.9.2
│   │
│   ├── Chatter.Rest.Hal.Core/       # Shared attribute
│   │   ├── contains: HalResponseAttribute only
│   │   ├── referenced at runtime by consumer projects
│   │   └── NuGet: referenced transitively by consumers
│   │
│   └── Chatter.Rest.Hal.CodeGenerators/   # Roslyn source generator
│       ├── references: Chatter.Rest.Hal.Core
│       ├── consumers add: <PackageReference ... PrivateAssets="all" />
│       └── NuGet: Chatter.Rest.Hal.CodeGenerators v0.2.5
│
└── test/
    ├── Chatter.Rest.Hal.Tests/                   # Tests for core library
    └── Chatter.Rest.Hal.CodeGenerators.Tests/    # Tests for source generator
```

### Package Responsibilities

**`Chatter.Rest.Hal`** — the core library. Provides all domain types, fluent builder API, JSON converters, and query extension methods. Has no dependencies outside `System.Text.Json`. Consumers reference this package to build and consume HAL documents.

**`Chatter.Rest.Hal.Core`** — ships only `HalResponseAttribute`. Exists as a separate package so consumer projects can reference the attribute at runtime without taking a dependency on the Roslyn analyzer assembly.

**`Chatter.Rest.Hal.CodeGenerators`** — the Roslyn incremental source generator. References `Chatter.Rest.Hal.Core` to access `HalResponseAttribute` during compilation. Consumers add it as a build-time-only reference (`PrivateAssets="all"`), meaning it does not appear in the consumer's published output or transitive dependency graph.
