# Performance Improvement TODO

Static-analysis-identified performance improvements for the Chatter.Rest.Hal packages. Each item is self-contained: a planner or coder agent can implement it without re-reading the original analysis.

## Priority Summary

| ID | Title | Impact | Effort | Category |
|----|-------|--------|--------|----------|
| PERF-01 | Replace `nameof().ToLower()` with `JsonEncodedText` constants | High | Low | allocation |
| PERF-02 | Eliminate redundant JsonNode index lookups | High | Low | json |
| PERF-03 | Replace `ToJsonString() == "null"` with `GetValueKind()` | High | Low | json / allocation |
| PERF-04 | Avoid deep-cloning entire JSON tree in ResourceConverter.Read | High | Medium | json / allocation |
| PERF-05 | Eliminate serialize-to-DOM-then-rewrite in ResourceConverter.Write | High | Medium | json / allocation |
| PERF-06 | Replace LINQ `.First()` with indexer `[0]` | Medium | Low | allocation / collection |
| PERF-07 | Cache fallback empty collection in Resource getters | Medium | Low | allocation |
| PERF-08 | Replace try/catch type coercion with TryGetValue | Medium | Low | json |
| PERF-09 | Upgrade source generator to ForAttributeWithMetadataName | Medium | Low | generator |
| PERF-10 | Move dedup/sort logic out of RegisterSourceOutput | Medium | Low | generator |
| PERF-11 | Bypass try/catch in StateObject getter for hot path | Medium | Low | allocation |
| PERF-12 | Replace LINQ duplicate guard in AddHalConverters | Low | Low | allocation |
| PERF-13 | Add dictionary-based O(1) lookup to collections | Low | Medium | collection |

---

## PERF-01: Replace `nameof().ToLower()` with `JsonEncodedText` constants in LinkObjectConverter
- **File(s):** `src/Chatter.Rest.Hal/Converters/LinkObjectConverter.cs` (lines 114-158)
- **Impact:** High
- **Effort:** Low
- **Category:** allocation
- **Status:** - [x] complete
- **Implementation Note:** Added 8 `static readonly JsonEncodedText` fields (href, templated, type, deprecation, name, title, profile, hreflang). All `WritePropertyName` calls in Write now use the pre-encoded fields. Committed in `perf/high-impact-fixes`.

### Problem
`Write` method calls `nameof(linkObject.Href).ToLower()`, `nameof(linkObject.Templated).ToLower()`, etc. for up to 8 properties on every `LinkObject` serialization. `string.ToLower()` allocates a new string each time. Property names are compile-time constants. In an API returning a HAL document with 50 link objects, this is 400 unnecessary string allocations per response.

### Current Code
```csharp
writer.WritePropertyName(nameof(linkObject.Href).ToLower());
writer.WritePropertyName(nameof(linkObject.Templated).ToLower());
// ... repeated for all 8 properties
```

### Fix
Declare `static readonly JsonEncodedText` fields for all 8 property names. `JsonEncodedText.Encode("href")` pre-encodes UTF-8 bytes and caches them -- eliminates both the string allocation and UTF-16 to UTF-8 transcoding on every write.
```csharp
private static readonly JsonEncodedText HrefProperty = JsonEncodedText.Encode("href");
private static readonly JsonEncodedText TemplatedProperty = JsonEncodedText.Encode("templated");
// ... all 8 properties
writer.WritePropertyName(HrefProperty); // zero-allocation
```

---

## PERF-02: Eliminate redundant JsonNode index lookups in LinkObjectConverter.Read
- **File(s):** `src/Chatter.Rest.Hal/Converters/LinkObjectConverter.cs` (lines 28-49)
- **Impact:** High
- **Effort:** Low
- **Category:** json
- **Status:** - [x] complete
- **Implementation Note:** All `node[nameof(...)]` lookups replaced with single lowercase string key lookups, result cached in local variable. Applied to all 8 properties in the Read method. Committed in `perf/high-impact-fixes`.

