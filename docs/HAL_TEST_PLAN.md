# HAL Specification Test Plan

Spec: https://datatracker.ietf.org/doc/html/draft-kelly-json-hal  
Media type: `application/hal+json`

Each section maps a normative or behavioral requirement from the spec to testable scenarios.
Coverage status uses: ✅ Covered | ⚠️ Partially Covered | ❌ Not Covered

---

## 1. Resource Object

The root of every HAL document MUST be a Resource Object — a JSON object that may contain any combination of state properties, `_links`, and `_embedded`.

### 1.1 Empty resource is valid
> A resource with no state, no `_links`, and no `_embedded` is a valid HAL document.

- ✅ `Tests.Empty_Object_Json_Should_Be_Valid_Resource`
- ✅ `Tests.Default_Resource_Should_Deserialize_To_Empty_Json_Object`

### 1.2 Resource state is arbitrary JSON
> Resource state properties are any JSON key/value pairs that are not `_links` or `_embedded`.

- ✅ `Tests.Must_Add_Complex_Object_As_Resource_State`
- ✅ `Tests.Must_Serialize_Resource_State_If_Exists`
- ✅ `ResourceTests.Getting_State_Should_Only_Return_Resource_State`
- ✅ `HalSerializationRoundTripTests.Resource_State_Does_Not_Expose__links_or__embedded_In_State`

### 1.3 State, _links, and _embedded are preserved through round-trip
- ✅ `Tests.Resource_Must_Be_Same_After_Serialization_And_Deserialization`
- ✅ `HalSerializationRoundTripTests.Resource_RoundTrip_Preserves_Links_And_Embedded_And_State`
- ✅ `ResourceBehaviorTests.Resource_Serialization_And_Deserialization_Should_Preserve_Links_And_Embedded`

### 1.4 Resource state supports tolerant reader (unknown properties preserved)
> Consumers SHOULD ignore unrecognised properties (tolerant reader pattern).

- ✅ `ResourceTests.Resource_State_Should_Allow_Tolerant_Reader_After_Deserialization`
- ✅ `HalDeserializationRobustnessTests.Extra_Random_Properties_Are_Preserved_In_State`

### 1.5 Root object MUST be a JSON object (not array, string, number, etc.)
- ✅ `HalDeserializationRobustnessTests.Root_Array_Is_Rejected_Or_Returns_Null`
- ✅ `HalDeserializationRobustnessTests.Root_String_Is_Rejected_Or_Returns_Null`
- ✅ `HalDeserializationRobustnessTests.Root_Number_Is_Rejected_Or_Returns_Null`
- ✅ `HalDeserializationRobustnessTests.Root_Boolean_Is_Rejected_Or_Returns_Null`
- ✅ `HalDeserializationRobustnessTests.Root_Null_Is_Rejected_Or_Returns_Null`

---

## 2. `_links`

`_links` is an optional property of a Resource Object. Its value is a JSON object whose keys are link relation types and whose values are either a Link Object or an array of Link Objects.

### 2.1 Absent `_links` produces an empty link collection
- ✅ `HalDeserializationRobustnessTests.Missing__links_Property_Produces_Empty_LinksCollection`
- ✅ `ResourceTests.Links_Should_Return_Empty_LinksCollection_If_Resource_Has_No_Links`

### 2.2 `_links` with a single Link Object (not array)
- ✅ `LinkBehaviorTests.Single_LinkObject_Serializes_As_Object_Not_Array`
- ✅ `LinkBehaviorTests.Reading_Single_LinkObject_From_Json_Object_Works`
- ✅ `HalLinksCollectionTests.Single_LinkObject_Serializes_As_Object_Not_Array`

### 2.3 `_links` with an array of Link Objects
- ✅ `Tests.Must_Deserialize_LinkObjectCollection_With_More_Than_One_LinkObject`
- ✅ `LinkBehaviorTests.Multiple_LinkObjects_Serializes_As_Array`
- ✅ `HalLinksCollectionTests.Multiple_LinkObjects_Serializes_As_Array`
- ✅ `LinkConvertersTests.Deserialize_Link_As_Array_Should_Parse_Multiple`

### 2.4 Servers SHOULD NOT change a relation between single-object and array form across responses
> Once a relation is expressed as an array, it should stay an array (and vice versa).

- ❌ No test verifies consistency enforcement or round-trip stability of the single-vs-array form.

### 2.5 Link relation type as a null value is handled gracefully
- ✅ `LinkBehaviorTests.Reading_Link_With_Null_Value_Produces_Link_With_No_LinkObjects`
- ✅ `HalLinksCollectionTests.Reading_Link_With_Null_Value_Produces_Link_With_No_LinkObjects`
- ✅ `LinkConvertersTests.Deserialize_Null_Link_Value_Should_Create_Empty_LinkObjects`

