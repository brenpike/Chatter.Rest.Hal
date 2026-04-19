using System.Text.Json;
using System.Linq;
using Xunit;

namespace Chatter.Rest.Hal.Tests
{
    public class HalLinksCollectionTests
    {
        [Fact]
        public void Single_LinkObject_Serializes_As_Object_Not_Array()
        {
            var res = TestHelpers.CreateResourceWithLink("self", "/items/1");

            var json = JsonSerializer.Serialize(res);

            // Expect _links.self to be an object, not an array
            Assert.Contains("\"_links\":{", json);
            Assert.Contains("\"self\":{", json);
            Assert.Contains("\"href\":\"/items/1\"", json);
        }

        [Fact]
        public void Multiple_LinkObjects_Serializes_As_Array()
        {
            var res = new Chatter.Rest.Hal.Resource();
            var link = new Chatter.Rest.Hal.Link("self");
            link.LinkObjects.Add(TestHelpers.CreateLinkObject("/items/1"));
            link.LinkObjects.Add(TestHelpers.CreateLinkObject("/items/2"));
            res.Links.Add(link);

            var json = JsonSerializer.Serialize(res);

            // Expect _links.self to be an array when multiple link objects exist
            Assert.Contains("\"_links\":{", json);
            Assert.Contains("\"self\":[", json);
            Assert.Contains("/items/1", json);
            Assert.Contains("/items/2", json);
        }

        [Fact]
        public void String_Shorthand_Deserializes_To_LinkObject()
        {
            var json = "{ \"_links\": { \"self\": \"/x\" } }";
            var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(json);

            Assert.NotNull(res);
            Assert.Single(res!.Links);
            var l = res.Links.Single();

            // Expect shorthand string to be interpreted as a single LinkObject with href == "/x"
            Assert.Single(l.LinkObjects);
            Assert.Equal("/x", l.LinkObjects.Single().Href);
        }

        [Fact]
        public void StringArray_Shorthand_Deserializes_To_Multiple_LinkObjects()
        {
            var json = "{ \"_links\": { \"friends\": [\"/f/1\",\"/f/2\"] } }";
            var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(json);

            Assert.NotNull(res);
            var friends = res!.Links.Single(l => l.Rel == "friends");

            Assert.Equal(2, friends.LinkObjects.Count);
            Assert.Equal("/f/1", friends.LinkObjects.ElementAt(0).Href);
            Assert.Equal("/f/2", friends.LinkObjects.ElementAt(1).Href);
        }

        [Fact]
        public void MixedArray_Shorthand_And_Object_Deserializes_To_LinkObjects()
        {
            var json = "{ \"_links\": { \"items\": [\"/a\", { \"href\": \"/b\", \"title\": \"B\" }] } }";
            var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(json);

            Assert.NotNull(res);
            var items = res!.Links.Single(l => l.Rel == "items");

            Assert.Equal(2, items.LinkObjects.Count);
            Assert.Equal("/a", items.LinkObjects.ElementAt(0).Href);
            Assert.Equal("/b", items.LinkObjects.ElementAt(1).Href);
            Assert.Equal("B", items.LinkObjects.ElementAt(1).Title);
        }

        [Fact]
        public void Reading_Link_With_Null_Value_Produces_Link_With_No_LinkObjects()
        {
            var json = "{ \"_links\": { \"self\": null } }";
            var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(json);

            Assert.NotNull(res);
            Assert.Single(res!.Links);
            var l = res.Links.Single();
            Assert.Equal("self", l.Rel);
            Assert.Empty(l.LinkObjects);
        }
    }
}
