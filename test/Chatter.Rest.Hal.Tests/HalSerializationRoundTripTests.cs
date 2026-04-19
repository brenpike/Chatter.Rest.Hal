using System.Text.Json;
using System.Text.Json.Nodes;
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
    }
}
