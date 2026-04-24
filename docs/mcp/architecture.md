# Chatter.Rest.Hal.Mcp -- Technical Architecture

This document is the source of truth for how the `Chatter.Rest.Hal.Mcp` package is built. It guides implementation decisions with enough detail for a developer to implement each type without ambiguity. For what the system does, see [requirements.md](requirements.md).

---

## Prerequisites

IDEA-04 (`Chatter.Rest.Hal.Client`) must be implemented before this package. It provides `HttpClient` extension methods (notably `GetHalAsync`) that this package uses to fetch and parse HAL resources over HTTP. See [docs/backlog.md](../backlog.md) for the IDEA-04 specification.

---

## Package Dependency Graph

```
Chatter.Rest.Hal.Mcp
  -> Chatter.Rest.Hal.Client   (IDEA-04; provides GetHalAsync)
  -> ModelContextProtocol       (Microsoft official C# MCP SDK)
      -> Chatter.Rest.Hal       (core types: Resource, LinkObject, LinkCollection, Link)
```

`Chatter.Rest.Hal.Client` itself depends on `Chatter.Rest.Hal` and `System.Net.Http`. The MCP package does not reference `System.Net.Http` directly -- all HTTP access flows through `Chatter.Rest.Hal.Client`.

---

## SDK Alignment

The package integrates with `ModelContextProtocol` (Microsoft's official C# MCP SDK) using the same patterns as the `dotnet new mcpserver` template:

- **Tool registration:** Tools are registered via an `IMcpServerBuilder` extension method (`WithHalApi`), following the same DI seam as `WithToolsFromAssembly()`.
- **Tool base class:** All tool instances (both HAL-derived and static) extend `McpServerTool` abstract base.
- **Dynamic tool collection:** `McpServerPrimitiveCollection<McpServerTool>` (accessed via `McpServerOptions.ToolCollection`) is mutated at runtime. The SDK auto-fires `tools/list_changed` when the collection's `Changed` event triggers.
- **No custom handlers:** No custom `ListToolsHandler` or `CallToolHandler` is needed. Standard collection mutation and `McpServerTool.InvokeAsync` dispatch suffice.

---

## Project Location

- Source: `src/Chatter.Rest.Hal.Mcp/`
- Tests: `test/Chatter.Rest.Hal.Mcp.Tests/`

---

## Namespaces

| Namespace | Visibility | Contents |
|---|---|---|
| `Chatter.Rest.Hal.Mcp` | Public | `HalMcpServerOptions`, `HalNavigationTool`, `NavigateToRootTool`, `HalMcpServerBuilderExtensions` |
| `Chatter.Rest.Hal.Mcp.Internal` | Internal | `HalToolCollectionManager`, `ToolNaming` |

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
        HttpClient httpClient,
        HalMcpServerOptions halOptions,
        McpServerOptions mcpOptions);
}
```

**Fields stored:**
- `_rel` -- the link relation name
- `_link` -- the `LinkObject` instance
- `_selfHref` -- the self href of the resource this link came from (used for tool naming)
- `_httpClient` -- the `HttpClient` for fetching
- `_halOptions` -- configuration
- `_mcpOptions` -- access to `McpServerOptions.ToolCollection`

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
    public NavigateToRootTool(
        HttpClient httpClient,
        HalMcpServerOptions halOptions,
        McpServerOptions mcpOptions);
}
```

**`ProtocolTool` property:**
- `Name` = `"navigate_to_root"`
- `Description` = `"Return to the API root and rediscover available actions"`
- `InputSchema` = `{ "type": "object", "properties": {} }` (no input parameters)

**`InvokeAsync`:**
1. Fetch `halOptions.RootUri` via `httpClient.GetHalAsync(halOptions.RootUri, cancellationToken: cancellationToken)`
2. If response is `null`: return `CallToolResult { IsError = true }` with message `$"Response from {halOptions.RootUri} was not a valid HAL resource"`
3. Call `HalToolCollectionManager.SwapTools(mcpOptions.ToolCollection, response, halOptions.RootUri, halOptions, httpClient, mcpOptions)`
4. Serialize response to HAL JSON
5. Return `CallToolResult { Content = [new TextContentBlock { Text = json }] }`
6. Exception catch-all: return `CallToolResult { IsError = true, Content = [ex.Message] }`

---

### `HalToolCollectionManager` (internal static)

Central logic for replacing the tool collection with tools derived from a HAL resource's links.

