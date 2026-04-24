# Chatter.Rest.Hal.Mcp -- Requirements

This document is the source of truth for what the `Chatter.Rest.Hal.Mcp` package does. Test scenarios are derived directly from the numbered requirements below. For how the system is built, see [architecture.md](architecture.md).

---

## Overview

`Chatter.Rest.Hal.Mcp` is a new NuGet package that bridges HAL HATEOAS APIs to MCP (Model Context Protocol) tool surfaces. It allows AI agents (Claude, Copilot, etc.) to autonomously navigate HAL APIs by discovering `_links` as MCP tools at runtime -- with zero endpoint pre-configuration.

HAL's `_links` collection and MCP tools are structurally isomorphic:

| HAL concept | MCP concept |
|---|---|
| `_links` collection | Available tools |
| Link relation (`rel`) | Tool name |
| `href` | Tool action / URI |
| Templated `href` + variables | Tool input schema |
| `title` | Tool description |
| `self` | Current resource URI |
| `_embedded` | Inline resource content |

The package converts these HAL structures into live MCP tools, updating the tool collection dynamically as the agent traverses from resource to resource.

### Package dependencies

- `Chatter.Rest.Hal.Client` (IDEA-04 in [docs/backlog.md](../backlog.md)) -- provides `GetHalAsync` HTTP helpers
- `ModelContextProtocol` -- Microsoft's official C# MCP SDK
- `Chatter.Rest.Hal` -- core HAL types (`Resource`, `LinkObject`, `LinkCollection`, etc.)

### Prerequisite

IDEA-04 (`Chatter.Rest.Hal.Client`) must be implemented before this package. It provides the HTTP client extensions (`GetHalAsync`) that this package uses to fetch and parse HAL resources. See [docs/backlog.md](../backlog.md) for IDEA-04 specification.

---

## Personas

### API owner

Has an existing HAL API and wants to expose it to AI agents with minimal code. Expects a one-liner integration (`WithHalApi(...)`) that makes the API navigable by any MCP-compatible agent without writing tool definitions by hand.

### AI application developer

Building an agent that needs to navigate a HAL API without hard-coding endpoints. Expects the agent to start at the root resource, discover available actions as tools, invoke them, and continue traversing -- all driven by the HAL `_links` the API already provides.

### Library consumer

Already uses `Chatter.Rest.Hal` for building or consuming HAL documents. Wants to add AI agent navigation to an existing application with minimal new dependencies and no changes to existing HAL resource construction.

---

## Functional Requirements

### Startup behavior

**REQ-01:** On server startup, the package fetches the configured root URI and converts its `_links` to MCP tools.

**REQ-02:** A persistent `navigate_to_root` tool is always registered and is never replaced during traversal. It must be present in the tool collection at all times, including before the initial root fetch completes.

**REQ-03:** If the root resource fetch fails on startup (network error, non-HAL response, non-success status code), startup does not throw. The error is captured and logged. The `navigate_to_root` tool remains available so the agent can retry root discovery.

### Tool naming

**REQ-04:** Each `LinkObject` in a resource's `_links` becomes one `McpServerTool` instance. For rels with multiple `LinkObject` entries (array relations), only the first `LinkObject` is converted to a tool in v1.

**REQ-05:** Tool name is derived as `{sanitized_self_href}__{rel}` where sanitization applies the following transforms to the self href in order:
1. Strip leading `/`
2. Replace `/` with `_`
3. Replace `{` with `_`
4. Replace `}` with `_`
5. Replace `-` with `_`
6. Lowercase the result
7. Map empty string (from root `/`) to `root`

Examples:

| self href | rel | tool name |
|---|---|---|
| `/` | `orders` | `root__orders` |
| `/orders/42` | `cancel` | `orders_42__cancel` |
| `/api/v1/customers` | `next` | `api_v1_customers__next` |
| `/orders/{id}` | `self` | `orders__id___self` |
| `/user-profiles` | `search` | `user_profiles__search` |

**REQ-06:** Tool description is `LinkObject.Title` when present and non-empty; otherwise `"Navigate to {rel}"`.

**REQ-07:** Tool input schema contains one `string` parameter per URI template variable in the link's `Href` (discovered via `LinkObject.GetTemplateVariables()`). Non-templated links have an empty input schema. For example, a link with `href="/orders/{id}"` and `templated=true` produces an input schema with a single `id` property of type `string`.

### Tool invocation

**REQ-08:** When a tool is called, the package resolves the templated href using `LinkObject.Expand(IDictionary<string, string>)` with the agent-supplied arguments. For non-templated links, `Href` is used directly.

