using System.Linq;
using System.Text.Json;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Converters
{
    public class LinkConvertersTests
    {
        [Fact]
        public void Deserialize_Single_Link_Object_Should_Parse()
        {
            var json = "{\"self\": { \"href\": \"/orders/123\" } }";
            var links = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkCollection>(json);
		    Assert.NotNull(links);
		    Assert.Single(links);
		    var link = links!.First();
		    Assert.Equal("self", link.Rel);
		    Assert.Single(link.LinkObjects);
		    Assert.Equal("/orders/123", link.LinkObjects.First().Href);
        }
		[Fact]
		public void Deserialize_Link_As_Array_Should_Parse_Multiple()
		{
			var json = "{\"friends\": [{ \"href\": \"/users/1\" }, { \"href\": \"/users/2\" }] }";
			var links = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkCollection>(json);
			Assert.NotNull(links);
			Assert.Single(links);
			var link = links!.First();
			Assert.Equal("friends", link.Rel);
			Assert.Equal(2, link.LinkObjects.Count);
			Assert.Equal("/users/1", link.LinkObjects.First().Href);
		}
		[Fact]
		public void Deserialize_Null_Link_Value_Should_Create_Empty_LinkObjects()
		{
			var json = "{\"next\": null }";
			var links = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkCollection>(json);
			Assert.NotNull(links);
			Assert.Single(links);
			var link = links!.First();
			Assert.Equal("next", link.Rel);
			Assert.Empty(link.LinkObjects);
		}
		[Fact]
		public void Deserialize_Should_Skip_Invalid_Rel_Names()
		{
			var json = "{\"\" : { \"href\": \"/bad\" }, \"valid\": { \"href\": \"/good\" } }";
			var links = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkCollection>(json);
			Assert.NotNull(links);
			// should skip the empty key and only include the valid rel
			Assert.Single(links);
			Assert.Equal("valid", links!.First().Rel);
		}
		[Fact]
		public void RoundTrip_Serialization_LinkCollection_Preserves_Data()
		{
			var loc = new Chatter.Rest.Hal.LinkObjectCollection
			{				new Chatter.Rest.Hal.LinkObject("/a/1"),
					new Chatter.Rest.Hal.LinkObject("/a/2")
			};
			var links = new Chatter.Rest.Hal.LinkCollection()
			{				new Chatter.Rest.Hal.Link("self") { LinkObjects = loc },
				new Chatter.Rest.Hal.Link("next")
			};
			var serial = JsonSerializer.Serialize(links);
			var deserial = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkCollection>(serial);
			Assert.NotNull(deserial);
			Assert.Equal(2, deserial.Count);
			var s = deserial.First(l => l.Rel == "self");
			Assert.Equal(2, s.LinkObjects.Count);
			Assert.Equal("/a/1", s.LinkObjects.First().Href);
		}	}
}