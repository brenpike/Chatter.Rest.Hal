# Chatter.Rest.Hal.Client -- Requirements

This document is the source of truth for what the `Chatter.Rest.Hal.Client` package does. Test scenarios are derived directly from the numbered requirements below. For how the system is built, see [architecture.md](architecture.md).

---

## Overview

`Chatter.Rest.Hal.Client` is a new NuGet package that provides an HTTP client for consuming HAL (Hypertext Application Language) APIs. It wraps `HttpClient` with HAL-aware request/response handling, typed deserialization into the `Resource` domain model, with `Resource<T>` defined in this package as a thin typed wrapper, and fluent link-traversal extension methods that let callers navigate a HAL API by following rels -- without writing HTTP boilerplate or RFC 6570 expansion code.

### Package dependencies

#### `Chatter.Rest.Hal.Client`

- `Chatter.Rest.Hal` — core HAL types (`Resource`, `LinkObject`, `LinkCollection`, `Link`)
- `Microsoft.Extensions.Logging.Abstractions` — `ILogger<T>` and `NullLogger<T>` (logging only; no DI, options, or HTTP factory)
- `System.Net.Http`

The base package depends only on `Microsoft.Extensions.Logging.Abstractions` for optional structured logging. All other `Microsoft.Extensions.*` packages remain in the companion. `Chatter.Rest.UriTemplates` is a transitive dependency via `Chatter.Rest.Hal`. The client calls `LinkObject.Expand()` from `Chatter.Rest.Hal`; it does not reference `Chatter.Rest.UriTemplates` APIs directly.

#### `Chatter.Rest.Hal.Client.DependencyInjection`

- `Chatter.Rest.Hal.Client` — base client package
- `Microsoft.Extensions.Http` — `IHttpClientFactory` and typed client support
- `Microsoft.Extensions.DependencyInjection.Abstractions` — `IServiceCollection`
- `Microsoft.Extensions.Options` — `IOptions<T>` and `Configure<T>`
- `Microsoft.Extensions.Logging.Abstractions` — `ILogger<T>` and `NullLogger<T>`

Callers who use `HalClient` directly without a DI container only need `Chatter.Rest.Hal.Client`.

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

**REQ-01:** `HalClient` implements `IHalClient` and wraps an `HttpClient` with HAL-specific request/response behavior. It accepts `HalClientOptions` directly via its constructor. The `Chatter.Rest.Hal.Client.DependencyInjection` companion package resolves `IOptions<HalClientOptions>.Value` before constructing `HalClient`, so the base package has no dependency on `Microsoft.Extensions.Options`.

