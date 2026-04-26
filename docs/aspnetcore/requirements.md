# Chatter.Rest.Hal.AspNetCore -- Requirements

This document is the source of truth for what the `Chatter.Rest.Hal.AspNetCore` package does. Test scenarios are derived directly from the numbered requirements below. For how the system is built, see [architecture.md](architecture.md).

---

## Overview

`Chatter.Rest.Hal.AspNetCore` is a new NuGet package that provides server-side ASP.NET Core integration for building HAL (Hypertext Application Language) APIs. It eliminates the manual wiring required to produce HAL responses from both MVC controllers and Minimal API endpoints, providing DI-friendly registration, a typed result type (`HalResult`) that works in both pipelines, a DI-injectable link builder (`IHalLinkBuilder`) backed by ASP.NET Core's `LinkGenerator`, opt-in auto-self link injection, and RFC 9457 Problem Details support via `IExceptionHandler`.

### Package dependencies

- `Chatter.Rest.Hal` -- core HAL types (`Resource`, `Resource<T>`, `LinkObject`, `LinkCollection`, `Link`)
- `Microsoft.AspNetCore.App` -- ASP.NET Core framework reference (not a NuGet package; framework reference only)

---

## Personas

### API developer

Building HAL APIs in ASP.NET Core using either MVC controllers or Minimal APIs. Wants DI-friendly registration, typed HAL responses, and link generation without writing HTTP boilerplate or manual JSON wiring.

### Minimal API developer

Prefers `IResult`-based endpoints over `IActionResult`. Wants `HalResults` factory methods that work identically to the built-in `Results` static class and a link builder injectable directly into endpoint delegates.

### Existing Chatter.Rest.Hal user

Already uses `Chatter.Rest.Hal` server-side for building HAL documents. Wants to add ASP.NET Core integration to the same app with a familiar API surface and a single `using Chatter.Rest.Hal.AspNetCore;` statement.

---

## Functional Requirements

### Registration

**REQ-01:** `AddHal(Action<HalOptions>)` is an extension method on `IServiceCollection` that registers: `IHalLinkBuilder` / `HalLinkBuilder` as a singleton, `IOptions<HalOptions>`, HAL JSON converters into `HttpJsonOptions` for the Minimal API pipeline, and (when MVC is present) HAL JSON converters and `application/hal+json` media type support into MVC `JsonOptions` and `MvcOptions`.

**REQ-02:** `AddHal(Action<HalOptions>)` is an extension method on `IMvcBuilder` that delegates to `services.AddHal()` and ensures MVC-specific wiring. The final DI state is identical to calling `services.AddHal()` when MVC is present.

**REQ-03:** Both `AddHal` overloads are idempotent. Multiple calls register services exactly once. A private sentinel service type (`HalRegistrationMarker`) is used to detect prior registration.

**REQ-04:** `UseHal()` is an extension method on `IApplicationBuilder` / `WebApplication` that registers the exception-handling middleware pipeline. It is required when `HalOptions.UseProblemDetails` is `true`. It is safe to call when `false` (no-op for error handling).

**REQ-05:** All global configuration is accessed via `IOptions<HalOptions>`. No static mutable state exists. Every component resolves `IOptions<HalOptions>` at runtime via dependency injection.

### HalOptions

**REQ-06:** `HalOptions.AutoSelfLink` is a `bool` property with default `false`. When `true`, `HalResult.CoreWriteAsync` injects a `"self"` link for all HAL responses that lack one.

**REQ-07:** `HalOptions.UseProblemDetails` is a `bool` property with default `false`. `AddHal` always registers `HalExceptionHandler` as an `IExceptionHandler` implementation because options values are not available at service registration time. `HalExceptionHandler.TryHandleAsync` checks `UseProblemDetails` at runtime and returns `false` (passes through) when `false`.

**REQ-08:** `HalOptions.MediaType` is a `string` property with default `"application/hal+json"`. This value is used as the `Content-Type` on all `HalResult` responses.

