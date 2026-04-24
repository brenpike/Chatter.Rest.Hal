# Chatter.Rest.Hal.Client -- Technical Architecture

This document is the source of truth for how the `Chatter.Rest.Hal.Client` package is built. It guides implementation decisions with enough detail for a developer to implement each type without ambiguity. For what the system does, see [requirements.md](requirements.md).

---

## Package Dependency Graph

```
Chatter.Rest.Hal.Client
  -> Chatter.Rest.Hal          (core types: Resource, Resource<T>, LinkObject, LinkCollection, Link)
  -> Chatter.Rest.UriTemplates (RFC 6570 URI template expansion via LinkObject.Expand())
  -> System.Net.Http           (no new transitive dependencies beyond this)
```

---

## Project Location

- Source: `src/Chatter.Rest.Hal.Client/`
- Tests: `test/Chatter.Rest.Hal.Client.Tests/`

---

## Namespaces

| Namespace | Visibility | Contents |
|---|---|---|
| `Chatter.Rest.Hal.Client` | Public | `IHalClient`, `HalClient`, `HalClientOptions`, `HalLinkNotFoundException`, `HalResponseException` |
| `Chatter.Rest.Hal.Client.Extensions` | Public | Extension methods on `Resource` and `HttpClient` |

---

## Key Types

### `HalClientOptions`

Configuration object for `HalClient`. Supports direct construction and the `IOptions<T>` pattern for DI.

```csharp
namespace Chatter.Rest.Hal.Client;

public sealed class HalClientOptions
{
    /// <summary>Default Accept header value. Default: "application/hal+json".</summary>
    public string MediaType { get; set; } = "application/hal+json";

    /// <summary>
    /// When true, non-HAL responses (wrong Content-Type) throw HalResponseException.
    /// When false (default), returns null.
    /// </summary>
    public bool StrictContentType { get; set; } = false;

    /// <summary>JsonSerializerOptions used for deserialization. Null = library defaults.</summary>
    public JsonSerializerOptions? JsonOptions { get; set; }
}
```

---

### `IHalClient`

The primary abstraction for HAL HTTP operations. Callers depend on this interface for testability.

```csharp
namespace Chatter.Rest.Hal.Client;

public interface IHalClient
{
    Task<Resource?> GetAsync(Uri uri, CancellationToken cancellationToken = default);
    Task<Resource<T>?> GetAsync<T>(Uri uri, CancellationToken cancellationToken = default);
    Task<Resource?> PostAsync(Uri uri, object body, CancellationToken cancellationToken = default);
    Task<Resource?> PostAsync(Uri uri, HttpContent content, CancellationToken cancellationToken = default);
    Task<Resource?> PutAsync(Uri uri, object body, CancellationToken cancellationToken = default);
    Task<Resource?> PutAsync(Uri uri, HttpContent content, CancellationToken cancellationToken = default);
    Task<Resource?> PatchAsync(Uri uri, object body, CancellationToken cancellationToken = default);
    Task<Resource?> PatchAsync(Uri uri, HttpContent content, CancellationToken cancellationToken = default);
    Task DeleteAsync(Uri uri, CancellationToken cancellationToken = default);
}
```

---

### `HalClient`

Sealed implementation of `IHalClient`. Wraps an `HttpClient` and applies HAL-specific request/response behavior.

```csharp
namespace Chatter.Rest.Hal.Client;

public sealed class HalClient : IHalClient
{
    public HalClient(HttpClient httpClient, HalClientOptions options);
    public HalClient(HttpClient httpClient, IOptions<HalClientOptions> options);
}
```

**Fields stored:**
- `_httpClient` -- the underlying `HttpClient`
- `_options` -- resolved `HalClientOptions` (unwrapped from `IOptions<T>` if applicable)

**Constructor behavior:**
- The `IOptions<HalClientOptions>` constructor unwraps `options.Value` and delegates to the primary constructor.

---

### `HalLinkNotFoundException`

Thrown before any HTTP request when a requested rel is not found on the resource.

```csharp
namespace Chatter.Rest.Hal.Client;

public sealed class HalLinkNotFoundException : Exception
{
    public string Rel { get; }
    public string? SelfHref { get; }

    public HalLinkNotFoundException(string rel, string? selfHref);
}
```

**Message format:** `"Link relation '{rel}' not found on resource '{selfHref}'."`

---

### `HalResponseException`

Thrown when `StrictContentType` is `true` and the response `Content-Type` does not indicate a HAL media type.

```csharp
namespace Chatter.Rest.Hal.Client;

public sealed class HalResponseException : Exception
{
    public Uri RequestUri { get; }
    public string? ContentType { get; }

    public HalResponseException(Uri requestUri, string? contentType);
}
```

