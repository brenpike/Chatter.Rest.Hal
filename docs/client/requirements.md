# Chatter.Rest.Hal.Client -- Requirements

This document is the source of truth for what the `Chatter.Rest.Hal.Client` package does. Test scenarios are derived directly from the numbered requirements below. For how the system is built, see [architecture.md](architecture.md).

---

## Overview

`Chatter.Rest.Hal.Client` is a new NuGet package that provides an HTTP client for consuming HAL (Hypertext Application Language) APIs. It wraps `HttpClient` with HAL-aware request/response handling, typed deserialization into the existing `Resource` / `Resource<T>` domain model, and fluent link-traversal extension methods that let callers navigate a HAL API by following rels -- without writing HTTP boilerplate or RFC 6570 expansion code.

### Package dependencies

- `Chatter.Rest.Hal` -- core HAL types (`Resource`, `Resource<T>`, `LinkObject`, `LinkCollection`, `Link`)
- `Chatter.Rest.UriTemplates` -- RFC 6570 URI template expansion (used for templated link traversal)
- `System.Net.Http` -- no new transitive dependencies beyond this

---

## Personas

### API consumer

Building a .NET client against a third-party or internal HAL API. Wants to navigate links and follow rels without writing HTTP boilerplate or RFC 6570 expansion code.

### Application developer

Building an app that calls a HAL API. Wants DI-friendly registration, typed deserialization, and clean error semantics -- not raw `HttpClient` management.

### Library consumer

Already uses `Chatter.Rest.Hal` for building HAL documents server-side. Wants to add client navigation to the same app with a familiar API surface.

---

## Functional Requirements

### Core client

**REQ-01:** `HalClient` implements `IHalClient` and wraps an `HttpClient` with HAL-specific request/response behavior. It accepts `HalClientOptions` directly or via `IOptions<HalClientOptions>` for DI integration.