**REQ-02:** `IHalClient` exposes async methods for all standard HTTP verbs: `GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, and `DeleteAsync`. All methods accept a `Uri` parameter and a `CancellationToken` with a default value.

**REQ-03:** `GetAsync` returns `Resource?` (untyped) or `Resource<T>?` (typed, where `Resource<T>` is a thin wrapper defined in this package that provides strongly-typed access to state via `State()`). Both overloads return `null` when the server responds with HTTP 404.

**REQ-04:** `PostAsync`, `PutAsync`, and `PatchAsync` each have four overloads:
- (a) Non-generic, object body: accepts `object` (serialized as JSON), returns `Resource?`
- (b) Non-generic, raw content: accepts `HttpContent`, returns `Resource?`
- (c) Generic, object body: `PostAsync<T>` / `PutAsync<T>` / `PatchAsync<T>` accepting `object` (serialized as JSON), returns `Resource<T>?`
- (d) Generic, raw content: `PostAsync<T>` / `PutAsync<T>` / `PatchAsync<T>` accepting `HttpContent`, returns `Resource<T>?`

`T` is constrained to reference types (`where T : class`). Overloads (c) and (d) deserialize the response as plain `Resource` and wrap it in `new Resource<T>(resource, jsonOptions)`, where `jsonOptions` is `HalClientOptions.JsonOptions` (or the library default when null). Passing `jsonOptions` ensures `Resource<T>.State()` honours custom serializer settings (REQ-15).

**REQ-05:** `DeleteAsync` returns `Task` (no body). It does not attempt to deserialize the response.

### Request behavior

**REQ-06:** All HTTP requests sent by `HalClient` include the `Accept` header set to the value of `HalClientOptions.AcceptMediaType`. The default media type is `"application/hal+json"`.

**REQ-07:** All async methods on `IHalClient` accept a `CancellationToken` parameter with a default value of `default`, enabling cooperative cancellation throughout the call chain.

**REQ-08b:** `HalClient` constructs request URIs using `UriKind.RelativeOrAbsolute`, accepting both absolute and relative URIs from HAL link hrefs. Relative URIs (e.g., `/orders/42`) are resolved against `HttpClient.BaseAddress` by the underlying `HttpClient`. Callers navigating APIs that return relative links must configure `BaseAddress` on the `HttpClient`. If `BaseAddress` is null and a relative URI is used, `HttpClient.SendAsync` throws `InvalidOperationException`.

### Response handling and error contract

**REQ-08:** When the server returns HTTP 404, `GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync` return `null`. `DeleteAsync` completes normally.

**REQ-08a:** When the server returns HTTP 204 No Content, or any 2xx response with an explicit `Content-Length: 0` header, `GetAsync`, `PostAsync`, `PutAsync`, and `PatchAsync` return `null` without performing Content-Type validation or deserialization. Responses that omit `Content-Length` (e.g., chunked transfer encoding) proceed normally to Content-Type validation and deserialization. `DeleteAsync` always skips body handling after successful status code processing (REQ-05), but still throws `HttpRequestException` via `EnsureSuccessStatusCode()` for non-2xx non-404 responses.

**REQ-09:** HTTP status-code behavior:
- **2xx with body:** `HalClient` deserializes and returns the response.
- **HTTP 204 or explicit Content-Length: 0:** `HalClient` returns `null` (or completes normally for `DeleteAsync`) without Content-Type validation or deserialization. Responses that omit `Content-Length` (e.g., chunked transfer encoding) proceed to Content-Type validation.
- **3xx (redirect):** `HttpClient` follows redirects automatically by default. Non-redirect 3xx responses are passed to `EnsureSuccessStatusCode()` and throw `HttpRequestException`.
- **400 / 401 / 403 / 409:** `EnsureSuccessStatusCode()` throws `HttpRequestException`. `HalClient` does not catch or wrap it.
- **404:** `HalClient` returns `null` (or completes normally for `DeleteAsync`). See REQ-08.
- **5xx:** `EnsureSuccessStatusCode()` throws `HttpRequestException`. `HalClient` does not catch or wrap it.
- Network errors and timeouts propagate naturally from `HttpClient` as `HttpRequestException` or `TaskCanceledException`. `HalClient` does not add retry or timeout logic.

**REQ-10:** Network errors and timeouts propagate naturally from `HttpClient`. `HalClient` does not add retry or timeout logic.

**REQ-11:** When the response `Content-Type` is not a HAL media type and `HalClientOptions.StrictContentType` is `false` (the default), the method returns `null`. A HAL media type is determined by comparing `response.Content.Headers.ContentType?.MediaType` case-insensitively to `HalClientOptions.ExpectedMediaType` (default: `"application/hal+json"`). Content type parameters such as `charset` are ignored in this comparison. A null `ContentType` header is treated as non-HAL.

**REQ-12:** When the response `Content-Type` is not a HAL media type (using the comparison defined in REQ-11) and `HalClientOptions.StrictContentType` is `true`, the method throws `HalResponseException`.

### Configuration

**REQ-13a:** `HalClientOptions.AcceptMediaType` controls the `Accept` header value sent on all requests. May include media type parameters. Default: `"application/hal+json"`.

**REQ-13b:** `HalClientOptions.ExpectedMediaType` controls the bare media type used for response Content-Type validation (REQ-11/REQ-12). Must be a bare media type without parameters (e.g., `"application/hal+json"`, not `"application/hal+json; charset=utf-8"`). Default: `"application/hal+json"`. **Validation:** `HalClient` constructor throws `ArgumentException` if `ExpectedMediaType` is `null`, empty, or contains a `';'` character.

**REQ-14:** `HalClientOptions.StrictContentType` controls non-HAL response behavior. Default: `false` (return `null`). When `true`, throws `HalResponseException`.

**REQ-15:** `HalClientOptions.JsonOptions` provides custom `JsonSerializerOptions` for deserialization. When `null`, library defaults are used.

### DI registration (requires `Chatter.Rest.Hal.Client.DependencyInjection`)

**REQ-16:** `AddHalClient(Action<HalClientOptions>)` is an extension method on `IServiceCollection` (available in the `Chatter.Rest.Hal.Client.DependencyInjection` package) that registers `IHalClient` / `HalClient` as a service and manages the `HttpClient` internally.

**REQ-17:** `AddHalOptions(Action<HalClientOptions>)` is an extension method on `IHttpClientBuilder` (available in the `Chatter.Rest.Hal.Client.DependencyInjection` package) that composes `HalClient` with `IHttpClientFactory`, enabling Polly policies, auth `DelegatingHandler`s, and named/typed client lifecycle management. Options are registered as named options keyed by `builder.Name` using `IOptionsMonitor<HalClientOptions>.Get(builder.Name)` for resolution, ensuring that multiple `AddHalOptions` calls on different builders use fully independent options.

> **Note:** Named options registered by `AddHalOptions` are fully isolated. However, `IHalClient` is registered as an unkeyed transient; multiple `AddHalOptions` calls result in multiple registrations and last-registration-wins semantics for `IHalClient`. Applications requiring multiple independent HAL clients should construct `HalClient` directly from `IHttpClientFactory`, or use .NET 8+ keyed services.

**REQ-18:** Both registration paths resolve `IHalClient` to `HalClient` in the DI container.

### Link traversal -- extension methods on `Resource`

**REQ-19:** `FollowLinkAsync(string rel, IHalClient client)` resolves a single non-templated link on the resource and performs an HTTP GET via the provided `IHalClient`. Returns `Resource?`. Throws `HalLinkNotFoundException` before any HTTP call if the rel is not present on the resource.

**REQ-19a:** When resolving a rel, if the resource contains duplicate `Link` entries with the same rel, the resolution throws `InvalidOperationException`. This is consistent with `LinkCollectionExtensions.GetLinkOrDefault`, which uses `SingleOrDefault` internally. This check occurs before any HTTP request is made.

**REQ-20:** `FollowLinkAsync<T>(string rel, IHalClient client)` performs the same resolution as REQ-19 but deserializes the response as `Resource<T>?`.

**REQ-21:** `FollowLinkAsync(string rel, IHalClient client, object variables)` resolves a templated link by delegating to `LinkObject.Expand()` with the provided variables, then performs an HTTP GET. Returns `Resource?`. Throws `HalLinkNotFoundException` if the rel is absent. (The `object variables` parameter is converted to `IDictionary<string, string>` via an internal `ObjectToDictionary` helper that reflects public properties before calling `LinkObject.Expand()`.)

**REQ-22:** `FollowLinkAsync<T>(string rel, IHalClient client, object variables)` performs the same templated resolution as REQ-21 but deserializes the response as `Resource<T>?`. (The `object variables` parameter is converted to `IDictionary<string, string>` via an internal `ObjectToDictionary` helper that reflects public properties before calling `LinkObject.Expand()`.)

**REQ-23:** `FollowLinksAsync(string rel, IHalClient client)` iterates all `LinkObject` entries for an array-valued rel, performs an HTTP GET for each, and yields results as `IAsyncEnumerable<Resource?>`. Throws `HalLinkNotFoundException` if the rel is absent.

**REQ-23a:** `FollowLinksAsync<T>(string rel, IHalClient client)` performs the same concurrent iteration as REQ-23 but yields `IAsyncEnumerable<Resource<T>?>` by calling `client.GetAsync<T>` for each link. `T` is constrained to reference types (`where T : class`). Throws `HalLinkNotFoundException` if the rel is absent.

**REQ-24:** Raw `HttpClient` overloads exist for every `FollowLinkAsync` and `FollowLinksAsync` signature. These use default `HalClientOptions` and accept `HttpClient` in place of `IHalClient`.

**REQ-24a:** Every extension method on `Resource` that returns `Resource?`, `IAsyncEnumerable<Resource?>`, or `Task` (untyped return) has a parallel overload on `Resource<T>` with identical signature except `this Resource<T> resource` replaces `this Resource resource`. These overloads delegate to `resource.Inner`. This covers all untyped-return variants of `FollowLinkAsync`, `FollowLinksAsync`, `PostToAsync`, `PutToAsync`, `PatchToAsync`, and `DeleteToAsync`.

**REQ-24b:** `Resource<T>` exposes typed traversal and mutation as instance methods using an `As`-suffix naming convention: `FollowLinkAsAsync<TResult>`, `FollowLinksAsAsync<TResult>`, `PostToAsAsync<TResult>`, `PutToAsAsync<TResult>`, `PatchToAsAsync<TResult>`. Each is defined as an instance method (not an extension method) so that `TResult` is the only method-level type parameter — callers specify only the response type. These methods delegate to the corresponding typed extension method on `Resource` via `_inner`. Full overload pairs (no-logger / with-logger) and all body variants (typed body, raw `HttpContent`) are provided, matching the pattern in REQ-25 through REQ-27. Mutation "As" methods are provided in two object-body forms:
- `PostToAsAsync<TBody, TResult>(string rel, TBody body, ...)`: **both type arguments must be specified explicitly** — C# does not allow partial type argument specification for generic methods. Use when the body type is distinct and known at the call site, e.g. `order.PostToAsAsync<CreateOrder, OrderResult>("rel", body, client)`.
- `PostToAsAsync<TResult>(string rel, object body, ...)`: body as `object`; single explicit type param. Delegates to `PostToAsync<object, TResult>` on `_inner`.

Each `IHalClient`-accepting "As" method has a parallel `HttpClient` overload that wraps the raw client in `new HalClient(client, new HalClientOptions())` with default options and delegates to the `IHalClient` overload. This matches the raw-`HttpClient` overload pattern for untyped traversal (REQ-24).

### Mutation -- extension methods on `Resource`

**REQ-25:** `PostToAsync` has eight overloads in four pairs (no-logger / with-logger, following REQ-42's overload-pair pattern):
- (a) `PostToAsync<TBody, TResponse>(string rel, TBody body, IHalClient client, CancellationToken ct = default)` returning `Resource<TResponse>?`
- (a-log) same with explicit `ILogger logger` before `ct`
- (b) `PostToAsync(string rel, object body, IHalClient client, CancellationToken ct = default)` returning `Resource?`
- (b-log) same with explicit `ILogger logger` before `ct`
- (c) `PostToAsync(string rel, HttpContent content, IHalClient client, CancellationToken ct = default)` returning `Resource?`
- (c-log) same with explicit `ILogger logger` before `ct`
- (d) `PostToAsync<TResponse>(string rel, HttpContent content, IHalClient client, CancellationToken ct = default)` returning `Resource<TResponse>?`
- (d-log) same with explicit `ILogger logger` before `ct`

All overloads throw `HalLinkNotFoundException` if the rel is absent. `TBody` has no constraint. `TResponse` is constrained to reference types (`where TResponse : class`).

**REQ-26:** `PutToAsync` has the same eight-overload pattern as `PostToAsync` (REQ-25), using HTTP PUT.

**REQ-27:** `PatchToAsync` has the same eight-overload pattern as `PostToAsync` (REQ-25), using HTTP PATCH.

**REQ-28:** `DeleteToAsync` has two overloads (a no-logger / with-logger pair following REQ-42's overload-pair pattern):
- (a) `DeleteToAsync(string rel, IHalClient client, CancellationToken ct = default)` — no logger
- (b) `DeleteToAsync(string rel, IHalClient client, ILogger logger, CancellationToken ct = default)` — with explicit logger

Both overloads return `Task` (no body deserialization). Both throw `HalLinkNotFoundException` if the rel is absent.

**REQ-29:** Raw `HttpClient` overloads exist for every `PostToAsync`, `PutToAsync`, `PatchToAsync`, and `DeleteToAsync` signature.

### HttpClient convenience extensions

**REQ-30:** `GetHalAsync(this HttpClient, Uri, HalClientOptions?, CancellationToken)` and `GetHalAsync<T>(this HttpClient, Uri, HalClientOptions?, CancellationToken)` provide top-level HAL fetch without constructing a `HalClient`.

**REQ-31:** `PostHalAsync(this HttpClient, Uri, object body, HalClientOptions?, CancellationToken)` and `PostHalAsync(this HttpClient, Uri, HttpContent content, HalClientOptions?, CancellationToken)` provide top-level HAL POST without constructing a `HalClient`.

### Error types

**REQ-32:** `HalLinkNotFoundException` is thrown before any HTTP request when a requested rel is not found on the resource. It carries the rel name and the resource's self href.

**REQ-33:** `HalResponseException` is thrown when `StrictContentType` is `true` and the response `Content-Type` is not a HAL media type.

### Testability

**REQ-34:** `IHalClient` is the primary abstraction for all client operations. Callers depend on the interface, enabling mock/stub injection in tests without requiring a live HTTP server.

### Logging

**REQ-35:** `HalClient` accepts `ILogger<HalClient>?` as an optional constructor parameter (defaulting to `null`). When `null`, `NullLogger<HalClient>.Instance` is used internally and no log output is produced. The `IHalClient` interface does not reference `ILogger`; logging is an implementation detail of `HalClient`. The base package depends on `Microsoft.Extensions.Logging.Abstractions` only — not the full DI stack. When registered via the DI companion package, the DI container injects `ILogger<HalClient>` automatically.

**REQ-36:** All log messages use structured logging with named placeholders (e.g., `{Method}`, `{Uri}`, `{StatusCode}`, `{Rel}`, `{ContentType}`) and never string concatenation or string interpolation in log calls.

**REQ-36a:** All `{Uri}` values in log messages must be redacted before logging. Only the scheme, host, and path are included; query strings and URL fragments are stripped. This prevents API keys or tokens embedded in query parameters from appearing in logs. User info (credentials in the form `user:password@host`) is also stripped from absolute URIs; `GetLeftPart(UriPartial.Path)` is not used because it retains user-info segments.

**REQ-37:** At `Debug` level, `HalClient` logs the HTTP method and request URI before sending each request, and the response status code after receiving the response.

**REQ-38:** At `Debug` level, `HalClient` logs when a 404 response is received and `null` is returned.

**REQ-39:** At `Warning` level, `HalClient` logs when the response `Content-Type` is not a HAL media type and `StrictContentType` is `false` (lenient mode: non-HAL response returned as `null`).

**REQ-40:** At `Error` level, `HalClient` logs deserialization failures, including the exception and the request URI.

**REQ-41:** At `Debug` level, `FollowLinkAsync` and `FollowLinksAsync` extension methods log rel resolution: the rel name and whether the link is templated. Logging occurs at the extension-method call site (not inside `HalClient`) because the extension has rel context that `HalClient` does not. Extension methods are provided in overload pairs: a no-logger overload (ending in `CancellationToken ct = default`) and a with-logger overload accepting an explicit non-optional `ILogger logger` followed by `CancellationToken ct = default`. Callers that want rel-resolution logging call the with-logger overload explicitly. This avoids the ergonomic problem where a `CancellationToken` argument is silently bound as the logger parameter.

**REQ-42:** At `Debug` level, `PostToAsync`, `PutToAsync`, `PatchToAsync`, and `DeleteToAsync` extension methods log the rel name and resolved URI before delegating to `IHalClient`. These methods follow the same overload-pair pattern as `FollowLinkAsync` (REQ-41): a no-logger overload and a with-logger overload accepting an explicit `ILogger logger`.

**REQ-43:** At `Debug` level, `AddHalClient` and `AddHalOptions` log confirmation that `IHalClient` has been registered. Because these methods run during service registration before the DI container is built, the confirmation log is emitted from the `HalClient` constructor on first instantiation (one-time `Debug` message: `"HalClient initialized"`).

**REQ-44:** All `Debug`-level log points in `HalClient` use `LoggerMessage` source generators to avoid string allocation when `Debug` logging is not enabled. `Trace`-level log points that iterate collections use explicit `IsEnabled(LogLevel.Trace)` guards before the loop. `Information`-level logging is limited to at most one startup message; no `Information` logging occurs on hot paths.

**REQ-45:** Auth headers, request bodies, and response bodies must never appear in log messages at any log level.

### Async and Cancellation

**REQ-46:** All I/O operations in `Chatter.Rest.Hal.Client` must be async end-to-end. Synchronous-over-async patterns (`.Result`, `.GetAwaiter().GetResult()`, `.Wait()`) and blocking calls (`Thread.Sleep`) are prohibited anywhere in the implementation. Every public method that performs I/O returns `Task`, `Task<T>`, or `IAsyncEnumerable<T>`.

**REQ-47:** Every async method that performs or delegates to I/O must accept a `CancellationToken` parameter and pass it through to all downstream async calls (`HttpClient.SendAsync`, `ReadAsStringAsync`, `JsonSerializer.DeserializeAsync`, etc.). On the public API surface the `CancellationToken` parameter defaults to `default`. Internal/private async methods may require the parameter (no default) to enforce threading discipline at the implementation level.

**REQ-48:** When an async method iterates a collection and performs an independent async I/O call for each element, the implementation must use `Task.WhenAll` (or equivalent) to execute the calls concurrently rather than awaiting each call sequentially in a loop. `FollowLinksAsync` is the primary case: it fetches multiple `LinkObject` entries for the same rel concurrently, buffers the results, and then yields them sequentially via `IAsyncEnumerable<Resource?>`. The return type of `FollowLinksAsync` is not changed.

---

## Error Handling Summary

| Scenario | Behavior |
|---|---|
| Rel not found on resource | Throw `HalLinkNotFoundException` (before any HTTP) |
| Duplicate rels on resource | Throw `InvalidOperationException` (before any HTTP) |
| HTTP 2xx with body | Deserialize and return |
| HTTP 204 or explicit `Content-Length: 0` | Return `null`; `DeleteAsync` completes normally (no body, no Content-Type check) |
| HTTP 3xx | `HttpClient` follows redirects; non-redirect throws `HttpRequestException` |
| HTTP 400 / 401 / 403 / 409 | Throw `HttpRequestException` |
| HTTP 404 | Return `null` (DELETE completes normally) |
| HTTP 5xx | Throw `HttpRequestException` |
| Network / timeout | `HttpClient` throws naturally |
| Non-HAL Content-Type, `StrictContentType = false` | Return `null` |
| Non-HAL Content-Type, `StrictContentType = true` | Throw `HalResponseException` |

---

## Integration Story

The following shows two ways to register `HalClient` in a .NET application:

> **Note:** Both registration options require the `Chatter.Rest.Hal.Client.DependencyInjection` package. To use `HalClient` without DI, construct it directly: `new HalClient(httpClient, new HalClientOptions())`.

### Option A -- simple registration

```csharp
// Manages HttpClient internally
services.AddHalClient(options =>
{
    options.AcceptMediaType = "application/hal+json";
    options.StrictContentType = true;
});
```

### Option B -- compose with IHttpClientFactory

```csharp
// Full IHttpClientFactory lifecycle: Polly, DelegatingHandlers, named clients
services.AddHttpClient("hal", c =>
        c.BaseAddress = new Uri("https://api.example.com"))
    .AddHalOptions(options =>
    {
        options.StrictContentType = true;
    });
