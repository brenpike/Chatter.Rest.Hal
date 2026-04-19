using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests
{
    public class HalSerializationRoundTripTests
    {
        [Fact]
        public void Resource_RoundTrip_Preserves_Links_And_Embedded_And_State()
        {
            // Arrange: create a resource with simple state, a link and an embedded resource
            var state = new { id = 123, name = "bob" };
            var resource = new Chatter.Rest.Hal.Resource(state);

            // add a link
            resource.Links.Add(TestHelpers.CreateLink("self", "/orders/123"));

            // add an embedded resource containing a nested Resource with state
            var embedded = new Chatter.Rest.Hal.EmbeddedResource("child");
            embedded.Resources.Add(new Chatter.Rest.Hal.Resource(new { childId = 2, title = "child resource" }));
            resource.Embedded.Add(embedded);

            // Act: serialize and deserialize
            var json = JsonSerializer.Serialize(resource);
            var deserialized = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(json);

            // Assert: links and embedded preserved
            Assert.NotNull(deserialized);
            Assert.Equal(resource.Links.Count, deserialized!.Links.Count);
            Assert.Equal(resource.Embedded.Count, deserialized.Embedded.Count);

            // state round-tripped
            var deserializedState = deserialized.State<JsonObject>();
            Assert.NotNull(deserializedState);
            Assert.Equal(123, deserializedState!["id"]!.GetValue<int>());
            Assert.Equal("bob", deserializedState["name"]!.GetValue<string>());
        }

        [Fact]
        public void Resource_State_Does_Not_Expose__links_or__embedded_In_State()
        {
            // Arrange: create a state object that itself contains _links and _embedded keys
            var state = new JsonObject
            {
                ["_links"] = new JsonObject
                {
                    ["self"] = new JsonObject { ["href"] = "http://example.com/inner" }
                },
                ["_embedded"] = new JsonObject
                {
                    ["inner"] = new JsonObject { ["foo"] = "bar" }
                },
                ["value"] = "ok"
            };

            var resource = new Chatter.Rest.Hal.Resource(state);

            // Act: serialize
            var json = JsonSerializer.Serialize(resource);

            // Parse the serialized JSON
            var node = JsonNode.Parse(json)!.AsObject();

            // The special properties should be represented at the resource top-level
            Assert.True(node.ContainsKey("_links"));
            Assert.True(node.ContainsKey("_embedded"));

            // And the original state's other properties should still be present
            Assert.True(node.ContainsKey("value"));
            Assert.Equal("ok", node["value"]!.GetValue<string>());

            // Ensure there are no extra nested occurrences of the special property names
            // (i.e. they were promoted to top-level and not left embedded inside some state wrapper)
            // We verify this by checking there is exactly one occurrence of the property name in the JSON text.
            var linksOccurrences = json.Split("\"_links\"").Length - 1;
            var embeddedOccurrences = json.Split("\"_embedded\"").Length - 1;

            Assert.Equal(1, linksOccurrences);
            Assert.Equal(1, embeddedOccurrences);
        }

        [Fact]
        public void Resource_With_Self_Link_Serializes_Self_Relation()
        {
            // HAL spec: each Resource Object SHOULD contain a 'self' link
            // whose value is the resource's URI. This test verifies the
            // self link is preserved through serialization round-trip.

            // Arrange: create a resource with a self link
            var state = new { id = 456, name = "order" };
            var resource = new Chatter.Rest.Hal.Resource(state);
            resource.Links.Add(TestHelpers.CreateLink("self", "/orders/456"));

            // Act: serialize and deserialize
            var json = JsonSerializer.Serialize(resource);
            var deserialized = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(json);

            // Assert: self link is preserved with correct href
            deserialized.Should().NotBeNull();
            var selfLink = deserialized!.Links.GetLinkOrDefault("self");
            selfLink.Should().NotBeNull();
            selfLink!.LinkObjects.Should().HaveCount(1);

            var selfLinkObject = selfLink.LinkObjects.First();
            selfLinkObject.Href.Should().Be("/orders/456");
        }

        [Fact]
        public void Resource_Self_Link_Is_Accessible_Via_Extension()
        {
            // HAL spec: each Resource Object SHOULD contain a 'self' link
            // whose value is the resource's URI. This test verifies the
            // GetLinkObjectOrDefault extension method correctly retrieves
            // the self link relation.

            // Arrange: create a resource with a self link
            var state = new { id = 789, type = "product" };
            var resource = new Chatter.Rest.Hal.Resource(state);
            resource.Links.Add(TestHelpers.CreateLink("self", "/products/789"));

            // Act: retrieve the self link using extension method
            var selfLinkObject = resource.GetLinkObjectOrDefault("self");

            // Assert: self link is accessible and has correct href
            selfLinkObject.Should().NotBeNull();
            selfLinkObject!.Href.Should().Be("/products/789");
        }

        [Fact]
        public void Resource_Without_Self_Link_Returns_Null_Via_Extension()
        {
            // Edge case: when a resource has no self link, the extension
            // method should return null rather than throwing an exception.

            // Arrange: create a resource without a self link
            var state = new { id = 999, type = "item" };
            var resource = new Chatter.Rest.Hal.Resource(state);

            // Act: attempt to retrieve the self link using extension method
            var selfLinkObject = resource.GetLinkObjectOrDefault("self");

            // Assert: extension method returns null for missing self link
            selfLinkObject.Should().BeNull();
        }
    }
}
