# Chatter.Rest.Hal.UriTemplates — RFC 6570 Test Plan

Spec: https://datatracker.ietf.org/doc/html/rfc6570  
Canonical test suite: https://github.com/uri-templates/uritemplate-test

Coverage status: ✅ Planned | ⚠️ Partial | ❌ Not Covered

---

## Shared Test Fixtures

All level-specific test classes share the following variable set, drawn from RFC 6570 Appendix A:

```csharp
var Variables = new Dictionary<string, string>
{
    ["var"]   = "value",
    ["hello"] = "Hello World!",
    ["empty"] = "",
    ["path"]  = "/foo/bar",
    ["x"]     = "1024",
    ["y"]     = "768",
};
// "undef" is intentionally absent — used to test undefined-variable behaviour
```

---

## 1. Level 1 — Simple String Expansion `{var}`

Class: `UriTemplateLevel1Tests`

Operator: none. Encoding: unreserved. Separator: `,`. Prefix: none.

### 1.1 RFC canonical examples

| Test name | Template | Expected |
|---|---|---|
| `SingleVar_SimpleValue` | `{var}` | `value` |
| `SingleVar_WithSpaceAndBang` | `{hello}` | `Hello%20World%21` |
| `SingleVar_EmptyValue` | `{empty}` | *(empty string)* |
| `SingleVar_Undefined` | `{undef}` | *(empty string)* |
| `SingleVar_WithSlashes` | `{path}` | `%2Ffoo%2Fbar` |

### 1.2 Literal text preservation

| Test name | Template | Expected |
|---|---|---|
| `NoExpression_LiteralOnly` | `/orders/list` | `/orders/list` |
| `LeadingLiteral` | `/orders/{var}` | `/orders/value` |
| `TrailingLiteral` | `{var}/orders` | `value/orders` |
| `MiddleLiteral` | `/orders/{var}/items` | `/orders/value/items` |
| `EmptyTemplate` | *(empty string)* | *(empty string)* |

### 1.3 Multiple expressions

| Test name | Template | Expected |
|---|---|---|
| `TwoExpressions` | `/orders/{x}/items/{y}` | `/orders/1024/items/768` |
| `ConsecutiveExpressions` | `{x}{y}` | `1024768` |

### 1.4 Encoding edge cases

| Test name | Template | Variables | Expected |
|---|---|---|---|
| `Encoding_SpaceEncoded` | `{hello}` | `hello="Hello World!"` | `Hello%20World%21` |
| `Encoding_SlashEncoded` | `{path}` | `path="/foo/bar"` | `%2Ffoo%2Fbar` |
| `Encoding_TildeNotEncoded` | `{var}` | `var="val~ue"` | `val~ue` |
| `Encoding_HyphenNotEncoded` | `{var}` | `var="val-ue"` | `val-ue` |
| `Encoding_DotNotEncoded` | `{var}` | `var="val.ue"` | `val.ue` |
| `Encoding_UnderscoreNotEncoded` | `{var}` | `var="val_ue"` | `val_ue` |
| `Encoding_ReservedColonEncoded` | `{var}` | `var="val:ue"` | `val%3Aue` |
| `Encoding_ReservedAmpersandEncoded` | `{var}` | `var="a&b"` | `a%26b` |

### 1.5 Guard conditions

| Test name | Input | Expected |
|---|---|---|
| `NullDictionary_Throws` | `variables = null` | `ArgumentNullException` |
| `EmptyDictionary_AllExpressionsEmpty` | `{var}`, `{}` | *(empty string)* |

---

## 2. Level 2 — Reserved and Fragment Expansion

Class: `UriTemplateLevel2Tests`

### 2.1 Reserved expansion `{+var}`

Operator: `+`. Encoding: reserved (`:/?#[]@!$&'()*+,;=%` pass through). Separator: `,`. Prefix: none.

