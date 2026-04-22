# Chatter.Rest.Hal — Serialization Reference

This document is a deep-dive reference for how `Chatter.Rest.Hal` serializes and deserializes HAL JSON documents. It covers converter wiring, configuration, force-array behavior, deserialization internals, and media type usage.

---

## 1. Two Serialization Paths

There are two ways converters are wired to domain types.

### 1.1 Attribute-wired (default)

Every domain type carries a `[JsonConverter(typeof(XConverter))]` attribute:

```csharp
[JsonConverter(typeof(ResourceConverter))]
public sealed record Resource : IHalPart { ... }

[JsonConverter(typeof(LinkCollectionConverter))]
public sealed record LinkCollection : ICollection<Link>, IHalPart { ... }
```

This means any call to `JsonSerializer.Serialize` / `JsonSerializer.Deserialize` works with no extra configuration — the runtime resolves the converter from the attribute automatically.

### 1.2 Explicit via `AddHalConverters()`

Call `options.AddHalConverters(halOptions?)` to register all eight converters on a specific `JsonSerializerOptions` instance:

```csharp
var options = new JsonSerializerOptions();
options.AddHalConverters();
```

Converters registered on `JsonSerializerOptions.Converters` take **precedence** over attribute-wired converters when those options are supplied to the serializer. This is the mechanism that makes per-options `HalJsonOptions` work — the converters are constructed with the specific `HalJsonOptions` instance at registration time.

Use this path when:
- You need per-options-instance `HalJsonOptions` configuration (e.g., different link array behavior per endpoint).
- You are integrating with ASP.NET Core's serializer pipeline (see Section 3).

When you never call `AddHalConverters()`, attribute-wired converters remain in effect and read `HalJsonOptions.Default` at write time.

---

## 2. `HalJsonOptions`

```csharp
// Chatter.Rest.Hal namespace
public sealed class HalJsonOptions
{
    public static readonly HalJsonOptions Default;
    public bool AlwaysUseArrayForLinks { get; set; } // default: false
}
```

### `Default`

`HalJsonOptions.Default` is a process-global singleton instance. Attribute-wired converters resolve options from this instance at write time (not at construction time), so mutations to `Default` affect all subsequent serializations that use attribute-wired converters.

**Mutate only at application startup, before any serialization occurs.** `bool` reads and writes are atomic on all .NET platforms, so no locking is required as long as mutations are confined to startup.

### `AlwaysUseArrayForLinks`

When `false` (the default), a link relation with a single `LinkObject` serializes as a JSON object. When `true`, all link relations serialize as JSON arrays regardless of count. This aligns with HAL spec guidance:

> "If you're unsure whether the link should be singular, assume it will be multiple."

**Warning:** Mutating `HalJsonOptions.Default.AlwaysUseArrayForLinks` after the application has started handling concurrent requests creates a race condition. Use a per-options instance via `AddHalConverters(new HalJsonOptions { ... })` if you need different behavior for different serialization contexts.

---

## 3. `AddHalConverters()`

```csharp
// Chatter.Rest.Hal.Extensions namespace
public static JsonSerializerOptions AddHalConverters(
    this JsonSerializerOptions options,
    HalJsonOptions? halOptions = null)
```

### Registration order

Converters are added to `options.Converters` in this order:

1. `LinkCollectionConverter(resolved)`
2. `LinkObjectCollectionConverter(resolved)`
3. `LinkConverter(resolved)`
4. `LinkObjectConverter()`
5. `ResourceConverter()`
6. `EmbeddedResourceCollectionConverter()`
7. `EmbeddedResourceConverter()`
8. `ResourceCollectionConverter()`

Where `resolved` is `halOptions ?? HalJsonOptions.Default`.

### Idempotency

The method checks `options.Converters.OfType<LinkCollectionConverter>().Any()` before adding converters. Calling `AddHalConverters()` more than once on the same options instance is safe and a no-op after the first call.