**Message format:** `"Response from '{requestUri}' has Content-Type '{contentType}', expected HAL media type."`

---

## HalClient Request/Response Flow

All HTTP methods in `HalClient` follow the same core flow. The verb-specific behavior is noted where it differs.

```
SendAsync(HttpMethod method, Uri uri, HttpContent? content, CancellationToken ct):
    // 1. Build request
    request = new HttpRequestMessage(method, uri)
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(_options.MediaType))
    if content is not null:
        request.Content = content

    // 2. Send request
    response = await _httpClient.SendAsync(request, ct)

    // 3. Handle 404
    if response.StatusCode == HttpStatusCode.NotFound:
        return null

    // 4. Ensure success (throws HttpRequestException for 5xx, etc.)
    response.EnsureSuccessStatusCode()

    // 5. Validate Content-Type
    responseContentType = response.Content.Headers.ContentType?.MediaType
    if responseContentType is not a HAL media type:
        if _options.StrictContentType:
            throw new HalResponseException(uri, responseContentType)
        return null

    // 6. Deserialize
    json = await response.Content.ReadAsStringAsync(ct)
    jsonOptions = _options.JsonOptions ?? defaultHalJsonOptions
    return JsonSerializer.Deserialize<Resource>(json, jsonOptions)
```

**Verb-specific behavior:**

- **`GetAsync`:** `method = HttpMethod.Get`, no content.
- **`GetAsync<T>`:** Same as `GetAsync` but deserializes as `Resource<T>`.
- **`PostAsync(object body)`:** `method = HttpMethod.Post`, body serialized to `StringContent` with `application/json` media type.
- **`PostAsync(HttpContent)`:** `method = HttpMethod.Post`, raw content passed through.
- **`PutAsync`:** Same pattern as `PostAsync` with `HttpMethod.Put`.
- **`PatchAsync`:** Same pattern as `PostAsync` with `HttpMethod.Patch`.
- **`DeleteAsync`:** `method = HttpMethod.Delete`, no content, no response deserialization. Calls `EnsureSuccessStatusCode()` and returns. Returns normally on 404.

**Object body serialization:** When a method accepts `object body`, the body is serialized via `JsonSerializer.Serialize(body, _options.JsonOptions ?? defaultHalJsonOptions)` and wrapped in `StringContent` with media type `"application/json"` and `UTF-8` encoding.

---

## DI Registration

### `HalClientServiceCollectionExtensions`

```csharp
namespace Chatter.Rest.Hal.Client;

public static class HalClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers IHalClient/HalClient with an internally managed HttpClient.
    /// </summary>
    public static IServiceCollection AddHalClient(
        this IServiceCollection services,
        Action<HalClientOptions> configure);
}
```

**Registration sequence:**
1. Configure `HalClientOptions` via `services.Configure(configure)`
2. Register `HttpClient` as a typed client for `HalClient`
3. Register `IHalClient` -> `HalClient` (transient, resolved via DI)
4. Return `services` for chaining

### `HalClientHttpClientBuilderExtensions`

```csharp
namespace Chatter.Rest.Hal.Client;

public static class HalClientHttpClientBuilderExtensions
{
    /// <summary>
    /// Composes HalClientOptions with an IHttpClientFactory-managed typed client.
    /// </summary>
    public static IHttpClientBuilder AddHalOptions(
        this IHttpClientBuilder builder,
        Action<HalClientOptions> configure);
}
```

**Registration sequence:**
1. Configure `HalClientOptions` via `builder.Services.Configure(configure)`
2. Register `IHalClient` -> `HalClient` (uses the typed `HttpClient` from the builder)
3. Return `builder` for chaining

---

## Link Traversal -- Extension Methods on `Resource`

All extension methods live in `Chatter.Rest.Hal.Client.Extensions`.

### Link resolution (shared logic)

```
ResolveLink(Resource resource, string rel):
    link = resource.Links?.FirstOrDefault(l => l.Rel == rel)
    if link is null or link.LinkObjects is empty:
        selfHref = resource.Links?
            .FirstOrDefault(l => l.Rel == "self")?
            .LinkObjects?.FirstOrDefault()?.Href
        throw new HalLinkNotFoundException(rel, selfHref)
    return link
```

This resolution is used by all `FollowLink`, `FollowLinks`, `PostTo`, `PutTo`, `PatchTo`, and `DeleteTo` methods. The `HalLinkNotFoundException` is thrown before any HTTP request (REQ-32).

---

### `FollowLink` -- single link, GET