| Test name | Template | Expected |
|---|---|---|
| `Plus_SimpleValue` | `{+var}` | `value` |
| `Plus_WithSpace` | `{+hello}` | `Hello%20World!` |
| `Plus_WithSlashes_PreservesSlashes` | `{+path}` | `/foo/bar` |
| `Plus_TrailingLiteral` | `{+path}/here` | `/foo/bar/here` |
| `Plus_InQueryContext` | `here?ref={+path}` | `here?ref=/foo/bar` |
| `Plus_EmptyValue` | `{+empty}` | *(empty string)* |
| `Plus_Undefined` | `{+undef}` | *(empty string)* |
| `Plus_MultipleVars` | `{+x,hello,y}` | `1024,Hello%20World!,768` |
| `Plus_MultipleVarsWithPath` | `{+path,x}/here` | `/foo/bar,1024/here` |
| `Plus_ReservedCharsPreserved` | `{+var}` | `value` (no encoding of reserved chars already in value) |
| `Plus_AmpersandPreserved` | `{+var}` | `a&b` (for `var="a&b"`) |
| `Plus_ColonPreserved` | `{+var}` | `a:b` (for `var="a:b"`) |

### 2.2 Fragment expansion `{#var}`

Operator: `#`. Encoding: reserved. Separator: `,`. Prefix: `#` (only when output non-empty).

| Test name | Template | Expected |
|---|---|---|
| `Hash_SimpleValue` | `{#var}` | `#value` |
| `Hash_WithSpace` | `{#hello}` | `#Hello%20World!` |
| `Hash_WithSlashes` | `{#path}` | `#/foo/bar` |
| `Hash_TrailingLiteral` | `{#path,x}/here` | `#/foo/bar,1024/here` |
| `Hash_MultipleVars` | `{#x,hello,y}` | `#1024,Hello%20World!,768` |
| `Hash_EmptyValue` | `{#empty}` | `#` |
| `Hash_Undefined_NoHash` | `{#undef}` | *(empty string — no lone `#`)* |

---

## 3. Level 3 — Multiple Variables and Operator Expansion

Class: `UriTemplateLevel3Tests`

### 3.1 No-operator multi-variable `{x,y}`

Multiple variables with no operator. Encoding: unreserved. Separator: `,`. Prefix: none.

| Test name | Template | Expected |
|---|---|---|
| `NoOp_TwoVars` | `{x,y}` | `1024,768` |
| `NoOp_ThreeVars` | `{x,hello,y}` | `1024,Hello%20World%21,768` |
| `NoOp_WithUndefined_OmitsUndefined` | `{x,undef,y}` | `1024,768` |
| `NoOp_AllUndefined` | `{undef,undef}` | *(empty string)* |
| `NoOp_WithEmpty` | `{x,empty,y}` | `1024,,768` |

### 3.2 Reserved multi-variable `{+x,y}`

| Test name | Template | Expected |
|---|---|---|
| `Plus_TwoVars` | `{+x,hello,y}` | `1024,Hello%20World!,768` |
| `Plus_WithPath` | `{+path,x}/here` | `/foo/bar,1024/here` |

### 3.3 Fragment multi-variable `{#x,y}`

| Test name | Template | Expected |
|---|---|---|
| `Hash_TwoVars` | `{#x,hello,y}` | `#1024,Hello%20World!,768` |

### 3.4 Label expansion `{.var}`

Operator: `.`. Encoding: unreserved. Separator: `.`. Prefix: `.` (only when non-empty).

| Test name | Template | Expected |
|---|---|---|
| `Dot_SingleVar` | `{.var}` | `.value` |
| `Dot_TwoVars` | `{.x,y}` | `.1024.768` |
| `Dot_Undefined` | `{.undef}` | *(empty string — no lone `.`)* |
| `Dot_EmptyValue` | `{.empty}` | `.` |
| `Dot_InPath` | `/api{.format}` | `/api.value` |
| `Dot_MixedDefinedUndefined` | `{.x,undef,y}` | `.1024.768` |

### 3.5 Path segment expansion `{/var}`

Operator: `/`. Encoding: unreserved. Separator: `/`. Prefix: `/` (only when non-empty).

| Test name | Template | Expected |
|---|---|---|
| `Slash_SingleVar` | `{/var}` | `/value` |
| `Slash_TwoVars` | `{/var,x}` | `/value/1024` |
| `Slash_Undefined` | `{/undef}` | *(empty string — no lone `/`)* |
| `Slash_EmptyValue` | `{/empty}` | `/` |
| `Slash_InPath` | `/base{/var}` | `/base/value` |
| `Slash_MixedDefinedUndefined` | `{/x,undef,y}` | `/1024/768` |

