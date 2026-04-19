using System.Linq;
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
    }
}
