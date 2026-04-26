# Feature Backlog

Ideas ordered by value. Dependencies noted per item — implement dependents first.

---

## IDEA-01: URI Template Expansion (RFC 6570) -- COMPLETED

**Status:** Completed -- shipped as standalone NuGet package [`Chatter.Rest.UriTemplates` v0.1.0](https://www.nuget.org/packages/Chatter.Rest.UriTemplates/). `Chatter.Rest.Hal` references it as a package dependency. Levels 1--3 implemented; Level 4 deferred.

**Value:** High — foundational; unlocks MCP and HTTP client ideas
**Difficulty:** Low–Medium
**Depends on:** Nothing
**Required by:** IDEA-02 (MCP), IDEA-04 (HTTP client)

### Description

`LinkObject` stores and marks templated `href` strings via `Templated = true` but provides no expansion mechanism. Add `GetTemplateVariables()` to parse `{variable}` tokens and `Expand(IDictionary<string, string> vars)` to substitute variables and return the resolved URI. RFC 6570 Level 1 (simple variable substitution) covers the vast majority of HAL use cases.

### Value-Add

Foundational missing capability in core. Enables MCP tool parameter binding, HTTP link traversal with parameters, and any consumer needing to resolve templated links to real URIs without bringing their own RFC 6570 implementation.

### Implementation (as delivered)

Shipped as a standalone NuGet package `Chatter.Rest.UriTemplates` (v0.1.0) rather than inline in core. `Chatter.Rest.Hal` references it as a package dependency.

- `LinkObject.GetTemplateVariables()` → `IReadOnlyList<string>` — delegates to `UriTemplate.GetVariables()`
- `LinkObject.Expand(IDictionary<string, string> variables)` → `string` — delegates to `UriTemplate.Expand()`
- Guard: only expand when `Templated == true`; return `Href` unchanged otherwise
- RFC 6570 Levels 1--3 fully implemented (simple, reserved, fragment, label, path, semicolon, query, query continuation)
- Level 4 (prefix `:N` and explode `*`) deferred — throws `NotSupportedException` at parse time

---

## IDEA-02: MCP Support (`Chatter.Rest.Hal.Mcp` package)

**Value:** High — differentiator; no other .NET HAL library has MCP support
**Difficulty:** Medium
**Depends on:** IDEA-01 (URI template expansion)
**Required by:** Nothing

### Description

New `Chatter.Rest.Hal.Mcp` NuGet package. HAL's `_links` collection is HATEOAS — machine-readable capability discovery. MCP tools are the same concept for AI agents. The two structures are isomorphic:

| HAL concept | MCP concept |
|---|---|
| `_links` collection | Available tools |
| Link relation (`rel`) | Tool name |
| `href` | Tool action / URI |
| Templated `href` + variables | Tool input schema |
| `title` | Tool description |
| `self` | Resource URI |
| CURIEs | Tool namespace |
| `_embedded` | Inline resource content |

Bridge the two: convert a HAL resource's links to MCP `Tool` definitions dynamically, and provide a `HalMcpServer` that wraps `HttpClient` + HAL parsing to expose a live HAL API as a dynamically-discovered MCP tool surface.

### Value-Add

Differentiator. Allows AI agents (Claude, Copilot, etc.) to autonomously navigate HAL APIs with zero pre-coded endpoint knowledge. The agent fetches the root resource, discovers its links as tools, calls them, receives new resources with new links, and continues traversing. HATEOAS fulfilling its original promise — for AI agents instead of browsers.

### Implementation

**In core library** (no new deps — POCO descriptors only):

```csharp
// Parse template variable names from href
linkObject.GetTemplateVariables(); // → ["id", "page"]

// Produce plain descriptors — no MCP SDK dependency
resource.GetMcpToolDescriptors(); // → IEnumerable<HalToolDescriptor>
```

**In `Chatter.Rest.Hal.Mcp`** (depends on `ModelContextProtocol` NuGet — Microsoft's official .NET MCP SDK):

```csharp
// Convert HAL resource links → MCP Tool definitions
IEnumerable<Tool> tools = resource.ToMcpTools();

// Convert single LinkObject → Tool with JSON Schema input parameters
Tool tool = linkObject.ToMcpTool();
// templated href vars become inputSchema properties
// {id} → { "id": { "type": "string" } }
// {page} → { "page": { "type": "string" } }

// Full MCP server wrapping a HAL API
var server = new HalMcpServer(httpClient, rootUri);
// - fetches root HAL resource on startup
// - exposes _links as MCP tools
// - on tool call: resolves templated href with agent-supplied args, makes HTTP request, parses HAL response
// - updates available tools dynamically as agent traverses to new resources
```

---

## IDEA-03: ASP.NET Core Integration (`Chatter.Rest.Hal.AspNetCore` package)

**Status:** In Progress -- see docs/aspnetcore/

**Value:** High — eliminates manual wiring for every server-side consumer
**Difficulty:** Medium
**Depends on:** Nothing
**Required by:** Nothing

### Description

New `Chatter.Rest.Hal.AspNetCore` NuGet package. Server-side integration for building HAL APIs in ASP.NET Core. Currently consumers must manually register converters, configure `JsonSerializerOptions`, and set response headers. This package makes HAL-first API development first-class in ASP.NET Core.

### Value-Add

Removes all manual wiring. `services.AddHal()` handles converter registration and options. `HalResult<T>` gives controllers a typed HAL response. Content negotiation middleware handles `Accept: application/hal+json` automatically.

### Implementation

New package: `Chatter.Rest.Hal.AspNetCore`
NuGet dependencies: `Microsoft.AspNetCore.Mvc.Core`, `Chatter.Rest.Hal`

- `services.AddHal(options => ...)` — registers `HalJsonOptions` via `IOptions<HalJsonOptions>`, wires HAL converters into the MVC `JsonSerializerOptions`
- `app.UseHal()` — middleware sets `Content-Type: application/hal+json` on HAL responses
- `HalResult<T> : IActionResult` — typed action result; serializes `Resource` with correct media type and status code
- `HalControllerBase` — optional base class exposing `HalOk(resource)`, `HalCreated(uri, resource)`, `HalNoContent()` helpers
- Content negotiation: respects `Accept: application/hal+json` request header; falls back to `application/json` if HAL not accepted

---

## IDEA-04: HTTP Client / Consumer Helpers (`Chatter.Rest.Hal.Client` package)

**Value:** High — closes the consume-and-navigate gap; library is currently build-and-serialize only
**Difficulty:** Low–Medium
**Depends on:** IDEA-01 (URI template expansion) for templated link traversal
**Required by:** IDEA-05 (pagination async traversal layer), IDEA-08 (HAL-FORMS execution)

### Description

New `Chatter.Rest.Hal.Client` NuGet package. Client-side helpers for consuming HAL APIs over HTTP. `HttpClient` extensions to fetch and parse HAL resources, and link traversal helpers that resolve hrefs (including templated), make the HTTP request, and return the next `Resource`.

### Value-Add

Consumers building API clients currently must manually fetch, parse JSON, deserialize, and follow links. This package collapses that to a single call. Templated link following (with variable substitution via IDEA-01) works without any consumer-side RFC 6570 code.

### Implementation

New package: `Chatter.Rest.Hal.Client`
NuGet dependencies: `System.Net.Http`, `Chatter.Rest.Hal`

```csharp
// Fetch and parse a HAL resource
Resource? resource = await httpClient.GetHalAsync(uri);
Resource? resource = await httpClient.PostHalAsync(uri, body);

// Follow a named link relation to the next resource
Resource? next = await resource.FollowLink("orders", httpClient);

// Follow a templated link with variable substitution
Resource? order = await resource.FollowLink("order", httpClient, new { id = "42" });

// Follow an array of link objects for a relation
IAsyncEnumerable<Resource> items = resource.FollowLinks("items", httpClient);
```

- All methods set `Accept: application/hal+json` and parse response as HAL
- `GetHalAsync` / `PostHalAsync` return `null` on non-HAL responses rather than throwing
- Variable substitution delegates to `LinkObject.Expand()` from IDEA-01

---

## IDEA-05: Pagination Helpers

**Value:** Medium–High — HAL pagination (`next`/`prev`/`first`/`last`) is ubiquitous; removes boilerplate from every paged API consumer
**Difficulty:** Low
**Depends on:** IDEA-04 (HTTP client helpers) for async traversal layer; core accessor layer has no dependencies
**Required by:** Nothing

### Description

Two layers: (a) pure link accessors added to core — no new dependencies, return `LinkObject?` for standard IANA pagination relations; (b) async page traversal in `Chatter.Rest.Hal.Client` that fetches pages over HTTP.

### Value-Add

Eliminates repeated `GetLinkObject("next")` / `GetLinkObject("prev")` boilerplate and provides a clean `IAsyncEnumerable<Resource>` page iteration story.

### Implementation

**In core library** (`ResourceExtensions` — no new deps):

```csharp
resource.GetNextLink()    // → LinkObject?
resource.GetPrevLink()    // → LinkObject?
resource.GetFirstLink()   // → LinkObject?
resource.GetLastLink()    // → LinkObject?
resource.HasNextPage()    // → bool
resource.GetPageLinks()   // → PaginationLinks (readonly struct with all four)
```

**In `Chatter.Rest.Hal.Client`** (async traversal, depends on IDEA-04):

```csharp
resource.FetchNextPage(httpClient)   // → Task<Resource?>
resource.FetchPrevPage(httpClient)   // → Task<Resource?>
resource.TraversePages(httpClient)   // → IAsyncEnumerable<Resource>
```

---

## IDEA-06: Source Generator Enhancements

**Value:** Medium — reduces boilerplate for source-generator users; typed accessors eliminate stringly-typed relation lookups
**Difficulty:** High
**Depends on:** Nothing
**Required by:** Nothing

### Description

Expand the `[HalResponse]` source generator beyond its current scope (injecting `Links` and `Embedded` properties). Three sub-features, implementable independently:

- **(a) Typed link accessors** — new `[HalLink("rel")]` attribute on a partial class property; generator emits a typed accessor delegating to `GetLinkObject`
- **(b) Converter registration codegen** — `[HalResponse]` triggers generation of a static `RegisterConverters(JsonSerializerOptions opts)` method
- **(c) Generic and nested class support** — fix the emitter to handle type parameter lists and outer class wrappers

### Value-Add

Typed accessors replace `GetLinkObject("orders")` with a generated `OrdersLink` property — compile-time safe, IntelliSense-visible, refactorable. Converter registration codegen removes a manual setup step from every consuming project.

### Implementation

- **(a)** New `[HalLink(string rel)]` attribute in `Chatter.Rest.Hal`. Generator reads it and emits: `public LinkObject? OrdersLink => Links.GetLinkObject("orders");`
- **(b)** `[HalResponse]` triggers generator to emit: `public static void RegisterConverters(JsonSerializerOptions opts) => opts.AddHalConverters();`
- **(c)** `Emitter.cs`: read `TypeParameterList` + `ConstraintClauses` and include them in the emitted class signature. Fix `GetNamespaceFrom` to walk and accumulate outer class wrappers rather than stopping at the first namespace node.

---

## IDEA-07: O(1) Index Parity for `ResourceCollection` and `LinkObjectCollection`

**Value:** Low — rarely queried by key in typical HAL usage; only matters for large collections with repeated lookups
**Difficulty:** Low
**Depends on:** Nothing
**Required by:** Nothing

### Description

`LinkCollection` and `EmbeddedResourceCollection` have dictionary-backed O(1) lookup via `TryGetByRel()` and `TryGetByName()` (added in PERF-13). `ResourceCollection` and `LinkObjectCollection` have no equivalent. CONS-14 documented this asymmetry as intentional but left no explanation in code.

### Value-Add

Closes the asymmetric API surface. Useful if consumers build large `LinkObjectCollection` instances and query by `Name` repeatedly. `ResourceCollection` has no natural string key — the fix there is documentation only.

### Implementation

- `LinkObjectCollection`: add `Dictionary<string, LinkObject>` keyed on `Name`; expose `TryGetByName(string name, out LinkObject? result)`. Note: `Name` is optional on `LinkObject` — only named items participate in the index.
- `ResourceCollection`: no natural string key exists. Add XML doc comment explaining the intentional absence; no behavior change.

---

## IDEA-08: HAL-FORMS Support (`Chatter.Rest.Hal.Forms` package)

**Value:** Medium–High — standards-track answer to embedding HTTP verb + body schema in HAL documents
**Difficulty:** Medium
**Depends on:** IDEA-04 (HTTP client helpers, for execution side)
**Required by:** Nothing

### Description

New `Chatter.Rest.Hal.Forms` NuGet package implementing the HAL-FORMS extension spec (https://rwcbook.github.io/hal-forms/). HAL's core `LinkObject` intentionally omits HTTP verb — it is a navigation primitive, not an action descriptor. HAL-FORMS is the standards-track extension that adds method + body schema to HAL documents via a `_templates` property, separate from `_links` and `_embedded`.

Keeps `LinkObject` spec-pure (no `method` property) while enabling rich form-based interactions for consumers who need embedded verb and body-schema hints.

### Value-Add

Allows HAL API servers to describe non-GET operations (POST, PUT, PATCH, DELETE) with full input schema in the document itself. Clients and AI agents discover forms the same way they discover links — no out-of-band knowledge of verbs or body shapes required. Standards-track extension with community tooling and recognized patterns.

### Implementation

New package: `Chatter.Rest.Hal.Forms`
NuGet dependencies: `Chatter.Rest.Hal`, `Chatter.Rest.Hal.Client` (IDEA-04, for execution)

**Domain types:**

```csharp
// A single HAL-FORMS template
public sealed record HalFormTemplate
{
    public string Method { get; init; }          // GET, POST, PUT, PATCH, DELETE
    public string? ContentType { get; init; }    // e.g. application/json
    public string? Target { get; init; }         // override href if present
    public IReadOnlyList<HalFormProperty> Properties { get; init; }
}

// A single form field descriptor
public sealed record HalFormProperty
{
    public string Name { get; init; }
    public bool Required { get; init; }
    public bool ReadOnly { get; init; }
    public string? Value { get; init; }
    public string? Prompt { get; init; }
    public string? Type { get; init; }           // text, number, email, etc.
    public string? Regex { get; init; }
}
```

**Serialization:**

- `HalFormsConverter` reads/writes `_templates` property on HAL document
- `_templates` is a dictionary keyed by template name (e.g. `"default"`, `"create-order"`)

**Builder extensions:**

```csharp
resource.WithTemplate("create-order", template => template
    .WithMethod("POST")
    .WithContentType("application/json")
    .WithProperty("productId", p => p.Required().OfType("number"))
    .WithProperty("quantity", p => p.Required().OfType("number")));
```

**Client integration (depends on IDEA-04):**

```csharp
// Read a named form template from resource
HalFormTemplate? form = resource.GetFormTemplate("create-order");

// Execute a form — uses method from template, submits body
Resource? result = await resource.FollowForm("create-order", body, httpClient);
```