### 3.6 Path-style parameter expansion `{;var}`

Operator: `;`. Encoding: unreserved. Separator: `;`. Prefix: `;` (only when non-empty).

Empty-value rule: variable name is included **without** `=` (e.g. `{;empty}` → `;empty`).

| Test name | Template | Expected |
|---|---|---|
| `Semicolon_SingleVar` | `{;x}` | `;x=1024` |
| `Semicolon_MultipleVars` | `{;x,y,empty}` | `;x=1024;y=768;empty` |
| `Semicolon_EmptyValue_NoEquals` | `{;empty}` | `;empty` |
| `Semicolon_Undefined_Omitted` | `{;undef}` | *(empty string)* |
| `Semicolon_MixedDefinedUndefined` | `{;x,undef,y}` | `;x=1024;y=768` |

### 3.7 Query string expansion `{?var}`

Operator: `?`. Encoding: unreserved. Separator: `&`. Prefix: `?` (only when non-empty).

Empty-value rule: variable name is included **with** `=` but no value (e.g. `{?empty}` → `?empty=`).

| Test name | Template | Expected |
|---|---|---|
| `Query_SingleVar` | `{?x}` | `?x=1024` |
| `Query_TwoVars` | `{?x,y}` | `?x=1024&y=768` |
| `Query_WithEmpty` | `{?x,y,empty}` | `?x=1024&y=768&empty=` |
| `Query_WithUndefined_OmitsUndefined` | `{?x,y,undef}` | `?x=1024&y=768` |
| `Query_Undefined_NoPrefix` | `{?undef}` | *(empty string — no lone `?`)* |
| `Query_EmptyOnly` | `{?empty}` | `?empty=` |
| `Query_MixedDefinedUndefined` | `{?x,undef,y}` | `?x=1024&y=768` |
| `Query_InFullPath` | `/orders{?x,y}` | `/orders?x=1024&y=768` |

### 3.8 Query continuation expansion `{&var}`

Operator: `&`. Encoding: unreserved. Separator: `&`. Prefix: `&` (only when non-empty).

Empty-value rule: same as `?` — variable name with `=` but no value.

| Test name | Template | Expected |
|---|---|---|
| `Ampersand_SingleVar` | `{&x}` | `&x=1024` |
| `Ampersand_TwoVars` | `{&x,y}` | `&x=1024&y=768` |
| `Ampersand_WithEmpty` | `{&x,y,empty}` | `&x=1024&y=768&empty=` |
| `Ampersand_WithUndefined_OmitsUndefined` | `{&x,y,undef}` | `&x=1024&y=768` |
| `Ampersand_Undefined_NoPrefix` | `{&undef}` | *(empty string — no lone `&`)* |
| `Ampersand_InQueryString` | `/orders?sort=date{&x,y}` | `/orders?sort=date&x=1024&y=768` |

---

## 4. `GetVariables()` Tests

Class: `UriTemplateGetVariablesTests`

Tests that `UriTemplate.GetVariables()` returns all variable names in template order, deduplicated, across all operator types.

| Test name | Template | Expected |
|---|---|---|
| `SingleLevel1Var` | `{var}` | `["var"]` |
| `MultipleLevel1Vars` | `{x,y}` | `["x", "y"]` |
| `Level2PlusVar` | `{+path}` | `["path"]` |
| `Level2HashVar` | `{#var}` | `["var"]` |
| `Level3QueryVars` | `{?status,page}` | `["status", "page"]` |
| `MixedExpressions` | `/orders/{id}{?status,page}` | `["id", "status", "page"]` |
| `DeduplicatesAcrossExpressions` | `{x}/foo/{x}` | `["x"]` |
| `NoExpressions` | `/literal` | `[]` |
| `AllOperators` | `{a}{+b}{#c}{.d}{/e}{;f}{?g}{&h}` | `["a","b","c","d","e","f","g","h"]` |

---

## 5. Edge Cases

Class: `UriTemplateEdgeCaseTests`

### 5.1 Template parsing edge cases

| Test name | Input | Expected |
|---|---|---|
| `EmptyTemplate` | `` | `` |
| `LiteralOnly` | `/orders/list` | `/orders/list` |
| `ExpressionAtStart` | `{var}/rest` | `value/rest` |
| `ExpressionAtEnd` | `/prefix/{var}` | `/prefix/value` |
| `ExpressionOnly` | `{var}` | `value` |
| `ConsecutiveExpressions` | `{x}{y}` | `1024768` |
| `MultipleExpressionsWithLiterals` | `/a/{x}/b/{y}/c` | `/a/1024/b/768/c` |