### Options resolution

`halOptions ?? HalJsonOptions.Default`. Pass an explicit `HalJsonOptions` instance to isolate behavior from the global default.

### ASP.NET Core integration

```csharp
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.AddHalConverters());
```

This wires all HAL converters into the ASP.NET Core serializer pipeline. To use a custom `HalJsonOptions` for all HAL responses globally:

```csharp
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.AddHalConverters(
        new HalJsonOptions { AlwaysUseArrayForLinks = true }));
```

---

## 4. Force-Array Serialization

There are two independent mechanisms for forcing a link relation to serialize as a JSON array rather than a single object.

### 4.1 Per-relation — builder API

Call `.AsArray()` on the link creation stage immediately after `AddLink()` / `AddSelf()` / `AddCuries()`:

```csharp
var resource = ResourceBuilder.WithState(new { })
    .AddLink("ea:orders").AsArray()
        .AddLinkObject("/orders/1")
    .Build();
```

`.AsArray()` can also be called on the link object properties stage (after `AddLinkObject()`), which traverses up to the parent `LinkBuilder` via `FindParent<Link>()`:

```csharp
var resource = ResourceBuilder.WithState(new { })
    .AddLink("ea:orders")
        .AddLinkObject("/orders/1").AsArray()
    .Build();
```

Both paths converge at `LinkBuilder.SetIsArray()`, which sets `_isArray = true`. When `BuildPart()` is called, `Link.IsArray` is set to `true` on the resulting domain object.

### 4.2 Global — options

**Option A:** Mutate `Default` at application startup:

```csharp
HalJsonOptions.Default.AlwaysUseArrayForLinks = true;
```

**Option B:** Supply a per-options instance:

```csharp
var options = new JsonSerializerOptions().AddHalConverters(
    new HalJsonOptions { AlwaysUseArrayForLinks = true });
```

### 4.3 Which converters respect which flag

| Converter | `Link.IsArray` | `AlwaysUseArrayForLinks` |
|---|---|---|
| `LinkCollectionConverter` | Yes | Yes |
| `LinkConverter` | Yes | Yes |
| `LinkObjectCollectionConverter` | No | Yes |

`LinkCollectionConverter` and `LinkConverter` apply `forceArray = AlwaysUseArrayForLinks || link.IsArray`. `LinkObjectCollectionConverter` only checks `AlwaysUseArrayForLinks` (it does not have access to a parent `Link`).

### 4.4 Output examples

Single-link relation **without** force-array:

```json
{
  "_links": {
    "self": { "href": "/orders/1" }
  }
}
```

Single-link relation **with** force-array (either mechanism):

```json
{
  "_links": {
    "self": [{ "href": "/orders/1" }]
  }
}
```

### 4.5 `EmbeddedResource.ForceWriteAsCollection`

This is a separate flag that applies to embedded resource collections, not link relations. It ensures an embedded entry serializes as a JSON array even when only one resource is present.

`ForceWriteAsCollection` is set automatically by the builder when `AddResources<T>(...)` is called — the bulk-add path that takes an `IEnumerable<T>`:

```csharp
ResourceCollectionBuilder.AddResources<T>(resources, builder?)
// sets ForceWriteAsCollection = true unconditionally
```

`EmbeddedResourceBuilder.BuildPart()` propagates the flag to the constructed `EmbeddedResource`:

```csharp
return new EmbeddedResource(_name)
{
    Resources = _resourceCollectionBuilder.BuildPart(),
    ForceWriteAsCollection = _resourceCollectionBuilder.ForceWriteAsCollection
};
```

`EmbeddedResourceCollectionConverter.Write` then checks this flag:

```csharp
if (embeddedvalue.Resources.Count == 1 && !embeddedvalue.ForceWriteAsCollection)
    // write as object
else
    // write as array
```