**REQ-02:** `IHalClient` exposes async methods for all standard HTTP verbs: `GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, and `DeleteAsync`. All methods accept a `Uri` parameter and a `CancellationToken` with a default value.

**REQ-03:** `GetAsync` returns `Resource?` (untyped) or `Resource<T>?` (typed). Both overloads return `null` when the server responds with HTTP 404.

**REQ-04:** `PostAsync`, `PutAsync`, and `PatchAsync` each have two overloads: one accepting an `object` body (serialized as JSON) and one accepting raw `HttpContent`. Both return `Resource?`.

**REQ-05:** `DeleteAsync` returns `Task` (no body). It does not attempt to deserialize the response.

### Request behavior

**REQ-06:** All HTTP requests sent by `HalClient` include the `Accept` header set to the value of `HalClientOptions.MediaType`. The default media type is `"application/hal+json"`.

**REQ-07:** All async methods on `IHalClient` accept a `CancellationToken` parameter with a default value of `default`, enabling cooperative cancellation throughout the call chain.

### Response handling and error contract

**REQ-08:** When the server returns HTTP 404, `GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync` return `null`. `DeleteAsync` completes normally.

**REQ-09:** When the server returns HTTP 5xx, the underlying `HttpClient` throws `HttpRequestException`. `HalClient` does not catch or wrap this exception.

**REQ-10:** Network errors and timeouts propagate naturally from `HttpClient`. `HalClient` does not add retry or timeout logic.

**REQ-11:** When the response `Content-Type` is not a HAL media type and `HalClientOptions.StrictContentType` is `false` (the default), the method returns `null`.

**REQ-12:** When the response `Content-Type` is not a HAL media type and `HalClientOptions.StrictContentType` is `true`, the method throws `HalResponseException`.

### Configuration

**REQ-13:** `HalClientOptions.MediaType` controls the `Accept` header value. Default: `"application/hal+json"`.

**REQ-14:** `HalClientOptions.StrictContentType` controls non-HAL response behavior. Default: `false` (return `null`). When `true`, throws `HalResponseException`.

**REQ-15:** `HalClientOptions.JsonOptions` provides custom `JsonSerializerOptions` for deserialization. When `null`, library defaults are used.

### DI registration

**REQ-16:** `AddHalClient(Action<HalClientOptions>)` is an extension method on `IServiceCollection` that registers `IHalClient` / `HalClient` as a service and manages the `HttpClient` internally.

**REQ-17:** `AddHalOptions(Action<HalClientOptions>)` is an extension method on `IHttpClientBuilder` that composes `HalClient` with `IHttpClientFactory`, enabling Polly policies, auth `DelegatingHandler`s, and named/typed client lifecycle management.

**REQ-18:** Both registration paths resolve `IHalClient` to `HalClient` in the DI container.

### Link traversal -- extension methods on `Resource`

**REQ-19:** `FollowLink(string rel, IHalClient client)` resolves a single non-templated link on the resource and performs an HTTP GET via the provided `IHalClient`. Returns `Resource?`. Throws `HalLinkNotFoundException` before any HTTP call if the rel is not present on the resource.

**REQ-20:** `FollowLink<T>(string rel, IHalClient client)` performs the same resolution as REQ-19 but deserializes the response as `Resource<T>?`.

**REQ-21:** `FollowLink(string rel, IHalClient client, object variables)` resolves a templated link by delegating to `LinkObject.Expand()` with the provided variables, then performs an HTTP GET. Returns `Resource?`. Throws `HalLinkNotFoundException` if the rel is absent.

**REQ-22:** `FollowLink<T>(string rel, IHalClient client, object variables)` performs the same templated resolution as REQ-21 but deserializes the response as `Resource<T>?`.

**REQ-23:** `FollowLinks(string rel, IHalClient client)` iterates all `LinkObject` entries for an array-valued rel, performs an HTTP GET for each, and yields results as `IAsyncEnumerable<Resource?>`. Throws `HalLinkNotFoundException` if the rel is absent.

**REQ-24:** Raw `HttpClient` overloads exist for every `FollowLink` and `FollowLinks` signature. These use default `HalClientOptions` and accept `HttpClient` in place of `IHalClient`.

### Mutation -- extension methods on `Resource`

**REQ-25:** `PostTo` has three overloads: (a) `PostTo<TBody, TResponse>(string rel, TBody body, IHalClient client)` returning `Resource<TResponse>?`, (b) `PostTo(string rel, object body, IHalClient client)` returning `Resource?`, and (c) `PostTo(string rel, HttpContent content, IHalClient client)` returning `Resource?`. All throw `HalLinkNotFoundException` if the rel is absent.

**REQ-26:** `PutTo` has the same three overloads as `PostTo` (REQ-25), using HTTP PUT.

**REQ-27:** `PatchTo` has the same three overloads as `PostTo` (REQ-25), using HTTP PATCH.

**REQ-28:** `DeleteTo(string rel, IHalClient client)` has a single overload with no body. It returns `Task` (not `Task<Resource?>`). Throws `HalLinkNotFoundException` if the rel is absent.

**REQ-29:** Raw `HttpClient` overloads exist for every `PostTo`, `PutTo`, `PatchTo`, and `DeleteTo` signature.

### HttpClient convenience extensions

**REQ-30:** `GetHalAsync(this HttpClient, Uri, HalClientOptions?, CancellationToken)` and `GetHalAsync<T>(this HttpClient, Uri, HalClientOptions?, CancellationToken)` provide top-level HAL fetch without constructing a `HalClient`.

**REQ-31:** `PostHalAsync(this HttpClient, Uri, object body, HalClientOptions?, CancellationToken)` and `PostHalAsync(this HttpClient, Uri, HttpContent content, HalClientOptions?, CancellationToken)` provide top-level HAL POST without constructing a `HalClient`.

### Error types

**REQ-32:** `HalLinkNotFoundException` is thrown before any HTTP request when a requested rel is not found on the resource. It carries the rel name and the resource's self href.

**REQ-33:** `HalResponseException` is thrown when `StrictContentType` is `true` and the response `Content-Type` is not a HAL media type.

### Testability

**REQ-34:** `IHalClient` is the primary abstraction for all client operations. Callers depend on the interface, enabling mock/stub injection in tests without requiring a live HTTP server.

### Logging

**REQ-35:** `HalClient` accepts `ILogger<HalClient>` via constructor injection. The `IHalClient` interface does not reference `ILogger`; logging is an implementation detail of `HalClient`.

**REQ-36:** All log messages use structured logging with named placeholders (e.g., `{Method}`, `{Uri}`, `{StatusCode}`, `{Rel}`, `{ContentType}`) and never string concatenation or string interpolation in log calls.

**REQ-37:** At `Debug` level, `HalClient` logs the HTTP method and request URI before sending each request, and the response status code after receiving the response.

**REQ-38:** At `Debug` level, `HalClient` logs when a 404 response is received and `null` is returned.

**REQ-39:** At `Warning` level, `HalClient` logs when the response `Content-Type` is not a HAL media type and `StrictContentType` is `false` (lenient mode: non-HAL response returned as `null`).

**REQ-40:** At `Error` level, `HalClient` logs deserialization failures, including the exception and the request URI.

**REQ-41:** At `Debug` level, `FollowLink` and `FollowLinks` extension methods log rel resolution: the rel name, the resolved href, and whether the link is templated. Logging occurs at the extension-method call site (not inside `HalClient`) because the extension has rel context that `HalClient` does not. Extension methods accept an optional `ILogger?` parameter (defaulting to `null`) for this purpose; callers that want rel-resolution logging pass their logger explicitly.

**REQ-42:** At `Debug` level, `PostTo`, `PutTo`, `PatchTo`, and `DeleteTo` extension methods log the rel name and resolved URI before delegating to `IHalClient`. These methods follow the same optional `ILogger?` parameter pattern as `FollowLink` (REQ-41).

**REQ-43:** At `Debug` level, `AddHalClient` and `AddHalOptions` log confirmation that `IHalClient` has been registered. Because these methods run during service registration before the DI container is built, the confirmation log is emitted from the `HalClient` constructor on first instantiation (one-time `Debug` message: `"HalClient initialized"`).

**REQ-44:** All `Debug`-level log points in `HalClient` use `LoggerMessage` source generators to avoid string allocation when `Debug` logging is not enabled. `Trace`-level log points that iterate collections use explicit `IsEnabled(LogLevel.Trace)` guards before the loop. `Information`-level logging is limited to at most one startup message; no `Information` logging occurs on hot paths.

**REQ-45:** Auth headers, request bodies, and response bodies must never appear in log messages at any log level.

---

## Error Handling Summary

| Scenario | Behavior |
|---|---|
| Rel not found on resource | Throw `HalLinkNotFoundException` (before any HTTP) |
| HTTP 404 | Return `null` |
| HTTP 5xx | Throw `HttpRequestException` (from `HttpClient`) |
| Network / timeout | `HttpClient` throws naturally |
| Non-HAL Content-Type, `StrictContentType = false` | Return `null` |
| Non-HAL Content-Type, `StrictContentType = true` | Throw `HalResponseException` |

---

## Integration Story

The following shows two ways to register `HalClient` in a .NET application:

### Option A -- simple registration

```csharp
// Manages HttpClient internally
services.AddHalClient(options =>
{
    options.MediaType = "application/hal+json";
    options.StrictContentType = true;
});
```

### Option B -- compose with IHttpClientFactory

```csharp
// Full IHttpClientFactory lifecycle: Polly, DelegatingHandlers, named clients
services.AddHttpClient<HalClient>(c =>
        c.BaseAddress = new Uri("https://api.example.com"))
    .AddHalOptions(options =>
    {
        options.StrictContentType = true;
    });
