using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests
{
    public class HalLinkAttributesValidationTests
    {
        [Fact]
        public void NonBoolean_Templated_Value_Treated_As_False()
        {
            var json = "{ \"_links\": { \"self\": { \"href\": \"/x\", \"templated\": \"true\" } } }";
            var node = JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
            var resource = node.Deserialize<Chatter.Rest.Hal.Resource>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            resource.Should().NotBeNull();
            var link = resource!.Links.Single(l => l.Rel == "self");
            link.LinkObjects.Should().HaveCount(1);
            var lo = link.LinkObjects.Single();

            // Non-boolean templated values (e.g. the string "true") should NOT be treated as boolean true
            lo.Templated.Should().NotBeTrue();
        }

        [Fact]
        public void Href_Empty_String_Is_Invalid_On_Deserialization()
        {
            var json = "{ \"_links\": { \"self\": { \"href\": \"\" } } }";
            var node = JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
            var resource = node.Deserialize<Chatter.Rest.Hal.Resource>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            resource.Should().NotBeNull();
            var link = resource!.Links.Single(l => l.Rel == "self");

            // LinkObjectConverter treats an empty href as invalid and returns null, so the Link will have no LinkObjects
            link.LinkObjects.Should().BeEmpty();
        }

        [Fact]
        public void NonString_Optional_Attributes_Are_Treated_As_Null()
        {
            var json = "{ \"_links\": { \"self\": { \"href\": \"/x\", \"type\": 123, \"deprecation\": true } } }";
            var node = JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
            var resource = node.Deserialize<Chatter.Rest.Hal.Resource>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            resource.Should().NotBeNull();
            var link = resource!.Links.Single(l => l.Rel == "self");
            link.LinkObjects.Should().HaveCount(1);
            var lo = link.LinkObjects.Single();

            // Non-string optional attributes should be treated as null rather than throwing
            lo.Type.Should().BeNull();
            lo.Deprecation.Should().BeNull();
        }
    }
}
