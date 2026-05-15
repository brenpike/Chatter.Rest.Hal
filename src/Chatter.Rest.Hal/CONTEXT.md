# HAL Domain & Serialization

The in-memory object model for HAL (Hypertext Application Language) documents, a fluent builder API for constructing them, and System.Text.Json converters for round-trip serialization.

## Language

### Document model

**Resource**:
A HAL document or an individual embedded item; the root aggregate that holds **State**, a **Link Collection**, and an **Embedded Resource Collection**.
_Avoid_: document, response, payload, HAL object

**State**:
The application-specific properties of a **Resource**, excluding reserved HAL properties (`_links`, `_embedded`).
_Avoid_: body, data, properties, content

**Link**:
A named entry in a **Link Collection**, identified by a **Relation** and containing zero or more **Link Objects**.
_Avoid_: hyperlink, URL entry, link relation entry

**Relation**:
A string that identifies the semantics of a **Link** (e.g., `"self"`, `"next"`, `"acme:widgets"`).
_Avoid_: rel type, link name, link key

**Link Object**:
A single hyperlink target within a **Link**, carrying an **Href** and optional properties (templated, type, deprecation, name, profile, title, hreflang).
_Avoid_: link entry, link item, href object

**Href**:
The URI or URI Template value on a **Link Object**; the only required property.
_Avoid_: URL, address, endpoint

**Embedded Resource**:
A named entry in an **Embedded Resource Collection**, identified by a name string and containing a **Resource Collection**.
_Avoid_: nested resource, sub-resource, child resource, embed

**Link Collection**:
The ordered set of **Links** on a **Resource**, serialized as the `_links` JSON property.
_Avoid_: links map, link set

**Link Object Collection**:
The ordered set of **Link Objects** within a single **Link**.
_Avoid_: link array, href list

**Embedded Resource Collection**:
The ordered set of **Embedded Resources** on a **Resource**, serialized as the `_embedded` JSON property.
_Avoid_: embeds, embedded map

**Resource Collection**:
The ordered set of **Resources** within a single **Embedded Resource**.
_Avoid_: resource array, resource list

### Reserved relations

**Self Link**:
A **Link** with **Relation** `"self"` that identifies the **Resource's** own URI.
_Avoid_: self-ref, canonical link, identity link

**CURIE**:
A compact URI prefix defined under the reserved `"curies"` **Relation**, enabling short-form **Relations** (e.g., `"acme:widgets"`) that expand to full URIs via a URI Template.
_Avoid_: compact URI, namespace, prefix

**CURIE Expansion**:
The process of replacing a prefixed **Relation** (e.g., `"acme:widgets"`) with its full URI by substituting the suffix into the CURIE's **Href** template at the `{rel}` token.
_Avoid_: prefix resolution, namespace expansion

### Serialization

**Force Array**:
A per-**Link** flag (`IsArray`) or a global option (`AlwaysUseArrayForLinks`) that forces a **Link** to serialize as a JSON array even when it contains a single **Link Object**.
_Avoid_: always-array, array mode

**Force Write As Collection**:
A per-**Embedded Resource** flag (`ForceWriteAsCollection`) that forces its **Resource Collection** to serialize as a JSON array even when it contains a single **Resource**.
_Avoid_: always-array (embedded), collection mode

**HAL JSON Options**:
Configuration object (`HalJsonOptions`) controlling HAL-specific serialization behavior, notably the global **Force Array** setting for links.
_Avoid_: serialization settings, converter config

### Builder

**Fluent Builder**:
The staged builder API (`ResourceBuilder`) for constructing a **Resource** graph through chained method calls, using stage interfaces to guide valid construction sequences.
_Avoid_: resource factory, DSL, construction API

**Stage Interface**:
An interface (e.g., `IResourceCreationStage`, `ILinkCreationStage`) that constrains which builder methods are available at each step of **Fluent Builder** construction.
_Avoid_: step interface, phase interface, builder contract

### Marker type

**IHalPart**:
Marker interface implemented by every HAL domain type (**Resource**, **Link**, **Link Object**, and all collection types); used as the generic constraint for the builder hierarchy.
_Avoid_: IHalType, IHalEntity

### External dependency

**URI Template**:
An RFC 6570 template string (provided by the external `Chatter.Rest.UriTemplates` package) used by **Link Object** for template expansion and by **CURIEs** for relation expansion.
_Avoid_: URL template, route template

## Relationships

- A **Resource** contains exactly one **Link Collection** and one **Embedded Resource Collection**
- A **Resource** contains zero or one **State** objects
- A **Link Collection** contains zero or more **Links**
- A **Link** is identified by exactly one **Relation** and contains one **Link Object Collection**
- A **Link Object Collection** contains zero or more **Link Objects**
- A **Link Object** carries exactly one **Href** and optional metadata properties
- An **Embedded Resource Collection** contains zero or more **Embedded Resources**
- An **Embedded Resource** is identified by a name and contains one **Resource Collection**
- A **Resource Collection** contains zero or more **Resources** (recursive: each may have its own **Links** and **Embedded Resources**)
- A **CURIE** is a **Link** under the reserved `"curies"` **Relation** whose **Link Objects** define prefix-to-template mappings
- **CURIE Expansion** uses a **CURIE's** **Href** (a **URI Template**) to resolve prefixed **Relations** to full URIs
- A **Self Link** is a **Link** with **Relation** `"self"`
- **Force Array** applies to **Links**; **Force Write As Collection** applies to **Embedded Resources** — both override the default singular/plural JSON shape
- The **Fluent Builder** produces a **Resource** graph; **Stage Interfaces** control the construction sequence
- Every HAL domain type implements **IHalPart**
- **HalResponseAttribute** (defined in HAL Shared Kernel) is not consumed directly in this context; it is consumed by HAL Code Generation, whose generated code references types from this context

## Example dialogue

> **Dev:** "When I add a **Link** with **Relation** `"next"`, does it automatically get a **Link Object**?"
> **Domain expert:** "No -- a **Link** starts with an empty **Link Object Collection**. You add **Link Objects** to it, each with its own **Href**."

> **Dev:** "If my **Link** only has one **Link Object**, will it serialize as a JSON object or a JSON array?"
> **Domain expert:** "By default it serializes as a single object. If you need a stable array shape, set **Force Array** on the **Link** -- or enable it globally via **HAL JSON Options**."

> **Dev:** "How does **CURIE Expansion** work for `acme:widgets`?"
> **Domain expert:** "It finds the **CURIE** **Link Object** with name `acme` under the `curies` **Relation**, takes its **Href** template, and substitutes `widgets` for the `{rel}` token."

## Flagged ambiguities

- "link" was used to mean both the relation-level entry (**Link**) and the individual hyperlink target (**Link Object**) -- resolved: **Link** is the relation-keyed container; **Link Object** is the target with an **Href**.
- "embedded" was used to mean both the named entry (**Embedded Resource**) and the collection property on a **Resource** (**Embedded Resource Collection**) -- resolved: these are distinct types at different levels of the hierarchy.
- "force array" applies to two distinct mechanisms: **Force Array** (on **Links**, controlling `_links` shape) and **Force Write As Collection** (on **Embedded Resources**, controlling `_embedded` shape) -- resolved: use the specific term for each.
