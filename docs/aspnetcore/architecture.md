# Chatter.Rest.Hal.AspNetCore -- Architecture

This document describes how `Chatter.Rest.Hal.AspNetCore` is built. For what it does, see [requirements.md](requirements.md).

---

## Package Dependency Graph

```
Chatter.Rest.Hal.AspNetCore
├── Chatter.Rest.Hal          (core HAL types: Resource, LinkObject, LinkCollection, ...)
└── Microsoft.AspNetCore.App  (framework reference: LinkGenerator, IActionResult, IResult, ...)
```

---

## Project Location

```
src/Chatter.Rest.Hal.AspNetCore/
├── Chatter.Rest.Hal.AspNetCore.csproj
├── HalOptions.cs
├── HalResult.cs
├── HalResults.cs
├── HalControllerBase.cs
├── HalProblem.cs
├── IHalLinkBuilder.cs
├── ServiceCollectionExtensions.cs
├── MvcBuilderExtensions.cs
├── ApplicationBuilderExtensions.cs
├── ControllerBaseExtensions.cs
├── ControllerLinkBuilderExtensions.cs
├── EndpointConventionExtensions.cs
└── Internal/
    ├── HalLinkBuilder.cs
    ├── HalExceptionHandler.cs
    ├── HalFormatterPatcher.cs
    ├── EnableHalAutoSelf.cs
    ├── DisableHalAutoSelf.cs
    └── HalRegistrationMarker.cs
```

---

## Namespaces

| Namespace | Visibility | Contents |
|---|---|---|
| `Chatter.Rest.Hal.AspNetCore` | Public | `HalOptions`, `HalResult`, `HalResults`, `HalControllerBase`, `IHalLinkBuilder`, `HalProblem`, `ServiceCollectionExtensions`, `MvcBuilderExtensions`, `ApplicationBuilderExtensions`, `ControllerBaseExtensions`, `ControllerLinkBuilderExtensions`, `EndpointConventionExtensions` |
| `Chatter.Rest.Hal.AspNetCore.Internal` | Internal | `HalLinkBuilder`, `HalExceptionHandler`, `HalFormatterPatcher`, `EnableHalAutoSelf`, `DisableHalAutoSelf`, `HalRegistrationMarker` |

A single `using Chatter.Rest.Hal.AspNetCore;` exposes all public types (REQ-05, REQ-10, REQ-15).

---

## Key Types

### `HalOptions`

Configuration root for the package. All components resolve `IOptions<HalOptions>` at runtime — no static singletons (REQ-05, REQ-10).

```csharp
public sealed class HalOptions
{
    /// <summary>
    /// When true, auto-self injection filters are globally active. Default: false. (REQ-06)
    /// </summary>
    public bool AutoSelfLink { get; set; } = false;

    /// <summary>
    /// When true, AddHal registers HalExceptionHandler as IExceptionHandler. Default: false. (REQ-07)
    /// </summary>
    public bool UseProblemDetails { get; set; } = false;

    /// <summary>
    /// Content-Type written on all HalResult responses. Default: "application/hal+json". (REQ-08)
    /// </summary>
    public string MediaType { get; set; } = "application/hal+json";

    /// <summary>
    /// Registers a custom exception-to-problem mapping. Most-derived type wins. (REQ-09)
    /// </summary>
    public HalOptions MapException<TException>(Func<TException, HalProblem> map)
        where TException : Exception;

    // Internal: populated by MapException<T>. Key = exception Type, Value = mapping delegate.
    internal Dictionary<Type, Func<Exception, HalProblem>> ExceptionMappings { get; }
}
```

### `IHalLinkBuilder`

DI-injectable abstraction for resolving route names to `LinkObject` instances via ASP.NET Core's `LinkGenerator`. Registered as singleton (REQ-11, REQ-15).

```csharp
public interface IHalLinkBuilder
{
    /// <summary>
    /// Resolves routeName to a concrete href via LinkGenerator. (REQ-11)
    /// </summary>
    LinkObject For(string routeName, object? routeValues = null);

    /// <summary>
    /// Returns LinkObject with Href = route URI pattern and Templated = true. (REQ-12)
    /// </summary>
    LinkObject Template(string routeName);
}
```