### Problem
`Read` method looks up the same `"href"` key twice -- once for the null check, once to get the value. Case-insensitive lookup on `JsonNode` is slower than a direct lowercase key match. Redundant traversal on the hot deserialization path.

### Current Code
```csharp
if (node[nameof(LinkObject.Href)] is null)   // lookup 1 -- PascalCase, case-insensitive
    return null;
var href = node[nameof(LinkObject.Href)]?.GetValue<string>();  // lookup 2 -- same key again
```

### Fix
Single lookup with lowercase key, cache result:
```csharp
var hrefNode = node["href"];  // single lookup, exact lowercase match
if (hrefNode is null)
    return null;
var href = hrefNode.GetValue<string>();
```
Apply same pattern to all other property lookups in this method.

---

## PERF-03: Replace `ToJsonString() == "null"` with `GetValueKind()` null check
- **File(s):** `src/Chatter.Rest.Hal/Converters/LinkConverter.cs` (lines 63, 71), `src/Chatter.Rest.Hal/Converters/LinkCollectionConverter.cs` (line 77)
- **Impact:** High
- **Effort:** Low
- **Category:** json / allocation
- **Status:** - [x] complete
- **Implementation Note:** Added `IsJsonNull(JsonNode?)` private static helper to both LinkConverter.cs and LinkCollectionConverter.cs. Uses `GetValueKind() == JsonValueKind.Null` on net8.0 (via `#if NET8_0_OR_GREATER`) and falls back to `ToJsonString() == "null"` on netstandard2.0 (STJ 6.x NuGet does not expose GetValueKind). Committed in `perf/high-impact-fixes`.

### Problem
`kvp.Value.ToJsonString() == "null"` serializes the entire JSON node to a new string just to compare against the 4-character literal `"null"`. Called for every link relation and every link object during deserialization. Unnecessary string allocation on the hot path.

### Current Code
```csharp
if (kvp.Value == null || kvp.Value.ToJsonString() == "null")
```

### Fix
Use `JsonValueKind` check -- no allocation, no serialization:
```csharp
if (kvp.Value == null || (kvp.Value is JsonValue jv && jv.GetValueKind() == JsonValueKind.Null))
```
Note: `GetValueKind()` is available on `JsonValue` in System.Text.Json 6+. Both `net8.0` and `netstandard2.0` targets in this project use System.Text.Json 6+. Wrap in a private helper if used in multiple places.

---

## PERF-04: Avoid deep-cloning entire JSON tree in ResourceConverter.Read
- **File(s):** `src/Chatter.Rest.Hal/Converters/ResourceConverter.cs` (lines 30-35)
- **Impact:** High
- **Effort:** Medium
- **Category:** json / allocation
- **Status:** - [x] complete
- **Implementation Note:** `jsonObjectCreator` lambda replaced with selective property iteration. Iterates `JsonObject` source properties, skips `_links`/`_embedded`, deep-clones only state values. Uses `JsonNode.DeepClone()` on net8.0 (`#if NET8_0_OR_GREATER`) and `Deserialize<JsonNode>()` per-value on netstandard2.0. Committed in `perf/high-impact-fixes`.

### Problem
`jsonObjectCreator` lambda deserializes the full `JsonNode` (deep clone of entire document including `_links` and `_embedded` subtrees) then removes two keys. For a resource with large embedded collections, this clones everything just to strip two entries.

### Current Code
```csharp
JsonObject? jsonObjectCreator()
{
    var cloneObject = node?.Deserialize<JsonNode>()?.AsObject();
    cloneObject?.Remove("_links");
    cloneObject?.Remove("_embedded");
    return cloneObject;
}
```

### Fix
Iterate original node properties, skip `_links`/`_embedded`, deep-clone only state properties:
```csharp
JsonObject? jsonObjectCreator()
{
    if (node is not JsonObject original) return null;
    var stateObject = new JsonObject();
    foreach (var kvp in original)
    {
        if (kvp.Key == "_links" || kvp.Key == "_embedded") continue;
        stateObject.Add(kvp.Key, kvp.Value?.DeepClone()); // net8.0: JsonNode.DeepClone()
    }
    return stateObject;
}
```
Note: `JsonNode.DeepClone()` is available on .NET 8. For `netstandard2.0` TFM, use `kvp.Value?.Deserialize<JsonNode>()` per-value as fallback. Check `#if NET8_0_OR_GREATER` preprocessor if needed.

