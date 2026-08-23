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

- `State<T>()` — returns the resource state as `T`, materialized as a detached projection from the state of record (the constructor-supplied `JsonElement`, or the lazy `_stateCreator` delegate on the deserialization path) on every call. Projections never become the serialization source, so mutating a returned object affects neither serialization nor equality; a state supplied directly as `T` is the one exception and is returned by reference. Returns `null` on failure rather than throwing.
- `As<T>()` — converts the full `Resource` (including `_links`/`_embedded`) to `T`. A parsed resource converts from the retained source node; an in-memory resource is re-serialized on every call, so links, embedded resources and state added after an earlier `As<T>()` call are reflected in the result. Use this to round-trip a HAL response into a typed DTO that declares `Links`/`Embedded` properties.
- **Lazy init via `Func<T>` delegates** — the internal deserialization constructor (`internal Resource(JsonNode?, Func<JsonObject?>, Func<LinkCollection?>, Func<EmbeddedResourceCollection?>)`) stores the three factory delegates. `Links` and `Embedded` property getters invoke these delegates on first access and cache the result, making deserialization allocation-lazy.
- `StateObject` — internal property that drives `ResourceConverter.Write`. Its getter calls `State<object>()`.

### `Link`

```csharp
public sealed record Link : IHalPart
{
    public Link(string rel);      // throws ArgumentException if null/whitespace

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
    public LinkObject(string href);   // throws ArgumentException if null/whitespace
    private LinkObject();             // same-document reference; Href == string.Empty

    internal static LinkObject SameDocumentReference();

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

- `SameDocumentReference()` — internal seam backed by the private parameterless constructor, producing a `LinkObject` whose `Href` is `string.Empty` (an RFC 3986 same-document reference). It is reachable only from the deserialization path, and it takes no parameter precisely so it cannot produce a null or whitespace-only href.

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
- The value is an object whose `href` is absent or JSON null.

A whitespace-only `href` does **not** yield `null` here — the precheck passes and the `Link` is returned with an empty `LinkObjectCollection`. An empty-string `href` is valid (RFC 3986 same-document reference) and materializes a `LinkObject`.

**`EmbeddedResourceCollectionConverter.Read`**

For each key in the `_embedded` object, dispatches:
- **JsonObject** → deserializes as a single `Resource`, adds to `embedded.Resources`.
- **JsonArray** → deserializes as `ResourceCollection`, assigns to `embedded.Resources`.
- Other/null → creates `EmbeddedResource` with empty `Resources`.

---

## Section 4 — Source Generator Pipeline

The generator lives in `Chatter.Rest.Hal.CodeGenerators`.

### `HalResponseGenerator` — Entry Point

```csharp
[Generator]
public class HalResponseGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context);
}
```

The pipeline has three stages:

1. **Post-initialization output** — emits `HalResponseAttribute` into the compilation, so the attribute is available to any project that installs the package.
2. **Discovery** — `SyntaxProvider.ForAttributeWithMetadataName("Chatter.Rest.Hal.HalResponseAttribute", ...)` with a syntax-only predicate matching `ClassDeclarationSyntax` and `RecordDeclarationSyntax`. The transform projects each match straight onto the equatable `HalTarget` model; nothing that changes identity per edit (syntax nodes, symbols, `Location`s) survives past this stage, which is what makes the downstream `Collect` cacheable.
3. **Emission** — targets with a model are collected, deduplicated, ordered, and handed to `Emitter.Emit`; diagnostics are reported from a separate per-target output.

Pipeline steps are named via `WithTrackingName` (see `TrackingNames`) so tests can assert that an unrelated edit reuses cached results.

### `Parser` — Projection and Validation Stage

Builds `HalClassInfo` for one annotated declaration: containing namespace, the declared name with its type parameter list, the chain of containing types (each with its own declaration keyword and type parameters), and the fully qualified metadata name (for example ``Ns.Outer`1+Inner``) used as the dedup key and hint-name basis.

It also reports why a declaration cannot receive the HAL members:

| Id | Severity | Condition |
|---|---|---|
| `HAL0001` | Error | The target is not declared `partial`. |
| `HAL0002` | Error | A type containing the target is not declared `partial`. |
| `HAL0003` | Warning | The target is a record; only class declarations are supported. |
| `HAL0004` | Error | The target already declares a member named `Links` or `Embedded`. |
| `HAL0005` | Error | The target or a containing type is file-local (`file` modifier); a re-declaration from a generated tree would be an unrelated type. |

Generation is skipped for a target that reports any of these, so the diagnostic is what the user sees rather than an unrelated `CS0260` or `CS0102`.

### `Emitter` — Code Generation Stage

For each unique model, re-declares the containing-type chain and the target itself as partials and adds the two HAL members with fully qualified type names:

```csharp
// Generated output shape
namespace MyApp
{
    partial class Outer<TOuter>
    {
        partial class MyResponse<TValue>
        {
            [global::System.Text.Json.Serialization.JsonPropertyName("_links")]
            public global::Chatter.Rest.Hal.LinkCollection? Links { get; set; }

            [global::System.Text.Json.Serialization.JsonPropertyName("_embedded")]
            public global::Chatter.Rest.Hal.EmbeddedResourceCollection? Embedded { get; set; }
        }
    }
}
```