Note: Expression-based overloads (`For<TController>`, `Template<TController>`) are extension methods in `ControllerLinkBuilderExtensions` — not members of this interface (REQ-13, REQ-14).

### `HalResult`

Single result type that works in both the MVC controller pipeline (`IActionResult`) and the Minimal API pipeline (`IResult`) (REQ-16).

```csharp
public sealed class HalResult : IActionResult, IResult
{
    private readonly Resource? _resource;
    private readonly int _statusCode;

    // Deferred builder support (used by HalResults.Ok<T>)
    // _builder is always Func<object, IHalLinkBuilder, Resource> — the typed
    // Func<T, IHalLinkBuilder, Resource> is wrapped by HalResults.Ok<T> before storage
    // to avoid the invalid cast Func<T,...> → Func<object,...> at invocation time.
    private readonly object? _state;
    private readonly Func<object, IHalLinkBuilder, Resource>? _builder;

    // Immediate constructor — used when Resource is already built (controller path, untyped Ok)
    public HalResult(Resource resource, int statusCode);

    // Deferred constructor — used by HalResults.Ok<T> where IHalLinkBuilder is not yet available.
    // Caller (HalResults.Ok<T>) must wrap the typed delegate:
    //   (object s, IHalLinkBuilder lb) => typedBuilder((T)s, lb)
    internal HalResult(object state, Func<object, IHalLinkBuilder, Resource> builder, int statusCode);

    // IResult (Minimal API pipeline)
    public Task ExecuteAsync(HttpContext httpContext);

    // IActionResult (MVC controller pipeline)
    public Task ExecuteResultAsync(ActionContext context);

    // Both delegate to the same core write logic — see Serialization Flow below.
    // CoreWriteAsync:
    //   1. Resolves deferred Resource (if _resource is null)
    //   2. Applies auto-self injection (3-way precedence) before writing
    //   3. Writes response via WriteAsJsonAsync
}
```

### `HalResults`

Static factory class parallel to ASP.NET Core's built-in `Results` static class (REQ-19, REQ-20, REQ-21).

```csharp
public static class HalResults
{
    // HTTP 200 -- untyped (REQ-19)
    public static HalResult Ok(Resource resource);

    // HTTP 200 -- typed; deferred execution (REQ-19)
    // Wraps builder as (object s, IHalLinkBuilder lb) => builder((T)s, lb) before storing,
    // so CoreWriteAsync can invoke _builder without an invalid generic delegate cast.
    // IHalLinkBuilder is resolved from RequestServices at ExecuteAsync/ExecuteResultAsync time.
    public static HalResult Ok<T>(T state, Func<T, IHalLinkBuilder, Resource> builder);

    // HTTP 201 (REQ-20)
    public static HalResult Created(string uri, Resource resource);

    // HTTP 202 (REQ-20)
    public static HalResult Accepted(Resource resource);

    // HTTP 204 -- no response body; returns IResult, not HalResult (REQ-20)
    public static IResult NoContent();

    // Problem responses -- Content-Type: application/problem+json (REQ-21)
    public static IResult NotFound(string title, string? detail);
    public static IResult ValidationProblem(IDictionary<string, string[]> errors);
    public static IResult Problem(int statusCode, string title, string? detail);
}
```

### `HalControllerBase`

Optional abstract base class. Adds no behavior beyond re-exposing extension methods from `ControllerBaseExtensions` as `protected` convenience members (REQ-23).

```csharp
public abstract class HalControllerBase : ControllerBase
{
    // All methods delegate to ControllerBaseExtensions using explicit static call syntax
    // to avoid infinite recursion (instance method binding would shadow extension methods)

    protected HalResult HalOk(Resource resource)
        => ControllerBaseExtensions.HalOk(this, resource);

    protected HalResult HalOk<T>(T state, Func<T, IHalLinkBuilder, Resource> builder)
        => ControllerBaseExtensions.HalOk(this, state, builder);

    protected HalResult HalCreated(string uri, Resource resource)
        => ControllerBaseExtensions.HalCreated(this, uri, resource);

    protected HalResult HalAccepted(Resource resource)
        => ControllerBaseExtensions.HalAccepted(this, resource);

    protected IActionResult HalNoContent()
        => ControllerBaseExtensions.HalNoContent(this);

    protected IActionResult HalNotFound(string title, string? detail)
        => ControllerBaseExtensions.HalNotFound(this, title, detail);

    protected IActionResult HalValidationProblem(IDictionary<string, string[]> errors)
        => ControllerBaseExtensions.HalValidationProblem(this, errors);

    protected IActionResult HalProblem(int statusCode, string title, string? detail)
        => ControllerBaseExtensions.HalProblem(this, statusCode, title, detail);
}
```

