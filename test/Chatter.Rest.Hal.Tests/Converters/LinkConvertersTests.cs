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
			{				new Chatter.Rest.Hal.LinkObject("/a/1"),
					new Chatter.Rest.Hal.LinkObject("/a/2")
			};
			var links = new Chatter.Rest.Hal.LinkCollection()
			{				new Chatter.Rest.Hal.Link("self") { LinkObjects = loc },
				new Chatter.Rest.Hal.Link("next")
			};
			var serial = JsonSerializer.Serialize(links);
			var deserial = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkCollection>(serial);
			Assert.NotNull(deserial);
			Assert.Equal(2, deserial.Count);
			var s = deserial.First(l => l.Rel == "self");
			Assert.Equal(2, s.LinkObjects.Count);
			Assert.Equal("/a/1", s.LinkObjects.First().Href);
		}

		[Fact]
		public void Serialize_Link_WithIsArrayTrue_SingleLinkObject_ProducesJsonArray()
		{
			// A link with IsArray=true and one link object must serialize as a JSON array,
			// preserving a stable response shape regardless of count.
			var link = new Chatter.Rest.Hal.Link("orders") { IsArray = true };
			link.LinkObjects.Add(new Chatter.Rest.Hal.LinkObject("/orders/1"));

			var links = new Chatter.Rest.Hal.LinkCollection { link };
			var json = JsonSerializer.Serialize(links);
			var doc = JsonDocument.Parse(json);

			var ordersNode = doc.RootElement.GetProperty("orders");
			Assert.Equal(JsonValueKind.Array, ordersNode.ValueKind);
			Assert.Equal(1, ordersNode.GetArrayLength());
			Assert.Equal("/orders/1", ordersNode[0].GetProperty("href").GetString());
		}

		[Fact]
		public void Serialize_Link_WithIsArrayFalse_SingleLinkObject_ProducesSingleObject()
		{
			// Default (IsArray=false) behavior: a single link object serializes as a plain object,
			// not an array.
			var link = new Chatter.Rest.Hal.Link("self");
			link.LinkObjects.Add(new Chatter.Rest.Hal.LinkObject("/orders/1"));

			var links = new Chatter.Rest.Hal.LinkCollection { link };
			var json = JsonSerializer.Serialize(links);
			var doc = JsonDocument.Parse(json);

			var selfNode = doc.RootElement.GetProperty("self");
			Assert.Equal(JsonValueKind.Object, selfNode.ValueKind);
			Assert.Equal("/orders/1", selfNode.GetProperty("href").GetString());
		}

		[Fact]
		public void Serialize_Link_WithIsArrayTrue_ZeroLinkObjects_ProducesEmptyArray()
		{
			// IsArray=true with zero link objects must produce [] — an empty JSON array.
			var link = new Chatter.Rest.Hal.Link("orders") { IsArray = true };

			var links = new Chatter.Rest.Hal.LinkCollection { link };
			var json = JsonSerializer.Serialize(links);
			var doc = JsonDocument.Parse(json);

			var ordersNode = doc.RootElement.GetProperty("orders");
			Assert.Equal(JsonValueKind.Array, ordersNode.ValueKind);
			Assert.Equal(0, ordersNode.GetArrayLength());
		}

		[Fact]
		public void Deserialize_LinkRelationAsJsonArray_SetsIsArrayTrue_OnLink()
		{
			// When the JSON represents a relation as an array, the deserialized Link must have
			// IsArray=true so round-trip serialization can reproduce the array form.
			var json = "{\"self\":[{\"href\":\"/orders/1\"}]}";
			var links = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkCollection>(json);

			Assert.NotNull(links);
			var link = links!.First(l => l.Rel == "self");
			Assert.True(link.IsArray);
			Assert.Single(link.LinkObjects);
			Assert.Equal("/orders/1", link.LinkObjects.First().Href);
		}

		[Fact]
		public void Deserialize_LinkRelationAsSingleObject_IsArrayDefaultsFalse()
		{
			// When the JSON represents a relation as a plain object, the deserialized Link
			// must have IsArray=false (the default).
			var json = "{\"self\":{\"href\":\"/orders/1\"}}";
			var links = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkCollection>(json);

			Assert.NotNull(links);
			var link = links!.First(l => l.Rel == "self");
			Assert.False(link.IsArray);
			Assert.Single(link.LinkObjects);
			Assert.Equal("/orders/1", link.LinkObjects.First().Href);
		}
	}
}
