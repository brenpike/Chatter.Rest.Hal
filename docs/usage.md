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
