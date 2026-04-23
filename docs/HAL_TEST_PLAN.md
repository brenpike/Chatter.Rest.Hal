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
> Once a relation is expressed as an array, it should stay an array (and vice versa). Implementors may force array form via `Link.IsArray`, `AsArray()`, or global `HalJsonOptions.AlwaysUseArrayForLinks`.

- ✅ `HalSerializationRoundTripTests.Link_Array_Form_Is_Preserved_Through_Roundtrip` — verifies array form is maintained through serialization round-trip
- ✅ `HalForceArrayTests.Serialize_WithAlwaysUseArrayTrue_ViaAddHalConverters_SingleLink_ProducesArray` — global opt-in via `AddHalConverters` forces array for single link object
- ✅ `HalForceArrayTests.Serialize_GlobalFalse_PerRelationIsArrayTrue_ProducesArray` — per-relation `IsArray` forces array when global is off
- ✅ `HalForceArrayTests.RoundTrip_LinkWithIsArrayTrue_PreservesArrayRepresentation` — array form preserved through full serialize/deserialize/re-serialize cycle
- ✅ `LinkConvertersTests.Deserialize_LinkRelationAsArray_SetsIsArrayTrue_OnLink` — deserializing an array relation sets `IsArray=true` for round-trip fidelity
- ✅ `LinkConvertersTests.Serialize_Link_WithIsArrayTrue_SingleLinkObject_ProducesJsonArray` — `Link.IsArray=true` emits array even with one link object
- ✅ `LinkConvertersTests.Serialize_Link_WithIsArrayTrue_ZeroLinkObjects_ProducesEmptyArray` — zero link objects with `IsArray=true` emits `[]`

### 2.5 Link relation type as a null value is handled gracefully
- ✅ `LinkBehaviorTests.Reading_Link_With_Null_Value_Produces_Link_With_No_LinkObjects`
- ✅ `HalLinksCollectionTests.Reading_Link_With_Null_Value_Produces_Link_With_No_LinkObjects`
- ✅ `LinkConvertersTests.Deserialize_Null_Link_Value_Should_Create_Empty_LinkObjects`

### 2.6 Link relation types follow IANA registry or URI conventions
> Custom link relation types SHOULD be URIs that provide documentation when dereferenced.

- ❌ Out of scope. The spec says SHOULD (not MUST) for URI-format relation types. This library is a serializer/deserializer, not a validator. Enforcing URI format at parse time would break tolerant-reader behavior. No test planned.

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
- ✅ `HalLinkAttributesValidationTests.Templated_Href_Without_Templated_Flag_Is_Handled_Gracefully` — verifies URI template href without `templated: true` is handled gracefully

### 3.3 `templated` is a boolean; non-boolean values default to false
- ✅ `HalLinkAttributesValidationTests.NonBoolean_Templated_Value_Treated_As_False`

### 3.4 `type` — media type hint (optional)
- ✅ `HalLinkObjectTests.LinkObject_Preserves_Optional_Attributes_On_Roundtrip` (covers all optional attributes)
- ✅ `HalLinkObjectTests.LinkObject_Type_Property_Serializes_And_Deserializes` — isolated test for `type` property

### 3.5 `deprecation` — URL indicating the link is deprecated (optional)
- ✅ Covered in roundtrip via `HalLinkObjectTests.LinkObject_Preserves_Optional_Attributes_On_Roundtrip`
- ✅ `HalLinkObjectTests.LinkObject_Deprecation_Property_Serializes_And_Deserializes` — isolated test for `deprecation` property

### 3.6 `name` — secondary key for disambiguation (optional)
- ✅ `LinkObjectCollectionExtensionsTests.GetLinkObject_ByName_*` (4 tests)
- ✅ `ResourceExtensionsTests.GetLinkObject_Should_Return_Link_If_LinkObject_With_Relation_And_Name_Exists`
- ✅ `HalLinkObjectTests.LinkObject_Name_Property_Serializes_And_Deserializes` — isolated serialization test for `name` property