### 2.6 Link relation types follow IANA registry or URI conventions
> Custom link relation types SHOULD be URIs that provide documentation when dereferenced.

- ❌ No test validates that link relation values are valid IANA or URI-format strings.

### 2.7 `self` link relation
> Each Resource Object SHOULD contain a `self` link whose value is the resource's URI.

- ✅ `HalSerializationRoundTripTests.Resource_With_Self_Link_Serializes_Self_Relation`
- ✅ `HalSerializationRoundTripTests.Resource_Self_Link_Is_Accessible_Via_Extension`
- ✅ `HalSerializationRoundTripTests.Resource_Without_Self_Link_Returns_Null_Via_Extension`

---

## 3. Link Objects

A Link Object represents a hyperlink. It MUST have an `href`; all other properties are optional.

### 3.1 `href` is required
- ✅ `HalLinkObjectTests.LinkObject_Serializes_Required_Href`
- ✅ `HalLinkObjectTests.LinkObject_Missing_Href_Produces_Null_On_Deserialization`
- ✅ `HalLinkAttributesValidationTests.Href_Empty_String_Is_Invalid_On_Deserialization`
- ✅ `LinkConvertersTests.Deserialize_Single_Link_Object_Should_Parse`

### 3.2 `href` may be a URI Template (RFC 6570)
> When `href` is a URI Template, `templated` MUST be `true`.

- ✅ `HalCuriesAndTemplatedTests.Templated_Link_Has_Templated_True_If_Provided`
- ✅ `HalCuriesAndTemplatedTests.Templated_Href_Does_Not_Automatically_Expand`
- ✅ `HalLinkObjectTests.LinkObject_Reads_Templated_True_For_Template`
- ⚠️ No test verifies that a URI Template `href` *without* `templated: true` is flagged or handled.

### 3.3 `templated` is a boolean; non-boolean values default to false
- ✅ `HalLinkAttributesValidationTests.NonBoolean_Templated_Value_Treated_As_False`

### 3.4 `type` — media type hint (optional)
- ✅ `HalLinkObjectTests.LinkObject_Preserves_Optional_Attributes_On_Roundtrip` (covers all optional attributes)
- ❌ No test specifically targets `type` serialization/deserialization in isolation.

### 3.5 `deprecation` — URL indicating the link is deprecated (optional)
- ✅ Covered in roundtrip via `HalLinkObjectTests.LinkObject_Preserves_Optional_Attributes_On_Roundtrip`
- ❌ No isolated test for `deprecation` property.

### 3.6 `name` — secondary key for disambiguation (optional)
- ✅ `LinkObjectCollectionExtensionsTests.GetLinkObject_ByName_*` (4 tests)
- ✅ `ResourceExtensionsTests.GetLinkObject_Should_Return_Link_If_LinkObject_With_Relation_And_Name_Exists`
- ❌ No serialization/deserialization test for `name` in isolation.

### 3.7 `profile` — URI hint for target resource profile (optional)
- ✅ Covered in roundtrip via `HalLinkObjectTests.LinkObject_Preserves_Optional_Attributes_On_Roundtrip`
- ❌ No isolated test for `profile`.

### 3.8 `title` — human-readable label (optional)
- ✅ Covered in roundtrip via `HalLinkObjectTests.LinkObject_Preserves_Optional_Attributes_On_Roundtrip`
- ❌ No isolated test for `title`.

### 3.9 `hreflang` — language indicator (optional)
- ✅ Covered in roundtrip via `HalLinkObjectTests.LinkObject_Preserves_Optional_Attributes_On_Roundtrip`
- ❌ No isolated test for `hreflang`.

### 3.10 Non-string optional attributes are treated as null
- ✅ `HalLinkAttributesValidationTests.NonString_Optional_Attributes_Are_Treated_As_Null`

### 3.11 Unknown Link Object properties are ignored (tolerant reader)
- ❌ No test verifies that extra unknown properties inside a Link Object do not cause errors.

---

## 4. `_embedded`

`_embedded` is an optional property of a Resource Object. Its value is a JSON object whose keys are link relation types and whose values are either a Resource Object or an array of Resource Objects.

### 4.1 Absent `_embedded` produces an empty embedded collection
- ✅ `ResourceTests.Embedded_Should_Return_Empty_EmbeddedCollection_If_Resource_Has_No_Embedded`

### 4.2 Embedded resource as a single object (not array)
- ✅ `HalEmbeddedTests.Embedded_Single_Writes_As_Object`
- ✅ `ResourceConvertersTests.Embedded_Single_Object_Should_Create_EmbeddedResource_With_Resource`

