using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
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

		[Fact]
		public void A_Custom_ResourceCollection_Converter_Wins_Over_The_Built_In_Shape_Rule_Standalone()
		{
			// An options-registered converter takes precedence over the built-in one, so it decides the
			// shape of the relation's value even when ForceWriteAsCollection is set.
			var options = new JsonSerializerOptions();
			options.Converters.Add(new StubResourceCollectionConverter());
			var embedded = new EmbeddedResource("orders") { ForceWriteAsCollection = true };
			embedded.Resources.Add(new Resource(new { id = 1 }));

			JsonSerializer.Serialize(embedded, options).Should().Be("{\"orders\":\"stub\"}");
		}

		[Fact]
		public void A_Custom_ResourceCollection_Converter_Wins_Over_The_Built_In_Shape_Rule_In_A_Collection()
		{
			var options = new JsonSerializerOptions();
			options.Converters.Add(new StubResourceCollectionConverter());
			var embedded = new EmbeddedResource("orders") { ForceWriteAsCollection = true };
			embedded.Resources.Add(new Resource(new { id = 1 }));
			var resource = new Resource(new { });
			resource.Embedded.Add(embedded);

			var node = JsonNode.Parse(JsonSerializer.Serialize(resource, options))!.AsObject();

			node["_embedded"]!["orders"]!.GetValue<string>().Should().Be("stub");
		}

		[Fact]
		public void Empty_Href_On_A_Link_Object_Round_Trips_As_An_Empty_String()
		{
			// Covers issue #120: the empty string is a valid RFC 3986 section 4.4 same-document reference and
			// HAL section 5.1 defines href by reference to RFC 3986, so href must survive both directions
			// instead of being dropped by the writer.
			var json = "{\"_links\":{\"self\":{\"href\":\"\"}}}";

			var resource = Resource.Parse(json);
			var node = JsonNode.Parse(JsonSerializer.Serialize(resource))!.AsObject();

			node["_links"]!["self"]!["href"]!.GetValue<string>().Should().BeEmpty();
		}

		[Fact]
		public void Empty_Href_Inside_A_Link_Array_Round_Trips_As_An_Empty_String()
		{
			// The array shape reaches the same read path through LinkObjectCollectionConverter, so it is pinned
			// separately from the single Link Object case.
			var json = "{\"_links\":{\"self\":[{\"href\":\"\"}]}}";

			var resource = Resource.Parse(json);
			var node = JsonNode.Parse(JsonSerializer.Serialize(resource))!.AsObject();

			node["_links"]!["self"]!.AsArray().Should().HaveCount(1);
			node["_links"]!["self"]![0]!["href"]!.GetValue<string>().Should().BeEmpty();
		}

		[Fact]
		public void Empty_Href_Round_Trips_Alongside_Its_Optional_Attributes()
		{
			// The same-document-reference branch and the normal branch share one optional-attribute helper, so
			// an optional sibling must survive the empty-href branch too.
			var json = "{\"_links\":{\"self\":{\"href\":\"\",\"title\":\"here\"}}}";

			var resource = Resource.Parse(json);
			var node = JsonNode.Parse(JsonSerializer.Serialize(resource))!.AsObject();

			node["_links"]!["self"]!["href"]!.GetValue<string>().Should().BeEmpty();
			node["_links"]!["self"]!["title"]!.GetValue<string>().Should().Be("here");
		}

		private sealed class StubResourceCollectionConverter : JsonConverter<ResourceCollection>
		{
			public override ResourceCollection? Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
			{
				reader.Skip();
				return new ResourceCollection();
			}

			public override void Write(Utf8JsonWriter writer, ResourceCollection value, JsonSerializerOptions options)
				=> writer.WriteStringValue("stub");
		}
	}
}
