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
