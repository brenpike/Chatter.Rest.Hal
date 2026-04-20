using System.Text.Json;
using Chatter.Rest.Hal.Builders;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests
{
    public class HalCuriesAndTemplatedTests
    {
        [Fact]
        public void Curies_Are_Parsed_As_Array_Of_LinkObjects()
        {
            var res = TestHelpers.LoadResourceFromFixture("curies_and_templated.json");

            res.Should().NotBeNull();

            var curies = res!.Links.SingleOrDefault(l => l.Rel == "curies");
            curies.Should().NotBeNull();
            curies!.LinkObjects.Count.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Templated_Link_Has_Templated_True_If_Provided()
        {
            var res = TestHelpers.LoadResourceFromFixture("curies_and_templated.json");

            res.Should().NotBeNull();

            var itemLink = res!.Links.SingleOrDefault(l => l.Rel == "ex:item");
            itemLink.Should().NotBeNull();

            var lo = itemLink!.LinkObjects.SingleOrDefault();
            lo.Should().NotBeNull();

            lo!.Templated.Should().BeTrue();
            lo.Href.Should().Contain("{id}");
        }

        [Fact]
        public void Templated_Href_Does_Not_Automatically_Expand()
        {
            var res = TestHelpers.LoadResourceFromFixture("curies_and_templated.json");

            res.Should().NotBeNull();

            var itemLink = res!.Links.SingleOrDefault(l => l.Rel == "ex:item");
            itemLink.Should().NotBeNull();

            var lo = itemLink!.LinkObjects.SingleOrDefault();
            lo.Should().NotBeNull();

            // The SDK should not expand templates during deserialization; the href should retain template variables
            lo!.Href.Should().Contain("{id}");
        }

        [Fact]
        public void Curie_Short_Form_Expands_To_Full_Uri()
        {
            var res = TestHelpers.LoadResourceFromFixture("curies_and_templated.json");

            res.Should().NotBeNull();

            // The CURIE definition is: "ex" -> "http://example.com/docs/{rel}"
            // When we expand "ex:widgets", the {rel} token should be replaced with "widgets"
            var expandedRelation = res!.Links.ExpandCurieRelation("ex:widgets");

            expandedRelation.Should().Be("http://example.com/docs/widgets");
        }

        [Fact]
        public void Curie_Expansion_Returns_Original_When_No_Matching_Prefix()
        {
            var res = TestHelpers.LoadResourceFromFixture("curies_and_templated.json");

            res.Should().NotBeNull();

            // "foo" is not a defined CURIE prefix in the fixture
            // The expansion should return the original relation unchanged
            var expandedRelation = res!.Links.ExpandCurieRelation("foo:bar");

            expandedRelation.Should().Be("foo:bar");
        }

        [Fact]
        public void Curie_Expansion_Handles_Missing_Rel_Token_In_Template()
        {
            // Build a resource with a CURIE definition that lacks the {rel} token
            var resourceWithInvalidCurie = ResourceBuilder.New()
                .AddSelf().AddLinkObject("/test")
                .AddCuries().AddLinkObject("http://example.com/docs/static", "broken")
                .AddLink("broken:widgets").AddLinkObject("/widgets")
                .Build();

            // When the CURIE template doesn't contain {rel}, expansion should return the original relation
            var expandedRelation = resourceWithInvalidCurie.Links.ExpandCurieRelation("broken:widgets");

            expandedRelation.Should().Be("broken:widgets");
        }

        [Fact]
        public void Curie_Expansion_With_Multiple_Curies()
        {
            // Build a resource with multiple CURIE definitions using the builder API
            // Note: The builder appears to support only one CURIE LinkObject per AddCuries() call,
            // so we test with one curie from the builder and verify the JSON fixture handles multiple
            var resourceWithOneCurie = ResourceBuilder.New()
                .AddSelf().AddLinkObject("/test")
                .AddCuries().AddLinkObject("http://example.com/relations/{rel}", "ex")
                .Build();

            // Verify that "ex:widgets" expands using the "ex" CURIE definition
            var expandedEx = resourceWithOneCurie.Links.ExpandCurieRelation("ex:widgets");
            expandedEx.Should().Be("http://example.com/relations/widgets");

            // Additionally test with deserialized JSON that contains multiple CURIEs
            var resourceFromJson = TestHelpers.LoadResourceFromFixture("curies_and_templated.json");
            resourceFromJson.Should().NotBeNull();

            // The fixture contains a CURIE "ex" -> "http://example.com/docs/{rel}"
            var expandedFromJson = resourceFromJson!.Links.ExpandCurieRelation("ex:item");
            expandedFromJson.Should().Be("http://example.com/docs/item");
        }

        [Fact]
        public void Templated_Href_Without_Templated_Flag_Is_Handled_Gracefully()
        {
            // HAL spec section 5.2: templated property is OPTIONAL
            // This test validates that the library accepts URI Template hrefs without templated: true
            // and handles them gracefully (permissive parsing behavior)

            // Test case 1: Standard URI Template with variable
            var resourceWithTemplate = ResourceBuilder.New()
                .AddSelf().AddLinkObject("/test")
                .AddLink("item").AddLinkObject("/items/{id}")
                .Build();

            var json = System.Text.Json.JsonSerializer.Serialize(resourceWithTemplate);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<Resource>(json);

            deserialized.Should().NotBeNull();
            var itemLink = deserialized!.Links.SingleOrDefault(l => l.Rel == "item");
            itemLink.Should().NotBeNull();
            itemLink!.LinkObjects.SingleOrDefault()!.Href.Should().Be("/items/{id}");

            // Test case 2: URI Template with nested braces (edge case)
            var resourceWithNestedBraces = ResourceBuilder.New()
                .AddSelf().AddLinkObject("/test")
                .AddLink("complex").AddLinkObject("/path/{outer{inner}}")
                .Build();

            var json2 = System.Text.Json.JsonSerializer.Serialize(resourceWithNestedBraces);
            var deserialized2 = System.Text.Json.JsonSerializer.Deserialize<Resource>(json2);

            deserialized2.Should().NotBeNull();
            var complexLink = deserialized2!.Links.SingleOrDefault(l => l.Rel == "complex");
            complexLink.Should().NotBeNull();
            complexLink!.LinkObjects.SingleOrDefault()!.Href.Should().Be("/path/{outer{inner}}");

            // Test case 3: Malformed template with unmatched braces (edge case)
            var resourceWithMalformedBraces = ResourceBuilder.New()
                .AddSelf().AddLinkObject("/test")
                .AddLink("malformed").AddLinkObject("/path/{notatemplate")
                .Build();

            var json3 = System.Text.Json.JsonSerializer.Serialize(resourceWithMalformedBraces);
            var deserialized3 = System.Text.Json.JsonSerializer.Deserialize<Resource>(json3);

            deserialized3.Should().NotBeNull();
            var malformedLink = deserialized3!.Links.SingleOrDefault(l => l.Rel == "malformed");
            malformedLink.Should().NotBeNull();
            malformedLink!.LinkObjects.SingleOrDefault()!.Href.Should().Be("/path/{notatemplate");

            // Test case 4: Empty template (edge case)
            var resourceWithEmptyTemplate = ResourceBuilder.New()
                .AddSelf().AddLinkObject("/test")
                .AddLink("empty").AddLinkObject("/path/{}")
                .Build();

            var json4 = System.Text.Json.JsonSerializer.Serialize(resourceWithEmptyTemplate);
            var deserialized4 = System.Text.Json.JsonSerializer.Deserialize<Resource>(json4);

            deserialized4.Should().NotBeNull();
            var emptyLink = deserialized4!.Links.SingleOrDefault(l => l.Rel == "empty");
            emptyLink.Should().NotBeNull();
            emptyLink!.LinkObjects.SingleOrDefault()!.Href.Should().Be("/path/{}");
        }

        [Fact]
        public void Curie_Definition_Serializes_As_Array_Of_LinkObjects()
        {
            // HAL spec section 5.4: CURIEs MUST be serialized as an array of Link Objects
            // This test validates round-trip serialization of CURIE definitions with multiple CURIEs.
            // Note: The builder API currently only supports adding one CURIE per AddCuries() call,
            // so we manually construct a resource with multiple CURIE LinkObjects.

            // Manually construct a resource with multiple CURIE definitions
            var curiesLink = new Link("curies");
            curiesLink.LinkObjects.Add(new LinkObject("https://docs.acme.com/relations/{rel}")
            {
                Name = "acme",
                Templated = true
            });
            curiesLink.LinkObjects.Add(new LinkObject("https://api.widgets.com/rels/{rel}")
            {
                Name = "widgets",
                Templated = true
            });

            var resource = new Resource();
            resource.Links.Add(new Link("self") { LinkObjects = { new LinkObject("/test") } });
            resource.Links.Add(curiesLink);
            resource.Links.Add(new Link("acme:orders") { LinkObjects = { new LinkObject("/orders") } });
            resource.Links.Add(new Link("widgets:product") { LinkObjects = { new LinkObject("/products/123") } });

            // Verify the resource structure before serialization
            var curiesLinkVerify = resource.Links.SingleOrDefault(l => l.Rel == "curies");
            curiesLinkVerify.Should().NotBeNull();
            curiesLinkVerify!.LinkObjects.Should().HaveCount(2);

            // Serialize the resource to JSON
            var json = JsonSerializer.Serialize(resource);

            // Parse the JSON to verify structure
            var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;

            // Verify _links exists
            root.TryGetProperty("_links", out var linksElement).Should().BeTrue();

            // Verify curies exists and is an array (when there are 2+ CURIEs)
            linksElement.TryGetProperty("curies", out var curiesElement).Should().BeTrue();
            curiesElement.ValueKind.Should().Be(JsonValueKind.Array);

            // Verify we have exactly 2 CURIEs
            var curiesArray = curiesElement.EnumerateArray().ToList();
            curiesArray.Should().HaveCount(2);

            // Verify first CURIE (acme)
            var acmeCurie = curiesArray[0];
            acmeCurie.TryGetProperty("name", out var acmeName).Should().BeTrue();
            acmeName.GetString().Should().Be("acme");

            acmeCurie.TryGetProperty("href", out var acmeHref).Should().BeTrue();
            var acmeHrefValue = acmeHref.GetString();
            acmeHrefValue.Should().Be("https://docs.acme.com/relations/{rel}");
            acmeHrefValue.Should().Contain("{rel}");

            acmeCurie.TryGetProperty("templated", out var acmeTemplated).Should().BeTrue();
            acmeTemplated.GetBoolean().Should().BeTrue();

            // Verify second CURIE (widgets)
            var widgetsCurie = curiesArray[1];
            widgetsCurie.TryGetProperty("name", out var widgetsName).Should().BeTrue();
            widgetsName.GetString().Should().Be("widgets");

            widgetsCurie.TryGetProperty("href", out var widgetsHref).Should().BeTrue();
            var widgetsHrefValue = widgetsHref.GetString();
            widgetsHrefValue.Should().Be("https://api.widgets.com/rels/{rel}");
            widgetsHrefValue.Should().Contain("{rel}");

            widgetsCurie.TryGetProperty("templated", out var widgetsTemplated).Should().BeTrue();
            widgetsTemplated.GetBoolean().Should().BeTrue();

            // Verify that links using CURIE prefixes are also serialized correctly
            linksElement.TryGetProperty("acme:orders", out var acmeOrdersLink).Should().BeTrue();
            linksElement.TryGetProperty("widgets:product", out var widgetsProductLink).Should().BeTrue();
        }

        [Fact]
        public void Curie_Expansion_Handles_Empty_Prefix()
        {
            // Build a resource with a valid CURIE definition
            var resource = ResourceBuilder.New()
                .AddSelf().AddLinkObject("/test")
                .AddCuries().AddLinkObject("http://example.com/docs/{rel}", "ex")
                .Build();

            // Test relation with empty prefix (":bar")
            // Should return the original relation unchanged since there's no prefix to match
            var expandedRelation = resource.Links.ExpandCurieRelation(":bar");

            expandedRelation.Should().Be(":bar");
        }

        [Fact]
        public void Curie_Expansion_Handles_Multiple_Colons()
        {
            // Build a resource with a valid CURIE definition
            var resource = ResourceBuilder.New()
                .AddSelf().AddLinkObject("/test")
                .AddCuries().AddLinkObject("http://example.com/docs/{rel}", "foo")
                .Build();

            // Test relation with multiple colons ("foo:bar:baz")
            // Should expand using first colon as delimiter; "bar:baz" becomes the reference value
            var expandedRelation = resource.Links.ExpandCurieRelation("foo:bar:baz");

            expandedRelation.Should().Be("http://example.com/docs/bar:baz");
        }

        [Fact]
        public void Curie_Expansion_When_Curies_Collection_Is_Empty()
        {
            // Build a resource with a curies link that has no LinkObjects
            var curiesLink = new Link("curies");
            // Deliberately leave the LinkObjects collection empty

            var resource = new Resource();
            resource.Links.Add(new Link("self") { LinkObjects = { new LinkObject("/test") } });
            resource.Links.Add(curiesLink);

            // Verify the curies link exists but is empty
            var curies = resource.Links.SingleOrDefault(l => l.Rel == "curies");
            curies.Should().NotBeNull();
            curies!.LinkObjects.Should().BeEmpty();

            // Test CURIE expansion with an empty curies collection
            // Should return the original relation unchanged since no CURIEs are defined
            var expandedRelation = resource.Links.ExpandCurieRelation("ex:widgets");

            expandedRelation.Should().Be("ex:widgets");
        }

        [Fact]
        public void Curie_Template_Contains_Rel_Token()
        {
            // HAL spec section 8: CURIE Link Objects MUST have a templated href containing {rel} placeholder
            // This test validates that CURIE definitions contain the required {rel} token in their href
            // and validates case-sensitive matching ({rel} not {REL})

            // Test case 1: Deserialize fixture with valid CURIE definition
            var resourceFromFixture = TestHelpers.LoadResourceFromFixture("curies_and_templated.json");
            resourceFromFixture.Should().NotBeNull();

            var curiesFromFixture = resourceFromFixture!.Links.SingleOrDefault(l => l.Rel == "curies");
            curiesFromFixture.Should().NotBeNull();

            // Assert each CURIE's href contains the exact case-sensitive {rel} token
            foreach (var curieObject in curiesFromFixture!.LinkObjects)
            {
                curieObject.Href.Should().Contain("{rel}", "CURIE href must contain the {rel} placeholder token");
            }

            // Test case 2: CURIE with {rel} at the start of the template
            var resourceRelAtStart = ResourceBuilder.New()
                .AddSelf().AddLinkObject("/test")
                .AddCuries().AddLinkObject("{rel}/docs", "start")
                .Build();

            var curiesAtStart = resourceRelAtStart.Links.SingleOrDefault(l => l.Rel == "curies");
            curiesAtStart.Should().NotBeNull();
            curiesAtStart!.LinkObjects.Should().ContainSingle()
                .Which.Href.Should().Contain("{rel}");

            // Test case 3: CURIE with {rel} in the middle of the template
            var resourceRelInMiddle = ResourceBuilder.New()
                .AddSelf().AddLinkObject("/test")
                .AddCuries().AddLinkObject("http://example.com/{rel}/docs", "middle")
                .Build();

            var curiesInMiddle = resourceRelInMiddle.Links.SingleOrDefault(l => l.Rel == "curies");
            curiesInMiddle.Should().NotBeNull();
            curiesInMiddle!.LinkObjects.Should().ContainSingle()
                .Which.Href.Should().Contain("{rel}");

            // Test case 4: CURIE with {rel} at the end of the template
            var resourceRelAtEnd = ResourceBuilder.New()
                .AddSelf().AddLinkObject("/test")
                .AddCuries().AddLinkObject("http://example.com/docs/{rel}", "end")
                .Build();

            var curiesAtEnd = resourceRelAtEnd.Links.SingleOrDefault(l => l.Rel == "curies");
            curiesAtEnd.Should().NotBeNull();
            curiesAtEnd!.LinkObjects.Should().ContainSingle()
                .Which.Href.Should().Contain("{rel}");

            // Test case 5: CURIE with multiple {rel} tokens (edge case)
            var resourceMultipleRel = ResourceBuilder.New()
                .AddSelf().AddLinkObject("/test")
                .AddCuries().AddLinkObject("http://example.com/{rel}/docs/{rel}", "multi")
                .Build();

            var curiesMultiple = resourceMultipleRel.Links.SingleOrDefault(l => l.Rel == "curies");
            curiesMultiple.Should().NotBeNull();
            curiesMultiple!.LinkObjects.Should().ContainSingle()
                .Which.Href.Should().Contain("{rel}");

            // Test case 6: Verify case-sensitive match - {REL} should NOT match
            var resourceUpperCase = ResourceBuilder.New()
                .AddSelf().AddLinkObject("/test")
                .AddCuries().AddLinkObject("http://example.com/{REL}/docs", "upper")
                .Build();

            var curiesUpperCase = resourceUpperCase.Links.SingleOrDefault(l => l.Rel == "curies");
            curiesUpperCase.Should().NotBeNull();
            curiesUpperCase!.LinkObjects.Should().ContainSingle()
                .Which.Href.Should().NotContain("{rel}", "case-sensitive: {REL} is not {rel}");
        }
    }
}