### 3.7 `profile` — URI hint for target resource profile (optional)
- ✅ Covered in roundtrip via `HalLinkObjectTests.LinkObject_Preserves_Optional_Attributes_On_Roundtrip`
- ✅ `HalLinkObjectTests.LinkObject_Profile_Property_Serializes_And_Deserializes` — isolated test for `profile` property

### 3.8 `title` — human-readable label (optional)
- ✅ Covered in roundtrip via `HalLinkObjectTests.LinkObject_Preserves_Optional_Attributes_On_Roundtrip`
- ✅ `HalLinkObjectTests.LinkObject_Title_Property_Serializes_And_Deserializes` — isolated test for `title` property

### 3.9 `hreflang` — language indicator (optional)
- ✅ Covered in roundtrip via `HalLinkObjectTests.LinkObject_Preserves_Optional_Attributes_On_Roundtrip`
- ✅ `HalLinkObjectTests.LinkObject_Hreflang_Property_Serializes_And_Deserializes` — isolated test for `hreflang` property

### 3.10 Non-string optional attributes are treated as null
- ✅ `HalLinkAttributesValidationTests.NonString_Optional_Attributes_Are_Treated_As_Null`

### 3.11 Unknown Link Object properties are ignored (tolerant reader)
- ✅ `HalLinkAttributesValidationTests.LinkObject_With_Unknown_Properties_Are_Ignored` — verifies unknown properties in Link Object do not cause errors

### 3.12 URI Template expansion (RFC 6570 Level 1)
> `LinkObject` provides helpers to extract template variables and expand Level 1 URI Templates per RFC 6570. Operator-prefixed expressions (Level 2+) are intentionally left unexpanded.

- ✅ `LinkObjectTemplateExpansionTests.GetTemplateVariables_SingleVariable_ReturnsList`
- ✅ `LinkObjectTemplateExpansionTests.GetTemplateVariables_MultipleVariables_ReturnsList`
- ✅ `LinkObjectTemplateExpansionTests.GetTemplateVariables_DuplicateVariables_ReturnsDistinct`
- ✅ `LinkObjectTemplateExpansionTests.GetTemplateVariables_NoVariables_ReturnsEmpty`
- ✅ `LinkObjectTemplateExpansionTests.GetTemplateVariables_NotTemplated_ReturnsEmpty`
- ✅ `LinkObjectTemplateExpansionTests.GetTemplateVariables_TemplatedNull_ReturnsEmpty`
- ✅ `LinkObjectTemplateExpansionTests.GetTemplateVariables_OperatorPrefixed_NotMatched`
- ✅ `LinkObjectTemplateExpansionTests.Expand_SingleVariable_Substituted`
- ✅ `LinkObjectTemplateExpansionTests.Expand_MultipleVariables_AllSubstituted`
- ✅ `LinkObjectTemplateExpansionTests.Expand_UnresolvedVariable_LeftAsIs`
- ✅ `LinkObjectTemplateExpansionTests.Expand_EmptyDictionary_HrefUnchanged`
- ✅ `LinkObjectTemplateExpansionTests.Expand_NotTemplated_ReturnsHrefUnchanged`
- ✅ `LinkObjectTemplateExpansionTests.Expand_TemplatedNull_ReturnsHrefUnchanged`
- ✅ `LinkObjectTemplateExpansionTests.Expand_NullVariables_ThrowsArgumentNullException`
- ✅ `LinkObjectTemplateExpansionTests.Expand_OperatorPrefixed_LeftUnchanged`
- ✅ `LinkObjectTemplateExpansionTests.Expand_MixedLevel1AndOperator_OnlyLevel1Expanded`
- ✅ `LinkObjectTemplateExpansionTests.Expand_ParamsTuple_SingleVariable_Substituted`
- ✅ `LinkObjectTemplateExpansionTests.Expand_ParamsTuple_MultipleVariables_AllSubstituted`
- ✅ `LinkObjectTemplateExpansionTests.Expand_ParamsTuple_EmptyParams_HrefUnchanged`
- ✅ `LinkObjectTemplateExpansionTests.Expand_ParamsTuple_UnresolvedVariable_LeftAsIs`
- ✅ `LinkObjectTemplateExpansionTests.Expand_ParamsTuple_NotTemplated_ReturnsHrefUnchanged`

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

