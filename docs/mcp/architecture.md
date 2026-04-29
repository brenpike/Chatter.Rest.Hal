# Chatter.Rest.Hal.Mcp -- Technical Architecture

This document is the source of truth for how the `Chatter.Rest.Hal.Mcp` package is built. It guides implementation decisions with enough detail for a developer to implement each type without ambiguity. For what the system does, see [requirements.md](requirements.md).

---

## Prerequisites

IDEA-04 (`Chatter.Rest.Hal.Client`) must be implemented before this package. It provides the `IHalClient` interface with `GetAsync(Uri uri, CancellationToken cancellationToken)` that this package uses to fetch and parse HAL resources over HTTP. See [docs/backlog.md](../backlog.md) for the IDEA-04 specification.

---

## Package Dependency Graph

```
Chatter.Rest.Hal.Mcp
  -> Chatter.Rest.Hal.Client   (IDEA-04; provides IHalClient)
  -> ModelContextProtocol       (Microsoft official C# MCP SDK)
  -> Chatter.Rest.Hal           (core types: Resource, LinkObject, LinkCollection, Link)
```

`Chatter.Rest.Hal.Client` itself depends on `Chatter.Rest.Hal` and `System.Net.Http`. The MCP package does not reference `System.Net.Http` directly — all HTTP access flows through `IHalClient`.

---

## SDK Alignment