---

## PERF-05: Eliminate serialize-to-DOM-then-rewrite pattern in ResourceConverter.Write
- **File(s):** `src/Chatter.Rest.Hal/Converters/ResourceConverter.cs` (lines 52-76)
- **Impact:** High
- **Effort:** Medium
- **Category:** json / allocation
- **Status:** - [x] complete
- **Implementation Note:** `SerializeToNode` DOM replaced with `JsonDocument.Parse(SerializeToUtf8Bytes(...))`. `JsonDocument` uses pooled memory and is read-only. `prop.WriteTo(writer)` writes name+value together. Also fixed pre-existing bug: `Links`/`Embedded` property filter now uses `options.PropertyNamingPolicy?.ConvertName(...)` to correctly handle CamelCase and other naming policies. Committed in `perf/high-impact-fixes`.

### Problem
`Write` serializes `StateObject` to an intermediate `JsonNode` DOM, then immediately iterates the DOM to re-write each property to the `Utf8JsonWriter`. This is a serialize-then-deserialize-then-reserialize anti-pattern. Allocates a full in-memory DOM for the state object on every resource write.

### Current Code
```csharp
var node = JsonSerializer.SerializeToNode(value.StateObject, options);
if (node != null)
{
    foreach (var item in node.AsObject())
    {
        // writes each property to writer
    }
}
```

### Fix
Serialize directly to bytes, then stream-copy with `Utf8JsonReader` -- no DOM allocation:
```csharp
var bytes = JsonSerializer.SerializeToUtf8Bytes(value.StateObject, options);
var reader = new Utf8JsonReader(bytes);
reader.Read(); // consume StartObject
while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
{
    var propName = reader.GetString();
    if (propName == "Links" || propName == "Embedded") { reader.Skip(); continue; }
    writer.WritePropertyName(propName!);
    reader.Read();
    writer.WriteRawValue(reader.GetRawText()); // or use JsonElement for structured copy
}
```
Note: `writer.WriteRawValue` requires `System.Text.Json` 6+. The filter for `"Links"` / `"Embedded"` should match whatever the serializer emits for those property names given the active `JsonNamingPolicy`.

---

## PERF-06: Replace LINQ `.First()` with indexer `[0]` in all collection converters
- **File(s):** `src/Chatter.Rest.Hal/Converters/LinkCollectionConverter.cs` (line 119), `LinkObjectCollectionConverter.cs` (line 102), `ResourceCollectionConverter.cs` (line 67), `EmbeddedResourceCollectionConverter.cs` (line 92), `LinkConverter.cs` (line 138)
- **Impact:** Medium
- **Effort:** Low
- **Category:** allocation / collection
- **Status:** - [x] complete
- **Implementation Note:** Added public read-only `this[int index]` indexer to LinkObjectCollection, LinkCollection, ResourceCollection, EmbeddedResourceCollection (delegating to backing Collection<T>). Replaced .First() with [0] in all 5 converter call sites. Removed unused using System.Linq from 4 of 5 converters. Committed in perf/remaining-fixes.

### Problem
`.First()` on `Collection<T>`-backed types calls `IEnumerable<T>.GetEnumerator()` which boxes the struct enumerator. Happens on every single-element link/resource write -- the most common HAL serialization case per the spec.

### Current Code
```csharp
JsonSerializer.Serialize(writer, link.LinkObjects.First(), options);
```

### Fix
Add `public T this[int index]` indexer to `LinkObjectCollection`, `LinkCollection`, `ResourceCollection`, `EmbeddedResourceCollection`:
```csharp
// In collection type:
public LinkObject this[int index] => _items[index]; // _items is the backing Collection<T>

// In converter:
JsonSerializer.Serialize(writer, link.LinkObjects[0], options); // no enumerator, no boxing
```
All backing collection types already wrap `Collection<T>` which supports `[int index]`.