```csharp
namespace Chatter.Rest.Hal.Mcp.Internal;

internal static class HalToolCollectionManager
{
    public static void SwapTools(
        McpServerPrimitiveCollection<McpServerTool> collection,
        Resource resource,
        string selfHref,
        HalMcpServerOptions halOptions,
        HttpClient httpClient,
        McpServerOptions mcpOptions);
}
```

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
        McpServerOptions mcpOptions,
        HttpClient httpClient,
        HalMcpServerOptions halOptions);

    public Task StartAsync(CancellationToken cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken);
}
```

**`StartAsync`:**
1. Fetch `halOptions.RootUri` via `httpClient.GetHalAsync(halOptions.RootUri, cancellationToken: cancellationToken)`
2. If successful: call `HalToolCollectionManager.SwapTools(...)` to populate tool collection with root's links
3. If fetch fails (any exception or null response): catch, log warning, do not throw. The `navigate_to_root` tool (already in the collection from registration) remains available for the agent to retry.

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
4. Create `NavigateToRootTool` singleton and add it to `McpServerOptions.ToolCollection`
5. Register `HalMcpStartupService` as `IHostedService`
6. Return `builder` for chaining

---

## Tool Naming Algorithm

```
ToolNaming.CreateToolName(selfHref, rel):
    segment = selfHref
        .TrimStart('/')
        .Replace each `{varname}` token with just `varname` (strip braces, keep inner name)
        .Replace('/', '_')
        .Replace('-', '_')
        .ToLowerInvariant()
    prefix = segment == "" ? "root" : segment
    return $"{prefix}__{rel}"
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

> **Note:** Template variable tokens (`{varname}`) are replaced as a unit — the inner name is preserved and the braces are dropped. Replacing `{` and `}` individually with `_` would produce trailing underscores in the sanitized prefix (e.g. `orders__id_`) and triple-underscore separators in the final tool name (`orders__id___rel`).

The double-underscore `__` separator between prefix and rel is intentional. Single underscores appear within the sanitized href segments, so the double underscore provides unambiguous parsing of prefix vs. rel.

---

## Tool Collection Swap Algorithm

```
SwapTools(collection, resource, selfHref, halOptions, httpClient, mcpOptions):
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
            if link.Rel in halOptions.ExcludeRels:
                continue
            linkObject = link.LinkObjects[0]  // first LinkObject only (v1)
            collection.Add(new HalNavigationTool(
                link.Rel, linkObject, effectiveSelf,
                httpClient, halOptions, mcpOptions))

    // 5. collection.Changed fires automatically
    //    -> SDK sends tools/list_changed notification
```

**Key behaviors:**
- `navigate_to_root` is always preserved (REQ-02)
- Self href is resolved from the resource's own `self` link when available, falling back to the provided `selfHref` parameter
- Only the first `LinkObject` per rel is used (REQ-04); array relations are not expanded in v1
- Rels in `ExcludeRels` are skipped (REQ-17)
- The `self` rel is included by default unless explicitly excluded (REQ-18)

---

## InvokeAsync Flow (HalNavigationTool)