**REQ-09:** `HalOptions.MapException<TException>(Func<TException, HalProblem>)` registers a custom exception-to-problem mapping. Multiple mappings are supported. When an exception is handled, the most-derived registered type wins.

**REQ-10:** `HalOptions` supports direct construction without DI (all properties have safe defaults) and resolution via `IOptions<HalOptions>` for DI scenarios. No property requires DI to be initialized.

### IHalLinkBuilder

**REQ-11:** `IHalLinkBuilder.For(string routeName, object? routeValues = null)` returns a `LinkObject` with `Href` resolved to a concrete URI via ASP.NET Core's `LinkGenerator`. Throws `InvalidOperationException` when the route name cannot be resolved.

**REQ-12:** `IHalLinkBuilder.Template(string routeName)` returns a `LinkObject` with `Href` set to the route URI pattern (e.g., `/orders/{id}`) and `Templated = true`.

**REQ-13:** Extension methods `For<TController>(Expression<Action<TController>>)`, `For<TController>(Expression<Func<TController, IActionResult>>)`, and `For<TController>(Expression<Func<TController, Task<IActionResult>>>)` on `IHalLinkBuilder` resolve the route name from the `Name` property of `[HttpGet]` / `[HttpPost]` / `[HttpPut]` / `[HttpPatch]` / `[HttpDelete]` / `[Route]` attributes on the target action, extract route values from the expression's arguments, and delegate to `For(routeName, routeValues)`. The `Task<IActionResult>` overload supports typical `async` controller actions. `[ActionName]` is not used for route name resolution because it does not create a named route for `LinkGenerator.GetPathByName`. These extension methods are in the `Chatter.Rest.Hal.AspNetCore` namespace.

**REQ-14:** `Template<TController>(Expression<Action<TController>>)`, `Template<TController>(Expression<Func<TController, IActionResult>>)`, and `Template<TController>(Expression<Func<TController, Task<IActionResult>>>)` extension methods on `IHalLinkBuilder` resolve the route name from action attributes using the same logic as REQ-13, then delegate to `Template(routeName)`. These overloads mirror the three `For<TController>` overloads defined in REQ-13. Same namespace as REQ-13.

**REQ-15:** `IHalLinkBuilder` is registered as a singleton in the DI container. It is safe to resolve at any DI lifetime scope.

### HalResult

**REQ-16:** `HalResult` implements both `IActionResult` (for MVC controllers) and `IResult` (for Minimal APIs). It is a single type that works in both pipelines without specialization.

**REQ-17:** `HalResult` serializes `Resource` directly via `response.WriteAsJsonAsync`. It does not rely on MVC output formatters as its primary serialization path.

**REQ-18:** `HalResult` sets the response `Content-Type` to `HalOptions.MediaType` and the HTTP status code from its constructor arguments. It resolves HAL-aware `JsonSerializerOptions` from the DI container at write time.

### HalResults factory

**REQ-19:** `HalResults.Ok(Resource)` and `HalResults.Ok<T>(T state, Func<T, IHalLinkBuilder, Resource> builder)` return a `HalResult` with HTTP 200. The generic overload stores the state and builder delegate for deferred execution; `IHalLinkBuilder` is resolved from the request service provider during `HalResult.ExecuteAsync` / `ExecuteResultAsync` when the `HttpContext` is available.

**REQ-20:** `HalResults.Created(string uri, Resource)` and `HalResults.Accepted(Resource)` return a `HalResult` with HTTP 201 and 202 respectively. `HalResults.NoContent()` returns an `IResult` with HTTP 204 and no response body; it does not produce a `HalResult` because 204 responses must not include a body.

**REQ-21:** `HalResults.NotFound(string title, string? detail)`, `HalResults.ValidationProblem(IDictionary<string, string[]> errors)`, and `HalResults.Problem(int statusCode, string title, string? detail)` return a problem result with `Content-Type: application/problem+json` (RFC 9457).