### 4.3 Embedded resource as an array
- ✅ `Tests.Must_Add_Multiple_Resources_To_EmbeddedResourceCollection`
- ✅ `Tests.Must_Deserialize_EmbeddedResourceCollection_With_More_Than_One_EmbeddedResource`

### 4.4 Forcing array serialization even for a single embedded resource
> Implementors may need to force array form to maintain API consistency.

- ✅ `HalEmbeddedTests.ForceWriteAsCollection_Writes_As_Array_Even_If_One_Item`

### 4.5 Embedded resources may themselves contain `_links` and `_embedded`
- ✅ `HalEmbeddedTests.Nested_Embedded_Resources_Are_Read`

### 4.6 Embedded resource with null value handled gracefully
- ✅ `ResourceConvertersTests.Embedded_Null_Value_Should_Create_Empty_EmbeddedResource`

### 4.7 Duplicate embedded relation names
> Behavior when the same relation name appears more than once in `_embedded`.

- ✅ `HalEmbeddedTests.Duplicate_Embedded_Names_Behavior`

### 4.8 Embedded resources may be partial/inconsistent representations
> The spec explicitly allows embedded resources to differ from the canonical resource at its `self` URI.

- ❌ No test documents or validates behavior with intentionally partial embedded resources.

---

## 5. CURIEs (Compact URIs)

CURIEs are established via the `curies` reserved link relation — an array of named Link Objects whose `href` is a URI Template containing the `{rel}` token. They allow shortening long link relation URIs.

### 5.1 `curies` is deserialized as an array of Link Objects
- ✅ `HalCuriesAndTemplatedTests.Curies_Are_Parsed_As_Array_Of_LinkObjects`

### 5.2 CURIE Link Objects have a `name` and a `href` URI Template with `{rel}`
- ✅ Covered implicitly in `HalCuriesAndTemplatedTests.Curies_Are_Parsed_As_Array_Of_LinkObjects`
- ❌ No test verifies that `{rel}` is present in the CURIE template href.

### 5.3 CURIE expansion: short form resolves to full URI
> e.g. `acme:widgets` with template `https://docs.acme.com/relations/{rel}` → `https://docs.acme.com/relations/widgets`

- ✅ `HalCuriesAndTemplatedTests.Curie_Short_Form_Expands_To_Full_Uri`
- ✅ `LinkCollectionExtensionsTests.ExpandCurieRelation_Should_Return_Full_Uri_When_Curie_Exists`

### 5.4 CURIE round-trip serialization
- ❌ No test serializes a resource with CURIEs and verifies the output JSON structure.

### 5.5 Undefined CURIE prefix is handled gracefully
- ❌ No test covers a relation using an undeclared CURIE prefix (e.g. `foo:bar` with no `foo` curie).

---

## 6. Normative Rules

### 6.1 Media type is `application/hal+json`
- ❌ No test validates the media type constant or that it is set correctly when the library is used in an HTTP context.

### 6.2 Reserved properties (`_links`, `_embedded`) MUST NOT appear in state
- ✅ `HalSerializationRoundTripTests.Resource_State_Does_Not_Expose__links_or__embedded_In_State`

### 6.3 `_links` property names are link relation types (strings)
- ✅ `LinkConvertersTests.Deserialize_Should_Skip_Invalid_Rel_Names`
- ❌ No test validates what constitutes a valid vs. invalid relation type name.

### 6.4 `_embedded` property names are link relation types (strings)
- ❌ No equivalent validation test for embedded relation name validity.

---

## 7. Builder API

The fluent builder must produce Resource Objects that conform to the spec.

### 7.1 Builder produces a valid HAL resource
- ✅ `Tests.Link`, `Tests.LinkObject`, and integration tests in `Tests.cs`
- ⚠️ `BuilderTests.test` — only one test exists; builder coverage is thin.

### 7.2 Builder correctly sets `templated` when a URI template is used
- ❌ No test verifies the builder sets `templated: true` when a URI Template href is provided.

### 7.3 Builder produces correct CURIE structure
- ❌ No builder test covers adding CURIEs.

### 7.4 Builder enforces staged construction (invalid transitions)
- ❌ No tests verify that invalid builder state transitions are caught at compile time or runtime.

### 7.5 Builder round-trip: built resource serializes to valid HAL JSON
- ✅ Partial coverage via integration tests in `Tests.cs`
- ❌ No explicit builder → serialize → deserialize → assert round-trip test.

---

## 8. Extension Methods

### 8.1 `GetLink` by relation
- ✅ `LinkCollectionExtensionsTests.GetLink_ByRelation_*` (3 tests)
- ✅ `ResourceExtensionsTests.GetLink_*` (5 tests)