### 5.2 Mixed-level expressions in one template

| Test name | Template | Expected |
|---|---|---|
| `Level1AndLevel3Query` | `/orders/{id}{?status,page}` | `/orders/42?status=open&page=2` |
| `Level1AndLevel2Reserved` | `/proxy/{+path}/tail` | `/proxy/foo/bar/tail` |
| `Level2AndLevel3` | `{+base}{/segment}` | `/root/value` |

For mixed-level tests, supplement the shared fixture with `id="42"`, `status="open"`, `page="2"`, `base="/root"`, `segment="value"` as needed.

### 5.3 Malformed template handling

| Test name | Input | Expected |
|---|---|---|
| `UnclosedBrace_Throws` | `/orders/{id` | `FormatException` |
| `NestedBraces_Throws` | `/orders/{{id}}` | `FormatException` |
| `EmptyExpression_Throws` | `/orders/{}` | `FormatException` |

### 5.4 Level 4 detection (not supported)

| Test name | Input | Expected |
|---|---|---|
| `PrefixModifier_Throws` | `{var:3}` | `NotSupportedException` |
| `ExplodeModifier_Throws` | `{list*}` | `NotSupportedException` |
| `ExplodeWithOperator_Throws` | `{/list*}` | `NotSupportedException` |

### 5.5 Case sensitivity

| Test name | Description | Expected |
|---|---|---|
| `VariableNames_CaseSensitive` | `{Var}` with `Var="upper"` and `var="lower"` in dict | `upper` (exact match, case-sensitive) |

---

## 6. `LinkObject` Integration Tests

Class: `LinkObjectUriTemplateIntegrationTests`

After `LinkObject` is refactored to delegate to `UriTemplate`, verify that the public `LinkObject` API behaves correctly with Level 2 and 3 templates (Level 1 is already covered by `LinkObjectTemplateExpansionTests`).

| Test name | Template | `Templated` | Call | Expected |
|---|---|---|---|---|
| `Level2_Plus_ExpandsReservedChars` | `/proxy/{+path}` | true | `Expand(("path", "/foo/bar"))` | `/proxy/foo/bar` |
| `Level2_Hash_ExpandsFragment` | `/page{#section}` | true | `Expand(("section", "intro"))` | `/page#intro` |
| `Level3_Query_BuildsQueryString` | `/orders{?status,page}` | true | `Expand(("status","open"),("page","2"))` | `/orders?status=open&page=2` |
| `Level3_Slash_BuildsPathSegment` | `/base{/segment}` | true | `Expand(("segment","value"))` | `/base/value` |
| `GetTemplateVariables_Level2_ReturnsVars` | `{+path}` | true | `GetTemplateVariables()` | `["path"]` |
| `GetTemplateVariables_Level3Query_ReturnsVars` | `{?status,page}` | true | `GetTemplateVariables()` | `["status", "page"]` |
| `NotTemplated_Level2Syntax_Unchanged` | `{+path}` | false | `Expand(("path","/foo"))` | `{+path}` |

---

## 7. Coverage Summary

| Area | Total | ✅ Planned |
|---|---|---|
| Level 1 — Simple string | 18 | 18 |
| Level 2 — Reserved (`+`) | 12 | 12 |
| Level 2 — Fragment (`#`) | 7 | 7 |
| Level 3 — No-op multi-var | 5 | 5 |
| Level 3 — Reserved multi-var (`+`) | 2 | 2 |
| Level 3 — Fragment multi-var (`#`) | 1 | 1 |
| Level 3 — Label (`.`) | 6 | 6 |
| Level 3 — Path (`/`) | 6 | 6 |
| Level 3 — Path-style params (`;`) | 5 | 5 |
| Level 3 — Query (`?`) | 8 | 8 |
| Level 3 — Query continuation (`&`) | 6 | 6 |
| `GetVariables()` | 9 | 9 |
| Edge cases | 15 | 15 |
| `LinkObject` integration | 7 | 7 |
| **Total** | **107** | **107** |
