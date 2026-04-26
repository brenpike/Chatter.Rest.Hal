# Chatter.Rest.Hal.Client -- Technical Architecture

This document is the source of truth for how the `Chatter.Rest.Hal.Client` package is built. It guides implementation decisions with enough detail for a developer to implement each type without ambiguity. For what the system does, see [requirements.md](requirements.md).

---

## Package Dependency Graph

```
Chatter.Rest.Hal.Client
  -> Chatter.Rest.Hal                             (core types: Resource, LinkObject, LinkCollection, Link)
  -> System.Net.Http
  -> Microsoft.Extensions.Logging.Abstractions   (ILogger<T>, NullLogger<T> — logging only; no DI, options, or HTTP factory)

Chatter.Rest.Hal.Client.DependencyInjection
  -> Chatter.Rest.Hal.Client
  -> Microsoft.Extensions.Http
  -> Microsoft.Extensions.DependencyInjection.Abstractions
  -> Microsoft.Extensions.Options
  -> Microsoft.Extensions.Logging.Abstractions
```

The base package takes a minimal dependency on `Microsoft.Extensions.Logging.Abstractions` for optional structured logging. DI registration, `IOptions<T>`, and `IHttpClientFactory` remain in the companion package. `Chatter.Rest.UriTemplates` is a transitive dependency via `Chatter.Rest.Hal` — the client package calls `LinkObject.Expand()` (defined in `Chatter.Rest.Hal`), not URI template APIs directly.

---

## Project Location

- Source: `src/Chatter.Rest.Hal.Client/`
- Tests:  `test/Chatter.Rest.Hal.Client.Tests/`

- Source: `src/Chatter.Rest.Hal.Client.DependencyInjection/`
- Tests:  `test/Chatter.Rest.Hal.Client.DependencyInjection.Tests/`

---

## Target Frameworks

Both packages target `net8.0` and later.

`net8.0` provides `HttpMethod.Patch`, `ReadAsStreamAsync(CancellationToken)`, and all other APIs used by this package natively — no compatibility shims or workarounds are required.

---

## Namespaces

| Namespace | Visibility | Contents |
|---|---|---|
| `Chatter.Rest.Hal.Client` | Public | `IHalClient`, `HalClient`, `HalClientOptions`, `Resource<T>`, `HalLinkNotFoundException`, `HalResponseException` |
| `Chatter.Rest.Hal.Client.Extensions` | Public | Extension methods on `Resource` and `HttpClient` (all `IHalClient` overloads provided in no-logger / with-logger pairs) |
| `Chatter.Rest.Hal.Client.DependencyInjection` | Public | `HalClientServiceCollectionExtensions`, `HalClientHttpClientBuilderExtensions` |

---

## Key Types

### `HalClientOptions`

Configuration object for `HalClient`. Supports direct construction. The DI companion package wraps it in `IOptions<HalClientOptions>` for configuration binding.

```csharp
namespace Chatter.Rest.Hal.Client;

public sealed class HalClientOptions
{
    /// <summary>
    /// Value sent in the Accept header on all requests. May include media type parameters.
    /// Default: "application/hal+json".
    /// </summary>
    public string AcceptMediaType { get; set; } = "application/hal+json";

    /// <summary>
    /// Bare media type (no parameters) used for response Content-Type validation.
    /// Comparison is case-insensitive against <c>response.Content.Headers.ContentType.MediaType</c>
    /// (which strips parameters). Must not include parameters such as <c>charset</c>.
    /// Default: "application/hal+json".
    /// </summary>
    public string ExpectedMediaType { get; set; } = "application/hal+json";

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

### `Resource<T>`

A typed wrapper around `Resource` that exposes the embedded state as a strongly-typed value. Defined in this package; not part of the core library.

```csharp
namespace Chatter.Rest.Hal.Client;

/// <summary>
/// A typed wrapper around <see cref="Resource"/> that exposes the embedded state
/// as a strongly-typed value. Defined in this package; not part of the core library.
/// </summary>
public sealed class Resource<T> where T : class
{
    private readonly Resource _inner;
    private readonly JsonSerializerOptions? _jsonOptions;

    /// <summary>
    /// Wraps an untyped resource, optionally capturing the serializer options used
    /// during deserialization so that State() can honour custom converters.
    /// </summary>
    public Resource(Resource inner, JsonSerializerOptions? jsonOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _jsonOptions = jsonOptions;
    }

    /// <summary>The underlying untyped resource.</summary>
    public Resource Inner => _inner;

    /// <summary>Links from the underlying resource.</summary>
    public LinkCollection? Links => _inner.Links;

    /// <summary>Embedded resources from the underlying resource.</summary>
    public EmbeddedResourceCollection? Embedded => _inner.Embedded;

    /// <summary>
    /// Deserializes the resource state as <typeparamref name="T"/> using the
    /// JsonSerializerOptions supplied at construction (if any).
    ///
    /// Delegates to <c>Resource.State&lt;T&gt;(JsonSerializerOptions?)</c>, which
    /// accepts options directly. No internal accessor or re-serialization is needed.
    /// </summary>
    public T? State() => _inner.State<T>(_jsonOptions);

    // ── Typed traversal (single explicit type param; T of receiver is already known) ──

    /// <summary>Follows a non-templated link as <typeparamref name="TResult"/>.</summary>
    public Task<Resource<TResult>?> FollowLinkAsAsync<TResult>(
        string rel, IHalClient client, CancellationToken ct = default) where TResult : class
        => _inner.FollowLinkAsync<TResult>(rel, client, ct);