---

## PERF-07: Cache fallback empty collection in Resource.Links and Resource.Embedded getters
- **File(s):** `src/Chatter.Rest.Hal/Resource.cs` (lines 63-91)
- **Impact:** Medium
- **Effort:** Low
- **Category:** allocation
- **Status:** - [x] complete
- **Implementation Note:** Resource.Links and Resource.Embedded getters changed from `return _impl ?? new T()` to `_impl = _creator() ?? new T(); return _impl` — fallback collection allocated once and cached. Committed in perf/remaining-fixes.

### Problem
`return _linksImpl ?? new LinkCollection()` allocates a new empty collection on every getter access when `_linksCreator()` returned null. `_linksImpl` stays null so subsequent accesses each allocate.

### Current Code
```csharp
public LinkCollection Links
{
    get
    {
        if (_linksImpl == null)
            _linksImpl = _linksCreator();
        return _linksImpl ?? new LinkCollection();  // allocates every time if creator returned null
    }
}
```

### Fix
Assign fallback to the backing field so it is created once:
```csharp
public LinkCollection Links
{
    get
    {
        if (_linksImpl == null)
            _linksImpl = _linksCreator() ?? new LinkCollection();
        return _linksImpl;
    }
}
```
Apply same fix to `Embedded` getter (`_embeddedImpl` / `EmbeddedResourceCollection`).

---

## PERF-08: Replace try/catch type coercion with TryGetValue in LinkObjectConverter helpers
- **File(s):** `src/Chatter.Rest.Hal/Converters/LinkObjectConverter.cs` (lines 57-99)
- **Impact:** Medium
- **Effort:** Low
- **Category:** json
- **Status:** - [x] complete
- **Implementation Note:** TryGetBooleanAsTrue and TryGetString in LinkObjectConverter replaced try/catch + GetValue<T>() with `node is JsonValue jv && jv.TryGetValue<T>(out var value)` pattern. No #if guard needed — TryGetValue<T> available on netstandard2.0 via System.Text.Json 6.x. Committed in perf/remaining-fixes.

### Problem
`TryGetBooleanAsTrue` and `TryGetString` use try/catch for control flow when `GetValue<T>()` throws on type mismatch. Exception handling is expensive -- allocates stack trace, unwinds frames. Called per property per `LinkObject` on deserialization hot path.

### Current Code
```csharp
private static bool? TryGetBooleanAsTrue(JsonNode? node)
{
    try { return node?.GetValue<bool>() == true ? true : null; }
    catch { return null; }
}
```

### Fix
Use `JsonValue.TryGetValue<T>()` (System.Text.Json 6+, available on both TFMs):
```csharp
private static bool? TryGetBooleanAsTrue(JsonNode? node)
{
    if (node is JsonValue jv && jv.TryGetValue<bool>(out var value))
        return value ? true : null;
    return null;
}

private static string? TryGetString(JsonNode? node)
{
    if (node is JsonValue jv && jv.TryGetValue<string>(out var value))
        return value;
    return null;
}
```

---

## PERF-09: Upgrade source generator to use ForAttributeWithMetadataName
- **File(s):** `src/Chatter.Rest.Hal.CodeGenerators/HalResponseGenerator.cs` (lines 9-16)
- **Impact:** Medium
- **Effort:** Low
- **Category:** generator
- **Status:** - [x] complete
- **Implementation Note:** HalResponseGenerator.cs switched from CreateSyntaxProvider to ForAttributeWithMetadataName("Chatter.Rest.Hal.HalResponseAttribute"). Microsoft.CodeAnalysis.CSharp.Workspaces bumped 4.1.0 → 4.3.1. Parser.cs deleted (all methods became dead code). Committed in perf/remaining-fixes.

### Problem
`CreateSyntaxProvider` with manual syntax filter + semantic model query runs semantic analysis on every `AttributeSyntax` in the compilation on every keystroke in the IDE. `ForAttributeWithMetadataName` (available since Microsoft.CodeAnalysis.CSharp 4.3.1) uses Roslyn's internal attribute cache and is significantly more efficient -- it avoids re-running semantic analysis on unchanged files.