- ✅ `HalEmbeddedTests.Embedded_Resources_May_Be_Partial_Representations`

---

## 5. CURIEs (Compact URIs)

CURIEs are established via the `curies` reserved link relation — an array of named Link Objects whose `href` is a URI Template containing the `{rel}` token. They allow shortening long link relation URIs.

### 5.1 `curies` is deserialized as an array of Link Objects
- ✅ `HalCuriesAndTemplatedTests.Curies_Are_Parsed_As_Array_Of_LinkObjects`

### 5.2 CURIE Link Objects have a `name` and a `href` URI Template with `{rel}`
- ✅ Covered implicitly in `HalCuriesAndTemplatedTests.Curies_Are_Parsed_As_Array_Of_LinkObjects`
- ✅ `HalCuriesAndTemplatedTests.Curie_Template_Contains_Rel_Token`

### 5.3 CURIE expansion: short form resolves to full URI
> e.g. `acme:widgets` with template `https://docs.acme.com/relations/{rel}` → `https://docs.acme.com/relations/widgets`

- ✅ `HalCuriesAndTemplatedTests.Curie_Short_Form_Expands_To_Full_Uri`
- ✅ `LinkCollectionExtensionsTests.ExpandCurieRelation_Should_Return_Full_Uri_When_Curie_Exists`

### 5.4 CURIE round-trip serialization
- ✅ `HalCuriesAndTemplatedTests.Curie_Definition_Serializes_As_Array_Of_LinkObjects` — serializes and verifies CURIE structure in JSON output

### 5.5 Undefined CURIE prefix is handled gracefully
- ✅ `HalCuriesAndTemplatedTests.Curie_Expansion_Returns_Original_When_No_Matching_Prefix` — verifies undefined CURIE prefix returns original relation

---

## 6. Normative Rules

### 6.1 Media type is `application/hal+json`
- ✅ `HalMediaTypeTests.MediaType_Constant_Is_Correct`

### 6.2 Reserved properties (`_links`, `_embedded`) MUST NOT appear in state
- ✅ `HalSerializationRoundTripTests.Resource_State_Does_Not_Expose__links_or__embedded_In_State`

### 6.3 `_links` property names are link relation types (strings)
- ✅ `LinkConvertersTests.Deserialize_Should_Skip_Invalid_Rel_Names`
- ✅ `HalLinkAttributesValidationTests.Link_Relation_Types_Are_Strings`

### 6.4 `_embedded` property names are link relation types (strings)
- ✅ `HalEmbeddedTests.Embedded_Relation_Names_Are_Strings` — validates IANA names, full URIs, CURIEs, and special-character relation names as embedded keys

---

## 7. Builder API

The fluent builder must produce Resource Objects that conform to the spec.

### 7.1 Builder produces a valid HAL resource
- ✅ `Tests.Link`, `Tests.LinkObject`, and integration tests in `Tests.cs`
- ✅ Builder coverage expanded: `Builder_Sets_Templated_True_For_URI_Template`, `Builder_Constructs_Valid_CURIE_Structure`, `Builder_Staged_Interfaces_Enforce_Valid_Construction_Order`, `Builder_RoundTrip_BuiltResource_SerializesAndDeserializesCorrectly`

### 7.2 Builder correctly sets `templated` when a URI template is used
- ✅ `BuilderTests.Builder_Sets_Templated_True_For_URI_Template` — verifies builder sets `templated: true` for URI template href

### 7.3 Builder produces correct CURIE structure
- ✅ `BuilderTests.Builder_Constructs_Valid_CURIE_Structure` — verifies builder can construct resources with valid CURIE definitions