Output file name: the fully qualified metadata name with `+` mapped to `.` and `` ` `` to `_`, plus `.g.cs` — for example `MyApp.Outer_1.MyResponse_1.g.cs`. A numeric suffix is appended if two metadata names sanitize onto the same hint.

### Attribute Delivery

`HalResponseAttribute` is emitted into the consuming compilation by the generator itself, as an `internal sealed` class in the `Chatter.Rest.Hal` namespace. `Chatter.Rest.Hal.Core` still declares a public copy for source compatibility, but it is not published as a package; a project referencing both sees `CS0436` and should drop the `Chatter.Rest.Hal.Core` reference.

The package declares a dependency on `Chatter.Rest.Hal`, which supplies the `LinkCollection` and `EmbeddedResourceCollection` types the generated members are typed as. Consumers still add the generator itself with `PrivateAssets="all"`.

> **Packable libraries need a direct runtime reference.** `PrivateAssets="all"` keeps the generator reference (and everything transitive under it, including `Chatter.Rest.Hal`) out of the nuspec produced by `dotnet pack`. An application project needs nothing more, but a class library that packs and exposes the generated `Links`/`Embedded` members must also add a direct, non-private `<PackageReference Include="Chatter.Rest.Hal" ... />` so its own consumers restore the assembly those members are typed against:
>
> ```xml
> <PackageReference Include="Chatter.Rest.Hal.CodeGenerators" Version="0.4.0" PrivateAssets="all" />
> <PackageReference Include="Chatter.Rest.Hal" Version="1.1.0" />
> ```

### Known Limitations

- **Records are not supported** — a `record` (or `record class`) target reports `HAL0003` and generates nothing. `AttributeTargets.Class` permits the annotation, so the diagnostic exists to make the gap visible.
- **Type-parameter constraints are not repeated** — a supplementary partial declaration may omit them, and repeating type-parameter attributes risks duplicate-attribute errors.

---

## Section 5 — Package and Project Relationships

```
Chatter.Rest.Hal.sln
├── src/
│   ├── Chatter.Rest.Hal/            # Core library
│   │   ├── depends on: System.Text.Json (inbox on net8.0, NuGet on netstandard2.0)
│   │   ├── depends on: Chatter.Rest.UriTemplates (external NuGet package)
│   │   └── NuGet: Chatter.Rest.Hal v1.1.0
│   │
│   ├── Chatter.Rest.Hal.Core/       # Shared attribute (unpublished)
│   │   ├── contains: HalResponseAttribute only
│   │   └── not packed; the generator emits its own copy of the
│   │       attribute into consuming compilations (see Section 4)
│   │
│   └── Chatter.Rest.Hal.CodeGenerators/   # Roslyn source generator
│       ├── depends on: Chatter.Rest.Hal (types used by generated source)
│       ├── consumers add: <PackageReference ... PrivateAssets="all" />
│       └── NuGet: Chatter.Rest.Hal.CodeGenerators v0.4.0
│
└── test/
    ├── Chatter.Rest.Hal.Tests/                   # Tests for core library
    └── Chatter.Rest.Hal.CodeGenerators.Tests/    # Tests for source generator
```

### Package Responsibilities

**`Chatter.Rest.Hal`** — the core library. Provides all domain types, fluent builder API, JSON converters, and query extension methods. Depends on `System.Text.Json` and the external NuGet package [`Chatter.Rest.UriTemplates`](https://www.nuget.org/packages/Chatter.Rest.UriTemplates/) for RFC 6570 URI template expansion. Consumers reference this package to build and consume HAL documents.

**`Chatter.Rest.Hal.Core`** — declares `HalResponseAttribute`. It is not published as a NuGet package; since 0.4.0 the generator emits its own copy of the attribute, so consumers do not need this project. See Section 4, Attribute Delivery.

**`Chatter.Rest.Hal.CodeGenerators`** — the Roslyn incremental source generator. It emits `HalResponseAttribute` into the compilation it runs in and depends on `Chatter.Rest.Hal` for the types the generated members use. Consumers add the generator itself as a build-time-only reference (`PrivateAssets="all"`), meaning the analyzer assembly does not appear in the consumer's published output. Packable libraries additionally declare a direct `Chatter.Rest.Hal` reference — `PrivateAssets="all"` suppresses the transitive runtime dependency from the packed nuspec (see Section 4, Attribute Delivery).

---

## Section 6 — UriTemplates Engine

The [`Chatter.Rest.UriTemplates`](https://www.nuget.org/packages/Chatter.Rest.UriTemplates/) package implements RFC 6570 URI Template expansion for Levels 1 through 3. It is an external NuGet package with no external dependencies of its own.

The core library references `Chatter.Rest.UriTemplates` as a NuGet package dependency. `LinkObject.Expand()` and `LinkObject.GetTemplateVariables()` delegate to `UriTemplate` for template parsing and expansion, upgrading the HAL library from Level 1-only support to full Levels 1-3 coverage.

Level 4 modifiers (prefix `:N` and explode `*`) are explicitly deferred and throw `NotSupportedException` at parse time.

For the complete type design, operator reference, encoding rules, and integration details, see [docs/uri-templates/architecture.md](uri-templates/architecture.md).