### Current Code
```csharp
var halResponseTypes = context.SyntaxProvider
    .CreateSyntaxProvider(
        static (node, _) => Parser.IsSyntaxTargetForGeneration(node),
        static (ctx, _) => Parser.GetSemanticTargetForGeneration(ctx)
    )
```

### Fix
Replace with `ForAttributeWithMetadataName`:
```csharp
var halResponseTypes = context.SyntaxProvider
    .ForAttributeWithMetadataName(
        "Chatter.Rest.Hal.HalResponseAttribute",  // fully qualified attribute name
        static (node, _) => node is ClassDeclarationSyntax,
        static (ctx, _) => (ClassDeclarationSyntax)ctx.TargetNode
    )
    .Collect();
```
Requires bumping `Microsoft.CodeAnalysis.CSharp` / `Microsoft.CodeAnalysis.CSharp.Workspaces` package reference from `4.1.0` to `4.3.1` in `Chatter.Rest.Hal.CodeGenerators.csproj`. Verify the `Parser` class methods can be simplified or removed after this change -- `IsSyntaxTargetForGeneration` and `GetSemanticTargetForGeneration` may become unnecessary.

---

## PERF-10: Move dedup/sort logic out of RegisterSourceOutput into a cached transform
- **File(s):** `src/Chatter.Rest.Hal.CodeGenerators/Emitter.cs` (lines 19-25), `HalResponseGenerator.cs`
- **Impact:** Medium
- **Effort:** Low
- **Category:** generator
- **Status:** - [x] complete
- **Implementation Note:** Introduced HalClassInfo readonly struct as typed intermediate result. Dedup/sort LINQ chain moved from Emitter.Emit into a .Select() cached transform in HalResponseGenerator. Emitter.Emit now receives pre-processed ImmutableArray<HalClassInfo>. EnumerableExtensions.cs deleted (NotNull<T> became dead code). Committed in perf/remaining-fixes.

### Problem
The LINQ chain `.NotNull().Select().GroupBy().Select().OrderBy().ThenBy().ToArray()` runs inside `RegisterSourceOutput`, which Roslyn cannot cache. This runs on every generation pass even when inputs have not changed.

### Current Code
```csharp
context.RegisterSourceOutput(halResponseTypes, static (ctx, classes) =>
{
    var results = classes.NotNull().Select(...).GroupBy(...).Select(...).OrderBy(...).ThenBy(...).ToArray();
    Emitter.Emit(ctx, results);
});
```

### Fix
Move the transformation into a `.Select(...)` call between `Collect()` and `RegisterSourceOutput`:
```csharp
var processedTypes = halResponseTypes
    .Collect()
    .Select(static (types, _) =>
        types.Where(t => t != null)
             .Select(...)
             .GroupBy(...)
             .Select(...)
             .OrderBy(...)
             .ThenBy(...)
             .ToImmutableArray());

context.RegisterSourceOutput(processedTypes, static (ctx, classes) =>
{
    Emitter.Emit(ctx, classes); // receives pre-processed, cached result
});
```
Use `ImmutableArray<T>` as the return type from the `.Select(...)` transform -- Roslyn uses structural equality on `ImmutableArray` to determine whether to re-run `RegisterSourceOutput`.

---

## PERF-11: Bypass try/catch in StateObject getter for ResourceConverter hot path
- **File(s):** `src/Chatter.Rest.Hal/Resource.cs` (lines 100-133, also StateObject getter)
- **Impact:** Medium
- **Effort:** Low
- **Category:** allocation
- **Status:** - [x] complete
- **Implementation Note:** Added internal object? CachedState property to Resource — lazily resolves state via _stateCreator()?.Deserialize<object>() without try/catch. ResourceConverter.Write now uses value.CachedState instead of value.StateObject. Public State<T>() retains try/catch for consumer safety. Committed in perf/remaining-fixes.

### Problem
`StateObject` (internal getter used by `ResourceConverter.Write`) calls `State<object>()` which wraps deserialization in a try/catch block. This try/catch executes on every resource serialization even after state is cached, because the getter calls the method rather than accessing `_stateObject` directly.

