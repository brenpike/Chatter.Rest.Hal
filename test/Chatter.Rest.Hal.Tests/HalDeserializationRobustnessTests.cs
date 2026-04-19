using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Chatter.Rest.Hal.Tests
{
    public class HalDeserializationRobustnessTests
    {
        [Fact]
        public void Missing__links_Property_Produces_Empty_LinksCollection()
        {
            var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>("{}");

            Assert.NotNull(res);
            Assert.Empty(res!.Links);
        }

        [Fact]
        public void Invalid_LinkObject_Shape_Returns_Null_LinkObject()
        {
            var json = "{ \"_links\": { \"self\": { \"title\": \"NoHref\" } } }";
            var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(json);

            Assert.NotNull(res);
            Assert.Single(res!.Links);
            var l = res.Links.Single();

            Assert.Equal("self", l.Rel);
            Assert.Empty(l.LinkObjects);
        }

        [Fact]
        public void Extra_Random_Properties_Are_Preserved_In_State()
        {
            var json = "{ \"foo\": \"bar\" }";
            var res = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(json);

            Assert.NotNull(res);

            var node = res!.As<JsonNode>();
            Assert.NotNull(node);

            var foo = node["foo"]?.GetValue<string>();
            Assert.Equal("bar", foo);
        }
    }
}