```csharp
// Non-templated, untyped
public static Task<Resource?> FollowLink(
    this Resource resource,
    string rel,
    IHalClient client,
    CancellationToken ct = default);

// Non-templated, typed
public static Task<Resource<T>?> FollowLink<T>(
    this Resource resource,
    string rel,
    IHalClient client,
    CancellationToken ct = default);

// Templated, untyped
public static Task<Resource?> FollowLink(
    this Resource resource,
    string rel,
    IHalClient client,
    object variables,
    CancellationToken ct = default);

// Templated, typed
public static Task<Resource<T>?> FollowLink<T>(
    this Resource resource,
    string rel,
    IHalClient client,
    object variables,
    CancellationToken ct = default);
```

**Non-templated flow:**
1. Call `ResolveLink(resource, rel)` to get the `Link`
2. Extract `linkObject = link.LinkObjects[0]`
3. Construct `Uri` from `linkObject.Href`
4. Call `client.GetAsync(uri, ct)` or `client.GetAsync<T>(uri, ct)`

**Templated flow:**
1. Call `ResolveLink(resource, rel)` to get the `Link`
2. Extract `linkObject = link.LinkObjects[0]`
3. Call `linkObject.Expand(variables)` to resolve the templated href (delegates to `Chatter.Rest.UriTemplates`)
4. Construct `Uri` from the expanded href
5. Call `client.GetAsync(uri, ct)` or `client.GetAsync<T>(uri, ct)`

---

### `FollowLinks` -- array-valued rel, GET

```csharp
public static IAsyncEnumerable<Resource?> FollowLinks(
    this Resource resource,
    string rel,
    IHalClient client,
    CancellationToken ct = default);
```

**Flow:**
1. Call `ResolveLink(resource, rel)` to get the `Link`
2. For each `linkObject` in `link.LinkObjects`:
   a. Construct `Uri` from `linkObject.Href`
   b. `yield return await client.GetAsync(uri, ct)`

Each link is fetched sequentially. The caller controls iteration via `await foreach`.

---

### `PostTo`, `PutTo`, `PatchTo` -- mutation

Three overloads per verb:

```csharp
// Typed body + typed response
public static Task<Resource<TResponse>?> PostTo<TBody, TResponse>(
    this Resource resource,
    string rel,
    TBody body,
    IHalClient client,
    CancellationToken ct = default);

// Object body
public static Task<Resource?> PostTo(
    this Resource resource,
    string rel,
    object body,
    IHalClient client,
    CancellationToken ct = default);

// Raw HttpContent
public static Task<Resource?> PostTo(
    this Resource resource,
    string rel,
    HttpContent content,
    IHalClient client,
    CancellationToken ct = default);
```

`PutTo` and `PatchTo` follow the same three-overload pattern, calling `client.PutAsync` and `client.PatchAsync` respectively.

**Flow (all mutation methods):**
1. Call `ResolveLink(resource, rel)` to get the `Link`
2. Extract `linkObject = link.LinkObjects[0]`
3. Construct `Uri` from `linkObject.Href`
4. Call the appropriate `client.PostAsync` / `client.PutAsync` / `client.PatchAsync` overload

---

### `DeleteTo` -- deletion

```csharp
public static Task DeleteTo(
    this Resource resource,
    string rel,
    IHalClient client,
    CancellationToken ct = default);
```

**Flow:**
1. Call `ResolveLink(resource, rel)` to get the `Link`
2. Extract `linkObject = link.LinkObjects[0]`
3. Construct `Uri` from `linkObject.Href`
4. Call `client.DeleteAsync(uri, ct)`

Returns `Task`, not `Task<Resource?>`. No response body is deserialized.

---

### Raw `HttpClient` overloads

Every `FollowLink`, `FollowLinks`, `PostTo`, `PutTo`, `PatchTo`, and `DeleteTo` method has a corresponding overload that accepts `HttpClient` in place of `IHalClient`. These overloads construct a `HalClient` with default `HalClientOptions` and delegate to the `IHalClient` overload.

```csharp
// Example: raw HttpClient overload for FollowLink
public static Task<Resource?> FollowLink(
    this Resource resource,
    string rel,
    HttpClient client,
    CancellationToken ct = default)
{
    var halClient = new HalClient(client, new HalClientOptions());
    return resource.FollowLink(rel, halClient, ct);
}
```

---

## HttpClient Convenience Extensions

Top-level fetch methods that do not require constructing a `HalClient` or having an existing `Resource`.

```csharp
namespace Chatter.Rest.Hal.Client.Extensions;

public static class HttpClientHalExtensions
{
    public static Task<Resource?> GetHalAsync(
        this HttpClient client,
        Uri uri,
        HalClientOptions? options = null,
        CancellationToken ct = default);

    public static Task<Resource<T>?> GetHalAsync<T>(
        this HttpClient client,
        Uri uri,
        HalClientOptions? options = null,
        CancellationToken ct = default);

    public static Task<Resource?> PostHalAsync(
        this HttpClient client,
        Uri uri,
        object body,
        HalClientOptions? options = null,
        CancellationToken ct = default);

    public static Task<Resource?> PostHalAsync(
        this HttpClient client,
        Uri uri,
        HttpContent content,
        HalClientOptions? options = null,
        CancellationToken ct = default);
}
```

