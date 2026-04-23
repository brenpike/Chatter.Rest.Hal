# Chatter.Rest.UriTemplates — Architecture & Requirements

Spec: https://datatracker.ietf.org/doc/html/rfc6570

---

## Overview

New project `src/Chatter.Rest.UriTemplates/` implementing RFC 6570 URI Template expansion for Levels 1–3. Ships as its own NuGet package (`Chatter.Rest.UriTemplates`). `Chatter.Rest.Hal` project-references it; `LinkObject.GetTemplateVariables()` and `Expand()` delegate to the new engine.

Level 4 (value modifiers — prefix `:N` and explode `*`) is explicitly deferred. See [Level 4 TODO](#level-4-todo).

---

## Requirements

### Functional

- Parse URI template strings containing `{expression}` tokens per RFC 6570 Section 2
- Expand Level 1, Level 2, and Level 3 expressions (see [Operator Reference](#operator-reference))
- Return literal text outside expressions unchanged
- Undefined variables (key absent from input dictionary) are omitted per RFC 6570 rules
- Empty-string values are handled per operator semantics (see [Operator Reference](#operator-reference))
- Percent-encode values per RFC 3986 rules appropriate to each operator (see [Encoding Rules](#encoding-rules))
- Detect Level 4 modifier syntax (`:N`, `*`) and throw `NotSupportedException` with a descriptive message

### Non-Functional

- No external NuGet dependencies
- Target `netstandard2.0` and `net8.0` (matching the rest of the solution)
- `Chatter.Rest.Hal` project-references `Chatter.Rest.UriTemplates`
- Package ID: `Chatter.Rest.UriTemplates`

### Out of Scope

| Feature | Reason |
|---|---|
| Level 4 prefix modifier `{var:N}` | Deferred — see [Level 4 TODO](#level-4-todo) |
| Level 4 explode modifier `{var*}` | Deferred — requires list/dict value types |
| List and dictionary value types | Tied to Level 4; `string` values are sufficient for Levels 1–3 |
| URI template composition or merging | Outside RFC 6570 scope |

---

## Solution Structure

```
src/
  Chatter.Rest.UriTemplates/
    Chatter.Rest.UriTemplates.csproj
    UriTemplate.cs               ← public entry point
    UriTemplateOperator.cs       ← operator enum
    UriTemplateExpression.cs     ← one parsed {expression}
    UriTemplateParser.cs         ← tokenises template into literals + expressions
    UriTemplateExpander.cs       ← applies per-operator expansion rules

test/
  Chatter.Rest.UriTemplates.Tests/
    Chatter.Rest.UriTemplates.Tests.csproj
    UriTemplateLevel1Tests.cs
    UriTemplateLevel2Tests.cs
    UriTemplateLevel3Tests.cs
    UriTemplateEdgeCaseTests.cs
    UriTemplateGetVariablesTests.cs
```

---

## Type Design

### `UriTemplateOperator` (enum)

```csharp
internal enum UriTemplateOperator
{
    None,        // Level 1 — simple string expansion: {var}
    Plus,        // Level 2 — reserved expansion: {+var}
    Hash,        // Level 2 — fragment expansion: {#var}
    Dot,         // Level 3 — label expansion: {.var}
    Slash,       // Level 3 — path segment expansion: {/var}
    Semicolon,   // Level 3 — path-style parameter expansion: {;var}
    Query,       // Level 3 — query string expansion: {?var}
    Ampersand,   // Level 3 — query continuation expansion: {&var}
}
```

### `UriTemplateExpression` (internal record)

Represents one parsed `{expression}` token.

```csharp
internal sealed record UriTemplateExpression(
    UriTemplateOperator Operator,
    IReadOnlyList<string> Variables
);
```

### `UriTemplateParser` (internal)

Scans a template string left to right. Emits a sequence of string literals and `UriTemplateExpression` objects.

Responsibilities:
- Detect the operator character immediately after `{` (if any)
- Split comma-separated variable names within the expression
- Validate variable names: RFC 6570 `varname` = `varchar *( ["."] varchar )` where `varchar = ALPHA / DIGIT / "_"` and pct-encoded sequences. For Levels 1–3, dots and pct-encoded names are uncommon but must not cause a parse error.
- Detect Level 4 modifier characters (`:` for prefix, `*` for explode) and throw `NotSupportedException` with the message: `"RFC 6570 Level 4 modifiers (':N' and '*') are not supported. See the backlog for Level 4 implementation status."`
- Throw `FormatException` for malformed templates (unclosed `{`, nested `{`)

### `UriTemplateExpander` (internal static)

Applies expansion rules for a single `UriTemplateExpression` given a variable dictionary.

```csharp
internal static class UriTemplateExpander
{
    internal static string Expand(
        UriTemplateExpression expression,
        IDictionary<string, string> variables);
}
```

Per-operator expansion algorithm:

1. For each variable in `expression.Variables`:
   - Look up in `variables` dictionary. If absent (undefined): skip entirely.
   - If present but empty string: apply operator-specific empty-value rule (see [Operator Reference](#operator-reference)).
   - If present and non-empty: encode per operator encoding rule, then format per operator.
2. Join the formatted variable results with the operator's separator.
3. Prepend the operator's prefix (if any) to the joined result.
4. If no variables produced output (all undefined): return empty string.

### `UriTemplate` (public)

The public entry point. Parses on construction; expands on demand.

```csharp
public sealed class UriTemplate
{
    public UriTemplate(string template);

    /// <summary>
    /// Expands the URI template using the provided variable dictionary.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="variables"/> is null.</exception>
    public string Expand(IDictionary<string, string> variables);

    /// <summary>
    /// Expands the URI template using the provided key-value pairs.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="variables"/> is null.</exception>
    public string Expand(params (string Key, string Value)[] variables);

    /// <summary>
    /// Returns all variable names referenced in the template, in order of appearance, deduplicated.
    /// Includes variables from all operator types (Levels 1–3).
    /// </summary>
    public IReadOnlyList<string> GetVariables();
}
```

---

## Operator Reference

The table below defines behaviour for every supported operator. "Prefix" is prepended to the entire expression result (only when at least one variable produced output). "Separator" joins multiple variable results within one expression. "Encoding" controls which characters are percent-encoded. "Empty value" describes output when a variable is present but is an empty string. "Undefined" describes output when a variable is absent from the input dictionary.

| Operator | Level | Example | Prefix | Separator | Encoding | Empty value | Undefined |
|---|---|---|---|---|---|---|---|
| *(none)* | 1 | `{var}` | — | `,` | unreserved | *(empty string)* | omit |
| `+` | 2 | `{+var}` | — | `,` | reserved | *(empty string)* | omit |
| `#` | 2 | `{#var}` | `#` | `,` | reserved | `#` | omit / no `#` |
| `.` | 3 | `{.var}` | `.` | `.` | unreserved | `.` | omit |
| `/` | 3 | `{/var}` | `/` | `/` | unreserved | `/` | omit |
| `;` | 3 | `{;var}` | `;` | `;` | unreserved | `;varname` *(no `=`)* | omit |
| `?` | 3 | `{?var}` | `?` | `&` | unreserved | `varname=` *(with `=`)* | omit |
| `&` | 3 | `{&var}` | `&` | `&` | unreserved | `varname=` *(with `=`)* | omit |

**Notes:**
- `#` prefix is emitted only when at least one variable produces a value. If all variables are undefined, the entire expression returns empty string (no lone `#`).
- `.`, `/` prefixes behave the same way — emitted only when output is non-empty.
- `;` empty-value rule: the variable name is included without `=` (e.g. `{;empty}` with `empty=""` → `;empty`).
- `?` and `&` empty-value rule: the variable name is included with `=` but no value (e.g. `{?empty}` with `empty=""` → `?empty=`).

---

## Encoding Rules

RFC 6570 defines two encoding strategies:

### Unreserved encoding (Level 1, `.`, `/`, `;`, `?`, `&`)

Pass through only unreserved characters unencoded. Percent-encode everything else as UTF-8 bytes.

Unreserved characters (RFC 3986 Section 2.3):
```
A–Z  a–z  0–9  -  .  _  ~
```

### Reserved encoding (Level 2: `+`, `#`)

Pass through both unreserved characters AND reserved characters unencoded. Percent-encode everything else.

Reserved characters (RFC 3986 Section 2.2 + `%`):
```
:  /  ?  #  [  ]  @  !  $  &  '  (  )  *  +  ,  ;  =  %
```
Pct-encoded sequences (`%XX`) in the source value are also passed through unencoded.

---

## Integration with `LinkObject`

After this project is implemented:

1. `LinkObject.GetTemplateVariables()` refactors to:
   ```csharp
   public IReadOnlyList<string> GetTemplateVariables() =>
       Templated == true && !string.IsNullOrEmpty(Href)
           ? new UriTemplate(Href).GetVariables()
           : Array.Empty<string>();
   ```

2. `LinkObject.Expand(IDictionary<string, string> variables)` refactors to:
   ```csharp
   public string Expand(IDictionary<string, string> variables)
   {
       if (variables is null) throw new ArgumentNullException(nameof(variables));
       if (Templated != true || string.IsNullOrEmpty(Href)) return Href;
       return new UriTemplate(Href).Expand(variables);
   }
   ```

3. The `params (string Key, string Value)[]` overload remains unchanged (delegates to the `IDictionary` overload).

### Behavioural changes after integration

| Behaviour | Before (Level 1 only) | After (Levels 1–3) |
|---|---|---|
| `GetTemplateVariables()` on `{?status,page}` | `[]` | `["status", "page"]` |
| `Expand()` on `{?status,page}` | `/orders{?status,page}` (unexpanded) | `/orders?status=open&page=2` |
| `GetTemplateVariables()` on `{+path}` | `[]` | `["path"]` |
| `Expand()` on `{+path}` | `/proxy/{+path}` (unexpanded) | `/proxy/foo/bar` |

Existing `LinkObjectTemplateExpansionTests` cover Level 1 behaviour and remain valid. No existing tests are expected to break; Level 2–3 expansion is additive.

---

## Level 4 TODO

Level 4 modifiers are not implemented. When the parser detects `:` (prefix) or `*` (explode) within an expression, it throws `NotSupportedException`.

Future implementation requires:
- `IDictionary<string, object>` value type (or a `UriTemplateValue` union type) to represent string, list, and dictionary values
- Per-operator explode logic for each of the 8 operators
- Prefix truncation logic applied before encoding
- New overloads on `UriTemplate.Expand()` accepting the richer value type

Track in the feature backlog as a stretch goal for IDEA-01.
