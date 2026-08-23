using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Converters
{
	/// <summary>
	/// Covers issue #97: HAL clients read the array-vs-object shape of a relation as the signal that it is
	/// a collection (draft-kelly-json-hal section 4.1.2), so a single-element array must not collapse to an
	/// object on round trip, and an embedded resource must serialize to the same shape standalone as it
	/// does inside an <c>_embedded</c> collection.
	/// </summary>
	public class HalRoundTripShapeTests
	{
		[Fact]
		public void Single_Element_Embedded_Array_Stays_An_Array()
		{
			var json = "{\"_embedded\":{\"orders\":[{\"id\":1}]}}";

			var resource = Resource.Parse(json);
			var node = JsonNode.Parse(JsonSerializer.Serialize(resource))!.AsObject();

			node["_embedded"]!["orders"].Should().BeOfType<JsonArray>();
			node["_embedded"]!["orders"]!.AsArray().Should().HaveCount(1);
			node["_embedded"]!["orders"]![0]!["id"]!.GetValue<int>().Should().Be(1);
		}

		[Fact]
		public void Single_Embedded_Object_Stays_An_Object()
		{
			var json = "{\"_embedded\":{\"orders\":{\"id\":1}}}";

			var resource = Resource.Parse(json);
			var node = JsonNode.Parse(JsonSerializer.Serialize(resource))!.AsObject();

			node["_embedded"]!["orders"].Should().BeOfType<JsonObject>();
			node["_embedded"]!["orders"]!["id"]!.GetValue<int>().Should().Be(1);
		}

		[Fact]
		public void Multi_Element_Embedded_Array_Stays_An_Array()
		{
			var json = "{\"_embedded\":{\"orders\":[{\"id\":1},{\"id\":2}]}}";

			var resource = Resource.Parse(json);
			var node = JsonNode.Parse(JsonSerializer.Serialize(resource))!.AsObject();

			node["_embedded"]!["orders"]!.AsArray().Should().HaveCount(2);
		}

		[Fact]
		public void Nested_Single_Element_Embedded_Array_Stays_An_Array()
		{
			var json = "{\"_embedded\":{\"orders\":[{\"_embedded\":{\"items\":[{\"sku\":\"a\"}]}}]}}";

			var resource = Resource.Parse(json);
			var node = JsonNode.Parse(JsonSerializer.Serialize(resource))!.AsObject();

			node["_embedded"]!["orders"]!.AsArray().Should().HaveCount(1);
			node["_embedded"]!["orders"]![0]!["_embedded"]!["items"]!.AsArray().Should().HaveCount(1);
		}

		[Fact]
		public void Reading_An_Embedded_Array_Sets_ForceWriteAsCollection()
		{
			var resource = Resource.Parse("{\"_embedded\":{\"orders\":[{\"id\":1}]}}");

			resource!.Embedded.GetEmbeddedResource("orders")!.ForceWriteAsCollection.Should().BeTrue();
		}

		[Fact]
		public void Reading_A_Single_Embedded_Object_Leaves_ForceWriteAsCollection_False()
		{
			var resource = Resource.Parse("{\"_embedded\":{\"orders\":{\"id\":1}}}");

			resource!.Embedded.GetEmbeddedResource("orders")!.ForceWriteAsCollection.Should().BeFalse();
		}

		[Fact]
		public void Standalone_EmbeddedResource_Honors_ForceWriteAsCollection()
		{
			var embedded = new EmbeddedResource("orders") { ForceWriteAsCollection = true };
			embedded.Resources.Add(new Resource(new { id = 1 }));

			JsonSerializer.Serialize(embedded).Should().Be("{\"orders\":[{\"id\":1}]}");
		}

		[Fact]
		public void Standalone_EmbeddedResource_Writes_Single_Resource_As_Object_By_Default()
		{
			var embedded = new EmbeddedResource("orders");
			embedded.Resources.Add(new Resource(new { id = 1 }));

			JsonSerializer.Serialize(embedded).Should().Be("{\"orders\":{\"id\":1}}");
		}

		[Fact]
		public void EmbeddedResource_Shape_Is_The_Same_Standalone_As_Inside_A_Collection()
		{
			var embedded = new EmbeddedResource("orders") { ForceWriteAsCollection = true };
			embedded.Resources.Add(new Resource(new { id = 1 }));

			var standalone = JsonNode.Parse(JsonSerializer.Serialize(embedded))!["orders"];

			var resource = new Resource(new { });
			resource.Embedded.Add(embedded);
			var inCollection = JsonNode.Parse(JsonSerializer.Serialize(resource))!["_embedded"]!["orders"];

			standalone!.ToJsonString().Should().Be(inCollection!.ToJsonString());
		}

		[Fact]
		public void Standalone_EmbeddedResource_Read_From_An_Array_Round_Trips_As_An_Array()
		{
			var embedded = JsonSerializer.Deserialize<EmbeddedResource>("{\"orders\":[{\"id\":1}]}");

			embedded!.ForceWriteAsCollection.Should().BeTrue();
			JsonSerializer.Serialize(embedded).Should().Be("{\"orders\":[{\"id\":1}]}");
		}

		[Fact]
		public void Single_Element_Link_Array_Stays_An_Array()
		{
			// Links already preserve their shape via Link.IsArray; pinned here alongside the embedded case.
			var json = "{\"_links\":{\"self\":[{\"href\":\"/orders/1\"}]}}";

			var resource = Resource.Parse(json);
			var node = JsonNode.Parse(JsonSerializer.Serialize(resource))!.AsObject();

			node["_links"]!["self"]!.AsArray().Should().HaveCount(1);
		}

		[Theory]
		[InlineData("{\"_links\":{\"self\":null}}")]
		[InlineData("{\"_links\":{\"self\":{}}}")]
		public void A_Relation_With_No_Link_Objects_Writes_As_An_Empty_Array(string json)
		{
			// Pinned decision: HAL permits only a Link Object or an array of Link Objects as a relation's
			// value, so an empty relation can only be written as [] — never as null, and never dropped.
			var resource = Resource.Parse(json);
			var node = JsonNode.Parse(JsonSerializer.Serialize(resource))!.AsObject();

			node["_links"]!["self"].Should().BeOfType<JsonArray>();
			node["_links"]!["self"]!.AsArray().Should().BeEmpty();
		}

		[Fact]
		public void An_Empty_Relation_Is_Stable_Across_A_Second_Round_Trip()
		{
			var once = JsonSerializer.Serialize(Resource.Parse("{\"_links\":{\"self\":null}}"));
			var twice = JsonSerializer.Serialize(Resource.Parse(once));

			twice.Should().Be(once);
		}
	}
}