### 8.2 `GetLinkObject` by relation and optional name
- ✅ `ResourceExtensionsTests.GetLinkObject_*` (12 tests)

### 8.3 `GetLinkObjectCollection` by relation
- ✅ `ResourceExtensionsTests.GetLinkObjectCollection_*` (4 tests)

### 8.4 `GetEmbeddedResources` by name
- ✅ `ResourceExtensionsTests.GetEmbeddedResources_*` (4 tests)
- ✅ `EmbeddedResourceCollectionExtensionsTests.GetEmbeddedResource_ByName_*` (3 tests)

### 8.5 `As<T>()` — cast resource collection to typed state
- ✅ `ResourceCollectionExtensionsTests.Should_Cast_All_Resources_In_ResourceCollection_To_Type_Parameter`
- ✅ `ResourceBehaviorTests.As_Should_RoundTrip_Resource_To_Typed_State`

---

## 9. Source Generator (`Chatter.Rest.Hal.CodeGenerators`)

### 9.1 `[HalResponse]` generates `Links` and `Embedded` properties
- ✅ `CodeGeneratorTests.GeneratedProperties_ArePresentOnce_WithCorrectTypes`

### 9.2 Generated properties include correct `[JsonPropertyName]` attributes
- ✅ `HalResponseGeneratorTests.GeneratorAddsLinksForFileScopedNamespaces`
- ✅ `HalResponseGeneratorTests.GeneratorAddsLinksForScopedNameSpaces`

### 9.3 Generator is idempotent across multiple compilations
- ✅ `CodeGeneratorTests.Generator_IsIdempotent_AfterMultipleCompiles`

### 9.4 Generator handles classes without `[HalResponse]` (no output)
- ❌ No test verifies that classes lacking the attribute are not modified.

### 9.5 Generator handles edge cases: generic classes, nested classes, abstract classes
- ❌ No tests for these edge cases.

---

## 10. Coverage Summary

| Area | Total Scenarios | ✅ Covered | ⚠️ Partial | ❌ Not Covered |
|---|---|---|---|---|
| Resource Object | 5 | 5 | 0 | 0 |
| `_links` | 7 | 6 | 0 | 1 |
| Link Objects | 11 | 4 | 2 | 5 |
| `_embedded` | 8 | 6 | 0 | 2 |
| CURIEs | 5 | 2 | 1 | 2 |
| Normative Rules | 4 | 2 | 0 | 2 |
| Builder API | 5 | 1 | 2 | 2 |
| Extension Methods | 5 | 5 | 0 | 0 |
| Source Generator | 5 | 3 | 0 | 2 |
| **Total** | **55** | **34** | **5** | **16** |

---

## 11. Priority Gap List

3 of 19 gaps have been addressed. Remaining gaps ordered by spec compliance risk (highest first):

1. ✅ **COMPLETED** — **[CURIE expansion]** Now tested via `HalCuriesAndTemplatedTests.Curie_Short_Form_Expands_To_Full_Uri` and `LinkCollectionExtensionsTests.ExpandCurieRelation_Should_Return_Full_Uri_When_Curie_Exists`. The `ExpandCurieRelation` extension method was also implemented.
2. ✅ **COMPLETED** — **[Root object validation]** Now tested via 5 new tests in `HalDeserializationRobustnessTests` covering array, string, number, boolean, and null roots.
3. ✅ **COMPLETED** — **[`self` link]** Now tested via 3 new tests in `HalSerializationRoundTripTests` validating serialization, extension access, and null-case handling.
4. **[`href` + `templated` consistency]** No test checks that a URI Template href without `templated: true` is flagged.
5. **[CURIE round-trip]** No test serializes CURIEs and validates the JSON output.
6. **[Undefined CURIE prefix]** No test for a relation using an undeclared CURIE prefix.
7. **[Link Object tolerant reader]** Unknown properties inside a Link Object not tested.
8. **[Builder: `templated` flag]** Builder does not have a test that `templated: true` is set for template hrefs.
9. **[Builder: CURIEs]** No builder test for constructing resources with CURIEs.
10. **[Isolated optional Link Object properties]** `type`, `deprecation`, `name`, `profile`, `title`, `hreflang` only tested together in a roundtrip; no per-property isolation tests.
11. **[Single-vs-array consistency]** No test for the SHOULD NOT change form between responses requirement.
12. **[Partial embedded resources]** No test documents behavior with intentionally partial embedded representations.
13. **[Source generator edge cases]** Generic, nested, and abstract classes with `[HalResponse]` not tested.
14. **[Media type constant]** `application/hal+json` not validated anywhere.
