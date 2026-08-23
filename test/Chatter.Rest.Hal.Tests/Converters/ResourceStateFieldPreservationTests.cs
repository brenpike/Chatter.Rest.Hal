using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Converters
{
	/// <summary>
	/// Covers issue #96: <see cref="Hal.Converters.ResourceConverter"/>'s write path must not drop state
	/// properties whose names happen to match the CLR property names <c>Links</c>/<c>Embedded</c> after the
	/// caller's naming policy is applied. HAL's reserved names are the literal strings <c>_links</c> and
	/// <c>_embedded</c> (draft-kelly-json-hal section 4.1); everything else in the state object is user data.
	/// </summary>
	public class ResourceStateFieldPreservationTests
	{
		private sealed class StateWithReservedLookalikes
		{
			public string Links { get; set; } = "some-value";
			public string Embedded { get; set; } = "x";
			public int Other { get; set; } = 1;
		}

		[Fact]
		public void State_Field_Named_Links_Survives_Serialization()
		{
			var resource = new Resource(new StateWithReservedLookalikes());
			resource.Links.Add(TestHelpers.CreateLink("self", "/orders/1"));

			var node = JsonNode.Parse(JsonSerializer.Serialize(resource))!.AsObject();

			node["Links"]!.GetValue<string>().Should().Be("some-value");
			node["Embedded"]!.GetValue<string>().Should().Be("x");
			node["Other"]!.GetValue<int>().Should().Be(1);
			node["_links"]!["self"]!["href"]!.GetValue<string>().Should().Be("/orders/1");
		}

		[Fact]
		public void CamelCased_State_Field_Named_links_Survives_Serialization()
		{
			// With a camelCase policy the old filter compared against "links"/"embedded", so a state field
			// literally named "links" was deleted.
			var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
			var resource = new Resource(new StateWithReservedLookalikes());
			resource.Links.Add(TestHelpers.CreateLink("self", "/orders/1"));

			var node = JsonNode.Parse(JsonSerializer.Serialize(resource, options))!.AsObject();

			node["links"]!.GetValue<string>().Should().Be("some-value");
			node["embedded"]!.GetValue<string>().Should().Be("x");
			node["other"]!.GetValue<int>().Should().Be(1);
			node["_links"]!["self"]!["href"]!.GetValue<string>().Should().Be("/orders/1");
		}

		[Fact]
		public void State_Fields_Named_Links_And_Embedded_Survive_A_Full_Round_Trip()
		{
			var json = "{\"Links\":\"some-value\",\"Embedded\":\"x\",\"other\":1,"
				+ "\"_links\":{\"self\":{\"href\":\"/orders/1\"}}}";

			var resource = Resource.Parse(json);
			var node = JsonNode.Parse(JsonSerializer.Serialize(resource))!.AsObject();

			node["Links"]!.GetValue<string>().Should().Be("some-value");
			node["Embedded"]!.GetValue<string>().Should().Be("x");
			node["other"]!.GetValue<int>().Should().Be(1);
			node["_links"]!["self"]!["href"]!.GetValue<string>().Should().Be("/orders/1");
		}

		[Fact]
		public void State_Field_Named_Links_Survives_When_The_Resource_Has_No_Links()
		{
			var resource = new Resource(new StateWithReservedLookalikes());

			var node = JsonNode.Parse(JsonSerializer.Serialize(resource))!.AsObject();

			node["Links"]!.GetValue<string>().Should().Be("some-value");
			node["Embedded"]!.GetValue<string>().Should().Be("x");
			node.ContainsKey("_links").Should().BeFalse();
		}

		private sealed class StateCarryingReservedNames
		{
			[JsonPropertyName("_links")]
			public string ReservedLinks { get; set; } = "state-links";

			[JsonPropertyName("_embedded")]
			public string ReservedEmbedded { get; set; } = "state-embedded";
		}

		[Fact]
		public void State_Property_Under_A_Reserved_Name_Is_Written_When_The_Resource_Has_None()
		{
			// Deserialization strips _links/_embedded from the state, so this can only arise from a
			// hand-built state object. With nothing to collide with, the state property is user data.
			var resource = new Resource(new StateCarryingReservedNames());

			var node = JsonNode.Parse(JsonSerializer.Serialize(resource))!.AsObject();

			node["_links"]!.GetValue<string>().Should().Be("state-links");
			node["_embedded"]!.GetValue<string>().Should().Be("state-embedded");
		}

		[Fact]
		public void Resource_Collections_Win_Over_State_Properties_Under_The_Same_Reserved_Name()
		{
			// Writing both would emit the member twice, which RFC 8259 leaves undefined.
			var resource = new Resource(new StateCarryingReservedNames());
			resource.Links.Add(TestHelpers.CreateLink("self", "/orders/1"));
			var embedded = new EmbeddedResource("ea:order");
			embedded.Resources.Add(new Resource(new { total = 30 }));
			resource.Embedded.Add(embedded);

			var json = JsonSerializer.Serialize(resource);
			var node = JsonNode.Parse(json)!.AsObject();

			node["_links"]!["self"]!["href"]!.GetValue<string>().Should().Be("/orders/1");
			node["_embedded"]!["ea:order"]!["total"]!.GetValue<int>().Should().Be(30);
			(json.Split("\"_links\"").Length - 1).Should().Be(1);
			(json.Split("\"_embedded\"").Length - 1).Should().Be(1);
		}
	}
}
