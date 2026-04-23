# URI Templates Usage Guide

Practical usage guide for `Chatter.Rest.Hal.UriTemplates`.

---

## 1. Overview

`Chatter.Rest.Hal.UriTemplates` is a standalone RFC 6570 URI Template expansion library for .NET. It supports **Levels 1 through 3** of the RFC 6570 specification, covering simple string expansion, reserved/fragment expansion, and all Level 3 operators (label, path segment, path-style parameter, form-style query, and form-style query continuation).

**Level 4** (prefix modifiers `:N` and explode `*`) is **not supported**. See [Level 4 Not Supported](#9-level-4-not-supported) for details.

**Spec:** [RFC 6570 — URI Template](https://datatracker.ietf.org/doc/html/rfc6570)

---

## 2. Installation

```bash
dotnet add package Chatter.Rest.Hal.UriTemplates
```

**Namespace:**

```csharp
using Chatter.Rest.Hal.UriTemplates;
```

The package targets `netstandard2.0` and `net8.0` with no external dependencies.

---

## 3. Quick Start

```csharp
using Chatter.Rest.Hal.UriTemplates;

var template = new UriTemplate("/orders{?status,page}");

var uri = template.Expand(new Dictionary<string, string>
{
    ["status"] = "shipped",
    ["page"] = "2"
});
// Result: "/orders?status=shipped&page=2"
```

---

## 4. API Reference

### `UriTemplate(string template)`

Constructor. Parses the template string eagerly on construction.

- Throws `ArgumentNullException` if `template` is null.
- Throws `FormatException` for malformed templates (unclosed `{`, nested `{`).
- Throws `NotSupportedException` for Level 4 modifier syntax (`:N` prefix, `*` explode).

```csharp
var template = new UriTemplate("/search{?q,lang}");
```

### `string Expand(IDictionary<string, string> variables)`

Expands the URI template using the provided variable dictionary.

- Throws `ArgumentNullException` if `variables` is null.
- Variables absent from the dictionary are treated as undefined and omitted per RFC 6570 rules.

```csharp
var uri = template.Expand(new Dictionary<string, string>
{
    ["q"] = "dotnet",
    ["lang"] = "en"
});
// Result: "/search?q=dotnet&lang=en"
```

### `string Expand(params (string Key, string Value)[] variables)`

Tuple convenience overload. First-wins for duplicate keys.

- Throws `ArgumentNullException` if `variables` is null.

```csharp
var uri = template.Expand(
    ("q", "dotnet"),
    ("lang", "en")
);
// Result: "/search?q=dotnet&lang=en"
```

Duplicate handling:

```csharp
var uri = template.Expand(
    ("q", "first"),
    ("q", "second")    // ignored — "first" wins
);
// Result: "/search?q=first"
```

### `IReadOnlyList<string> GetVariables()`

Returns all variable names referenced in the template, deduplicated, in order of first appearance.

```csharp
var template = new UriTemplate("/orders{?status,page}{&lang}");
var vars = template.GetVariables();
// Result: ["status", "page", "lang"]
```

---

## 5. Operator Examples

### Level 1 — Simple String Expansion `{var}`

```csharp
var t = new UriTemplate("/users/{id}");
t.Expand(("id", "42"));
// Result: "/users/42"
```

Values are percent-encoded using unreserved encoding:

```csharp
var t = new UriTemplate("/search/{query}");
t.Expand(("query", "hello world"));
// Result: "/search/hello%20world"
```

### Level 2 — Reserved Expansion `{+var}`

Reserved characters in the value are passed through unencoded:

```csharp
var t = new UriTemplate("/proxy/{+path}");
t.Expand(("path", "foo/bar/baz"));
// Result: "/proxy/foo/bar/baz"
```

### Level 2 — Fragment Expansion `{#var}`

Prepends `#` to the expanded value:

```csharp
var t = new UriTemplate("/page{#section}");
t.Expand(("section", "overview"));
// Result: "/page#overview"
```

### Level 3 — Label Expansion `{.var}`

Prepends `.` and uses `.` as separator:

```csharp
var t = new UriTemplate("/api{.version}");
t.Expand(("version", "v2"));
// Result: "/api.v2"

var t2 = new UriTemplate("/host{.sub,domain}");
t2.Expand(("sub", "www"), ("domain", "example"));
// Result: "/host.www.example"
```

### Level 3 — Path Segment Expansion `{/var}`

Prepends `/` and uses `/` as separator:

```csharp
var t = new UriTemplate("/files{/dir,file}");
t.Expand(("dir", "photos"), ("file", "cat.jpg"));
// Result: "/files/photos/cat.jpg"
```

### Level 3 — Path-Style Parameter Expansion `{;var}`

Prepends `;` and uses `;` as separator. Empty values include the name without `=`:

```csharp
var t = new UriTemplate("/matrix{;x,y}");
t.Expand(("x", "1"), ("y", "2"));
// Result: "/matrix;x=1;y=2"

// Empty value:
t.Expand(("x", ""), ("y", "2"));
// Result: "/matrix;x;y=2"
```

### Level 3 — Form-Style Query Expansion `{?var}`

Prepends `?` and uses `&` as separator. Empty values include `=`:

```csharp
var t = new UriTemplate("/orders{?status,page}");
t.Expand(("status", "shipped"), ("page", "2"));
// Result: "/orders?status=shipped&page=2"

// Empty value:
t.Expand(("status", ""));
// Result: "/orders?status="
```

### Level 3 — Form-Style Query Continuation `{&var}`

Prepends `&` and uses `&` as separator. Use this for appending to an existing query string:

```csharp
var t = new UriTemplate("/orders?mode=list{&status,page}");
t.Expand(("status", "shipped"), ("page", "2"));
// Result: "/orders?mode=list&status=shipped&page=2"
```

---

## 6. Encoding Rules

The library applies two encoding strategies per RFC 6570:

**Unreserved encoding** (Level 1, `.`, `/`, `;`, `?`, `&` operators):
Only unreserved characters pass through unencoded. Everything else is percent-encoded as UTF-8 bytes.

Unreserved characters: `A-Z a-z 0-9 - . _ ~`

```csharp
var t = new UriTemplate("/search/{query}");
t.Expand(("query", "hello world!"));
// Result: "/search/hello%20world%21"
```

**Reserved encoding** (Level 2: `+` and `#` operators):
Both unreserved and reserved characters pass through unencoded. Only characters outside both sets are percent-encoded.

Reserved characters: `: / ? # [ ] @ ! $ & ' ( ) * + , ; = %`

```csharp
var t = new UriTemplate("{+path}");
t.Expand(("path", "/foo/bar?q=1"));
// Result: "/foo/bar?q=1"   (slashes, ?, = all preserved)
```

---

## 7. LinkObject Integration

When using `Chatter.Rest.Hal`, the `LinkObject` class has two methods that delegate to the URI template engine:

### `LinkObject.GetTemplateVariables()`

Returns all variable names from the link's `Href` when `Templated` is `true`. Returns an empty list when `Templated` is `false` or `Href` is empty.

```csharp
var resource = ResourceBuilder.New()
    .AddLink("search").AddLinkObject("/orders{?status,page}").Templated()
    .Build();

var link = resource!.GetLinkObjectOrDefault("search");
var vars = link!.GetTemplateVariables();
// Result: ["status", "page"]
```

### `LinkObject.Expand()`

Expands the URI template with provided variables. Returns `Href` unchanged when `Templated` is not `true`.

```csharp
var expandedUri = link!.Expand(new Dictionary<string, string>
{
    ["status"] = "shipped",
    ["page"] = "2"
});
// Result: "/orders?status=shipped&page=2"

// Tuple overload:
var expandedUri2 = link!.Expand(("status", "shipped"), ("page", "2"));
// Result: "/orders?status=shipped&page=2"
```

---

## 8. Undefined Variables

When a variable referenced in the template is absent from the provided dictionary, it is treated as **undefined** and omitted entirely per RFC 6570 rules. No placeholder or literal `{var}` text is left in the output.

```csharp
var t = new UriTemplate("/orders{?status,page}");

// Only "status" provided; "page" is undefined:
t.Expand(("status", "shipped"));
// Result: "/orders?status=shipped"

// All variables undefined:
t.Expand(new Dictionary<string, string>());
// Result: "/orders"
```

This applies consistently across all operator types. Operator prefixes (`?`, `#`, `.`, `/`, `;`, `&`) are only emitted when at least one variable in the expression produces a value.

---

## 9. Level 4 Not Supported

RFC 6570 Level 4 defines two value modifiers:

- **Prefix** (`{var:3}`) — truncate the value to a maximum length before expansion
- **Explode** (`{var*}`) — expand list or associative array values into separate key=value pairs

This library does **not** support Level 4. Attempting to parse a template containing these modifiers throws `NotSupportedException`:

```csharp
// Throws NotSupportedException:
var t = new UriTemplate("/search{?query:10}");

// Throws NotSupportedException:
var t = new UriTemplate("/items{?list*}");
```

The exception message is: `"RFC 6570 Level 4 modifiers (':N' and '*') are not supported. See the backlog for Level 4 implementation status."`

Level 4 is deferred because it requires list and dictionary value types, which are beyond the `IDictionary<string, string>` API surface. See [docs/uri-templates/architecture.md](architecture.md) for the Level 4 TODO and future implementation requirements.