### Fix
Add a direct internal property that bypasses the try/catch and accesses the backing field directly. `ResourceConverter` can use this internal accessor instead of `StateObject`:
```csharp
// Add internal property to Resource:
internal object? CachedState
{
    get
    {
        if (_stateObject == null)
            _stateObject = _stateCreator()?.Deserialize<object>();
        return _stateObject;
    }
}
```
Then update `ResourceConverter.Write` to use `value.CachedState` instead of `value.StateObject`. The public `State<T>()` method retains its try/catch for safe public API use.

---

## PERF-12: Replace LINQ duplicate guard in AddHalConverters with for-loop
- **File(s):** `src/Chatter.Rest.Hal/Extensions/JsonSerializerOptionsExtensions.cs` (line 29)
- **Impact:** Low
- **Effort:** Low
- **Category:** allocation
- **Status:** - [x] complete
- **Implementation Note:** AddHalConverters duplicate guard replaced with a for-loop checking options.Converters[i] is LinkCollectionConverter. Removed now-unused using System.Linq from JsonSerializerOptionsExtensions.cs. Committed in perf/remaining-fixes.

### Problem
`options.Converters.OfType<LinkCollectionConverter>().Any()` allocates a LINQ enumerator to check for duplicate converters. Called at startup/configuration time -- not a hot path, but easy to improve.

### Current Code
```csharp
options.Converters.OfType<LinkCollectionConverter>().Any()
```

### Fix
Replace with a simple `for` loop:
```csharp
bool alreadyAdded = false;
for (int i = 0; i < options.Converters.Count; i++)
{
    if (options.Converters[i] is LinkCollectionConverter) { alreadyAdded = true; break; }
}
if (!alreadyAdded) { /* add converters */ }
```

---

## PERF-13: Add dictionary-based O(1) lookup to LinkCollection and EmbeddedResourceCollection
- **File(s):** `src/Chatter.Rest.Hal/Extensions/LinkCollectionExtensions.cs` (line 16), `src/Chatter.Rest.Hal/Extensions/EmbeddedResourceCollectionExtensions.cs` (line 17), `src/Chatter.Rest.Hal/LinkCollection.cs`, `src/Chatter.Rest.Hal/EmbeddedResourceCollection.cs`
- **Impact:** Low
- **Effort:** Medium
- **Category:** collection
- **Status:** - [x] complete
- **Implementation Note:** Added private Dictionary<string, Link> _index (StringComparer.Ordinal) to LinkCollection and Dictionary<string, EmbeddedResource> _index to EmbeddedResourceCollection. Index maintained on Add/Remove/Clear. Exposed TryGetByRel(string, out Link?) and TryGetByName(string, out EmbeddedResource?) for O(1) lookup. Extension methods (GetLinkOrDefault, GetEmbeddedResource) retain SingleOrDefault semantics — throws on duplicate rels per existing tests. Committed in perf/remaining-fixes.

### Problem
`links.SingleOrDefault(l => l.Rel.Equals(relation))` and equivalent do a linear O(n) scan with LINQ allocation. For HATEOAS-heavy responses with 20+ link relations, this is O(n) per lookup. The `LinkCollection` and `EmbeddedResourceCollection` types wrap `Collection<T>` and have no index.

### Fix
Add an internal `Dictionary<string, Link>` (and equivalent for embedded) alongside the backing `Collection<T>`. Populate on `Add`. Expose an internal or public `TryGetByRel(string rel, out Link link)` method. Update extension methods to use it:
```csharp
// In LinkCollection:
private readonly Dictionary<string, Link> _index = new(StringComparer.OrdinalIgnoreCase);

public void Add(Link link)
{
    _links.Add(link);
    _index[link.Rel] = link; // last writer wins for duplicate rels
}

public bool TryGetByRel(string rel, out Link? link) => _index.TryGetValue(rel, out link);
```
Note: Adds memory overhead proportional to number of link relations -- only meaningful optimization for documents with many relations (10+).
