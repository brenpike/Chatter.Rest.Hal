using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests
{
    public class HalLinkObjectTests
    {
        [Fact]
        public void LinkObject_Serializes_Required_Href()
        {
            var lo = TestHelpers.CreateLinkObject("/orders/1");
            var json = JsonSerializer.Serialize(lo);

            // Should contain the required href property
            TestHelpers.AssertJsonContainsNormalized(json, "{ \"href\": \"/orders/1\" }");
        }

        [Fact]
        public void LinkObject_Reads_Templated_True_For_Template()
        {
            var resource = TestHelpers.LoadResourceFromFixture("templated_link.json");
            var link = resource.Links.FirstOrDefault(l => l.Rel == "self");
            link.Should().NotBeNull();
            link!.LinkObjects.Should().HaveCount(1);
            var lo = link.LinkObjects.First();
            lo.Templated.Should().BeTrue();
        }

        [Fact]
        public void LinkObject_Missing_Href_Produces_Null_On_Deserialization()
        {
            var resource = TestHelpers.LoadResourceFromFixture("invalid_link_missing_href.json");
            var link = resource.Links.FirstOrDefault(l => l.Rel == "self");
            link.Should().NotBeNull();

            // The LinkObjectConverter returns null for malformed link objects (missing href)
            // The LinkCollectionConverter will add a Link with no LinkObjects in this case.
            link!.LinkObjects.Should().BeEmpty();
        }

        [Fact]
        public void LinkObject_Preserves_Optional_Attributes_On_Roundtrip()
        {
            var original = new Chatter.Rest.Hal.LinkObject("/orders/{id}")
            {
                Templated = true,
                Type = "application/json",
                Deprecation = "http://example.com/deprecated",
                Name = "order",
                Profile = "http://example.com/profile",
                Title = "Order",
                Hreflang = "en-US"
            };

            var json = JsonSerializer.Serialize(original);
            var round = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkObject>(json);

            round.Should().NotBeNull();
            round!.Href.Should().Be(original.Href);
            round.Templated.Should().Be(original.Templated);
            round.Type.Should().Be(original.Type);
            round.Deprecation.Should().Be(original.Deprecation);
            round.Name.Should().Be(original.Name);
            round.Profile.Should().Be(original.Profile);
            round.Title.Should().Be(original.Title);
            round.Hreflang.Should().Be(original.Hreflang);
        }
    }
}