### 7.4 Builder enforces staged construction (invalid transitions)
- ✅ `BuilderTests.Builder_Staged_Interfaces_Enforce_Valid_Construction_Order` — verifies via reflection that `IResourceCreationStage` does not expose `AddLinkObject` and that `IResourceLinkCreationStage` does; documents compile-time enforcement

### 7.5 Builder round-trip: built resource serializes to valid HAL JSON
- ✅ `BuilderTests.Builder_RoundTrip_BuiltResource_SerializesAndDeserializesCorrectly` — builds a resource with state, links, and embedded; serializes and deserializes; asserts all components are preserved. Chained `.AddLinkObject()` calls (fixed in commit `83dfb97`) are verified: the `collection` relation asserts `HaveCount(2)` with both `/items` and `/items/latest`.

### 7.6 Builder supports `AsArray()` for per-relation forced array serialization
- ✅ `BuilderTests.Builder_AsArray_SetsFlagOnLink` — `AsArray()` after `AddLink()` sets `Link.IsArray=true` on built link
- ✅ `BuilderTests.Builder_AsArray_AfterAddLinkObject_SetsFlagOnLink` — `AsArray()` after `AddLinkObject()` propagates to parent link
- ✅ `BuilderTests.Builder_WithoutAsArray_IsArrayDefaultsFalse` — confirms default is false (no breaking change)
- ✅ `BuilderTests.Builder_AddSelf_AsArray_Works` — `AsArray()` works on self link
- ✅ `BuilderTests.Builder_AddCuries_AsArray_Works` — `AsArray()` works on curies link

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
- ✅ `CodeGeneratorTests.Class_Without_HalResponse_Attribute_Is_Not_Modified` — verifies `PlainClass` (no attribute) has no generated `Links` or `Embedded` properties

### 9.5 Generator handles edge cases: generic classes, nested classes, abstract classes
- ✅ `CodeGeneratorTests.Abstract_Class_With_HalResponse_Gets_Generated_Properties` — verifies abstract partial classes receive generated `Links` and `Embedded` properties
- ❌ Generic classes: the emitter (`Emitter.cs`) reads only `Identifier.Text` and omits `TypeParameterList` and `ConstraintClauses`, so generated output drops type parameters and produces a compile error. Not supported by design — generic HAL response types are outside the spec scope.
- ❌ Nested classes: `GetNamespaceFrom` walks ancestors for namespace/file-scoped namespace nodes only and does not accumulate outer class wrappers. The generated file emits a top-level partial class with the inner class name, which collides or produces errors. Not supported by design.

---

## 10. Coverage Summary

| Area | Total Scenarios | ✅ Covered | ⚠️ Partial | ❌ Not Covered |
|---|---|---|---|---|
| Resource Object | 5 | 5 | 0 | 0 |
| `_links` | 7 | 6 | 0 | 1 |
| Link Objects | 12 | 12 | 0 | 0 |
| `_embedded` | 8 | 8 | 0 | 0 |
| CURIEs | 5 | 5 | 0 | 0 |
| Normative Rules | 4 | 4 | 0 | 0 |
| Builder API | 6 | 6 | 0 | 0 |
| Extension Methods | 5 | 5 | 0 | 0 |
| Source Generator | 5 | 4 | 0 | 1 |
| **Total** | **57** | **55** | **0** | **2** |

---

## 11. Priority Gap List

All 20 gaps have been addressed or classified. 18 are fully covered (✅); 2 are classified as known out-of-scope limitations (❌): generic class support and nested class support in the source generator emitter.