### HalControllerBase and controller extensions

**REQ-22:** `HalOk`, `HalCreated`, `HalAccepted`, `HalNoContent`, `HalNotFound`, `HalValidationProblem`, and `HalProblem` are extension methods on `ControllerBase`. No inheritance from `HalControllerBase` is required to use them.

**REQ-23:** `HalControllerBase` is an optional abstract class that inherits `ControllerBase`. It re-exposes the factory methods from REQ-22 as `protected` members. It adds no behavior beyond the extension methods.

**REQ-24:** `HalOk<T>(T state, Func<T, IHalLinkBuilder, Resource> builder)` resolves `IHalLinkBuilder` from `HttpContext.RequestServices`, invokes `builder(state, linkBuilder)` immediately to produce a `Resource`, and passes the result to `HalResult(resource, 200)` using the immediate constructor. Unlike `HalResults.Ok<T>`, builder execution is not deferred because `HttpContext` is available at the call site in MVC.

### Auto-self link injection

**REQ-25:** When `HalOptions.AutoSelfLink` is `true`, every `HalResult` response whose `Resource` does not already contain a `"self"` link gets one injected before the response is written to the HTTP stream.

**REQ-26:** The injected `"self"` link is produced by calling `IHalLinkBuilder.For(currentRouteName, currentRouteValues)`, where the route name and values are derived from `HttpContext` endpoint metadata and route data.

**REQ-27:** Injection is skipped when the `Resource` already contains a rel named `"self"`. Existing self links are never overwritten.

**REQ-28:** `WithHalAutoSelf()` on `IEndpointConventionBuilder` forces auto-self injection for a single endpoint regardless of the global setting. `WithoutHalAutoSelf()` suppresses auto-self injection for a single endpoint regardless of the global setting. Both override `HalOptions.AutoSelfLink` at the endpoint level.

**REQ-29:** Auto-self injection for both Minimal API endpoints and MVC controller actions is implemented inside `HalResult.CoreWriteAsync`, which runs before response serialization. `HalResult` reads `IOptions<HalOptions>` and per-endpoint metadata markers (`EnableHalAutoSelf`, `DisableHalAutoSelf`) from `HttpContext` at write time, applying 3-way precedence before calling `WriteAsJsonAsync`.

### Problem Details / RFC 9457

**REQ-30:** `AddHal` always registers `HalExceptionHandler` as an `IExceptionHandler` implementation. `UseHal()` activates the exception-handling middleware pipeline only when `HalOptions.UseProblemDetails` is `true` (resolved via `IOptions<HalOptions>` at middleware build time). `HalExceptionHandler.TryHandleAsync` additionally guards itself with a runtime check on `UseProblemDetails`.

**REQ-31:** `HalExceptionHandler.TryHandleAsync` walks the exception type hierarchy against registered `ExceptionMappings` (most-derived type first). On a match, it writes the mapped `HalProblem` to the response and returns `true`. When no match is found, it writes a generic HTTP 500 problem response.

**REQ-32:** All problem responses use `Content-Type: application/problem+json` as specified by RFC 9457. Problem responses do not include HAL `_links` or `_embedded` properties in v1.

**REQ-33:** `HalProblem` provides static factory methods: `NotFound(string title, string? detail)`, `Conflict(string title, string? detail)`, `ValidationProblem(IDictionary<string, string[]> errors)`, and `Problem(int statusCode, string title, string? detail)`.

**REQ-34:** Custom mappings registered via `HalOptions.MapException<T>` take precedence over built-in defaults. When multiple mappings could match, the most-derived registered exception type wins.

### Serialization

**REQ-35:** `HalResult` writes the response body using `response.WriteAsJsonAsync(resource, jsonOptions)` with HAL-aware `JsonSerializerOptions`. It does not delegate to MVC output formatters for this path.