There is no public builder API to set `ForceWriteAsCollection` independently. It is exclusively a consequence of using `AddResources<T>(...)`.

---

## 5. Deserialization Behavior

### 5.1 `ResourceConverter.Read`

Parses the entire JSON input into a `JsonNode` tree. Rather than immediately deserializing `_links` and `_embedded`, it captures three lazy `Func<T>` delegates:

- `linkCollectionCreator` — deserializes `node["_links"]` on first access of `Resource.Links`
- `embeddedCollectionCreator` — deserializes `node["_embedded"]` on first access of `Resource.Embedded`
- `jsonObjectCreator` — clones the node, removes `_links` and `_embedded`, returns the remainder as the state `JsonObject`

These delegates are allocated once and evaluated only when the corresponding property is first accessed. This means deserialization of linked and embedded sub-graphs is deferred until needed.

### 5.2 `LinkCollectionConverter.Read`

Handles three input shapes for each link relation value:

| Shape | Behavior |
|---|---|
| JSON object (`{ "href": "..." }`) | Deserializes as a single `LinkObject`; `Link.IsArray` remains `false` |
| JSON array (`[{ "href": "..." }, ...]`) | Deserializes as `LinkObjectCollection`; sets `Link.IsArray = true` |
| JSON string (`"rel": "/path"`) | Creates a `LinkObject` with that string as `href` |
| `null` | Creates a `Link` with no `LinkObjects` |

### 5.3 `LinkConverter.Read`

Expects a single-property JSON object where the property name is the link relation. Returns `null` (tolerant, does not throw) for any of these malformed inputs:

- Not a JSON object
- A JSON object with more than one property
- A blank relation key
- A link object value missing `href`
- An array entry missing `href`

When the value is a JSON array, sets `Link.IsArray = true` on the resulting `Link`.

### 5.4 `Resource.State<T>()`

Lazily deserializes the resource's state portion into a strongly typed object:

```csharp
public T? State<T>() where T : class
```

- If the internal state is a `JsonElement`, deserializes it to `T` and caches the result.
- If the internal state is null, invokes the `_stateCreator` delegate (which returns the JSON minus `_links`/`_embedded`) and deserializes that.
- Returns `null` on any exception.
- **Special guard:** When `T == typeof(Link)`, requires the JSON object to have exactly one property before deserializing. This prevents a multi-property state DTO from being misidentified as a HAL link.

### 5.5 `Resource.As<T>()`

Casts the entire `Resource` (including `_links` and `_embedded`) to a strongly typed object:

```csharp
public T? As<T>() where T : class
```

- Serializes the `Resource` to a `JsonNode` via `JsonSerializer.SerializeToNode(this)` if not already cached in `_resourceNode`.
- Deserializes that node to `T`.
- Returns `null` on any exception.
- The `_resourceNode` is cached after the first call — subsequent calls to `As<T>()` skip re-serialization.
- Because the full resource (including `_links` and `_embedded`) is included in the serialized node, use this method for DTOs decorated with `[HalResponse]` (from the source generator) that declare `Links` and `Embedded` properties.

### 5.6 `EmbeddedResourceCollectionConverter.Read`

Dispatches per embedded entry based on whether the JSON value is an object or an array:

| Value shape | Behavior |
|---|---|
| JSON object | Deserializes as a single `Resource`; adds to `EmbeddedResource.Resources` |
| JSON array | Deserializes as `ResourceCollection`; assigns entire collection to `EmbeddedResource.Resources` |
| `null` or other | Leaves `EmbeddedResource.Resources` empty |

---

## 6. Media Type

```csharp
public const string MediaType = "application/hal+json";
```

`Resource.MediaType` is the normative HAL media type as defined in Section 4 of the HAL specification. Use this constant when setting `Content-Type` or `Accept` headers rather than hard-coding the string:

```csharp
response.ContentType = Resource.MediaType;
// or
request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Resource.MediaType));
```
