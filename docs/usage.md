Usage Guide — Chatter.Rest.Hal

This guide expands on the README examples with copy-paste snippets you can run in small console apps or tests. It focuses on the core API: creating resources, adding links (and link objects), embedding resources, serializing, and deserializing.

Prerequisites
- .NET SDK (6+)
- Package: Chatter.Rest.Hal

dotnet add package Chatter.Rest.Hal

1) Simple resource (build + serialize)

```csharp
using System;
using System.Text.Json;
using Chatter.Rest.Hal;

var resource = ResourceBuilder
    .WithState(new { message = "Hello, HAL!" })
    .AddSelf().AddLinkObject("/api/greeting")
    .Build();

var json = JsonSerializer.Serialize(resource, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);
```

This will print a HAL+JSON document with the state under the root and a `_links.self` link.

2) Adding links and link objects

Link objects contain metadata (href, title, templated, name etc.). Use the fluent API to express intent:

```csharp
var resource = ResourceBuilder.WithState(new { })
    // add a simple self link
    .AddSelf().AddLinkObject("/orders")

    // add curies (namespaced rels)
    .AddCuries().AddLinkObject("http://example.com/docs/rels/{rel}", "ea")

    // add a templated search link
    .AddLink("ea:find").AddLinkObject("/orders{?id}").Templated()

    // add multiple link objects to the same relation
    .AddLink("ea:admin")
        .AddLinkObject("/admins/2").WithTitle("Fred")
        .AddLinkObject("/admins/5").WithTitle("Kate")
    .Build();
```

3) Embedding resources

Embed a single resource or a collection. When embedding collections you can use AddResources and provide a lambda to configure each embedded resource's links.

```csharp
public class Order { public string Id { get; set; } public float Total { get; set; } }
var orders = new List<Order> {
    new Order { Id = "1", Total = 10 },
    new Order { Id = "2", Total = 20 }
};

var resource = ResourceBuilder.WithState(new { currentlyProcessing = 2 })
    .AddSelf().AddLinkObject("/orders")
    .AddEmbedded("ea:order")
        .AddResources(orders, (o, builder) => {
            builder
                .AddSelf().AddLinkObject($"/orders/{o.Id}")
                .AddLink("ea:basket").AddLinkObject("/baskets/{basketId}").Templated();
        })
    .Build();
```

4) Serialization and deserialization

Serialize using System.Text.Json. To deserialize back to a generic Resource object:

```csharp
var resource = JsonSerializer.Deserialize<Resource>(json);
// access state as a strongly-typed object
var state = resource.State<MyStateType>();

// or cast the Resource to a strongly-typed DTO
var dto = resource.As<MyResponseType>();
```

Notes & tips
- Use anonymous objects (new { ... }) for quick state payloads in examples. For production prefer strongly-typed classes.
- The library integrates with optional source generators. Add the Chatter.Rest.Hal.CodeGenerators package and decorate response DTOs with [HalResponse] for generated HAL-aware members.
- Link helpers: .Templated(), .WithTitle(), .WithName() (for curies), and chaining multiple .AddLinkObject(...) calls adds multiple link objects to the same relation.
- The examples are intentionally small so they can be pasted into unit tests or simple console apps for verification.

If you need a compact API reference, see docs/api.md.

---

5) Force-array serialization

The HAL spec recommends that servers keep the shape of a link relation stable across responses. If a relation sometimes returns one link and sometimes multiple, always serializing as an array prevents clients from breaking when the count changes.

**Why it matters**

Without force-array, a single link object serializes as a plain JSON object:

```json
"self": { "href": "/orders" }
```

With force-array enabled (per-relation or globally), it always serializes as a JSON array:

```json
"self": [{ "href": "/orders" }]
```

Clients that only handle arrays will break on the first form. Stabilizing the shape at build time or at startup removes that fragility.

**Per-relation — call `AsArray()` on the link creation stage**

`AsArray()` can be called after `AddLink()` (before any `AddLinkObject()`) or after `AddLinkObject()`. Both positions produce the same result.

```csharp
// AsArray() before AddLinkObject — declares intent up front
var resource = ResourceBuilder.WithState(new { })
    .AddSelf().AddLinkObject("/orders")
    .AddLink("ea:orders").AsArray()
        .AddLinkObject("/orders/1")
    .Build();

// AsArray() after AddLinkObject — equivalent result
var resource = ResourceBuilder.WithState(new { })
    .AddSelf().AddLinkObject("/orders")
    .AddLink("ea:orders")
        .AddLinkObject("/orders/1").AsArray()
    .Build();
```

**Global — configure `HalJsonOptions` at startup**

Mutate `HalJsonOptions.Default` once at application startup, before any serialization occurs. This affects all attribute-wired converters for the lifetime of the process.