    /// <summary>Follows a non-templated link as <typeparamref name="TResult"/> with rel-resolution logging.</summary>
    public Task<Resource<TResult>?> FollowLinkAsAsync<TResult>(
        string rel, IHalClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => _inner.FollowLinkAsync<TResult>(rel, client, logger, ct);

    /// <summary>Follows a templated link as <typeparamref name="TResult"/>.</summary>
    public Task<Resource<TResult>?> FollowLinkAsAsync<TResult>(
        string rel, IHalClient client, object variables, CancellationToken ct = default) where TResult : class
        => _inner.FollowLinkAsync<TResult>(rel, client, variables, ct);

    /// <summary>Follows a templated link as <typeparamref name="TResult"/> with rel-resolution logging.</summary>
    public Task<Resource<TResult>?> FollowLinkAsAsync<TResult>(
        string rel, IHalClient client, object variables, ILogger logger, CancellationToken ct = default) where TResult : class
        => _inner.FollowLinkAsync<TResult>(rel, client, variables, logger, ct);

    /// <summary>Follows all links for an array-valued rel, yielding each as <typeparamref name="TResult"/>.</summary>
    public IAsyncEnumerable<Resource<TResult>?> FollowLinksAsAsync<TResult>(
        string rel, IHalClient client, CancellationToken ct = default) where TResult : class
        => _inner.FollowLinksAsync<TResult>(rel, client, ct);

    /// <summary>Follows all links for an array-valued rel as <typeparamref name="TResult"/> with logging.</summary>
    public IAsyncEnumerable<Resource<TResult>?> FollowLinksAsAsync<TResult>(
        string rel, IHalClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => _inner.FollowLinksAsync<TResult>(rel, client, logger, ct);

    /// <summary>POSTs to a linked resource and returns the response as <typeparamref name="TResult"/>. Both TBody and TResult must be specified explicitly.</summary>
    public Task<Resource<TResult>?> PostToAsAsync<TBody, TResult>(
        string rel, TBody body, IHalClient client, CancellationToken ct = default) where TResult : class
        => _inner.PostToAsync<TBody, TResult>(rel, body, client, ct);

    /// <summary>POSTs to a linked resource and returns the response as <typeparamref name="TResult"/>. Both TBody and TResult must be specified explicitly. With logging.</summary>
    public Task<Resource<TResult>?> PostToAsAsync<TBody, TResult>(
        string rel, TBody body, IHalClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => _inner.PostToAsync<TBody, TResult>(rel, body, client, logger, ct);

    /// <summary>POSTs an object body to a linked resource as <typeparamref name="TResult"/>. Single explicit type param; body typed as object.</summary>
    public Task<Resource<TResult>?> PostToAsAsync<TResult>(
        string rel, object body, IHalClient client, CancellationToken ct = default) where TResult : class
        => _inner.PostToAsync<object, TResult>(rel, body, client, ct);

    /// <summary>POSTs an object body to a linked resource as <typeparamref name="TResult"/>. With logging.</summary>
    public Task<Resource<TResult>?> PostToAsAsync<TResult>(
        string rel, object body, IHalClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => _inner.PostToAsync<object, TResult>(rel, body, client, logger, ct);

    /// <summary>POSTs raw content to a linked resource and returns the response as <typeparamref name="TResult"/>.</summary>
    public Task<Resource<TResult>?> PostToAsAsync<TResult>(
        string rel, HttpContent content, IHalClient client, CancellationToken ct = default) where TResult : class
        => _inner.PostToAsync<TResult>(rel, content, client, ct);

    /// <summary>POSTs raw content to a linked resource and returns the response as <typeparamref name="TResult"/>. With logging.</summary>
    public Task<Resource<TResult>?> PostToAsAsync<TResult>(
        string rel, HttpContent content, IHalClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => _inner.PostToAsync<TResult>(rel, content, client, logger, ct);

    /// <summary>PUTs to a linked resource and returns the response as <typeparamref name="TResult"/>. Both TBody and TResult must be specified explicitly.</summary>
    public Task<Resource<TResult>?> PutToAsAsync<TBody, TResult>(
        string rel, TBody body, IHalClient client, CancellationToken ct = default) where TResult : class
        => _inner.PutToAsync<TBody, TResult>(rel, body, client, ct);

    /// <summary>PUTs to a linked resource as <typeparamref name="TResult"/>. With logging.</summary>
    public Task<Resource<TResult>?> PutToAsAsync<TBody, TResult>(
        string rel, TBody body, IHalClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => _inner.PutToAsync<TBody, TResult>(rel, body, client, logger, ct);

    /// <summary>PUTs an object body to a linked resource as <typeparamref name="TResult"/>. Single explicit type param; body typed as object.</summary>
    public Task<Resource<TResult>?> PutToAsAsync<TResult>(
        string rel, object body, IHalClient client, CancellationToken ct = default) where TResult : class
        => _inner.PutToAsync<object, TResult>(rel, body, client, ct);

    /// <summary>PUTs an object body to a linked resource as <typeparamref name="TResult"/>. With logging.</summary>
    public Task<Resource<TResult>?> PutToAsAsync<TResult>(
        string rel, object body, IHalClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => _inner.PutToAsync<object, TResult>(rel, body, client, logger, ct);

    /// <summary>PUTs raw content to a linked resource and returns the response as <typeparamref name="TResult"/>.</summary>
    public Task<Resource<TResult>?> PutToAsAsync<TResult>(
        string rel, HttpContent content, IHalClient client, CancellationToken ct = default) where TResult : class
        => _inner.PutToAsync<TResult>(rel, content, client, ct);

    /// <summary>PUTs raw content to a linked resource as <typeparamref name="TResult"/>. With logging.</summary>
    public Task<Resource<TResult>?> PutToAsAsync<TResult>(
        string rel, HttpContent content, IHalClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => _inner.PutToAsync<TResult>(rel, content, client, logger, ct);

    /// <summary>PATCHes a linked resource and returns the response as <typeparamref name="TResult"/>. Both TBody and TResult must be specified explicitly.</summary>
    public Task<Resource<TResult>?> PatchToAsAsync<TBody, TResult>(
        string rel, TBody body, IHalClient client, CancellationToken ct = default) where TResult : class
        => _inner.PatchToAsync<TBody, TResult>(rel, body, client, ct);

    /// <summary>PATCHes a linked resource as <typeparamref name="TResult"/>. With logging.</summary>
    public Task<Resource<TResult>?> PatchToAsAsync<TBody, TResult>(
        string rel, TBody body, IHalClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => _inner.PatchToAsync<TBody, TResult>(rel, body, client, logger, ct);

    /// <summary>PATCHes an object body to a linked resource as <typeparamref name="TResult"/>. Single explicit type param; body typed as object.</summary>
    public Task<Resource<TResult>?> PatchToAsAsync<TResult>(
        string rel, object body, IHalClient client, CancellationToken ct = default) where TResult : class
        => _inner.PatchToAsync<object, TResult>(rel, body, client, ct);

    /// <summary>PATCHes an object body to a linked resource as <typeparamref name="TResult"/>. With logging.</summary>
    public Task<Resource<TResult>?> PatchToAsAsync<TResult>(
        string rel, object body, IHalClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => _inner.PatchToAsync<object, TResult>(rel, body, client, logger, ct);

    /// <summary>PATCHes raw content to a linked resource and returns the response as <typeparamref name="TResult"/>.</summary>
    public Task<Resource<TResult>?> PatchToAsAsync<TResult>(
        string rel, HttpContent content, IHalClient client, CancellationToken ct = default) where TResult : class
        => _inner.PatchToAsync<TResult>(rel, content, client, ct);

    /// <summary>PATCHes raw content to a linked resource as <typeparamref name="TResult"/>. With logging.</summary>
    public Task<Resource<TResult>?> PatchToAsAsync<TResult>(
        string rel, HttpContent content, IHalClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => _inner.PatchToAsync<TResult>(rel, content, client, logger, ct);

    // ── HttpClient convenience overloads (construct HalClient with default options) ──

    /// <inheritdoc cref="FollowLinkAsAsync{TResult}(string, IHalClient, CancellationToken)"/>
    public Task<Resource<TResult>?> FollowLinkAsAsync<TResult>(
        string rel, HttpClient client, CancellationToken ct = default) where TResult : class
        => FollowLinkAsAsync<TResult>(rel, new HalClient(client, new HalClientOptions()), ct);

    /// <inheritdoc cref="FollowLinkAsAsync{TResult}(string, IHalClient, ILogger, CancellationToken)"/>
    public Task<Resource<TResult>?> FollowLinkAsAsync<TResult>(
        string rel, HttpClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => FollowLinkAsAsync<TResult>(rel, new HalClient(client, new HalClientOptions()), logger, ct);

    /// <inheritdoc cref="FollowLinkAsAsync{TResult}(string, IHalClient, object, CancellationToken)"/>
    public Task<Resource<TResult>?> FollowLinkAsAsync<TResult>(
        string rel, HttpClient client, object variables, CancellationToken ct = default) where TResult : class
        => FollowLinkAsAsync<TResult>(rel, new HalClient(client, new HalClientOptions()), variables, ct);

    /// <inheritdoc cref="FollowLinkAsAsync{TResult}(string, IHalClient, object, ILogger, CancellationToken)"/>
    public Task<Resource<TResult>?> FollowLinkAsAsync<TResult>(
        string rel, HttpClient client, object variables, ILogger logger, CancellationToken ct = default) where TResult : class
        => FollowLinkAsAsync<TResult>(rel, new HalClient(client, new HalClientOptions()), variables, logger, ct);

    /// <inheritdoc cref="FollowLinksAsAsync{TResult}(string, IHalClient, CancellationToken)"/>
    public IAsyncEnumerable<Resource<TResult>?> FollowLinksAsAsync<TResult>(
        string rel, HttpClient client, CancellationToken ct = default) where TResult : class
        => FollowLinksAsAsync<TResult>(rel, new HalClient(client, new HalClientOptions()), ct);

    /// <inheritdoc cref="FollowLinksAsAsync{TResult}(string, IHalClient, ILogger, CancellationToken)"/>
    public IAsyncEnumerable<Resource<TResult>?> FollowLinksAsAsync<TResult>(
        string rel, HttpClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => FollowLinksAsAsync<TResult>(rel, new HalClient(client, new HalClientOptions()), logger, ct);

    /// <inheritdoc cref="PostToAsAsync{TBody, TResult}(string, TBody, IHalClient, CancellationToken)"/>
    public Task<Resource<TResult>?> PostToAsAsync<TBody, TResult>(
        string rel, TBody body, HttpClient client, CancellationToken ct = default) where TResult : class
        => PostToAsAsync<TBody, TResult>(rel, body, new HalClient(client, new HalClientOptions()), ct);

    /// <inheritdoc cref="PostToAsAsync{TBody, TResult}(string, TBody, IHalClient, ILogger, CancellationToken)"/>
    public Task<Resource<TResult>?> PostToAsAsync<TBody, TResult>(
        string rel, TBody body, HttpClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => PostToAsAsync<TBody, TResult>(rel, body, new HalClient(client, new HalClientOptions()), logger, ct);

    /// <inheritdoc cref="PostToAsAsync{TResult}(string, object, IHalClient, CancellationToken)"/>
    public Task<Resource<TResult>?> PostToAsAsync<TResult>(
        string rel, object body, HttpClient client, CancellationToken ct = default) where TResult : class
        => PostToAsAsync<TResult>(rel, body, new HalClient(client, new HalClientOptions()), ct);

    /// <inheritdoc cref="PostToAsAsync{TResult}(string, object, IHalClient, ILogger, CancellationToken)"/>
    public Task<Resource<TResult>?> PostToAsAsync<TResult>(
        string rel, object body, HttpClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => PostToAsAsync<TResult>(rel, body, new HalClient(client, new HalClientOptions()), logger, ct);

    /// <inheritdoc cref="PostToAsAsync{TResult}(string, HttpContent, IHalClient, CancellationToken)"/>
    public Task<Resource<TResult>?> PostToAsAsync<TResult>(
        string rel, HttpContent content, HttpClient client, CancellationToken ct = default) where TResult : class
        => PostToAsAsync<TResult>(rel, content, new HalClient(client, new HalClientOptions()), ct);

    /// <inheritdoc cref="PostToAsAsync{TResult}(string, HttpContent, IHalClient, ILogger, CancellationToken)"/>
    public Task<Resource<TResult>?> PostToAsAsync<TResult>(
        string rel, HttpContent content, HttpClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => PostToAsAsync<TResult>(rel, content, new HalClient(client, new HalClientOptions()), logger, ct);

    /// <inheritdoc cref="PutToAsAsync{TBody, TResult}(string, TBody, IHalClient, CancellationToken)"/>
    public Task<Resource<TResult>?> PutToAsAsync<TBody, TResult>(
        string rel, TBody body, HttpClient client, CancellationToken ct = default) where TResult : class
        => PutToAsAsync<TBody, TResult>(rel, body, new HalClient(client, new HalClientOptions()), ct);

    /// <inheritdoc cref="PutToAsAsync{TBody, TResult}(string, TBody, IHalClient, ILogger, CancellationToken)"/>
    public Task<Resource<TResult>?> PutToAsAsync<TBody, TResult>(
        string rel, TBody body, HttpClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => PutToAsAsync<TBody, TResult>(rel, body, new HalClient(client, new HalClientOptions()), logger, ct);

    /// <inheritdoc cref="PutToAsAsync{TResult}(string, object, IHalClient, CancellationToken)"/>
    public Task<Resource<TResult>?> PutToAsAsync<TResult>(
        string rel, object body, HttpClient client, CancellationToken ct = default) where TResult : class
        => PutToAsAsync<TResult>(rel, body, new HalClient(client, new HalClientOptions()), ct);

    /// <inheritdoc cref="PutToAsAsync{TResult}(string, object, IHalClient, ILogger, CancellationToken)"/>
    public Task<Resource<TResult>?> PutToAsAsync<TResult>(
        string rel, object body, HttpClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => PutToAsAsync<TResult>(rel, body, new HalClient(client, new HalClientOptions()), logger, ct);

    /// <inheritdoc cref="PutToAsAsync{TResult}(string, HttpContent, IHalClient, CancellationToken)"/>
    public Task<Resource<TResult>?> PutToAsAsync<TResult>(
        string rel, HttpContent content, HttpClient client, CancellationToken ct = default) where TResult : class
        => PutToAsAsync<TResult>(rel, content, new HalClient(client, new HalClientOptions()), ct);

    /// <inheritdoc cref="PutToAsAsync{TResult}(string, HttpContent, IHalClient, ILogger, CancellationToken)"/>
    public Task<Resource<TResult>?> PutToAsAsync<TResult>(
        string rel, HttpContent content, HttpClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => PutToAsAsync<TResult>(rel, content, new HalClient(client, new HalClientOptions()), logger, ct);

    /// <inheritdoc cref="PatchToAsAsync{TBody, TResult}(string, TBody, IHalClient, CancellationToken)"/>
    public Task<Resource<TResult>?> PatchToAsAsync<TBody, TResult>(
        string rel, TBody body, HttpClient client, CancellationToken ct = default) where TResult : class
        => PatchToAsAsync<TBody, TResult>(rel, body, new HalClient(client, new HalClientOptions()), ct);

    /// <inheritdoc cref="PatchToAsAsync{TBody, TResult}(string, TBody, IHalClient, ILogger, CancellationToken)"/>
    public Task<Resource<TResult>?> PatchToAsAsync<TBody, TResult>(
        string rel, TBody body, HttpClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => PatchToAsAsync<TBody, TResult>(rel, body, new HalClient(client, new HalClientOptions()), logger, ct);

    /// <inheritdoc cref="PatchToAsAsync{TResult}(string, object, IHalClient, CancellationToken)"/>
    public Task<Resource<TResult>?> PatchToAsAsync<TResult>(
        string rel, object body, HttpClient client, CancellationToken ct = default) where TResult : class
        => PatchToAsAsync<TResult>(rel, body, new HalClient(client, new HalClientOptions()), ct);

    /// <inheritdoc cref="PatchToAsAsync{TResult}(string, object, IHalClient, ILogger, CancellationToken)"/>
    public Task<Resource<TResult>?> PatchToAsAsync<TResult>(
        string rel, object body, HttpClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => PatchToAsAsync<TResult>(rel, body, new HalClient(client, new HalClientOptions()), logger, ct);

    /// <inheritdoc cref="PatchToAsAsync{TResult}(string, HttpContent, IHalClient, CancellationToken)"/>
    public Task<Resource<TResult>?> PatchToAsAsync<TResult>(
        string rel, HttpContent content, HttpClient client, CancellationToken ct = default) where TResult : class
        => PatchToAsAsync<TResult>(rel, content, new HalClient(client, new HalClientOptions()), ct);

    /// <inheritdoc cref="PatchToAsAsync{TResult}(string, HttpContent, IHalClient, ILogger, CancellationToken)"/>
    public Task<Resource<TResult>?> PatchToAsAsync<TResult>(
        string rel, HttpContent content, HttpClient client, ILogger logger, CancellationToken ct = default) where TResult : class
        => PatchToAsAsync<TResult>(rel, content, new HalClient(client, new HalClientOptions()), logger, ct);
}
```

> **Mutation overload shapes for `PostToAsAsync` / `PutToAsAsync` / `PatchToAsAsync`:**
> - `PostToAsAsync<TBody, TResult>(string rel, TBody body, ...)` -- strongly-typed body; **both type arguments must be specified explicitly** (`TBody` is not inferred when `TResult` is provided — C# does not allow partial type argument specification). Use when the body type is known and distinct, e.g. `order.PostToAsAsync<CreateOrder, OrderResult>("rel", body, client)`.
> - `PostToAsAsync<TResult>(string rel, object body, ...)` -- body passed as `object`; single explicit type param. Delegates to `_inner.PostToAsync<object, TResult>(...)`. Use when only the response type matters.
> - `PostToAsAsync<TResult>(string rel, HttpContent content, ...)` -- raw content; single explicit type param.

`Resource<T>` depends on `IHalClient`, `ILogger`, `HttpClient`, and `HttpContent` — all of which are in the same package. The "As" suffix signals "treat the response as `TResult`" and avoids the generic-inference ambiguity of trying to specify both the receiver type `T` and a return type on the same extension method. All `IHalClient`-accepting instance methods have a parallel `HttpClient` overload that wraps the client in `new HalClient(client, new HalClientOptions())` and delegates to the `IHalClient` overload.

`Resource<T>` is a client-side convenience type. It wraps the untyped `Resource` returned by deserialization and provides strongly-typed access to the embedded state via `State()`. The core `Chatter.Rest.Hal` library exposes `Resource.State<T>(JsonSerializerOptions?)` as a public overload, so `Resource<T>.State()` delegates directly — no internal accessor, `InternalsVisibleTo`, or re-serialization path is needed.

> **Implementation note:** `Resource.State<T>(JsonSerializerOptions?)` is a public overload in the core library. The no-arg `Resource.State<T>()` delegates to it internally, using the `JsonSerializerOptions` captured when the `Resource` was deserialized (via `JsonSerializer.DeserializeAsync<Resource>` — the converter stores the options on the instance). `Resource<T>.State()` simply calls `_inner.State<T>(_jsonOptions)`. No internal accessor or `InternalsVisibleTo` is required.

---

### `IHalClient`

The primary abstraction for HAL HTTP operations. Callers depend on this interface for testability.

```csharp
namespace Chatter.Rest.Hal.Client;

public interface IHalClient
{
    Task<Resource?> GetAsync(Uri uri, CancellationToken cancellationToken = default);
    Task<Resource<T>?> GetAsync<T>(Uri uri, CancellationToken cancellationToken = default) where T : class;
    Task<Resource?> PostAsync(Uri uri, object body, CancellationToken cancellationToken = default);
    Task<Resource?> PostAsync(Uri uri, HttpContent content, CancellationToken cancellationToken = default);
    Task<Resource<T>?> PostAsync<T>(Uri uri, object body, CancellationToken cancellationToken = default) where T : class;
    Task<Resource<T>?> PostAsync<T>(Uri uri, HttpContent content, CancellationToken cancellationToken = default) where T : class;
    Task<Resource?> PutAsync(Uri uri, object body, CancellationToken cancellationToken = default);
    Task<Resource?> PutAsync(Uri uri, HttpContent content, CancellationToken cancellationToken = default);
    Task<Resource<T>?> PutAsync<T>(Uri uri, object body, CancellationToken cancellationToken = default) where T : class;
    Task<Resource<T>?> PutAsync<T>(Uri uri, HttpContent content, CancellationToken cancellationToken = default) where T : class;
    Task<Resource?> PatchAsync(Uri uri, object body, CancellationToken cancellationToken = default);
    Task<Resource?> PatchAsync(Uri uri, HttpContent content, CancellationToken cancellationToken = default);
    Task<Resource<T>?> PatchAsync<T>(Uri uri, object body, CancellationToken cancellationToken = default) where T : class;
    Task<Resource<T>?> PatchAsync<T>(Uri uri, HttpContent content, CancellationToken cancellationToken = default) where T : class;
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
    public HalClient(HttpClient httpClient, HalClientOptions options, ILogger<HalClient>? logger = null);
}
```

**Fields stored:**
- `_httpClient` -- the underlying `HttpClient`
- `_options` -- resolved `HalClientOptions`
- `_logger` -- the logger. When `null`, `NullLogger<HalClient>.Instance` is used.

**Constructor behavior:**
- Stores `httpClient`, `options`, and `logger ?? NullLogger<HalClient>.Instance`.
- Validates `options.ExpectedMediaType`: throws `ArgumentException` if the value is null, empty, or contains a `';'` character. Message: `"HalClientOptions.ExpectedMediaType must be a bare media type without parameters (e.g., \"application/hal+json\")."`.

> **Note:** The DI companion package resolves `IOptions<HalClientOptions>.Value` before constructing `HalClient`, so the base package has no dependency on `Microsoft.Extensions.Options`.

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

**Message format:** `"Link relation '{rel}' not found on resource '{RedactUri(selfHref)}'."` The `SelfHref` property retains the raw (unredacted) value for programmatic access. `RedactUri` applied to `selfHref` strips query strings and fragments before embedding in the message text, preventing query-token leakage when exception messages are logged.

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

**Message format:** `"Response from '{RedactUri(requestUri)}' has Content-Type '{contentType}', expected HAL media type."` The `RequestUri` property retains the raw (unredacted) value for programmatic access. `RedactUri` strips query strings and fragments before embedding in the message text.

---

## HalClient Request/Response Flow

All HTTP methods in `HalClient` follow the same core flow. The verb-specific behavior is noted where it differs.

```
SendAsync(HttpMethod method, Uri uri, HttpContent? content, CancellationToken ct):
    // 1. Build request
    using var request = new HttpRequestMessage(method, uri)
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(_options.AcceptMediaType))
    if content is not null:
        request.Content = content

    // 2. Send request
    using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)

    // 3. Handle 404
    if response.StatusCode == HttpStatusCode.NotFound:
        return null

    // 4. Ensure success (throws HttpRequestException for non-2xx except 404)
    response.EnsureSuccessStatusCode()

    // 4a. Handle empty / no-content responses (all verbs)
    if response.StatusCode == HttpStatusCode.NoContent or response.Content.Headers.ContentLength == 0:
        return null

    // 5. Validate Content-Type
    // HAL media type: compare MediaType (no parameters) case-insensitively to _options.ExpectedMediaType
    // Null ContentType header is treated as non-HAL
    responseContentType = response.Content.Headers.ContentType?.MediaType
    isHal = responseContentType is not null &&
            string.Equals(responseContentType, _options.ExpectedMediaType, StringComparison.OrdinalIgnoreCase)
    if not isHal:
        if _options.StrictContentType:
            throw new HalResponseException(uri, responseContentType)
        return null

    // 6. Deserialize
    stream = await response.Content.ReadAsStreamAsync(ct)
    jsonOptions = _options.JsonOptions ?? defaultHalJsonOptions
    return await JsonSerializer.DeserializeAsync<Resource>(stream, jsonOptions, ct)
```

**Verb-specific behavior:**

- **`GetAsync`:** `method = HttpMethod.Get`, no content.
- **`GetAsync<T>`:** Same as `GetAsync` but wraps the deserialized `Resource`: `return resource is null ? null : new Resource<T>(resource, jsonOptions)`.
- **`PostAsync(object body)`:** `method = HttpMethod.Post`, body serialized to `StringContent` with `application/json` media type.
- **`PostAsync(HttpContent)`:** `method = HttpMethod.Post`, raw content passed through.
- **`PostAsync<T>(object body)`:** Same as `PostAsync(object body)` but wraps the deserialized `Resource` in `new Resource<T>(resource, jsonOptions)`.
- **`PostAsync<T>(HttpContent)`:** Same as `PostAsync(HttpContent)` but wraps the deserialized `Resource` in `new Resource<T>(resource, jsonOptions)`.
- **`PutAsync`:** Same pattern as `PostAsync` with `HttpMethod.Put`.
- **`PutAsync<T>`:** Same pattern with `HttpMethod.Put`; wraps deserialized `Resource` in `new Resource<T>(resource, jsonOptions)`.
- **`PatchAsync`:** Same pattern as `PostAsync` with `HttpMethod.Patch`.
- **`PatchAsync<T>`:** Same pattern with `HttpMethod.Patch`; wraps deserialized `Resource` in `new Resource<T>(resource, jsonOptions)`.
- **`DeleteAsync`:** `method = HttpMethod.Delete`, no content, no response deserialization. Calls `EnsureSuccessStatusCode()` and returns. Returns normally on 404.

Where `jsonOptions` is `_options.JsonOptions ?? defaultHalJsonOptions` (already resolved earlier in `SendAsync`).

**Object body serialization:** When a method accepts `object body`, the body is serialized via `JsonSerializer.Serialize(body, _options.JsonOptions ?? defaultHalJsonOptions)` and wrapped in `StringContent` with media type `"application/json"` and `UTF-8` encoding.

> **`HttpContent` ownership:** When a caller passes raw `HttpContent` to a method overload (e.g., `PostAsync(Uri, HttpContent, CT)`), `HalClient` attaches it to the `HttpRequestMessage`. Disposing the `HttpRequestMessage` (via `using var`) also disposes the attached content. **Callers must not reuse `HttpContent` instances after passing them to `HalClient`.** Callers who need to send the same content to multiple endpoints must create a new `HttpContent` for each call.

---

## DI Registration

> DI registration types live in `Chatter.Rest.Hal.Client.DependencyInjection`. The base package has no DI dependency.

### `HalClientServiceCollectionExtensions`

```csharp
namespace Chatter.Rest.Hal.Client.DependencyInjection;

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
2. Register `IHalClient`/`HalClient` using the factory overload of `AddHttpClient`:

   services.AddHttpClient<IHalClient, HalClient>((httpClient, sp) =>
       new HalClient(
           httpClient,
           sp.GetRequiredService<IOptions<HalClientOptions>>().Value,
           sp.GetService<ILogger<HalClient>>()))

   The factory receives the configured `HttpClient` and the `IServiceProvider`.
   It resolves `IOptions<HalClientOptions>.Value` for options and
   `ILogger<HalClient>` (nullable) for optional logging.

3. Return `services` for chaining

> **Note:** `AddHttpClient<TClient, TImplementation>(Func<HttpClient, IServiceProvider, TImplementation>)` is a valid overload in `Microsoft.Extensions.Http` on net8.0+. It wires typed `HttpClient` lifecycle management via `IHttpClientFactory` while allowing explicit constructor control. This is necessary because `HalClientOptions` is not directly resolvable from DI — only `IOptions<HalClientOptions>` is. Resolving `IHalClient` from the container invokes the factory and returns the constructed `HalClient`.

### `HalClientHttpClientBuilderExtensions`

```csharp
namespace Chatter.Rest.Hal.Client.DependencyInjection;

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
1. Configure named `HalClientOptions` via `builder.Services.Configure<HalClientOptions>(builder.Name, configure)`. Using a named options instance keyed by `builder.Name` prevents multiple `AddHalOptions` calls from sharing or overwriting each other's options.
2. Register `IHalClient` as a typed client attached to this specific `IHttpClientBuilder`:

   ```csharp
   builder.AddTypedClient<IHalClient>((httpClient, sp) =>
       new HalClient(
           httpClient,
           sp.GetRequiredService<IOptionsMonitor<HalClientOptions>>().Get(builder.Name),
           sp.GetService<ILogger<HalClient>>()))
   ```

   `builder.AddTypedClient<IHalClient>` attaches the typed client registration to the named `HttpClient` managed by this builder. `IOptionsMonitor<HalClientOptions>.Get(builder.Name)` resolves the named options snapshot for this specific client. Multiple `AddHalOptions` calls on different builders are fully isolated — each gets its own named options and its own typed client registration.

3. Return `builder` for chaining

> **Warning: Multiple `AddHalOptions` calls:** Named options are isolated per `builder.Name`, but `AddTypedClient<IHalClient>` registers `IHalClient` as an unkeyed transient. Multiple `AddHalOptions` calls add multiple `IHalClient` transient registrations -- resolving `IHalClient` from the container returns the last-registered implementation. For applications that need multiple independent HAL endpoints, construct `HalClient` instances directly from `IHttpClientFactory`:
>
> ```csharp
> var httpClient = factory.CreateClient("payments-api");
> IHalClient client = new HalClient(httpClient, paymentsOptions);
> ```
>
> In .NET 8+, keyed DI services (`AddKeyedTransient<IHalClient>("payments-api", ...)`) provide named resolution without manual construction.

---

## Link Traversal -- Extension Methods on `Resource`

All extension methods live in `Chatter.Rest.Hal.Client.Extensions`.

### `ObjectToDictionary` -- internal helper

Templated link-traversal overloads accept `object variables` for convenience. Before calling `LinkObject.Expand()`, the extension method converts `variables` to `IDictionary<string, string>` via this internal helper:

```
ObjectToDictionary(object variables):
    if variables is IDictionary<string, string> dict:
        return dict
    result = new Dictionary<string, string>()
    foreach property in variables.GetType().GetProperties(Public | Instance):
        value = property.GetValue(variables)
        if value is not null:
            result[property.Name] = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    return result
```

> **Null handling:** Properties with a null value are skipped and do not appear in the expanded template variables. **Invariant culture:** `Convert.ToString(value, CultureInfo.InvariantCulture)` ensures culture-independent formatting for numeric and date values — callers should not rely on locale-specific formats for URI template variables. **Duplicate keys:** Not possible; each public property name is unique within a type. **Supported value types:** Any type whose `IConvertible` or `IFormattable` implementation is culture-safe. Callers passing exotic types (e.g., `DateTimeOffset`, `Guid`) should pre-format values as strings.

This helper is called by all `FollowLinkAsync` templated overloads accepting `object variables`. The result is passed directly to `LinkObject.Expand(IDictionary<string, string>)`.

---

### Link resolution (shared logic)

```
ResolveLink(Resource resource, string rel):
    link = resource.Links?.GetLinkOrDefault(rel)
    if link is null or link.LinkObjects is empty:
        selfHref = resource.Links?
            .FirstOrDefault(l => l.Rel == "self")?
            .LinkObjects?.FirstOrDefault()?.Href
        throw new HalLinkNotFoundException(rel, selfHref)
    return link
```

Uses `LinkCollectionExtensions.GetLinkOrDefault(rel)` which calls `SingleOrDefault` internally. If duplicate rels exist on the resource, `InvalidOperationException` is thrown -- consistent with existing library behavior.

This resolution is used by all `FollowLinkAsync`, `FollowLinksAsync`, `PostToAsync`, `PutToAsync`, `PatchToAsync`, and `DeleteToAsync` methods. The `HalLinkNotFoundException` is thrown before any HTTP request (REQ-32).

---

### `FollowLinkAsync` -- single link, GET

```csharp
// Non-templated, untyped — no logger
public static Task<Resource?> FollowLinkAsync(
    this Resource resource,
    string rel,
    IHalClient client,
    CancellationToken ct = default);

// Non-templated, untyped — with logger
public static Task<Resource?> FollowLinkAsync(
    this Resource resource,
    string rel,
    IHalClient client,
    ILogger logger,
    CancellationToken ct = default);

// Non-templated, typed — no logger
public static Task<Resource<T>?> FollowLinkAsync<T>(
    this Resource resource,
    string rel,
    IHalClient client,
    CancellationToken ct = default) where T : class;

// Non-templated, typed — with logger
public static Task<Resource<T>?> FollowLinkAsync<T>(
    this Resource resource,
    string rel,
    IHalClient client,
    ILogger logger,
    CancellationToken ct = default) where T : class;

// Templated, untyped — no logger
public static Task<Resource?> FollowLinkAsync(
    this Resource resource,
    string rel,
    IHalClient client,
    object variables,
    CancellationToken ct = default);

// Templated, untyped — with logger
public static Task<Resource?> FollowLinkAsync(
    this Resource resource,
    string rel,
    IHalClient client,
    object variables,
    ILogger logger,
    CancellationToken ct = default);

// Templated, typed — no logger
public static Task<Resource<T>?> FollowLinkAsync<T>(
    this Resource resource,
    string rel,
    IHalClient client,
    object variables,
    CancellationToken ct = default) where T : class;

// Templated, typed — with logger
public static Task<Resource<T>?> FollowLinkAsync<T>(
    this Resource resource,
    string rel,
    IHalClient client,
    object variables,
    ILogger logger,
    CancellationToken ct = default) where T : class;
```

**Non-templated flow:**
1. Call `ResolveLink(resource, rel)` to get the `Link`
2. Extract `linkObject = link.LinkObjects[0]`
3. Construct `Uri` from `linkObject.Href` via `new Uri(linkObject.Href, UriKind.RelativeOrAbsolute)`
4. Call `client.GetAsync(uri, ct)` or `client.GetAsync<T>(uri, ct)`

> **Relative URI handling:** `Uri(href, UriKind.RelativeOrAbsolute)` accepts both absolute and relative URIs. Relative URIs (e.g., `/orders/42`) are resolved against `HttpClient.BaseAddress` by the underlying `HttpClient`. If `BaseAddress` is null and the URI is relative, `HttpClient.SendAsync` throws `InvalidOperationException`. Callers navigating HAL APIs with relative links must configure `BaseAddress` on the `HttpClient`.

**Templated flow:**
1. Call `ResolveLink(resource, rel)` to get the `Link`
2. Extract `linkObject = link.LinkObjects[0]`
3. Convert `object variables` via `ObjectToDictionary(variables)`, then call `linkObject.Expand(dict)` (delegates to `Chatter.Rest.UriTemplates`; `LinkObject` exposes dictionary/tuple-based `Expand` overloads)
4. Construct `Uri` from the expanded href via `new Uri(expandedHref, UriKind.RelativeOrAbsolute)`
5. Call `client.GetAsync(uri, ct)` or `client.GetAsync<T>(uri, ct)`

---

### `FollowLinksAsync` -- array-valued rel, GET

```csharp
// No logger
public static IAsyncEnumerable<Resource?> FollowLinksAsync(
    this Resource resource,
    string rel,
    IHalClient client,
    CancellationToken ct = default);

// With logger
public static IAsyncEnumerable<Resource?> FollowLinksAsync(
    this Resource resource,
    string rel,
    IHalClient client,
    ILogger logger,
    CancellationToken ct = default);

// Typed — no logger
public static IAsyncEnumerable<Resource<T>?> FollowLinksAsync<T>(
    this Resource resource,
    string rel,
    IHalClient client,
    CancellationToken ct = default) where T : class;

// Typed — with logger
public static IAsyncEnumerable<Resource<T>?> FollowLinksAsync<T>(
    this Resource resource,
    string rel,
    IHalClient client,
    ILogger logger,
    CancellationToken ct = default) where T : class;
```

**Flow:**
1. Call `ResolveLink(resource, rel)` to get the `Link`
2. Launch all fetches concurrently:
   ```
   tasks = link.LinkObjects
       .Select(lo => client.GetAsync(new Uri(lo.Href, UriKind.RelativeOrAbsolute), ct))
       .ToArray()
   ```
3. Await all: `results = await Task.WhenAll(tasks)`
4. Yield results sequentially:
   ```
   foreach (var result in results)
       yield return result
   ```

All links are fetched concurrently via `Task.WhenAll`. Results are buffered and then yielded sequentially. The caller still consumes results via `await foreach` over `IAsyncEnumerable<Resource?>`.

**Typed flow (`FollowLinksAsync<T>`):** Identical to the untyped flow except step 2 calls `client.GetAsync<T>(new Uri(lo.Href, UriKind.RelativeOrAbsolute), ct)` for each link object, producing `Task<Resource<T>?>[]` results.

---

### `PostToAsync`, `PutToAsync`, `PatchToAsync` -- mutation

Eight-overload pattern (four no-logger + four with-logger overloads):

```csharp
// Typed body + typed response — no logger
public static Task<Resource<TResponse>?> PostToAsync<TBody, TResponse>(
    this Resource resource,
    string rel,
    TBody body,
    IHalClient client,
    CancellationToken ct = default) where TResponse : class;

// Typed body + typed response — with logger
public static Task<Resource<TResponse>?> PostToAsync<TBody, TResponse>(
    this Resource resource,
    string rel,
    TBody body,
    IHalClient client,
    ILogger logger,
    CancellationToken ct = default) where TResponse : class;

// Object body — no logger
public static Task<Resource?> PostToAsync(
    this Resource resource,
    string rel,
    object body,
    IHalClient client,
    CancellationToken ct = default);

// Object body — with logger
public static Task<Resource?> PostToAsync(
    this Resource resource,
    string rel,
    object body,
    IHalClient client,
    ILogger logger,
    CancellationToken ct = default);

// Raw HttpContent — no logger
public static Task<Resource?> PostToAsync(
    this Resource resource,
    string rel,
    HttpContent content,
    IHalClient client,
    CancellationToken ct = default);

// Raw HttpContent — with logger
public static Task<Resource?> PostToAsync(
    this Resource resource,
    string rel,
    HttpContent content,
    IHalClient client,
    ILogger logger,
    CancellationToken ct = default);

// Raw HttpContent — typed response — no logger
public static Task<Resource<TResponse>?> PostToAsync<TResponse>(
    this Resource resource,
    string rel,
    HttpContent content,
    IHalClient client,
    CancellationToken ct = default) where TResponse : class;

// Raw HttpContent — typed response — with logger
public static Task<Resource<TResponse>?> PostToAsync<TResponse>(
    this Resource resource,
    string rel,
    HttpContent content,
    IHalClient client,
    ILogger logger,
    CancellationToken ct = default) where TResponse : class;
```

`PutToAsync` and `PatchToAsync` follow the same eight-overload pattern (four no-logger + four with-logger overloads, with only `where TResponse : class` on the typed overloads — no `TBody` constraint), calling `client.PutAsync` and `client.PatchAsync` respectively.

**Flow (all mutation methods):**
1. Call `ResolveLink(resource, rel)` to get the `Link`
2. Extract `linkObject = link.LinkObjects[0]`
3. Construct `Uri` from `linkObject.Href` via `new Uri(linkObject.Href, UriKind.RelativeOrAbsolute)`
4. Call the appropriate `client.PostAsync` / `client.PutAsync` / `client.PatchAsync` overload (typed overload calls `client.PostAsync<TResponse>` / `client.PutAsync<TResponse>` / `client.PatchAsync<TResponse>` for the typed extension method; the typed `HttpContent` overload calls `client.PostAsync<TResponse>(uri, content, ct)` and returns the result directly)

---

### `DeleteToAsync` -- deletion

```csharp
// No logger
public static Task DeleteToAsync(
    this Resource resource,
    string rel,
    IHalClient client,
    CancellationToken ct = default);

// With logger
public static Task DeleteToAsync(
    this Resource resource,
    string rel,
    IHalClient client,
    ILogger logger,
    CancellationToken ct = default);
```

**Flow:**
1. Call `ResolveLink(resource, rel)` to get the `Link`
2. Extract `linkObject = link.LinkObjects[0]`
3. Construct `Uri` from `linkObject.Href` via `new Uri(linkObject.Href, UriKind.RelativeOrAbsolute)`
4. Call `client.DeleteAsync(uri, ct)`

Returns `Task`, not `Task<Resource?>`. No response body is deserialized.

---

### Raw `HttpClient` overloads

Every `FollowLinkAsync`, `FollowLinksAsync`, `PostToAsync`, `PutToAsync`, `PatchToAsync`, and `DeleteToAsync` method has a corresponding overload that accepts `HttpClient` in place of `IHalClient`. These overloads construct a `HalClient` with default `HalClientOptions` and delegate to the `IHalClient` overload.

```csharp
// Example: raw HttpClient overload for FollowLinkAsync
public static Task<Resource?> FollowLinkAsync(
    this Resource resource,
    string rel,
    HttpClient client,
    CancellationToken ct = default)
{
    var halClient = new HalClient(client, new HalClientOptions());
    return resource.FollowLinkAsync(rel, halClient, ct);
}
```

---

### `Resource<T>` extension overloads and instance methods

**Untyped-return extension overloads:** Every extension method on `Resource` that returns `Resource?`, `IAsyncEnumerable<Resource?>`, or `Task` also has a parallel `this Resource<T> resource` extension overload that delegates to `resource.Inner`. For example:

```csharp
// Extension overload — untyped return, delegates to Inner
public static Task<Resource?> FollowLinkAsync<T>(
    this Resource<T> resource,
    string rel,
    IHalClient client,
    CancellationToken ct = default) where T : class
    => resource.Inner.FollowLinkAsync(rel, client, ct);
```

This pattern applies to all untyped-return variants of `FollowLinkAsync`, `FollowLinksAsync`, `PostToAsync`, `PutToAsync`, `PatchToAsync`, and `DeleteToAsync`.

**Typed-return instance methods ("As" naming):** For typed traversal and typed mutations returning `Resource<TResult>?` or `IAsyncEnumerable<Resource<TResult>?>`, `Resource<T>` exposes dedicated instance methods with an `As` suffix (`FollowLinkAsAsync<TResult>`, `FollowLinksAsAsync<TResult>`, `PostToAsAsync<TResult>`, `PutToAsAsync<TResult>`, `PatchToAsAsync<TResult>`). These are defined as instance methods — not extension methods — so `TResult` is the only method-level type parameter, avoiding the C# limitation that prevents partial type argument specification on extension methods. See the `Resource<T>` class definition above for the full list.

**Callers navigating from `Resource<T>`:**

```csharp
Resource<OrderSummary> order = await client.GetAsync<OrderSummary>(uri);

// Untyped traversal via extension overload:
Resource? raw = await order.FollowLinkAsync("related", client);

// Typed traversal via instance method (single explicit type param):
Resource<OrderDetails>? details = await order.FollowLinkAsAsync<OrderDetails>("details", client);

// Typed mutation via instance method:
Resource<ConfirmResult>? result = await order.PostToAsAsync<ConfirmResult>("confirm", new { force = true }, client);
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
        CancellationToken ct = default) where T : class;

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
| Duplicate rels on resource | Throw before HTTP | `InvalidOperationException` |
| HTTP 2xx with body | Deserialize and return | -- |
| HTTP 204 or explicit `Content-Length: 0` | Return `null`; `DeleteAsync` completes normally (no body, no Content-Type check) | -- |
| HTTP 301/302/3xx (redirect) | `HttpClient` follows redirects by default; non-redirect 3xx reaches `EnsureSuccessStatusCode` and throws | `HttpRequestException` |
| HTTP 400 / 401 / 403 / 409 | Throw | `HttpRequestException` |
| HTTP 404 | Return `null` (DELETE completes normally) | -- |
| HTTP 5xx | Throw | `HttpRequestException` |
| Network error / timeout | Throw | `HttpRequestException` / `TaskCanceledException` |
| Non-HAL Content-Type, strict off | Return `null` | -- |
| Non-HAL Content-Type, strict on | Throw | `HalResponseException` |

---

## Serialization

**Request serialization:** Object bodies are serialized via `JsonSerializer.Serialize(body, jsonOptions)` where `jsonOptions` is `HalClientOptions.JsonOptions` or library defaults. The resulting JSON is wrapped in `StringContent` with `Content-Type: application/json; charset=utf-8`.

**Response deserialization:** Response bodies are always deserialized as plain `Resource` via `JsonSerializer.DeserializeAsync<Resource>(stream, jsonOptions, ct)` with HAL converters applied. For typed methods (`GetAsync<T>`, `PostAsync<T>`, etc.), the deserialized `Resource` is then wrapped: `return new Resource<T>(resource, jsonOptions)`. `Resource<T>` is never passed directly to `JsonSerializer` — it has no JSON converter. The response stream is obtained via `ReadAsStreamAsync(ct)`. The `jsonOptions` are `HalClientOptions.JsonOptions` or library defaults (which include `AddHalConverters()`).

The resolved `jsonOptions` are also passed to `new Resource<T>(resource, jsonOptions)` so that `Resource<T>.State()` can apply the same custom options when the caller accesses typed state. This satisfies REQ-15 for typed API consumers.

---

## Async Conventions

### Async-only I/O (REQ-46)

All I/O in the package is async end-to-end. No sync-over-async wrappers (`.Result`, `.GetAwaiter().GetResult()`, `.Wait()`) or blocking calls (`Thread.Sleep`) appear anywhere in the implementation. Every public method that performs I/O returns `Task`, `Task<T>`, or `IAsyncEnumerable<T>`.

---

### CancellationToken threading (REQ-47)

Every async method -- public, internal, or private -- that performs or delegates to I/O accepts a `CancellationToken` and passes it to every downstream async call. Public API parameters default to `default`. Internal/private async methods may require the parameter (no default) to catch missing-token bugs at compile time.

Specific threading points:
- `HalClient.SendAsync` passes `ct` to `HttpClient.SendAsync`, `ReadAsStreamAsync(ct)`, and `JsonSerializer.DeserializeAsync`
- All `FollowLinkAsync` / `FollowLinksAsync` overloads pass `ct` through to `client.GetAsync`
- All `PostToAsync` / `PutToAsync` / `PatchToAsync` / `DeleteToAsync` overloads pass `ct` through to `client.PostAsync` / `client.PutAsync` / `client.PatchAsync` / `client.DeleteAsync`
- `GetHalAsync` and `PostHalAsync` convenience extensions pass `ct` through to the `HalClient` methods they delegate to

---

### Parallel fetches (REQ-48)

`FollowLinksAsync` is the one method in the package where independent async I/O calls iterate a collection. It uses `Task.WhenAll` to execute all fetches concurrently. The results are buffered in a `Task<Resource?>[]` and then yielded one at a time via `IAsyncEnumerable<Resource?>`. This preserves the existing return type while eliminating sequential-await overhead.

Single-element and empty cases require no special handling: `Task.WhenAll` on a single task has negligible overhead; `ResolveLink` throws `HalLinkNotFoundException` before `FollowLinksAsync` reaches the fetch step when `LinkObjects` is empty.

> **No built-in concurrency limiting.** `FollowLinksAsync` uses unbounded `Task.WhenAll`. Callers with very large link arrays (thousands of entries) should compose their own concurrency control via `SemaphoreSlim` or batching. This is explicitly out of scope for v1.

---

## Logging Architecture

### ILogger<T> Injection

`HalClient` accepts `ILogger<HalClient>?` as an optional constructor parameter (defaulting to `null`). When `null`, `NullLogger<HalClient>.Instance` is used and no log output is produced. This allows callers who do not use DI to construct `HalClient` directly without a logger.

The base package depends on `Microsoft.Extensions.Logging.Abstractions` only — no DI container, options framework, or HTTP factory.

When registered via `Chatter.Rest.Hal.Client.DependencyInjection`, the DI container injects `ILogger<HalClient>` automatically from the registered logging providers.

**Extension method logging:** `FollowLinkAsync`, `FollowLinksAsync`, `PostToAsync`, `PutToAsync`, `PatchToAsync`, and `DeleteToAsync` extension methods are provided in overload pairs (no-logger and with-logger) for rel-resolution logging (REQ-41, REQ-42). These are defined directly in the base extension class (see [Extension Method Logging](#extension-method-logging)).

**Rejected alternative:** A `LoggingHalClient : IHalClient` decorator in the DI package. Rejected because it requires the DI package for any logging, complicates the type hierarchy, and adds no benefit over explicit logger overloads.

---

### URI Redaction

Full URIs must never appear in log messages because query strings may contain API keys or tokens (REQ-45). All `{Uri}` log parameters use a `RedactUri` helper that strips query and fragment before logging:

```
RedactUri(Uri uri):
    if uri is relative:
        // strip query and fragment from relative URI string
        uriString = uri.OriginalString
        queryIndex = uriString.IndexOf('?')
        fragmentIndex = uriString.IndexOf('#')
        cutIndex = min of queryIndex and fragmentIndex (ignoring -1)
        return cutIndex >= 0 ? uriString[..cutIndex] : uriString
    // absolute URI: rebuild from scheme + host + port + path only
    // GetLeftPart(UriPartial.Path) keeps user:password@ — use UriBuilder to exclude it
    return new UriBuilder
    {
        Scheme = uri.Scheme,
        Host   = uri.Host,
        Port   = uri.IsDefaultPort ? -1 : uri.Port,
        Path   = uri.AbsolutePath
    }.Uri.ToString()
```

Log points that accept a `Uri` parameter pass it through `RedactUri` before logging. Raw URIs must never appear in log output.

---

### LoggerMessage Source Generators

All log points are defined as `[LoggerMessage]` attributed static partial methods (REQ-44). These are declared as a partial class on `HalClient` or in a companion file `HalClientLog.cs`.

| Log method | Level | Event ID | Message template | Parameters |
|---|---|---|---|---|
| `LogSendingRequest` | Debug | 1 | `"Sending {Method} {Uri}"` | `Method`, `Uri` |
| `LogReceivedResponse` | Debug | 2 | `"Received {StatusCode} from {Method} {Uri}"` | `StatusCode`, `Method`, `Uri` |
| `LogNotFoundReturningNull` | Debug | 3 | `"Received 404 for {Method} {Uri}, returning null"` | `Method`, `Uri` |
| `LogNonHalContentType` | Warning | 4 | `"Response from {Uri} has Content-Type '{ContentType}', expected HAL media type; returning null"` | `Uri`, `ContentType` |
| `LogDeserializationFailed` | Error | 5 | `"Failed to deserialize HAL response from {Uri}"` | `Uri` (exception passed as `Exception` arg) |
| `LogResolvingLink` | Debug | 6 | `"Resolving rel '{Rel}', templated={Templated}"` | `Rel`, `Templated` |
| `LogMutationRequest` | Debug | 7 | `"Sending {Method} to rel '{Rel}', uri={Uri}"` | `Method`, `Rel`, `Uri` |
| `LogHalClientInitialized` | Debug | 8 | `"HalClient initialized"` | (none) |

`{Uri}` values are redacted via `RedactUri` (scheme + host + path only; query and fragment stripped). Link href values (`SelfHref`, `Href`) are not logged because they may contain query tokens.

Event IDs are scoped to the `HalClient` category. `LogHalClientInitialized` is emitted once per `HalClient` instance construction (REQ-43).

---

### IsEnabled Guards

`LoggerMessage` source generators handle the `IsEnabled` check internally for fixed log points. Explicit `IsEnabled` guards are only needed when building expensive log state (e.g., constructing a string from iteration) before the log call. `HalClient` has no such iterating log points; all log messages use fixed structured parameters.

---

### Sensitive Data

Auth headers, request bodies, and response bodies must never appear in log messages (REQ-45). The log point table above lists only safe parameters: HTTP method, URI, status code, rel name, and content type.

---

### Extension Method Logging

All extension methods in `Chatter.Rest.Hal.Client.Extensions` are provided in overload pairs:
- A **no-logger** overload ending in `CancellationToken ct = default` — normal async usage, no logging.
- A **with-logger** overload accepting an explicit `ILogger logger` (non-optional) followed by `CancellationToken ct = default` — callers who want rel-resolution debug logging pass their logger explicitly.

Using separate overloads (rather than an optional `ILogger?` parameter) avoids the ergonomic trap where a `CancellationToken` argument is silently bound as the logger parameter.

When a logger overload is called with a non-null logger, it logs rel resolution at Debug level before delegating to `IHalClient`. The no-logger overload skips all logging.

---

## Test Strategy

### Unit tests

**`HalClient` request behavior:**
- Sets `Accept` header to `HalClientOptions.AcceptMediaType` on all requests
- Sends correct HTTP method for each verb
- Serializes object body as JSON `StringContent`
- Passes raw `HttpContent` through unchanged
- Includes `CancellationToken` in all HTTP calls

**`HalClient` response handling:**
- Returns `null` on HTTP 404
- Throws `HttpRequestException` on 5xx
- Returns deserialized `Resource` on success with HAL Content-Type
- Returns `Resource<T>` wrapping a deserialized `Resource` on typed GET success (verifies `State()` returns expected typed value)
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

**`FollowLinkAsync`:**
- Resolves non-templated link and calls `GetAsync`
- Resolves templated link via `LinkObject.Expand()` and calls `GetAsync`
- Throws `HalLinkNotFoundException` when rel is absent
- Returns typed `Resource<T>` for generic overload
- Raw `HttpClient` overload delegates correctly

**`FollowLinksAsync`:**
- Iterates all `LinkObject` entries for array-valued rel
- Yields each fetched `Resource?` via `IAsyncEnumerable`
- Throws `HalLinkNotFoundException` when rel is absent
- Fetches all links concurrently (not sequentially) -- verify via mock that all `GetAsync` calls are initiated before any result is awaited

**`PostToAsync` / `PutToAsync` / `PatchToAsync`:**
- Resolves link and calls correct HTTP method
- Typed overload returns `Resource<TResponse>`
- Object body overload serializes and sends
- `HttpContent` overload passes through
- Throws `HalLinkNotFoundException` when rel is absent

**`DeleteToAsync`:**
- Resolves link and calls `DeleteAsync`
- Returns `Task` (no body)
- Throws `HalLinkNotFoundException` when rel is absent

**`GetHalAsync` / `PostHalAsync` (HttpClient extensions):**
- Constructs `HalClient` with provided or default options
- Delegates to correct `IHalClient` method

**`Resource<T>` "As" methods:**
- `FollowLinkAsAsync<TResult>` accepts a single explicit type param; `T` of the receiver is already known
- `FollowLinksAsAsync<TResult>` yields `IAsyncEnumerable<Resource<TResult>?>`
- `PostToAsAsync<TBody, TResult>`: both type arguments must be specified explicitly (C# does not allow partial type argument specification)
- `PostToAsAsync<TResult>(..., HttpContent, ...)` returns `Resource<TResult>?`
- Untyped-return `Resource<T>` extension overloads delegate correctly to `.Inner` for all verbs

**Exception message redaction:**
- `HalLinkNotFoundException.Message` does not contain query strings or fragments from `SelfHref`; `SelfHref` property retains raw value
- `HalResponseException.Message` does not contain query strings or fragments from `RequestUri`; `RequestUri` property retains raw value

**GET 204 behavior:**
- `GetAsync` with a 204 or explicit `Content-Length: 0` response returns `null` without Content-Type validation (same behavior as mutations)

### Integration tests

**DI registration -- `AddHalClient`:**
- Resolves `IHalClient` from container
- Applies configured `HalClientOptions`
- `HttpClient` is managed internally

**DI registration -- `AddHalOptions` with `IHttpClientFactory`:**
- Resolves `IHalClient` from container
- `HttpClient` lifecycle managed by factory
- Options are applied correctly
- Named option isolation: multiple `AddHalOptions` calls use independent named options keyed by `builder.Name`; a second call does not affect the first call's options. Note: resolving `IHalClient` from the container returns the last-registered implementation (last-wins behavior for unkeyed transients — see multi-client warning in the registration section).

**End-to-end traversal:**
- Mock `HttpClient` (via `HttpMessageHandler`) returning HAL JSON
- GET root -> `FollowLinkAsync` to sub-resource -> `PostToAsync` to create -> `DeleteToAsync` to remove
- Verify correct URIs, methods, headers, and deserialized responses at each step

### Logging

- Verify `HalClient` emits `Debug` log before and after each HTTP call when a real logger is provided
- Verify `HalClient` emits `Warning` when non-HAL Content-Type is received in lenient mode
- Verify `HalClient` emits `Error` on deserialization failure, including exception
- Verify no log calls throw when `NullLogger<HalClient>` is used (covers no-logging construction path)
- Use `Microsoft.Extensions.Logging.Testing.FakeLogger<HalClient>` or a mock `ILogger<HalClient>` to assert exact log level, event ID, and message structure
- Verify `LogHalClientInitialized` is emitted exactly once per `HalClient` construction