### `HalProblem`

Static factory for RFC 9457 problem responses. Internally backed by `HalProblemPayload` — an internal record that serializes as a compliant `application/problem+json` document (REQ-33).

```csharp
public static class HalProblem
{
    public static HalProblem NotFound(string title, string? detail);
    public static HalProblem Conflict(string title, string? detail);
    public static HalProblem ValidationProblem(IDictionary<string, string[]> errors);
    public static HalProblem Problem(int statusCode, string title, string? detail);
}

// Internal serialization payload -- RFC 9457
internal sealed record HalProblemPayload
{
    public string? Type    { get; init; }    // URI identifying problem type
    public string  Title   { get; init; }    // short human-readable summary
    public int     Status  { get; init; }    // HTTP status code
    public string? Detail  { get; init; }    // human-readable explanation
    public IDictionary<string, string[]>? Errors { get; init; }  // validation errors
}
```

### `ServiceCollectionExtensions`

Entry point for registration in Minimal API and generic host applications (REQ-01, REQ-03). See Registration Sequence below for the full pseudocode.

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHal(
        this IServiceCollection services,
        Action<HalOptions> configure);
}
```

### `MvcBuilderExtensions`

Entry point for registration when MVC is already configured (REQ-02). Delegates to `services.AddHal()`.

```csharp
public static class MvcBuilderExtensions
{
    public static IMvcBuilder AddHal(
        this IMvcBuilder builder,
        Action<HalOptions> configure);
}
```

### `ApplicationBuilderExtensions`

Activates the exception-handling middleware pipeline (REQ-04, REQ-30).

```csharp
public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseHal(this IApplicationBuilder app);
}
```

### `ControllerBaseExtensions`

Extension methods on `ControllerBase`. No inheritance required (REQ-22, REQ-24).

```csharp
public static class ControllerBaseExtensions
{
    public static HalResult HalOk(this ControllerBase controller, Resource resource);

    // Resolves IHalLinkBuilder from controller.HttpContext.RequestServices, invokes builder(state, linkBuilder)
    // immediately to produce a Resource, then returns HalResult(resource, 200) — the immediate constructor.
    // Unlike HalResults.Ok<T>, no deferred execution is needed here: HttpContext is available at call time. (REQ-24)
    public static HalResult HalOk<T>(
        this ControllerBase controller,
        T state,
        Func<T, IHalLinkBuilder, Resource> builder);

    public static HalResult HalCreated(this ControllerBase controller, string uri, Resource resource);
    public static HalResult HalAccepted(this ControllerBase controller, Resource resource);
    // Returns new NoContentResult() directly — does NOT delegate to HalResults.NoContent()
    // because HalResults.NoContent() returns IResult (Minimal API), not IActionResult (MVC)
    public static IActionResult HalNoContent(this ControllerBase controller);
    public static IActionResult HalNotFound(this ControllerBase controller, string title, string? detail);
    public static IActionResult HalValidationProblem(this ControllerBase controller, IDictionary<string, string[]> errors);
    public static IActionResult HalProblem(this ControllerBase controller, int statusCode, string title, string? detail);
}
```

### `ControllerLinkBuilderExtensions`

Expression-based overloads of `For` and `Template` on `IHalLinkBuilder`. Resolves route names by reflecting on controller action attributes (REQ-13, REQ-14).

```csharp
public static class ControllerLinkBuilderExtensions
{
    // Resolves route name from [HttpGet]/[Route] Name property, extracts args, delegates to For()

    // For void actions
    public static LinkObject For<TController>(
        this IHalLinkBuilder builder,
        Expression<Action<TController>> action)
        where TController : ControllerBase;

    // For synchronous IActionResult actions
    public static LinkObject For<TController>(
        this IHalLinkBuilder builder,
        Expression<Func<TController, IActionResult>> action)
        where TController : ControllerBase;

