using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal.Converters;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Converters
{
	/// <summary>
	/// Covers issue #98: write/registration cleanups — primitive resource state, the per-converter
	/// registration guard in <see cref="JsonSerializerOptionsExtensions.AddHalConverters"/>, the
	/// multi-relation drop in <see cref="EmbeddedResourceConverter"/>, and the node fast paths that
	/// replace re-serializing each retained subtree.
	/// </summary>
	public class ConverterWriteAndRegistrationCleanupTests
	{
		[Theory]
		[InlineData("hello")]
		[InlineData(42)]
		[InlineData(true)]
		[InlineData(1.5)]
		public void Primitive_Resource_State_Throws_JsonException_On_Write(object state)
		{
			Action act = () => JsonSerializer.Serialize(new Resource(state));

			act.Should().Throw<JsonException>()
				.WithMessage("*must serialize to a JSON object*");
		}

		[Fact]
		public void Array_Resource_State_Throws_JsonException_On_Write()
		{
			Action act = () => JsonSerializer.Serialize(new Resource(new[] { 1, 2, 3 }));

			act.Should().Throw<JsonException>()
				.WithMessage("*must serialize to a JSON object*");
		}

		[Fact]
		public void Primitive_State_Inside_An_Embedded_Resource_Also_Throws_JsonException()
		{
			var resource = new Resource(new { id = 1 });
			var embedded = new EmbeddedResource("orders");
			embedded.Resources.Add(new Resource("hello"));
			resource.Embedded.Add(embedded);

			Action act = () => JsonSerializer.Serialize(resource);

			act.Should().Throw<JsonException>();
		}

		[Fact]
		public void Object_Resource_State_Still_Writes()
		{
			JsonSerializer.Serialize(new Resource(new { id = 1 })).Should().Be("{\"id\":1}");
		}

		[Fact]
		public void Null_Resource_State_Writes_An_Empty_Object()
		{
			JsonSerializer.Serialize(new Resource(null)).Should().Be("{}");
		}

		[Fact]
		public void AddHalConverters_Registers_Every_Converter()
		{
			var options = new JsonSerializerOptions().AddHalConverters();

			AssertAllHalConvertersPresent(options);
		}

		[Fact]
		public void AddHalConverters_Fills_In_The_Rest_When_One_Converter_Was_Registered_By_Hand()
		{
			// The old guard checked only for LinkCollectionConverter, so this silently no-opped and the
			// remaining seven converters were never registered.
			var options = new JsonSerializerOptions();
			options.Converters.Add(new LinkCollectionConverter());

			options.AddHalConverters();

			AssertAllHalConvertersPresent(options);
		}

		[Fact]
		public void AddHalConverters_Is_Idempotent()
		{
			var options = new JsonSerializerOptions().AddHalConverters().AddHalConverters();

			options.Converters.Should().HaveCount(8);
			AssertAllHalConvertersPresent(options);
		}

		[Fact]
		public void AddHalConverters_Applies_HalJsonOptions_To_The_Converters_It_Adds()
		{
			var options = new JsonSerializerOptions().AddHalConverters(new HalJsonOptions { AlwaysUseArrayForLinks = true });
			var resource = Resource.Parse("{\"_links\":{\"self\":{\"href\":\"/orders/1\"}}}");

			var node = JsonNode.Parse(JsonSerializer.Serialize(resource, options))!.AsObject();

			node["_links"]!["self"]!.AsArray().Should().HaveCount(1);
		}

		[Fact]
		public void AddHalConverters_Leaves_A_Consumers_Own_Converter_In_Front()
		{
			var options = new JsonSerializerOptions();
			options.Converters.Add(new StubResourceConverter());

			options.AddHalConverters();

			options.Converters.Should().HaveCount(9);
			JsonSerializer.Serialize(new Resource(new { id = 1 }), options).Should().Be("\"stub\"");
		}

		private sealed class StubResourceConverter : JsonConverter<Resource>
		{
			public override Resource? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				=> new Resource();

			public override void Write(Utf8JsonWriter writer, Resource value, JsonSerializerOptions options)
				=> writer.WriteStringValue("stub");
		}

		private static void AssertAllHalConvertersPresent(JsonSerializerOptions options)
		{
			var types = new[]
			{
				typeof(LinkCollectionConverter),
				typeof(LinkObjectCollectionConverter),
				typeof(LinkConverter),
				typeof(LinkObjectConverter),
				typeof(ResourceConverter),
				typeof(EmbeddedResourceCollectionConverter),
				typeof(EmbeddedResourceConverter),
				typeof(ResourceCollectionConverter)
			};

			foreach (var type in types)
			{
				options.Converters.Count(c => c.GetType() == type).Should().Be(1, "exactly one {0} should be registered", type.Name);
			}
		}

		[Fact]
		public void Standalone_EmbeddedResource_Rejects_A_Multi_Relation_Object()
		{
			// A single EmbeddedResource holds one name, so the extra relations had nowhere to go and were
			// silently dropped.
			Action act = () => JsonSerializer.Deserialize<EmbeddedResource>("{\"orders\":{\"id\":1},\"customers\":{\"id\":2}}");

			act.Should().Throw<JsonException>()
				.WithMessage("*2 relation names*");
		}

		[Fact]
		public void Standalone_EmbeddedResource_Still_Accepts_A_Single_Relation_Object()
		{
			var embedded = JsonSerializer.Deserialize<EmbeddedResource>("{\"orders\":{\"id\":1}}");

			embedded!.Name.Should().Be("orders");
			embedded.Resources.Should().HaveCount(1);
		}

		[Fact]
		public void EmbeddedResourceCollection_Still_Reads_Every_Relation()
		{
			var resource = Resource.Parse("{\"_embedded\":{\"orders\":{\"id\":1},\"customers\":{\"id\":2}}}");

			resource!.Embedded.Select(e => e.Name).Should().BeEquivalentTo(new[] { "orders", "customers" });
		}

		[Fact]
		public void Nested_Documents_Materialize_From_The_Retained_Node_Tree()
		{
			// The node fast paths replace a per-level re-serialize/re-parse of every subtree; the values
			// they produce must be identical to what the round-tripping path produced.
			var json = "{\"id\":0,\"_links\":{\"self\":{\"href\":\"/a\"},\"next\":[{\"href\":\"/b\",\"templated\":true}]},"
				+ "\"_embedded\":{\"orders\":[{\"id\":1,\"_embedded\":{\"items\":[{\"sku\":\"a\",\"_links\":{\"self\":{\"href\":\"/i\"}}}]}}]}}";

			var resource = Resource.Parse(json);

			resource!.GetLinkObjectOrDefault("self")!.Href.Should().Be("/a");
			var next = resource.GetLinkObjects("next")!;
			next.Should().HaveCount(1);
			next[0].Href.Should().Be("/b");
			next[0].Templated.Should().BeTrue();

			var order = resource.Embedded.GetResourceCollection("orders")![0];
			var item = order.Embedded.GetResourceCollection("items")![0];
			item.GetLinkObjectOrDefault("self")!.Href.Should().Be("/i");
			item.State<JsonObject>()!["sku"]!.GetValue<string>().Should().Be("a");
		}

		[Fact]
		public void A_Custom_LinkObject_Converter_Still_Takes_Precedence_Over_The_Node_Fast_Path()
		{
			var options = new JsonSerializerOptions();
			options.Converters.Add(new StubLinkObjectConverter());

			var resource = Resource.Parse("{\"_links\":{\"self\":{\"href\":\"/a\"},\"next\":[{\"href\":\"/b\"}]}}", options);

			resource!.GetLinkObjectOrDefault("self")!.Href.Should().Be("/stub");
			resource.GetLinkObjects("next")![0].Href.Should().Be("/stub");
		}

		private sealed class StubLinkObjectConverter : JsonConverter<LinkObject>
		{
			public override LinkObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				reader.Skip();
				return new LinkObject("/stub");
			}

			public override void Write(Utf8JsonWriter writer, LinkObject value, JsonSerializerOptions options)
				=> throw new NotSupportedException();
		}
	}
}