**REQ-36:** `AddHal` patches `SystemTextJsonOutputFormatter` to add `application/hal+json` to its list of supported media types. This enables content negotiation for controller actions that return a raw `Resource` without wrapping it in a `HalResult`.

**REQ-37:** Content negotiation behavior: a request with `Accept: application/hal+json` receives a response with `Content-Type: application/hal+json` via the patched STJ formatter. A request with `Accept: application/json` receives `Content-Type: application/json` as a fallback. A `HalResult` always produces `Content-Type: application/hal+json` regardless of the `Accept` header value.

---

## Error Handling Summary

| Scenario | Behavior |
|---|---|
| Exception matches `MapException<T>` | Custom problem response via mapped `HalProblem` |
| Exception not matched, `UseProblemDetails = true` | HTTP 500 `application/problem+json` response |
| `LinkGenerator` cannot resolve route name | Throw `InvalidOperationException` |
| Expression route name unresolvable | Throw `InvalidOperationException` |
| `AddHal` called multiple times | Idempotent; services registered exactly once |

---

## Integration Story

The following shows two ways to integrate `Chatter.Rest.Hal.AspNetCore` in a .NET 8 application.

### Option A -- Minimal API

```csharp
// Register HAL services
builder.Services.AddHal(o =>
{
    o.AutoSelfLink = true;
    o.UseProblemDetails = true;
    o.MapException<NotFoundException>(ex => HalProblem.NotFound(ex.Message, null));
});
app.UseHal();

// Endpoint
app.MapGet("/orders/{id}", async (int id, IHalLinkBuilder links, OrderRepo repo) =>
{
    var order = await repo.GetAsync(id);
    return HalResults.Ok(order, (o, lnk) => Resource.Create(o)
        .WithLink("self",   lnk.For("orders:get", new { id }))
        .WithLink("search", lnk.Template("orders:search")));
}).WithName("orders:get");
```

### Option B -- MVC Controllers

```csharp
// Register HAL services alongside MVC
builder.Services.AddControllers().AddHal(o =>
{
    o.AutoSelfLink = true;
    o.UseProblemDetails = true;
});

// Controller
[ApiController, Route("api/orders")]
public class OrdersController(IHalLinkBuilder links) : HalControllerBase
{
    [HttpGet("{id}", Name = "orders:get")]
    public async Task<IActionResult> Get(int id)
    {
        var order = await repo.GetAsync(id);
        if (order is null) return HalNotFound("Order not found", null);
        return HalOk(order, (o, lnk) => Resource.Create(o)
            .WithLink("self",   lnk.For<OrdersController>(c => c.Get(id)))
            .WithLink("cancel", lnk.For<OrdersController>(c => c.Cancel(id))));
    }
}
```

Both options register `IHalLinkBuilder` and `IOptions<HalOptions>` in the DI container. Option A uses Minimal API endpoint delegates. Option B uses MVC controllers with expression-based link generation.

---

## Behavioral Scenarios

These scenarios describe end-to-end behavior for test derivation.

### Scenario: Minimal API returns HAL response

1. `AddHal` registered on `IServiceCollection`
2. Endpoint calls `HalResults.Ok(order, (o, lnk) => Resource.Create(o).WithLink("self", lnk.For("orders:get", new { id })))`
3. `HalResult.ExecuteAsync` sets status 200 and `Content-Type: application/hal+json`
4. Response body is the JSON-serialized `Resource` with `_links`
5. Client receives `200 OK` with `application/hal+json` and correct `_links`

### Scenario: Controller returns HAL response with expression links

1. `AddHal` registered on `IMvcBuilder`
2. Controller action calls `HalOk(order, (o, lnk) => Resource.Create(o).WithLink("self", lnk.For<OrdersController>(c => c.Get(id))))`
3. `For<OrdersController>` resolves route name from `[HttpGet(Name = ...)]` attribute, extracts `id` from expression args
4. `IHalLinkBuilder.For(routeName, routeValues)` resolves concrete href via `LinkGenerator`
5. Response is `200 OK` with `application/hal+json` and correct resolved `_links` hrefs