    // For async Task<IActionResult> actions (covers typical async controller actions)
    public static LinkObject For<TController>(
        this IHalLinkBuilder builder,
        Expression<Func<TController, Task<IActionResult>>> action)
        where TController : ControllerBase;

    // Resolves route name, delegates to Template()

    // For void actions
    public static LinkObject Template<TController>(
        this IHalLinkBuilder builder,
        Expression<Action<TController>> action)
        where TController : ControllerBase;

    // For synchronous IActionResult actions
    public static LinkObject Template<TController>(
        this IHalLinkBuilder builder,
        Expression<Func<TController, IActionResult>> action)
        where TController : ControllerBase;

    // For async Task<IActionResult> actions
    public static LinkObject Template<TController>(
        this IHalLinkBuilder builder,
        Expression<Func<TController, Task<IActionResult>>> action)
        where TController : ControllerBase;
}
```

### `EndpointConventionExtensions`

Per-endpoint auto-self overrides (REQ-28).

```csharp
public static class EndpointConventionExtensions
{
    // Forces auto-self injection for this endpoint regardless of global setting.
    // Adds EnableHalAutoSelf metadata marker.
    public static IEndpointConventionBuilder WithHalAutoSelf(
        this IEndpointConventionBuilder builder)
    {
        builder.Add(b => b.Metadata.Add(new EnableHalAutoSelf()));
        return builder;
    }

    // Suppresses auto-self injection for this endpoint regardless of global setting.
    // Adds DisableHalAutoSelf metadata marker.
    public static IEndpointConventionBuilder WithoutHalAutoSelf(
        this IEndpointConventionBuilder builder)
    {
        builder.Add(b => b.Metadata.Add(new DisableHalAutoSelf()));
        return builder;
    }
}
```

---

## Internal Types

### `HalLinkBuilder`

Concrete implementation of `IHalLinkBuilder`. Sealed. Depends on `LinkGenerator` and `IHttpContextAccessor` (REQ-11, REQ-12, REQ-15).

```csharp
internal sealed class HalLinkBuilder : IHalLinkBuilder
{
    private readonly LinkGenerator _linkGenerator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HalLinkBuilder(LinkGenerator linkGenerator, IHttpContextAccessor httpContextAccessor);

    public LinkObject For(string routeName, object? routeValues = null)
    {
        // Calls linkGenerator.GetPathByName(httpContext, routeName, routeValues)
        // Throws InvalidOperationException if result is null
        // Returns new LinkObject { Href = resolvedPath }
    }