The package integrates with `ModelContextProtocol` (Microsoft's official C# MCP SDK) using the same patterns as the `dotnet new mcpserver` template:

- **Tool registration:** Tools are registered via an `IMcpServerBuilder` extension method (`WithHalApi`), following the same DI seam as `WithToolsFromAssembly()`.
- **Tool base class:** All tool instances (both HAL-derived and static) extend `McpServerTool` abstract base.
- **Dynamic tool collection:** `McpServerPrimitiveCollection<McpServerTool>` (accessed via `McpServerOptions.ToolCollection`) is mutated at runtime. The SDK auto-fires `tools/list_changed` when the collection's `Changed` event triggers.
- **No custom handlers:** No custom `ListToolsHandler` or `CallToolHandler` is needed. Standard collection mutation and `McpServerTool.InvokeAsync` dispatch suffice.
- **`ScopeRequests = true` (default):** The MCP SDK creates an `IServiceScope` per tool invocation and exposes it as `RequestContext.Services`. Scoped services (including `IHalClient` and `IHalToolCollectionManager`) are correctly resolved per-invocation from this scope.
- **Tool collection singletons:** `McpServerTool` instances stored in the tool collection are singletons. Scoped services must never be captured in tool constructor fields. Instead, tools resolve scoped dependencies from `RequestContext.Services` inside `InvokeAsync`.

---

## Project Location

- Source: `src/Chatter.Rest.Hal.Mcp/`
- Tests: `test/Chatter.Rest.Hal.Mcp.Tests/`

---

## Namespaces

| Namespace | Visibility | Contents |
|---|---|---|
| `Chatter.Rest.Hal.Mcp` | Public | `HalMcpServerOptions`, `HalNavigationTool`, `NavigateToRootTool`, `HalMcpServerBuilderExtensions` |
| `Chatter.Rest.Hal.Mcp.Internal` | Internal | `IHalToolCollectionManager`, `HalToolCollectionManager`, `ToolNaming` |

---

## Key Types

### `HalMcpServerOptions`

Configuration object for the MCP-HAL bridge. Registered as a singleton via the options pattern.

```csharp
namespace Chatter.Rest.Hal.Mcp;

public sealed class HalMcpServerOptions
{
    /// <summary>
    /// The root URI of the HAL API. Required. Fetched on startup and when the
    /// agent calls navigate_to_root.
    /// </summary>
    public string RootUri { get; set; } = string.Empty;

    /// <summary>
    /// Link relations that are not converted to tools. Default: ["curies"].
    /// </summary>
    public ISet<string> ExcludeRels { get; set; } = new HashSet<string> { "curies" };
}
```

**Validation:** `RootUri` is validated during `WithHalApi` registration. If null, empty, or whitespace, `ArgumentException` is thrown immediately (REQ-19).

---

### `HalNavigationTool : McpServerTool`

Wraps a single `LinkObject` and its relation name. One instance per non-excluded rel in the current resource's `_links`.

**Constructor:**

```csharp
namespace Chatter.Rest.Hal.Mcp;

public sealed class HalNavigationTool : McpServerTool
{
    public HalNavigationTool(
        string rel,
        LinkObject link,
        string selfHref,
        ILogger<HalNavigationTool> logger);
}
```

**Fields stored:**
- `_rel` -- the link relation name
- `_link` -- the `LinkObject` instance
- `_selfHref` -- the self href of the resource this link came from (used for tool naming)
- `_logger` -- `ILogger<HalNavigationTool>`

`IHalClient`, `HalMcpServerOptions`, and `IHalToolCollectionManager` are resolved from `RequestContext.Services` inside `InvokeAsync` (not stored as constructor fields) because they may be scoped services and `HalNavigationTool` instances are singletons in the tool collection.

**`ProtocolTool` property (override):**

Built from:
- `Name` = `ToolNaming.CreateToolName(selfHref, rel)`
- `Description` = `link.Title ?? $"Navigate to {rel}"`
- `InputSchema` = JSON Schema with one `string` property per template variable from `link.GetTemplateVariables()`. Non-templated links produce `{ "type": "object", "properties": {} }`.

Example input schema for `href="/orders/{id}"`:
```json
{
    "type": "object",
    "properties": {
        "id": { "type": "string" }
    },
    "required": ["id"]
}
```

**`InvokeAsync` method:** See [InvokeAsync Flow](#invokeasync-flow-halnavigationtool) below.

---

### `NavigateToRootTool : McpServerTool`

Persistent static tool that is never removed from the tool collection.

```csharp
namespace Chatter.Rest.Hal.Mcp;

public sealed class NavigateToRootTool : McpServerTool
{
    public NavigateToRootTool();
}
```

`IHalClient`, `HalMcpServerOptions`, `IHalToolCollectionManager`, and `ILogger<NavigateToRootTool>` are all resolved from `RequestContext.Services` inside `InvokeAsync`. No dependencies are injected via constructor -- `NavigateToRootTool` is instantiated during service registration before the DI container is built.

**`ProtocolTool` property:**
- `Name` = `"navigate_to_root"`
- `Description` = `"Return to the API root and rediscover available actions"`
- `InputSchema` = `{ "type": "object", "properties": {} }` (no input parameters)

**`InvokeAsync`:**
1. Resolve `IHalClient halClient`, `HalMcpServerOptions halOptions`, `IHalToolCollectionManager manager` from `request.Services`
2. Fetch `halOptions.RootUri` via `halClient.GetAsync(new Uri(halOptions.RootUri, UriKind.RelativeOrAbsolute), cancellationToken)`
3. If response is `null`: return `CallToolResult { IsError = true }` with message `"No HAL resource returned from {halOptions.RootUri} (resource not found or response was not HAL)"`
4. Call `manager.SwapTools(response, halOptions.RootUri)`
5. Serialize response to HAL JSON
6. Return `CallToolResult { Content = [new TextContentBlock { Text = json }] }`
7. Catch `HttpRequestException ex` when status code available: return `CallToolResult { IsError = true }` with message `"HTTP {(int)status} {reason}: {halOptions.RootUri}"`
8. Catch `Exception ex`: return `CallToolResult { IsError = true, Content = [ex.Message] }`

---

### `IHalToolCollectionManager` (internal interface)

```csharp
namespace Chatter.Rest.Hal.Mcp.Internal;

internal interface IHalToolCollectionManager
{
    void SwapTools(Resource resource, string selfHref);
}
```

---

### `HalToolCollectionManager : IHalToolCollectionManager` (internal scoped)

Scoped DI service that replaces the tool collection with tools derived from a HAL resource's links. Registered as `IHalToolCollectionManager` (scoped).

```csharp
namespace Chatter.Rest.Hal.Mcp.Internal;

internal sealed class HalToolCollectionManager : IHalToolCollectionManager
{
    private static readonly object _swapLock = new();

    public HalToolCollectionManager(
        McpServerOptions mcpOptions,
        HalMcpServerOptions halOptions,
        ILoggerFactory loggerFactory);

    public void SwapTools(Resource resource, string selfHref);
}
```

**Fields stored:**
- `_mcpOptions` -- access to `McpServerOptions.ToolCollection`
- `_halOptions` -- configuration (ExcludeRels)
- `_loggerFactory` -- used to create `ILogger<HalNavigationTool>` for each tool instance

`_swapLock` is `static readonly` so only one swap executes at a time across all scoped instances.

See [Tool Collection Swap Algorithm](#tool-collection-swap-algorithm) below.

---

### `ToolNaming` (internal static)

Pure function for deriving MCP tool names from HAL self href and rel.

```csharp
namespace Chatter.Rest.Hal.Mcp.Internal;

internal static class ToolNaming
{
    public static string CreateToolName(string selfHref, string rel);
}
```

See [Tool Naming Algorithm](#tool-naming-algorithm) below.

---

### `HalMcpStartupService : IHostedService`

Hosted service that fetches the root resource on startup and populates the initial tool collection.

```csharp
namespace Chatter.Rest.Hal.Mcp;

public sealed class HalMcpStartupService : IHostedService
{
    public HalMcpStartupService(
        IServiceProvider serviceProvider,
        HalMcpServerOptions halOptions,
        ILogger<HalMcpStartupService> logger);

    public Task StartAsync(CancellationToken cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken);
}
```

**`StartAsync`:**
1. Create async scope: `await using var scope = _serviceProvider.CreateAsyncScope()`
2. Resolve `IHalClient halClient` and `IHalToolCollectionManager manager` from `scope.ServiceProvider`
3. Fetch `halOptions.RootUri` via `halClient.GetAsync(new Uri(halOptions.RootUri, UriKind.RelativeOrAbsolute), cancellationToken)`
4. If successful and non-null: call `manager.SwapTools(response, halOptions.RootUri)` to populate the tool collection
5. If response is null: log warning, do not throw
6. If fetch throws (any exception): catch, log warning, do not throw. The `navigate_to_root` tool (already in the collection from registration) remains available for the agent to retry.

**`StopAsync`:** No-op. Returns `Task.CompletedTask`.

---

### `HalMcpServerBuilderExtensions`

The single public entry point for wiring HAL-MCP integration into the DI container.

```csharp
namespace Chatter.Rest.Hal.Mcp;

public static class HalMcpServerBuilderExtensions
{
    public static IMcpServerBuilder WithHalApi(
        this IMcpServerBuilder builder,
        Action<HalMcpServerOptions> configure);
}
```

**Registration sequence:**
1. Create `HalMcpServerOptions` instance, invoke `configure` delegate
2. Validate `RootUri` is not null/empty/whitespace; throw `ArgumentException` if invalid (REQ-19)
3. Register `HalMcpServerOptions` as singleton
4. Register `IHalToolCollectionManager` → `HalToolCollectionManager` as scoped
5. Instantiate `NavigateToRootTool` (no constructor dependencies) and add it to `McpServerOptions.ToolCollection`
6. Register `HalMcpStartupService` as `IHostedService`
7. Return `builder` for chaining

---

## Tool Naming Algorithm

Both the self href and the rel are sanitized independently using the same steps, then joined with `__`. The combined name is truncated to a 62-character budget.

### Sanitization (applied to each part)

```
Sanitize(input):
    s = input
    // 1. Strip URI scheme prefix: if "://" present, remove scheme and "://" only; keep authority
    if s contains "://":
        s = s after "://"   // e.g., "https://api.example.com/orders" -> "api.example.com/orders"
    // 2. Lowercase
    s = s.ToLowerInvariant()
    // 3. Replace {varname} tokens: remove braces, keep inner name
    s = Regex.Replace(s, @"\{([^}]+)\}", "$1")
    // 4. Replace non-[a-z0-9] characters with "_" (leading "/" is handled here)
    s = Regex.Replace(s, @"[^a-z0-9]", "_")
    // 5. Collapse consecutive underscores
    s = Regex.Replace(s, @"_+", "_")
    // 6. Trim leading/trailing underscores
    s = s.Trim('_')
    // 7. Map empty string to "root"
    return s == "" ? "root" : s
```

### Truncation (budget = 62)

```
CreateToolName(selfHref, rel):
    prefix = Sanitize(selfHref)
    relPart = Sanitize(rel)

    if relPart.Length >= 62:
        return relPart[..62]

    prefixBudget = 62 - relPart.Length - 2  // reserve 2 chars for "__" separator

    if prefixBudget <= 0:
        return relPart

    prefix = prefix[..Min(prefixBudget, prefix.Length)]

    if prefix == "":
        return relPart

    return prefix + "__" + relPart
```

### Examples

| selfHref | rel | tool name |
|---|---|---|
| `/` | `orders` | `root__orders` |
| `/orders/42` | `cancel` | `orders_42__cancel` |
| `/api/v1/customers` | `next` | `api_v1_customers__next` |
| `/orders/{id}` | `self` | `orders_id__self` |
| `/user-profiles` | `search` | `user_profiles__search` |
| `/` | `self` | `root__self` |
| `/api/v2/users/abc-def` | `edit` | `api_v2_users_abc_def__edit` |
| `/orders/{id}/lines/{lineId}` | `delete` | `orders_id_lines_lineid__delete` |
| `https://api.example.com/orders` | `cancel` | `api_example_com_orders__cancel` |

The double-underscore `__` separator between prefix and rel is intentional. Single underscores appear within sanitized segments; the double underscore provides unambiguous parsing of prefix vs. rel.

---

## Tool Collection Swap Algorithm

```
HalToolCollectionManager.SwapTools(resource, selfHref):
    lock (_swapLock):
        collection = _mcpOptions.ToolCollection

        // 1. Preserve navigate_to_root
        navigateToRoot = collection entries where tool name == "navigate_to_root"

        // 2. Clear and restore persistent tools
        collection.Clear()
        for each tool in navigateToRoot:
            collection.Add(tool)

        // 3. Resolve actual self href from the resource if available
        effectiveSelf = selfHref
        if resource.Links is not null:
            for each link in resource.Links:
                if link.Rel == "self" and link.LinkObjects has entries:
                    effectiveSelf = link.LinkObjects[0].Href
                    break

        // 4. Add one HalNavigationTool per non-excluded rel
        if resource.Links is not null:
            for each link in resource.Links:
                if link.Rel in _halOptions.ExcludeRels:
                    continue
                linkObject = link.LinkObjects[0]  // first LinkObject only (v1)
                logger = _loggerFactory.CreateLogger<HalNavigationTool>()
                collection.Add(new HalNavigationTool(
                    link.Rel, linkObject, effectiveSelf, logger))

    // 5. collection.Changed fires automatically (outside lock)
    //    -> SDK sends tools/list_changed notification
```

**Key behaviors:**
- `navigate_to_root` is always preserved (REQ-02)
- Self href is resolved from the resource's own `self` link when available, falling back to the provided `selfHref` parameter
- Only the first `LinkObject` per rel is used (REQ-04); array relations are not expanded in v1
- Rels in `ExcludeRels` are skipped (REQ-17)
- The `self` rel is included by default unless explicitly excluded (REQ-18)
- `_swapLock` is `static readonly` — only one swap runs at a time across all scoped instances (REQ-42)
- `ILogger<HalNavigationTool>` is created per tool via `_loggerFactory.CreateLogger<HalNavigationTool>()` (REQ-24)

---

## InvokeAsync Flow (HalNavigationTool)

```
InvokeAsync(request, cancellationToken):
    try:
        // 1. Resolve scoped services
        halClient = request.Services.GetRequiredService<IHalClient>()
        halOptions = request.Services.GetRequiredService<HalMcpServerOptions>()
        manager   = request.Services.GetRequiredService<IHalToolCollectionManager>()

        // 2. Coerce arguments from JsonElement to string
        rawArgs = request.Params?.Arguments ?? empty
        args = rawArgs.ToDictionary(
            k => k.Key,
            v => v.Value.ValueKind == JsonValueKind.String
                ? v.Value.GetString() ?? string.Empty
                : v.Value.ToString())

        // 3. Resolve href
        if _link.Templated == true:
            resolvedHref = _link.Expand(args)
        else:
            resolvedHref = _link.Href

        // 4. Fetch resource
        response = await halClient.GetAsync(new Uri(resolvedHref, UriKind.RelativeOrAbsolute), cancellationToken)

        // 5. Handle non-HAL response
        if response is null:
            return CallToolResult
            {
                IsError = true,
                Content = [TextContentBlock("No HAL resource returned from {resolvedHref} (resource not found or response was not HAL)")]
            }

        // 6. Swap tool collection with new resource's links
        manager.SwapTools(response, resolvedHref)

        // 7. Serialize response to HAL JSON
        json = JsonSerializer.Serialize(response, halJsonSerializerOptions)

        // 8. Return content
        return CallToolResult
        {
            Content = [TextContentBlock(json)]
        }

    catch HttpRequestException ex when status code available:
        return CallToolResult
        {
            IsError = true,
            Content = [TextContentBlock("HTTP {(int)status} {reason}: {resolvedHref}")]
        }

    catch Exception ex:
        return CallToolResult
        {
            IsError = true,
            Content = [TextContentBlock(ex.Message)]
        }
```

**Important:** `InvokeAsync` never throws (REQ-14, REQ-15, REQ-16). All failures are returned as `CallToolResult { IsError = true }`.

**Tool collection update on error:** When the HTTP request fails or the response is not HAL, the tool collection is NOT modified. The agent retains its current set of tools and can retry or navigate elsewhere.

---

## Error Handling Summary

| Condition | Behavior | Error message format |
|---|---|---|
| HTTP non-success status | `IsError = true`, no collection change | `"HTTP {statusCode} {reasonPhrase}: {resolvedHref}"` |
| Response is not valid HAL (null) | `IsError = true`, no collection change | `"No HAL resource returned from {href} (resource not found or response was not HAL)"` |
| Exception during invocation | `IsError = true`, no collection change | `ex.Message` |
| Startup root fetch failure | Logged warning, no throw | `navigate_to_root` remains; agent can retry |
| Relative href without BaseAddress | `IsError = true` via Exception catch | `ex.Message` (`InvalidOperationException`) |

Applies symmetrically to `HalNavigationTool` and `NavigateToRootTool`.

---

## Input Schema Construction

For a `LinkObject` with `Templated == true`, the input schema is built from `LinkObject.GetTemplateVariables()`:

```csharp
BuildInputSchema(LinkObject link):
    variables = link.GetTemplateVariables()  // IReadOnlyList<string>
    if variables.Count == 0:
        return { "type": "object", "properties": {} }

    properties = {}
    required = []
    for each varName in variables:
        properties[varName] = { "type": "string" }
        required.Add(varName)

    return {
        "type": "object",
        "properties": properties,
        "required": required
    }
```

All template variables are typed as `string` in v1 (REQ-07). The `required` array includes all variables -- the agent must supply values for all template parameters.

---

## Argument Coercion

MCP arguments arrive as `IDictionary<string, JsonElement>`. Before calling `LinkObject.Expand(IDictionary<string, string>)`, coerce to `string`:

```csharp
var args = rawArgs.ToDictionary(
    kvp => kvp.Key,
    kvp => kvp.Value.ValueKind == JsonValueKind.String
        ? kvp.Value.GetString() ?? string.Empty
        : kvp.Value.ToString());
```

This handles both JSON string values (`"42"`) and non-string JSON values (numbers, booleans) that MCP clients may send for template variables typed as `string` in the input schema (REQ-43).

---

## Serialization

When returning HAL resource content in `CallToolResult`, the resource is serialized using `System.Text.Json.JsonSerializer.Serialize<Resource>()` with HAL converters applied (via `JsonSerializerOptions.AddHalConverters()`). This produces standard HAL JSON including `_links`, `_embedded`, and state properties.

---

## Target Framework

The package targets `net8.0` (REQ-44). This aligns with the minimum TFM of `ModelContextProtocol` and `Chatter.Rest.Hal.Client`.

---

## Async Conventions

### Async-only I/O (REQ-37)

All I/O in the package is async end-to-end. No sync-over-async wrappers (`.Result`, `.GetAwaiter().GetResult()`, `.Wait()`) or blocking calls (`Thread.Sleep`) appear anywhere. `HalMcpStartupService.StartAsync`, `HalNavigationTool.InvokeAsync`, and `NavigateToRootTool.InvokeAsync` are all inherently async and delegate to async `HttpClient` extension methods.

---

### CancellationToken threading (REQ-38)

Every async call site passes the `CancellationToken` received from the framework:
- `HalMcpStartupService.StartAsync` receives `CancellationToken` from `IHostedService.StartAsync` and passes it to `IHalClient.GetAsync`
- `HalNavigationTool.InvokeAsync` receives `CancellationToken` from `McpServerTool.InvokeAsync(RequestContext, CancellationToken)` and passes it to `IHalClient.GetAsync`
- `NavigateToRootTool.InvokeAsync` receives `CancellationToken` from the same `McpServerTool` contract and passes it to `IHalClient.GetAsync`

The `CancellationToken` is not stored as a field. It flows through the call chain as a parameter at every level.

---

### Parallelism (REQ-39)

No parallelism is needed in this package. Each tool invocation performs a single HTTP GET. `SwapTools` is a synchronous in-memory collection mutation. There are no loops performing independent async I/O calls, so `Task.WhenAll` is not applicable.

---

## Logging Architecture

### ILogger<T> Injection

Each stateful type accepts `ILogger<T>` via constructor injection (REQ-24). Updated constructor signatures:

```csharp
public sealed class HalMcpStartupService : IHostedService
{
    public HalMcpStartupService(
        IServiceProvider serviceProvider,
        HalMcpServerOptions halOptions,
        ILogger<HalMcpStartupService> logger);
}

public sealed class HalNavigationTool : McpServerTool
{
    public HalNavigationTool(
        string rel,
        LinkObject link,
        string selfHref,
        ILogger<HalNavigationTool> logger);
}

public sealed class NavigateToRootTool : McpServerTool
{
    public NavigateToRootTool();
    // ILogger<NavigateToRootTool> resolved from RequestContext.Services inside InvokeAsync
}
```

**`HalToolCollectionManager` (internal scoped):** Accepts `ILoggerFactory` via constructor injection and uses it to create `ILogger<HalNavigationTool>` for each tool instance it constructs (REQ-24). It does not hold a logger for its own swap operations; swap-triggered log messages (tool count, individual tool names) are emitted by the calling tool or startup service, which has richer context about the triggering event.

**`HalMcpServerBuilderExtensions.WithHalApi`:** Runs during service registration before the DI container is built, so `ILogger` is unavailable at this stage. The configured-root-uri `Debug` log is deferred to `HalMcpStartupService.StartAsync` instead. If `RootUri` validation fails, `ArgumentException` is thrown without logging — the exception message is descriptive, and the caller's exception handler is the appropriate place to log it. For the same reason, `NavigateToRootTool` accepts no constructor dependencies and resolves all required services (including `ILogger<NavigateToRootTool>`) from `RequestContext.Services` inside `InvokeAsync`.

---

### LoggerMessage Source Generators

All log points are defined as `[LoggerMessage]` attributed static partial methods (REQ-35). Event IDs are scoped per category (per type).

**`HalMcpStartupService` log points:**

| Log method | Level | Event ID | Message template | Parameters |
|---|---|---|---|---|
| `LogConfiguredRootUri` | Debug | 1 | `"Configured RootUri: {RootUri}"` | `RootUri` |
| `LogStartupFetchBegin` | Debug | 2 | `"Fetching root resource from {RootUri}"` | `RootUri` |
| `LogStartupComplete` | Information | 3 | `"HAL MCP tools initialized: {ToolCount} tools registered from {RootUri}"` | `ToolCount`, `RootUri` |
| `LogStartupFailed` | Warning | 4 | `"Failed to fetch root resource from {RootUri} on startup"` | `RootUri` (exception passed as `Exception` arg) |

**`HalNavigationTool` log points:**

| Log method | Level | Event ID | Message template | Parameters |
|---|---|---|---|---|
| `LogToolInvoking` | Debug | 1 | `"Invoking tool '{ToolName}', fetching {Href}"` | `ToolName`, `Href` |
| `LogToolResponse` | Debug | 2 | `"Tool '{ToolName}' received {StatusCode} from {Href}"` | `ToolName`, `StatusCode`, `Href` |
| `LogToolHttpError` | Warning | 3 | `"Tool '{ToolName}' received HTTP {StatusCode} from {Href}"` | `ToolName`, `StatusCode`, `Href` |
| `LogToolNonHalResponse` | Warning | 4 | `"Tool '{ToolName}' received non-HAL response from {Href}"` | `ToolName`, `Href` |
| `LogToolException` | Error | 5 | `"Tool '{ToolName}' failed"` | `ToolName` (exception passed as `Exception` arg) |
| `LogToolsSwapped` | Debug | 6 | `"Tool collection updated: {RemovedCount} removed, {AddedCount} added"` | `RemovedCount`, `AddedCount` |
| `LogToolAdded` | Trace | 7 | `"Tool added: {AddedToolName}"` | `AddedToolName` |

**`NavigateToRootTool` log points:**

| Log method | Level | Event ID | Message template | Parameters |
|---|---|---|---|---|
| `LogRootInvoking` | Debug | 1 | `"Navigating to root {RootUri}"` | `RootUri` |
| `LogRootResponse` | Debug | 2 | `"Root navigation received {StatusCode} from {RootUri}"` | `StatusCode`, `RootUri` |
| `LogRootHttpError` | Warning | 3 | `"Root navigation received HTTP {StatusCode} from {RootUri}"` | `StatusCode`, `RootUri` |
| `LogRootNonHalResponse` | Warning | 4 | `"Root resource at {RootUri} was not a valid HAL resource"` | `RootUri` |
| `LogRootException` | Error | 5 | `"Root navigation failed"` | (exception passed as `Exception` arg) |
| `LogRootToolsSwapped` | Debug | 6 | `"Root tool collection updated: {RemovedCount} removed, {AddedCount} added"` | `RemovedCount`, `AddedCount` |
| `LogRootToolAdded` | Trace | 7 | `"Tool added: {AddedToolName}"` | `AddedToolName` |

---

### SwapTools Call-Site Logging Pattern

The calling tool or startup service logs before and after calling `SwapTools`. The Trace-level tool-name loop uses an explicit `IsEnabled` guard (REQ-33):

```
// manager = IHalToolCollectionManager resolved from RequestContext.Services
// mcpOptions = resolved from RequestContext.Services (for collection count)
var previousCount = mcpOptions.ToolCollection.Count;
manager.SwapTools(response, resolvedHref);
var addedCount = mcpOptions.ToolCollection.Count - 1; // minus navigate_to_root
LogToolsSwapped(previousCount - 1, addedCount);

if (_logger.IsEnabled(LogLevel.Trace))
    foreach (var tool in mcpOptions.ToolCollection)
        if (tool.ProtocolTool.Name != "navigate_to_root")
            LogToolAdded(tool.ProtocolTool.Name);
```

The `-1` adjusts for the persistent `navigate_to_root` tool, which is not counted in the "removed" or "added" totals.

---

### Sensitive Data

Request bodies, response bodies, auth headers, and API keys must never appear in log messages (REQ-36). Tool names and hrefs are safe to log.

---

## Test Strategy

### Unit tests

**`ToolNaming.CreateToolName`** -- pure function, exhaustive input/output table:
- Root `/` with various rels
- Multi-segment paths
- Paths with template variables: `{id}` → inner name preserved, braces dropped (e.g. `/orders/{id}` → `orders_id__rel`)
- Paths with multiple template variables: `/orders/{id}/lines/{lineId}` → `orders_id_lines_lineid__rel`
- Paths with hyphens
- Edge cases: trailing slashes, double slashes, empty rel

**`HalToolCollectionManager.SwapTools`** -- core swap logic:
- Replaces all tools except `navigate_to_root`
- Excludes rels in `ExcludeRels`
- Includes `self` by default
- Preserves `navigate_to_root` after swap
- Uses resource's self link href when available
- Falls back to provided selfHref when resource has no self link
- Handles resource with empty `_links`
- Handles resource with null `Links`

**`HalNavigationTool.ProtocolTool`** -- property construction:
- Tool name matches naming algorithm
- Description uses `Title` when present
- Description falls back to `"Navigate to {rel}"` when `Title` is null/empty
- Input schema has correct properties for templated links
- Input schema is empty for non-templated links

**`HalNavigationTool.InvokeAsync`** -- invocation behavior:
- Resolves templated href with supplied arguments
- Uses `Href` directly for non-templated links
- Returns HAL JSON content on success
- Swaps tool collection on success
- Returns `IsError = true` for null response (non-HAL)
- Returns `IsError = true` for HTTP failure
- Returns `IsError = true` for exceptions
- Does not modify tool collection on error

**`NavigateToRootTool.InvokeAsync`** -- root navigation:
- Fetches configured `RootUri`
- Resets tool collection to root's links
- Preserves itself in collection after reset
- Returns root HAL JSON as content
- Returns `IsError = true` on failure

### Integration tests

**`WithHalApi` wiring** -- full DI container integration:
- Mock `HttpClient` returning HAL JSON
- Verify `HalMcpServerOptions` is registered as singleton
- Verify `navigate_to_root` is in tool collection after startup
- Verify root's links appear as tools after startup
- Verify startup failure is handled gracefully (no throw)
- Verify `ArgumentException` on missing `RootUri`

**End-to-end traversal** -- multi-step navigation:
- Start at root -> navigate to sub-resource -> navigate to child -> return to root
- Verify tool collection updates correctly at each step
- Verify content returned at each step matches the fetched resource

### Logging

- Verify `HalMcpStartupService` emits `Information` with tool count on successful startup
- Verify `HalMcpStartupService` emits `Warning` (not `Error`) when root fetch fails on startup
- Verify `HalNavigationTool.InvokeAsync` emits `Debug` before fetch, `Warning` on HTTP error, `Warning` on non-HAL response, `Error` on exception
- Verify `NavigateToRootTool.InvokeAsync` follows the same pattern
- Verify Trace-level tool-name loop does not execute when Trace logging is disabled (confirms `IsEnabled` guard works)
- Verify no log calls throw when a `NullLogger<T>` is used
- Use `Microsoft.Extensions.Logging.Testing.FakeLogger<T>` or a mock `ILogger<T>` to assert log level, event ID, and message content

### Async conventions

- Verify `CancellationToken` is passed through to `IHalClient.GetAsync` in `HalNavigationTool.InvokeAsync` (mock `IHalClient` asserts token received)
- Verify `CancellationToken` is passed through to `IHalClient.GetAsync` in `NavigateToRootTool.InvokeAsync`
- Verify `CancellationToken` is passed through to `IHalClient.GetAsync` in `HalMcpStartupService.StartAsync`
- Verify cancellation is honored: when a pre-cancelled token is supplied, the operation throws `OperationCanceledException` without making an HTTP request