### Scenario: Auto-self injected when resource has no self link

1. `HalOptions.AutoSelfLink = true`
2. Endpoint returns `HalResult` whose `Resource` has no `"self"` link
3. `HalResult.CoreWriteAsync` detects absence of `"self"` before serializing
4. `HalResult.CoreWriteAsync` calls `IHalLinkBuilder.For(currentRouteName, currentRouteValues)`
5. `"self"` link is added to `Resource` before response is written

### Scenario: Auto-self skipped when resource already has self link

1. `HalOptions.AutoSelfLink = true`
2. Endpoint returns `HalResult` whose `Resource` already has a `"self"` link
3. Filter checks for `"self"` presence and finds it
4. No injection occurs; existing `"self"` link is unchanged in the response

### Scenario: Auto-self suppressed by WithoutHalAutoSelf()

1. `HalOptions.AutoSelfLink = true` globally
2. Endpoint is decorated with `.WithoutHalAutoSelf()`
3. `DisableHalAutoSelf` metadata marker is present on the endpoint
4. Filter reads metadata and skips injection regardless of global setting
5. Response does not contain an injected `"self"` link

### Scenario: Registered exception mapped to problem response

1. `HalOptions.MapException<NotFoundException>(ex => HalProblem.NotFound(ex.Message, null))` registered
2. `HalOptions.UseProblemDetails = true`; `UseHal()` called
3. Endpoint throws `NotFoundException`
4. `HalExceptionHandler.TryHandleAsync` finds mapping for `NotFoundException`
5. Response is `404 Not Found` with `Content-Type: application/problem+json` and RFC 9457 payload

### Scenario: Unregistered exception returns 500 problem response

1. `HalOptions.UseProblemDetails = true`; `UseHal()` called
2. No mapping registered for `InvalidOperationException`
3. Endpoint throws `InvalidOperationException`
4. `HalExceptionHandler.TryHandleAsync` finds no matching mapping
5. Response is `500 Internal Server Error` with `Content-Type: application/problem+json`

### Scenario: Template link builder returns templated LinkObject

1. `IHalLinkBuilder.Template("orders:search")` called
2. `HalLinkBuilder` retrieves the route URI pattern for `"orders:search"` (e.g., `/orders/{query}`)
3. Returns `LinkObject { Href = "/orders/{query}", Templated = true }`
4. Caller includes the link in the `Resource` as an RFC 6570 URI template

### Scenario: Content negotiation with raw Resource return

1. `AddHal` registered on `IMvcBuilder`
2. Controller action returns a raw `Resource` (not wrapped in `HalResult`)
3. Request includes `Accept: application/hal+json`
4. Patched `SystemTextJsonOutputFormatter` matches the media type
5. Response is `200 OK` with `Content-Type: application/hal+json` and HAL-serialized body

### Scenario: Idempotent registration

1. Application startup calls `services.AddHal(...)` twice
2. `HalRegistrationMarker` sentinel detected on second call
3. Second call returns immediately without registering duplicate services
4. DI container contains each service exactly once; no error is thrown

---

## Out of Scope (v1)

The following capabilities are explicitly deferred and must not be implemented in the initial version:

- **HAL-FORMS integration** -- form-based action templates are not interpreted or rendered
- **CURIE expansion in link builder** -- CURIE rels are not expanded; callers use compact form
- **Automatic pagination link injection** -- next/prev/first/last links are not auto-generated
- **Response caching / ETag** -- no HTTP cache or ETag header handling
- **HAL resources in error responses** -- problem responses contain no `_links` or `_embedded`
- **TypedResults integration** -- `TypedResults.Ok<HalResult>` patterns are not supported
- **XML HAL** -- only JSON serialization is supported
- **OpenAPI / Swashbuckle annotation** -- no automatic OpenAPI schema generation for HAL types