**Flow:** Each method creates a `HalClient(client, options ?? new HalClientOptions())` and delegates to the corresponding `IHalClient` method.

---

## Error Handling Summary

| Condition | Behavior | Error type |
|---|---|---|
| Rel not found on resource | Throw before HTTP | `HalLinkNotFoundException` |
| HTTP 404 | Return `null` | -- |
| HTTP 5xx | Throw | `HttpRequestException` (from `HttpClient`) |
| Network / timeout | Throw | `HttpRequestException` / `TaskCanceledException` (from `HttpClient`) |
| Non-HAL Content-Type, strict off | Return `null` | -- |
| Non-HAL Content-Type, strict on | Throw | `HalResponseException` |

---

## Serialization

**Request serialization:** Object bodies are serialized via `JsonSerializer.Serialize(body, jsonOptions)` where `jsonOptions` is `HalClientOptions.JsonOptions` or library defaults. The resulting JSON is wrapped in `StringContent` with `Content-Type: application/json; charset=utf-8`.

**Response deserialization:** Response bodies are deserialized via `JsonSerializer.Deserialize<Resource>(json, jsonOptions)` or `JsonSerializer.Deserialize<Resource<T>>(json, jsonOptions)` with HAL converters applied. The `jsonOptions` are `HalClientOptions.JsonOptions` or library defaults (which include `AddHalConverters()`).

---

## Test Strategy

### Unit tests

**`HalClient` request behavior:**
- Sets `Accept` header to `HalClientOptions.MediaType` on all requests
- Sends correct HTTP method for each verb
- Serializes object body as JSON `StringContent`
- Passes raw `HttpContent` through unchanged
- Includes `CancellationToken` in all HTTP calls

**`HalClient` response handling:**
- Returns `null` on HTTP 404
- Throws `HttpRequestException` on 5xx
- Returns deserialized `Resource` on success with HAL Content-Type
- Returns deserialized `Resource<T>` on typed GET success
- Returns `null` on non-HAL Content-Type when `StrictContentType = false`
- Throws `HalResponseException` on non-HAL Content-Type when `StrictContentType = true`
- `DeleteAsync` returns normally on 404
- `DeleteAsync` does not attempt response deserialization
- Uses custom `JsonOptions` when provided

**`HalLinkNotFoundException`:**
- Carries correct `Rel` and `SelfHref` properties
- Message format matches spec

**`HalResponseException`:**
- Carries correct `RequestUri` and `ContentType` properties
- Message format matches spec

### Extension method tests

**`FollowLink`:**
- Resolves non-templated link and calls `GetAsync`
- Resolves templated link via `LinkObject.Expand()` and calls `GetAsync`
- Throws `HalLinkNotFoundException` when rel is absent
- Returns typed `Resource<T>` for generic overload
- Raw `HttpClient` overload delegates correctly

**`FollowLinks`:**
- Iterates all `LinkObject` entries for array-valued rel
- Yields each fetched `Resource?` via `IAsyncEnumerable`
- Throws `HalLinkNotFoundException` when rel is absent

**`PostTo` / `PutTo` / `PatchTo`:**
- Resolves link and calls correct HTTP method
- Typed overload returns `Resource<TResponse>`
- Object body overload serializes and sends
- `HttpContent` overload passes through
- Throws `HalLinkNotFoundException` when rel is absent

**`DeleteTo`:**
- Resolves link and calls `DeleteAsync`
- Returns `Task` (no body)
- Throws `HalLinkNotFoundException` when rel is absent

**`GetHalAsync` / `PostHalAsync` (HttpClient extensions):**
- Constructs `HalClient` with provided or default options
- Delegates to correct `IHalClient` method

### Integration tests

**DI registration -- `AddHalClient`:**
- Resolves `IHalClient` from container
- Applies configured `HalClientOptions`
- `HttpClient` is managed internally

**DI registration -- `AddHalOptions` with `IHttpClientFactory`:**
- Resolves `IHalClient` from container
- `HttpClient` lifecycle managed by factory
- Options are applied correctly

**End-to-end traversal:**
- Mock `HttpClient` (via `HttpMessageHandler`) returning HAL JSON
- GET root -> `FollowLink` to sub-resource -> `PostTo` to create -> `DeleteTo` to remove
- Verify correct URIs, methods, headers, and deserialized responses at each step