    public LinkObject Template(string routeName)
    {
        // Retrieves route URI pattern for routeName from endpoint data source
        // Returns new LinkObject { Href = pattern, Templated = true }
    }
}
```

### `HalExceptionHandler`

`IExceptionHandler` implementation for RFC 9457 Problem Details responses (REQ-30, REQ-31, REQ-32, REQ-34).

```csharp
internal sealed class HalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Guard: handler registered unconditionally; return false when disabled to pass through
        if not _options.Value.UseProblemDetails:
            return false

        // Walk exception type hierarchy against _options.Value.ExceptionMappings
        //   (most-derived registered type first)
        // On match:
        //   problem = mapping(exception)
        //   httpContext.Response.StatusCode = problem.Status
        //   httpContext.Response.ContentType = "application/problem+json"
        //   await httpContext.Response.WriteAsJsonAsync(problem.Payload, cancellationToken)
        //   return true
        // No match:
        //   write HTTP 500 HalProblem.Problem(500, "Internal Server Error", null)
        //   return true
    }
}
```

### `HalFormatterPatcher`

Static utility that patches ASP.NET Core's STJ output formatter to support `application/hal+json` content negotiation (REQ-36).

```csharp
internal static class HalFormatterPatcher
{
    public static void PatchOutputFormatter(MvcOptions options)
    {
        // Find SystemTextJsonOutputFormatter in options.OutputFormatters
        // Add "application/hal+json" to its SupportedMediaTypes if not already present
    }
}
```

### `DisableHalAutoSelf`

Internal sealed record used as an endpoint metadata marker. Presence suppresses auto-self injection for the decorated endpoint (REQ-28).

```csharp
internal sealed record DisableHalAutoSelf;
```

### `EnableHalAutoSelf`

Internal sealed record used as an endpoint metadata marker. Presence forces auto-self injection for the decorated endpoint regardless of `HalOptions.AutoSelfLink` (REQ-28).

```csharp
internal sealed record EnableHalAutoSelf;
```

### `HalRegistrationMarker`

Internal sealed class used as a DI sentinel for idempotent registration. `AddHal` returns immediately if this type is already registered (REQ-03).

```csharp
internal sealed class HalRegistrationMarker;
```

---

## Registration Sequence

`ServiceCollectionExtensions.AddHal` executes the following sequence (REQ-01, REQ-03, REQ-05, REQ-06, REQ-07):

```
AddHal(IServiceCollection services, Action<HalOptions> configure):
    if services has HalRegistrationMarker:
        return services                                     // idempotent guard (REQ-03)

    services.AddSingleton<HalRegistrationMarker>()

    services.Configure<HalOptions>(configure)              // -> IOptions<HalOptions> (REQ-05)
    services.AddHttpContextAccessor()
    services.AddSingleton<IHalLinkBuilder, HalLinkBuilder>()

    // Minimal API pipeline -- always registered
    services.Configure<JsonOptions>(o =>
        o.JsonSerializerOptions.AddHalConverters())        // HAL converters for HttpJsonOptions

    // MVC pipeline -- registered only when MVC is present
    if services has IApiDescriptionGroupCollectionProvider:
        services.Configure<MvcOptions>(o =>
            HalFormatterPatcher.PatchOutputFormatter(o))   // application/hal+json content negotiation (REQ-36)
        services.Configure<MvcJsonOptions>(o =>
            o.JsonSerializerOptions.AddHalConverters())    // HAL converters for MVC JsonOptions

    // HalExceptionHandler always registered. services.Configure<HalOptions>() defers the configure
    // delegate — UseProblemDetails cannot be read at registration time. The handler checks
    // _options.Value.UseProblemDetails at runtime and returns false when disabled (passes through
    // to other handlers). UseHal() additionally guards UseExceptionHandler() via IOptions<HalOptions>.
    // (REQ-07, REQ-30)
    services.AddExceptionHandler<HalExceptionHandler>()

    return services
```

`MvcBuilderExtensions.AddHal` delegates directly:

```
AddHal(IMvcBuilder builder, Action<HalOptions> configure):
    builder.Services.AddHal(configure)
    return builder
```

`ApplicationBuilderExtensions.UseHal` activates the middleware pipeline:

```
UseHal(IApplicationBuilder app):
    options = app.ApplicationServices.GetRequiredService<IOptions<HalOptions>>().Value
    if options.UseProblemDetails:
        app.UseExceptionHandler()          // activates IExceptionHandler pipeline (REQ-04, REQ-30)
    return app
```

---

## HalResult Serialization Flow

Both `ExecuteAsync` (Minimal API) and `ExecuteResultAsync` (MVC) delegate to the same core write logic (REQ-16, REQ-17, REQ-18, REQ-35):

```
CoreWriteAsync(HttpContext context):
    context.Response.StatusCode  = _statusCode
    context.Response.ContentType = _options.Value.MediaType          // e.g. "application/hal+json"

    // Step 1: Deferred builder path — resolve IHalLinkBuilder and invoke builder to produce Resource
    resource = _resource
    if resource is null:
        linkBuilder = context.RequestServices.GetRequiredService<IHalLinkBuilder>()
        resource = _builder!(_state!, linkBuilder)   // safe: already Func<object,...> (see HalResult ctor)

    // Step 2: Auto-self injection (REQ-25, REQ-26, REQ-27, REQ-28, REQ-29)
    //   Runs before serialization so the self link is included in the response body.
    //   3-way precedence:
    //     1. EnableHalAutoSelf metadata present  → inject
    //     2. DisableHalAutoSelf metadata present → skip
    //     3. Neither                             → follow _options.Value.AutoSelfLink
    endpoint      = context.GetEndpoint()
    enableMarker  = endpoint?.Metadata.GetMetadata<EnableHalAutoSelf>()
    disableMarker = endpoint?.Metadata.GetMetadata<DisableHalAutoSelf>()

    if disableMarker is null:
        if enableMarker is not null or _options.Value.AutoSelfLink:
            if not resource.Links.TryGetByRel("self", out _):         // REQ-27: skip if already present
                linkBuilder ??= context.RequestServices.GetRequiredService<IHalLinkBuilder>()
                routeName      = endpoint?.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName
                routeValues    = context.Request.RouteValues
                selfLinkObject = linkBuilder.For(routeName, routeValues)
                resource.Links.Add(new Link("self") { LinkObjects = { selfLinkObject } })

    // Step 3: Serialize
    jsonOptions = context.RequestServices
        .GetRequiredService<IOptions<JsonOptions>>()
        .Value.JsonSerializerOptions                                  // HAL-aware options from DI

    await context.Response.WriteAsJsonAsync(resource, jsonOptions)   // direct stream write (REQ-17)