```
InvokeAsync(request, cancellationToken):
    try:
        // 1. Extract arguments
        args = request.Params?.Arguments as IDictionary<string, string>
            ?? empty dictionary

        // 2. Resolve href
        if _link.Templated == true:
            resolvedHref = _link.Expand(args)
        else:
            resolvedHref = _link.Href

        // 3. Fetch resource
        response = await _httpClient.GetHalAsync(resolvedHref, cancellationToken: cancellationToken)

        // 4. Handle non-HAL response
        if response is null:
            return CallToolResult
            {
                IsError = true,
                Content = [TextContentBlock("Response from {resolvedHref} was not a valid HAL resource")]
            }

        // 5. Swap tool collection with new resource's links
        HalToolCollectionManager.SwapTools(
            _mcpOptions.ToolCollection, response, resolvedHref,
            _halOptions, _httpClient, _mcpOptions)

        // 6. Serialize response to HAL JSON
        json = JsonSerializer.Serialize(response, halJsonSerializerOptions)

        // 7. Return content
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
| Response is not valid HAL | `IsError = true`, no collection change | `"Response from {resolvedHref} was not a valid HAL resource"` |
| Exception during invocation | `IsError = true`, no collection change | `ex.Message` |
| Startup root fetch failure | Logged warning, no throw | `navigate_to_root` remains; agent can retry |

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

## Serialization

When returning HAL resource content in `CallToolResult`, the resource is serialized using `System.Text.Json.JsonSerializer.Serialize<Resource>()` with HAL converters applied (via `JsonSerializerOptions.AddHalConverters()`). This produces standard HAL JSON including `_links`, `_embedded`, and state properties.

---

## Async Conventions

### Async-only I/O (REQ-37)

All I/O in the package is async end-to-end. No sync-over-async wrappers (`.Result`, `.GetAwaiter().GetResult()`, `.Wait()`) or blocking calls (`Thread.Sleep`) appear anywhere. `HalMcpStartupService.StartAsync`, `HalNavigationTool.InvokeAsync`, and `NavigateToRootTool.InvokeAsync` are all inherently async and delegate to async `HttpClient` extension methods.

---

### CancellationToken threading (REQ-38)

Every async call site passes the `CancellationToken` received from the framework:
- `HalMcpStartupService.StartAsync` receives `CancellationToken` from `IHostedService.StartAsync` and passes it to `GetHalAsync`
- `HalNavigationTool.InvokeAsync` receives `CancellationToken` from `McpServerTool.InvokeAsync(RequestContext, CancellationToken)` and passes it to `GetHalAsync`
- `NavigateToRootTool.InvokeAsync` receives `CancellationToken` from the same `McpServerTool` contract and passes it to `GetHalAsync`

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
        McpServerOptions mcpOptions,
        HttpClient httpClient,
        HalMcpServerOptions halOptions,
        ILogger<HalMcpStartupService> logger);
}

public sealed class HalNavigationTool : McpServerTool
{
    public HalNavigationTool(
        string rel,
        LinkObject link,
        string selfHref,
        HttpClient httpClient,
        HalMcpServerOptions halOptions,
        McpServerOptions mcpOptions,
        ILogger<HalNavigationTool> logger);
}

public sealed class NavigateToRootTool : McpServerTool
{
    public NavigateToRootTool(
        HttpClient httpClient,
        HalMcpServerOptions halOptions,
        McpServerOptions mcpOptions,
        ILogger<NavigateToRootTool> logger);
}
```

**`HalToolCollectionManager` (internal static):** Does not accept `ILogger`. It is a pure collection-mutation helper. All logging for tool-swap operations is performed at the call site (the tool or startup service that calls `SwapTools`), which has richer context (which tool triggered the swap, the root URI, the current navigation href). Injecting a logger into a static helper would make it impure and reduce testability without improving log quality.

**`HalMcpServerBuilderExtensions.WithHalApi`:** Runs during service registration before the DI container is built, so `ILogger` is unavailable at this stage. The configured-root-uri `Debug` log is deferred to `HalMcpStartupService.StartAsync` instead. If `RootUri` validation fails, `ArgumentException` is thrown without logging — the exception message is descriptive, and the caller's exception handler is the appropriate place to log it.

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
| `LogRootNonHalResponse` | Warning | 3 | `"Root resource at {RootUri} was not a valid HAL resource"` | `RootUri` |
| `LogRootException` | Error | 4 | `"Root navigation failed"` | (exception passed as `Exception` arg) |
| `LogRootToolsSwapped` | Debug | 5 | `"Root tool collection updated: {RemovedCount} removed, {AddedCount} added"` | `RemovedCount`, `AddedCount` |
| `LogRootToolAdded` | Trace | 6 | `"Tool added: {AddedToolName}"` | `AddedToolName` |

---

### SwapTools Call-Site Logging Pattern

The calling tool or startup service logs before and after calling `SwapTools`. The Trace-level tool-name loop uses an explicit `IsEnabled` guard (REQ-33):

```
var previousCount = _mcpOptions.ToolCollection.Count;
HalToolCollectionManager.SwapTools(
    _mcpOptions.ToolCollection, response, resolvedHref,
    _halOptions, _httpClient, _mcpOptions);
var addedCount = _mcpOptions.ToolCollection.Count - 1; // minus navigate_to_root
LogToolsSwapped(previousCount - 1, addedCount);

if (_logger.IsEnabled(LogLevel.Trace))
    foreach (var tool in _mcpOptions.ToolCollection)
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

- Verify `CancellationToken` is passed through to `GetHalAsync` in `HalNavigationTool.InvokeAsync` (mock `HttpClient` asserts token received)
- Verify `CancellationToken` is passed through to `GetHalAsync` in `NavigateToRootTool.InvokeAsync`
- Verify `CancellationToken` is passed through to `GetHalAsync` in `HalMcpStartupService.StartAsync`
- Verify cancellation is honored: when a pre-cancelled token is supplied, the operation throws `OperationCanceledException` without making an HTTP request