1. ✅ **COMPLETED** — **[CURIE expansion]** Now tested via `HalCuriesAndTemplatedTests.Curie_Short_Form_Expands_To_Full_Uri` and `LinkCollectionExtensionsTests.ExpandCurieRelation_Should_Return_Full_Uri_When_Curie_Exists`. The `ExpandCurieRelation` extension method was also implemented.
2. ✅ **COMPLETED** — **[Root object validation]** Now tested via 5 new tests in `HalDeserializationRobustnessTests` covering array, string, number, boolean, and null roots.
3. ✅ **COMPLETED** — **[`self` link]** Now tested via 3 new tests in `HalSerializationRoundTripTests` validating serialization, extension access, and null-case handling.
4. ✅ **COMPLETED** — **[`href` + `templated` consistency]** Now tested via `HalLinkAttributesValidationTests.Templated_Href_Without_Templated_Flag_Is_Handled_Gracefully`.
5. ✅ **COMPLETED** — **[CURIE round-trip]** Now tested via `HalCuriesAndTemplatedTests.Curie_Definition_Serializes_As_Array_Of_LinkObjects`.
6. ✅ **COMPLETED** — **[Undefined CURIE prefix]** Now tested via `HalCuriesAndTemplatedTests.Curie_Expansion_Returns_Original_When_No_Matching_Prefix`.
7. ✅ **COMPLETED** — **[Link Object tolerant reader]** Now tested via `HalLinkAttributesValidationTests.LinkObject_With_Unknown_Properties_Are_Ignored`.
8. ✅ **COMPLETED** — **[Builder: `templated` flag]** Now tested via `BuilderTests.Builder_Sets_Templated_True_For_URI_Template`.
9. ✅ **COMPLETED** — **[Builder: CURIEs]** Now tested via `BuilderTests.Builder_Constructs_Valid_CURIE_Structure`.
10. ✅ **COMPLETED** — **[Isolated optional Link Object properties]** Now tested via 6 new tests in `HalLinkObjectTests` covering `type`, `deprecation`, `name`, `profile`, `title`, and `hreflang` properties individually.
11. ✅ **COMPLETED** — **[Single-vs-array consistency]** Now tested via `HalSerializationRoundTripTests.Link_Array_Form_Is_Preserved_Through_Roundtrip`.
12. ✅ **COMPLETED** — **[Partial embedded resources]** Now tested via `HalEmbeddedTests.Embedded_Resources_May_Be_Partial_Representations`.
13. ✅ **COMPLETED** — **[CURIE template `{rel}` token]** Now tested via `HalCuriesAndTemplatedTests.Curie_Template_Contains_Rel_Token`.
14. ✅ **COMPLETED** — **[Media type constant]** Now tested via `HalMediaTypeTests.MediaType_Constant_Is_Correct`.
15. ✅ **COMPLETED** — **[Link relation type validation]** Now tested via `HalLinkAttributesValidationTests.Link_Relation_Types_Are_Strings`.
16. ✅ **COMPLETED** — **[Embedded relation name validation]** Now tested via `HalEmbeddedTests.Embedded_Relation_Names_Are_Strings`.
17. ✅ **COMPLETED** — **[Builder state transitions]** Now tested via `BuilderTests.Builder_Staged_Interfaces_Enforce_Valid_Construction_Order`; compile-time enforcement documented via reflection assertions.
18. ✅ **COMPLETED** — **[Builder round-trip]** Tested via `BuilderTests.Builder_RoundTrip_BuiltResource_SerializesAndDeserializesCorrectly`. The chained `.AddLinkObject()` bug was fixed in commit `83dfb97` (delegate chained calls to parent collection). The test asserts `collectionLink.LinkObjects.Should().HaveCount(2)` confirming two chained link objects survive round-trip.
19. ✅ **COMPLETED** — **[Source generator: classes without attribute]** Now tested via `CodeGeneratorTests.Class_Without_HalResponse_Attribute_Is_Not_Modified`.
20. ✅ **COMPLETED** — **[Source generator: edge cases]** Abstract classes tested via `CodeGeneratorTests.Abstract_Class_With_HalResponse_Gets_Generated_Properties`. Generic and nested classes are classified as ❌ known emitter limitations (see section 9.5) — outside HAL spec scope; no tests planned.