```

The MVC path (`ExecuteResultAsync`) extracts `HttpContext` from `ActionContext.HttpContext` before delegating.

---

## Auto-Self Injection Flow

Auto-self injection is handled inside `HalResult.CoreWriteAsync` for both the Minimal API and MVC controller pipelines (REQ-25, REQ-26, REQ-27, REQ-28, REQ-29). Because `CoreWriteAsync` runs before `response.WriteAsJsonAsync`, the `"self"` link is always present in the serialized body when injected.

The full pseudocode is shown in the Serialization Flow section above. Summary:

1. Read `EnableHalAutoSelf` and `DisableHalAutoSelf` metadata from `context.GetEndpoint()`
2. Apply 3-way precedence: enable marker → inject; disable marker → skip; neither → follow `HalOptions.AutoSelfLink`
3. Skip if `resource.Links.TryGetByRel("self", out _)` returns `true` — link already present (REQ-27)
4. Resolve `IHalLinkBuilder` from `context.RequestServices` (reuses the deferred builder instance if already resolved in Step 1 of `CoreWriteAsync`)
5. Derive route name from `IEndpointNameMetadata.EndpointName`; route values from `context.Request.RouteValues`
6. Call `IHalLinkBuilder.For(routeName, routeValues)` and add to `resource.Links`

For MVC controller actions, `HttpContext` is extracted from `ActionContext.HttpContext` before `CoreWriteAsync` is called. Route name resolution via `IEndpointNameMetadata` works for named attribute routes (e.g., `[HttpGet(Name = "orders:get")]`).

---

## Expression Resolution

`ControllerLinkBuilderExtensions.For<TController>` resolves a route name and route values from an expression tree (REQ-13, REQ-14):

```
For<TController>(Expression<Action<TController>> action):
1. Cast action.Body to MethodCallExpression; get MethodInfo
2. Check [HttpGet/Post/Put/Patch/Delete] attribute Name property -> use as routeName
   Note: [ActionName] is NOT checked; it changes the MVC action name but does not
   create a named route for LinkGenerator.GetPathByName