**REQ-09:** The resolved URI is fetched via HTTP GET using the configured `HttpClient` (through `Chatter.Rest.Hal.Client`'s `GetHalAsync`).

**REQ-10:** On a successful HAL response, the tool collection is replaced with the new resource's `_links` converted to tools. The `navigate_to_root` tool is preserved across all replacements.

**REQ-11:** Replacing the tool collection triggers the SDK's `tools/list_changed` notification automatically. The package mutates `McpServerPrimitiveCollection<McpServerTool>` (from `McpServerOptions.ToolCollection`); the SDK fires the notification when the collection's `Changed` event triggers. No manual notification code is required.

**REQ-12:** The HAL resource JSON (including `_embedded` if present) is returned as text content in the `CallToolResult`. The content is the full serialized HAL JSON of the fetched resource.

**REQ-13:** The `navigate_to_root` tool fetches the configured root URI, replaces the tool collection with the root resource's links (preserving itself), and returns the root resource as text content in the `CallToolResult`.

### Error handling

**REQ-14:** If the HTTP response is not a valid HAL resource (response parses to `null`), the tool returns `CallToolResult { IsError = true }` with a descriptive message including the resolved URI. The tool does not throw.

**REQ-15:** If the HTTP request fails (non-success status code), the tool returns `CallToolResult { IsError = true }` with the HTTP status code, reason phrase, and resolved URI. The tool does not throw.

**REQ-16:** If an exception occurs during tool invocation (network failure, deserialization error, etc.), the tool returns `CallToolResult { IsError = true }` with the exception message. The tool does not throw.

### Configuration

**REQ-17:** `ExcludeRels` option -- a set of rel names that are not converted to tools. Default value is `["curies"]`.

**REQ-18:** The `self` rel is included by default (agent can re-fetch the current resource). Users may add `"self"` to `ExcludeRels` to suppress it.

**REQ-19:** `RootUri` is required. If `RootUri` is null, empty, or whitespace at configuration time, the package must throw `ArgumentException` during service registration to produce a clear startup failure.

### Scope constraints (v1)

**REQ-20:** Only HTTP GET is supported. No POST, PUT, or DELETE requests are made by any tool.

**REQ-21:** Only stdio MCP transport is supported. HTTP transport (which requires per-session tool collections) is out of scope for v1.

**REQ-22:** CURIE expansion is not performed. CURIE rels appear as their compact form. CURIEs are excluded by default via REQ-17's default `ExcludeRels` containing `"curies"`.

**REQ-23:** `MaxDepth` traversal limiting is not implemented. The agent may traverse indefinitely.

### Logging

**REQ-24:** `HalMcpStartupService`, `HalNavigationTool`, and `NavigateToRootTool` each accept `ILogger<T>` via constructor injection. `HalToolCollectionManager` is an internal static helper and does not hold an `ILogger`; logging for tool-swap operations is performed at the call site (the tool or startup service that calls `SwapTools`), which has richer context about the triggering event.

**REQ-25:** All log messages use structured logging with named placeholders (e.g., `{ToolName}`, `{Href}`, `{StatusCode}`, `{ToolCount}`, `{RootUri}`, `{Rel}`) and never string concatenation or string interpolation in log calls.

**REQ-26:** At `Information` level, `HalMcpStartupService` logs one message on successful root fetch and tool registration, including the number of tools registered and the root URI. This is the only `Information`-level log in the package.

**REQ-27:** At `Warning` level, `HalMcpStartupService` logs when the root fetch fails on startup, including the exception and the root URI. Startup does not throw (per REQ-03).

**REQ-28:** At `Debug` level, `HalNavigationTool.InvokeAsync` logs the tool name and resolved href before each HTTP fetch, and the response status code after receiving the response.

**REQ-29:** At `Warning` level, `HalNavigationTool.InvokeAsync` logs when the HTTP response is a non-success status code, including the status code and resolved href.

**REQ-30:** At `Warning` level, `HalNavigationTool.InvokeAsync` logs when the response is not a valid HAL resource, including the resolved href.

**REQ-31:** At `Error` level, `HalNavigationTool.InvokeAsync` logs unexpected exceptions during tool invocation, including the exception and the tool name.

**REQ-32:** `NavigateToRootTool.InvokeAsync` follows the same logging pattern as `HalNavigationTool` (REQ-28 through REQ-31) using its own `ILogger<NavigateToRootTool>`.

**REQ-33:** At `Debug` level, after `SwapTools` completes, the calling tool or startup service logs the number of tools removed and the number of tools added. At `Trace` level, each individual tool name added to the collection is logged inside an `IsEnabled(LogLevel.Trace)` guard to avoid iteration overhead when `Trace` is not enabled.

**REQ-34:** At `Debug` level, `HalMcpStartupService.StartAsync` logs the configured `RootUri` before fetching. Because `WithHalApi` runs during service registration before the DI container is built, the configured-root-uri log is deferred to `HalMcpStartupService.StartAsync` rather than emitted from `WithHalApi` directly. If `RootUri` validation fails in `WithHalApi`, the `ArgumentException` is thrown without logging — the exception message is descriptive and the caller's exception handler is the correct place to log it.

**REQ-35:** All `Debug`-level log points use `LoggerMessage` source generators to avoid string allocation when `Debug` logging is not enabled. All `Trace`-level log points that iterate collections use explicit `IsEnabled(LogLevel.Trace)` guards before the loop. No `Information`-level logging occurs on hot paths (only on startup).

**REQ-36:** Request bodies, response bodies, auth headers, and API keys must never appear in log messages at any log level. Tool names and hrefs are safe to log.

---

## Integration Story

The following shows what a user writes in their `Program.cs` to expose a HAL API as an MCP server:

```csharp
// User's dotnet new mcpserver project:
builder.Services.AddHttpClient("hal", c =>
    c.BaseAddress = new Uri("https://api.example.com/"));

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithHalApi(options =>
    {
        options.RootUri = "https://api.example.com/";
        options.ExcludeRels = ["curies", "self"];  // override default
    });
```

Key points:
- `WithHalApi` is an extension method on `IMcpServerBuilder`, following the same pattern as `WithStdioServerTransport()` and `WithToolsFromAssembly()`
- `HttpClient` is resolved from DI (standard `IHttpClientFactory` pattern)
- `HalMcpServerOptions` is configured via the standard `Action<T>` options pattern
- The user does not register individual tools -- all tools are discovered dynamically from HAL `_links`

---

## Behavioral Scenarios

These scenarios describe end-to-end behavior for test derivation.

### Scenario: Agent discovers root API

1. MCP server starts with `RootUri = "https://api.example.com/"`
2. Startup fetches `GET https://api.example.com/`
3. Response is a HAL resource with `_links: { self: { href: "/" }, orders: { href: "/orders" }, customers: { href: "/customers" } }`
4. Tool collection contains: `navigate_to_root`, `root__self`, `root__orders`, `root__customers`
5. Each tool has the correct description and empty input schema (no template variables)

### Scenario: Agent navigates to a sub-resource

1. Agent calls tool `root__orders`
2. Package fetches `GET /orders`
3. Response has `_links: { self: { href: "/orders" }, find: { href: "/orders/{id}", templated: true }, next: { href: "/orders?page=2" } }`
4. Tool collection is replaced: `navigate_to_root`, `orders__self`, `orders__find`, `orders__next`
5. `orders__find` has input schema `{ id: string }`
6. `CallToolResult` content is the full HAL JSON of the orders resource

### Scenario: Agent uses a templated link

1. Agent calls tool `orders__find` with `{ "id": "42" }`
2. Package calls `LinkObject.Expand({ "id": "42" })` which resolves to `/orders/42`
3. Package fetches `GET /orders/42`
4. Response has `_links: { self: { href: "/orders/42" }, cancel: { href: "/orders/42/cancel" } }`
5. Tool collection is replaced: `navigate_to_root`, `orders_42__self`, `orders_42__cancel`
6. `CallToolResult` content is the full HAL JSON of order 42

### Scenario: Agent returns to root

1. From any resource, agent calls `navigate_to_root`
2. Package fetches `GET https://api.example.com/`
3. Tool collection is reset to root's links plus `navigate_to_root`
4. `CallToolResult` content is the root resource HAL JSON

### Scenario: HTTP error during navigation

1. Agent calls a tool that resolves to `/orders/999`
2. Server responds with `404 Not Found`
3. Tool returns `CallToolResult { IsError = true }` with message `"HTTP 404 Not Found: /orders/999"`
4. Tool collection is not modified (previous tools remain)

### Scenario: Non-HAL response

1. Agent calls a tool that resolves to `/health`
2. Server responds with `200 OK` but body is plain text, not HAL JSON
3. `GetHalAsync` returns `null`
4. Tool returns `CallToolResult { IsError = true }` with message `"Response from /health was not a valid HAL resource"`
5. Tool collection is not modified

### Scenario: Startup failure

1. MCP server starts with `RootUri = "https://api.example.com/"`
2. Root fetch throws `HttpRequestException` (server unreachable)
3. Startup catches the exception and logs a warning
4. Tool collection contains only `navigate_to_root`
5. Agent can call `navigate_to_root` to retry

### Scenario: Excluded rels

1. Root resource has `_links: { self: { href: "/" }, curies: [...], orders: { href: "/orders" } }`
2. Default `ExcludeRels` is `["curies"]`
3. Tool collection contains: `navigate_to_root`, `root__self`, `root__orders`
4. No tool is created for the `curies` rel

---

## Out of Scope (v1)

The following capabilities are explicitly deferred and must not be implemented in the initial version:

- **HTTP method inference** -- all traversal is GET; no POST/PUT/DELETE
- **HTTP transport** -- per-session tool collections require HTTP transport support in the SDK; stdio only for v1
- **CURIE expansion** -- CURIE rels are excluded, not expanded
- **HAL-FORMS support** -- form-based actions are not converted to tools
- **MaxDepth traversal cap** -- no limit on how deep the agent can navigate
- **Tool list union mode** -- no accumulation of tools across traversals; each navigation replaces the tool set
- **Typed input schemas beyond `string`** -- all template variables are `string` parameters
- **Array relation expansion** -- rels with multiple `LinkObject` entries use only the first entry (REQ-04)