```

Both register `IHalClient` -> `HalClient` in DI. Option B uses `IHttpClientFactory` lifecycle.

---

## Behavioral Scenarios

These scenarios describe end-to-end behavior for test derivation.

### Scenario: Follow a single link

1. Caller has a `Resource` with `_links: { self: { href: "/orders" }, item: { href: "/orders/42" } }`
2. Caller calls `resource.FollowLink("item", halClient)`
3. `HalClient` sends `GET /orders/42` with `Accept: application/hal+json`
4. Server responds with `200 OK` and a HAL resource
5. Method returns `Resource` deserialized from the response

### Scenario: Follow a typed link

1. Caller has a `Resource` with `_links: { details: { href: "/orders/42/details" } }`
2. Caller calls `resource.FollowLink<OrderDetails>("details", halClient)`
3. `HalClient` sends `GET /orders/42/details` with `Accept: application/hal+json`
4. Server responds with `200 OK` and a HAL resource with state
5. Method returns `Resource<OrderDetails>` with deserialized state

### Scenario: Follow a templated link

1. Caller has a `Resource` with `_links: { find: { href: "/orders/{id}", templated: true } }`
2. Caller calls `resource.FollowLink("find", halClient, new { id = "42" })`
3. `LinkObject.Expand()` resolves href to `/orders/42`
4. `HalClient` sends `GET /orders/42`
5. Method returns the deserialized `Resource`

### Scenario: Follow array-valued links

1. Caller has a `Resource` with `_links: { item: [{ href: "/orders/1" }, { href: "/orders/2" }, { href: "/orders/3" }] }`
2. Caller calls `resource.FollowLinks("item", halClient)`
3. Method sends `GET /orders/1`, `GET /orders/2`, `GET /orders/3` sequentially
4. Yields each `Resource?` via `IAsyncEnumerable`

### Scenario: Post to a linked resource

1. Caller has a `Resource` with `_links: { create: { href: "/orders" } }`
2. Caller calls `resource.PostTo("create", new { product = "Widget" }, halClient)`
3. `HalClient` sends `POST /orders` with JSON body and `Accept: application/hal+json`
4. Server responds with `201 Created` and a HAL resource
5. Method returns the deserialized `Resource`

### Scenario: Delete a linked resource

1. Caller has a `Resource` with `_links: { cancel: { href: "/orders/42/cancel" } }`
2. Caller calls `resource.DeleteTo("cancel", halClient)`
3. `HalClient` sends `DELETE /orders/42/cancel`
4. Method returns `Task` (no body deserialization)

### Scenario: Rel not found

1. Caller has a `Resource` with `_links: { self: { href: "/orders" } }`
2. Caller calls `resource.FollowLink("nonexistent", halClient)`
3. Method throws `HalLinkNotFoundException` with rel `"nonexistent"` and self href `"/orders"`
4. No HTTP request is made

### Scenario: HTTP 404 response

1. Caller calls `resource.FollowLink("item", halClient)`
2. `HalClient` sends `GET /orders/42`
3. Server responds with `404 Not Found`
4. Method returns `null`

### Scenario: Non-HAL response with strict mode

1. `HalClientOptions.StrictContentType = true`
2. Caller calls `resource.FollowLink("health", halClient)`
3. `HalClient` sends `GET /health`
4. Server responds with `200 OK` and `Content-Type: text/plain`
5. Method throws `HalResponseException`

### Scenario: Non-HAL response with lenient mode

1. `HalClientOptions.StrictContentType = false` (default)
2. Caller calls `resource.FollowLink("health", halClient)`
3. `HalClient` sends `GET /health`
4. Server responds with `200 OK` and `Content-Type: text/plain`
5. Method returns `null`

### Scenario: Convenience extension on raw HttpClient

1. Caller has an `HttpClient` but no `HalClient`
2. Caller calls `httpClient.GetHalAsync(new Uri("/orders"))`
3. Extension sends `GET /orders` with `Accept: application/hal+json`
4. Server responds with `200 OK` and a HAL resource
5. Method returns the deserialized `Resource`

---

## Out of Scope (v1)

The following capabilities are explicitly deferred and must not be implemented in the initial version:

- **Automatic pagination** -- `FollowLinks` does not chase `next` links; it iterates a single array-valued rel
- **Retry / resilience** -- no built-in retry; users compose Polly via `IHttpClientFactory`
- **Caching** -- no HTTP cache or ETag handling
- **HAL-FORMS support** -- form-based actions are not interpreted
- **CURIE expansion** -- CURIE rels are not expanded; callers use compact form
- **Batch requests** -- no multi-resource fetch optimization
- **Streaming / SSE** -- no server-sent events or streaming response support
- **Authentication** -- no built-in auth; users configure `DelegatingHandler`s via `IHttpClientFactory`