```

Both register `IHalClient` -> `HalClient` in DI. Option B uses a named client `"hal"` to configure the `HttpClient`; `AddHalOptions` registers `IHalClient` via `AddTypedClient<IHalClient>` keyed to that named client.

---

## Behavioral Scenarios

These scenarios describe end-to-end behavior for test derivation.

### Scenario: Follow a single link

1. Caller has a `Resource` with `_links: { self: { href: "/orders" }, item: { href: "/orders/42" } }`
2. Caller calls `resource.FollowLinkAsync("item", halClient)`
3. `HalClient` sends `GET /orders/42` with `Accept: application/hal+json`
4. Server responds with `200 OK` and a HAL resource
5. Method returns `Resource` deserialized from the response

### Scenario: Follow a typed link

1. Caller has a `Resource` with `_links: { details: { href: "/orders/42/details" } }`
2. Caller calls `resource.FollowLinkAsync<OrderDetails>("details", halClient)`
3. `HalClient` sends `GET /orders/42/details` with `Accept: application/hal+json`
4. Server responds with `200 OK` and a HAL resource with state
5. Method returns `Resource<OrderDetails>` with deserialized state

### Scenario: Follow a templated link

1. Caller has a `Resource` with `_links: { find: { href: "/orders/{id}", templated: true } }`
2. Caller calls `resource.FollowLinkAsync("find", halClient, new { id = "42" })`
3. `LinkObject.Expand()` resolves href to `/orders/42`
4. `HalClient` sends `GET /orders/42`
5. Method returns the deserialized `Resource`

### Scenario: Follow array-valued links

1. Caller has a `Resource` with `_links: { item: [{ href: "/orders/1" }, { href: "/orders/2" }, { href: "/orders/3" }] }`
2. Caller calls `resource.FollowLinksAsync("item", halClient)`
3. Method sends `GET /orders/1`, `GET /orders/2`, `GET /orders/3` concurrently via `Task.WhenAll`
4. Buffers all results, then yields each `Resource?` sequentially via `IAsyncEnumerable`

### Scenario: Post to a linked resource

1. Caller has a `Resource` with `_links: { create: { href: "/orders" } }`
2. Caller calls `resource.PostToAsync("create", new { product = "Widget" }, halClient)`
3. `HalClient` sends `POST /orders` with JSON body and `Accept: application/hal+json`
4. Server responds with `201 Created` and a HAL resource
5. Method returns the deserialized `Resource`

### Scenario: Delete a linked resource

1. Caller has a `Resource` with `_links: { cancel: { href: "/orders/42/cancel" } }`
2. Caller calls `resource.DeleteToAsync("cancel", halClient)`
3. `HalClient` sends `DELETE /orders/42/cancel`
4. Method returns `Task` (no body deserialization)

### Scenario: Rel not found

1. Caller has a `Resource` with `_links: { self: { href: "/orders" } }`
2. Caller calls `resource.FollowLinkAsync("nonexistent", halClient)`
3. Method throws `HalLinkNotFoundException` with rel `"nonexistent"` and self href `"/orders"`
4. No HTTP request is made

### Scenario: HTTP 404 response

1. Caller calls `resource.FollowLinkAsync("item", halClient)`
2. `HalClient` sends `GET /orders/42`
3. Server responds with `404 Not Found`
4. Method returns `null`

### Scenario: Non-HAL response with strict mode

1. `HalClientOptions.StrictContentType = true`
2. Caller calls `resource.FollowLinkAsync("health", halClient)`
3. `HalClient` sends `GET /health`
4. Server responds with `200 OK` and `Content-Type: text/plain`
5. Method throws `HalResponseException`

### Scenario: Non-HAL response with lenient mode

1. `HalClientOptions.StrictContentType = false` (default)
2. Caller calls `resource.FollowLinkAsync("health", halClient)`
3. `HalClient` sends `GET /health`
4. Server responds with `200 OK` and `Content-Type: text/plain`
5. Method returns `null`

### Scenario: 204 No Content on mutation

1. Caller has a `Resource` with `_links: { update: { href: "/orders/42" } }`
2. Caller calls `resource.PutToAsync("update", new { status = "shipped" }, halClient)`
3. `HalClient` sends `PUT /orders/42` with JSON body and `Accept: application/hal+json`
4. Server responds with `204 No Content` (no body)
5. Method returns `null` without performing Content-Type validation

### Scenario: Convenience extension on raw HttpClient

1. Caller has an `HttpClient` but no `HalClient`
2. Caller calls `httpClient.GetHalAsync(new Uri("/orders"))`
3. Extension sends `GET /orders` with `Accept: application/hal+json`
4. Server responds with `200 OK` and a HAL resource
5. Method returns the deserialized `Resource`

---

## Out of Scope (v1)

The following capabilities are explicitly deferred and must not be implemented in the initial version:

- **Automatic pagination** -- `FollowLinksAsync` does not chase `next` links; it iterates a single array-valued rel
- **Retry / resilience** -- no built-in retry; users compose Polly via `IHttpClientFactory`
- **Caching** -- no HTTP cache or ETag handling
- **HAL-FORMS support** -- form-based actions are not interpreted
- **CURIE expansion** -- CURIE rels are not expanded; callers use compact form
- **Batch requests** -- no multi-resource fetch optimization
- **Streaming / SSE** -- no server-sent events or streaming response support
- **Authentication** -- no built-in auth; users configure `DelegatingHandler`s via `IHttpClientFactory`
- **Concurrency limiting for `FollowLinksAsync`** -- no built-in semaphore or throttling; `FollowLinksAsync` uses unbounded `Task.WhenAll`. Callers with very large link arrays compose their own concurrency control via `SemaphoreSlim` or similar.