```csharp
// Affects all serialization that uses attribute-wired converters
HalJsonOptions.Default.AlwaysUseArrayForLinks = true;
```

Or scope it to a specific `JsonSerializerOptions` instance (see section 6 for `AddHalConverters`):

```csharp
var options = new JsonSerializerOptions()
    .AddHalConverters(new HalJsonOptions { AlwaysUseArrayForLinks = true });

var json = JsonSerializer.Serialize(resource, options);
```

---

6) `HalJsonOptions` and `AddHalConverters`

By default the library wires converters via `[JsonConverter]` attributes on domain types. No setup is required for basic serialization. When you need custom options — indentation, force-array, ASP.NET Core integration — use `AddHalConverters()`.

```csharp
// Default wiring — no setup needed
var json = JsonSerializer.Serialize(resource);

// Explicit options with default HalJsonOptions
var options = new JsonSerializerOptions { WriteIndented = true }
    .AddHalConverters();
var json = JsonSerializer.Serialize(resource, options);

// Explicit options with custom HalJsonOptions
var options = new JsonSerializerOptions()
    .AddHalConverters(new HalJsonOptions { AlwaysUseArrayForLinks = true });
var json = JsonSerializer.Serialize(resource, options);

// ASP.NET Core integration
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.AddHalConverters());
```

`AddHalConverters()` is idempotent — calling it more than once on the same `JsonSerializerOptions` instance is safe and has no effect after the first call.

Note: when you supply explicit `JsonSerializerOptions` to `JsonSerializer`, the options-registered converters from `AddHalConverters()` take precedence over the attribute-wired converters. Both code paths produce identical HAL output under the same `HalJsonOptions`.

---

7) Deserialization and navigation extensions

After deserializing a HAL document to a `Resource`, use the extension methods in `ResourceExtensions`, `LinkCollectionExtensions`, and `ResourceCollectionExtensions` to navigate it without manual null checks or LINQ.

```csharp
using System.Text.Json;
using Chatter.Rest.Hal;

var resource = JsonSerializer.Deserialize<Resource>(json);

// Access state as a strongly-typed DTO
var state = resource.State<MyStateDto>();

// Cast the Resource directly to a typed DTO (useful with [HalResponse] DTOs — see section 8)
var dto = resource.As<MyResponseDto>();

// Get the Link (rel + its link objects) for a relation
var selfLink = resource.GetLinkOrDefault("self");

// Get the first link object for a relation
var selfLinkObject = resource.GetLinkObjectOrDefault("self");
var selfHref = selfLinkObject?.Href;

// Get a named link object within a relation (e.g., one of several ea:admin entries)
var adminLink = resource.GetLinkObjectOrDefault("ea:admin", "Fred");

// Get the full collection of link objects for a relation
var orderLinks = resource.GetLinkObjects("ea:order");

// Navigate embedded resources — returns IEnumerable<OrderDto?>
var orders = resource.GetEmbeddedResources<OrderDto>("ea:order");

// CURIE expansion — resolves a compact relation to its full URI
// Given a curies entry: { "name": "ea", "href": "http://example.com/docs/rels/{rel}", "templated": true }
var expandedRel = resource.Links.ExpandCurieRelation("ea:find");
// returns "http://example.com/docs/rels/find"
// Returns the original relation unchanged if no matching CURIE definition is found.
```

---

8) `[HalResponse]` source generator

The `Chatter.Rest.Hal.CodeGenerators` package generates HAL properties on your DTOs at compile time so that your response class serializes directly as a HAL document without needing `Resource` as a wrapper.

**Package setup**

```bash
dotnet add package Chatter.Rest.Hal.CodeGenerators
dotnet add package Chatter.Rest.Hal.Core
```

**Decorate your DTO**

Your class must be `partial`, non-generic, and non-nested.

```csharp
using Chatter.Rest.Hal;

[HalResponse]
public partial class OrderResponse
{
    public string Id { get; set; }
    public decimal Total { get; set; }
}
```

**What the generator produces (at compile time, in `obj/`)**

```csharp
public partial class OrderResponse
{
    [JsonPropertyName("_links")]
    public LinkCollection? Links { get; set; }

    [JsonPropertyName("_embedded")]
    public EmbeddedResourceCollection? Embedded { get; set; }
}
```

`OrderResponse` now has `_links` and `_embedded` properties with the correct JSON names. When serialized with `System.Text.Json`, it produces a valid HAL document.

**Constraints**

| Constraint | Reason |
|---|---|
| Class must be `partial` | The generator needs to emit into the same type declaration |
| Class must be non-generic | Type parameter substitution is not supported by the generator |
| Class must be non-nested | Roslyn emit targets top-level type declarations only |