3. Else check [Route] attribute Name property -> use as routeName
4. Else throw InvalidOperationException("No named route found on action method '{method.Name}'.
   Annotate the action with [HttpGet(Name = \"...\")], [HttpPost(Name = \"...\")], or
   [Route(Name = \"...\")] to provide a route name for LinkGenerator.")
   Note: Falling back to method.Name is omitted because an unnamed route would cause
   LinkGenerator.GetPathByName to return null and throw InvalidOperationException anyway.
5. Extract route values:
   For each argument expression in MethodCallExpression.Arguments:
     - ConstantExpression -> use .Value directly
     - MemberExpression (captured closure) -> compile and invoke lambda to get value
     - Other -> throw InvalidOperationException("Expression argument not supported")
   Build RouteValueDictionary from parameter name -> value pairs
6. Delegate to _builder.For(routeName, routeValues)
```

Limitation: Only constant values and captured closures are supported in expression arguments. Method calls and complex expressions will throw `InvalidOperationException`.

---

## Error Handling Summary

| Condition | Behavior | REQ |
|---|---|---|
| Exception matches `MapException<T>` | Custom problem response; `Content-Type: application/problem+json` | REQ-31, REQ-34 |
| Exception not matched | HTTP 500 `application/problem+json` response | REQ-31 |
| `LinkGenerator` cannot resolve route name | `InvalidOperationException` thrown from `HalLinkBuilder.For` | REQ-11 |
| Expression route name unresolvable | `InvalidOperationException` thrown from `ControllerLinkBuilderExtensions` | REQ-13 |
| `AddHal` called multiple times | Idempotent; returns `services` immediately after first registration | REQ-03 |

---

## Test Strategy

### Unit tests

- **`HalOptions`**: verify defaults for all properties; verify `MapException<T>` stores mapping in `ExceptionMappings` keyed by `typeof(TException)`; verify most-derived type wins when multiple mappings registered
- **`HalResult`**: verify `ExecuteAsync` sets correct status code and `Content-Type`; verify `ExecuteResultAsync` delegates to same write logic; verify `WriteAsJsonAsync` is called with the `Resource` and HAL `JsonSerializerOptions`; verify auto-self injected when `AutoSelfLink = true` and `"self"` absent; verify `EnableHalAutoSelf` forces injection when `AutoSelfLink = false`; verify `DisableHalAutoSelf` suppresses injection when `AutoSelfLink = true`; verify injection skipped when `"self"` already present
- **`HalResults`**: verify each factory method returns correct HTTP status; verify `Ok<T>` creates a deferred `HalResult` that resolves `IHalLinkBuilder` from `HttpContext.RequestServices` during `ExecuteAsync`; verify `NoContent()` returns `IResult` that produces `204` with no response body
- **`HalControllerBase` + `ControllerBaseExtensions`**: verify `HalOk`, `HalCreated`, `HalAccepted`, `HalNotFound`, `HalValidationProblem`, and `HalProblem` delegate to the matching `HalResults` factory methods; verify `HalNoContent` returns `new NoContentResult()` directly (does not delegate to `HalResults.NoContent()` which returns `IResult`); verify `HalOk<T>` resolves `IHalLinkBuilder` from `HttpContext.RequestServices`
- **`HalProblem`**: verify each factory sets correct `Status`, `Title`, `Detail`, and `Type` URI per RFC 9457; verify `ValidationProblem` includes `Errors` in payload
- **`HalLinkBuilder`** (internal): verify `For()` calls `LinkGenerator.GetPathByName` with correct arguments; verify `InvalidOperationException` on null result; verify `Template()` returns `LinkObject` with `Templated = true`
- **`ControllerLinkBuilderExtensions`**: verify expression with `[HttpGet(Name = ...)]` extracts correct route name; verify expression with `[Route(Name = ...)]` extracts correct route name; verify action with no named route throws `InvalidOperationException`; verify `[ActionName]` alone (no named route) throws `InvalidOperationException`; verify constant args produce correct route values; verify captured closure args produce correct route values; verify unsupported expression type throws `InvalidOperationException`
- **`HalExceptionHandler`**: verify matched exception type produces correct status and `Content-Type: application/problem+json`; verify unmatched exception produces HTTP 500; verify most-derived registered type wins over base type

### Integration tests (`WebApplicationFactory`)

- **`AddHal(IServiceCollection)`**: resolves `IHalLinkBuilder`, `IOptions<HalOptions>`, HAL converters present in `HttpJsonOptions`
- **`AddHal(IMvcBuilder)`**: same as above plus `SystemTextJsonOutputFormatter` supports `application/hal+json`
- **Idempotency**: double `AddHal(...)` call results in no duplicate services in DI container
- **Minimal API end-to-end**: `GET` endpoint returns `HalResults.Ok(resource)` -> `200 OK`, `Content-Type: application/hal+json`, `_links` present in body
- **Controller end-to-end**: `GET` action returns `HalOk(resource)` -> `200 OK`, `Content-Type: application/hal+json`, `_links` present in body
- **Auto-self injection**: resource without `"self"` + `AutoSelfLink = true` -> `"self"` present in response `_links`
- **Auto-self skip**: resource with existing `"self"` + `AutoSelfLink = true` -> existing link unchanged
- **`WithoutHalAutoSelf()`**: endpoint marked with `WithoutHalAutoSelf()` -> no `"self"` injected even when global setting is `true`
- **Problem Details -- registered exception**: `MapException<NotFoundException>` registered + endpoint throws `NotFoundException` -> `404 Not Found`, `Content-Type: application/problem+json`, RFC 9457 payload
- **Problem Details -- fallback**: unregistered exception + `UseProblemDetails = true` -> `500 Internal Server Error`, `Content-Type: application/problem+json`
- **Content negotiation**: controller action returns raw `Resource` + `Accept: application/hal+json` request -> response `Content-Type: application/hal+json` via patched STJ formatter
