using System;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Converters
{
	/// <summary>
	/// Covers issue #94 and the malformed-input exception contract recorded in #86: malformed or
	/// structurally unexpected HAL JSON always surfaces as <see cref="JsonException"/>, never as
	/// <see cref="InvalidOperationException"/> or <see cref="ArgumentException"/>.
	/// </summary>
	public class HalMalformedInputExceptionContractTests
	{
		/// <summary>
		/// Deserializes and touches the lazily materialized collections, so the assertion holds
		/// wherever the converter chain decides to surface the failure.
		/// </summary>
		private static Action Deserializing(string json) => () =>
		{
			var resource = Resource.Parse(json);
			_ = resource?.Links;
			_ = resource?.Embedded;
		};

		// #94.1 — LinkObjectConverter: a non-string href previously threw InvalidOperationException.

		[Theory]
		[InlineData("{\"_links\":{\"self\":{\"href\":123}}}")]
		[InlineData("{\"_links\":{\"self\":{\"href\":true}}}")]
		[InlineData("{\"_links\":{\"self\":{\"href\":{}}}}")]
		[InlineData("{\"_links\":{\"self\":{\"href\":[]}}}")]
		public void NonString_Href_Throws_JsonException(string json)
		{
			Deserializing(json).Should().Throw<JsonException>();
		}

		[Theory]
		[InlineData("{\"href\":123}")]
		[InlineData("{\"href\":{}}")]
		[InlineData("{\"href\":[]}")]
		public void NonString_Href_Throws_JsonException_When_Deserializing_A_LinkObject_Directly(string json)
		{
			Action act = () => JsonSerializer.Deserialize<LinkObject>(json);

			act.Should().Throw<JsonException>();
		}

		// #94.2 — LinkObjectConverter: a non-object Link Object previously threw InvalidOperationException.

		[Theory]
		[InlineData("{\"_links\":{\"self\":[[]]}}")]
		[InlineData("{\"_links\":{\"self\":[[{\"href\":\"/a\"}]]}}")]
		public void NonObject_Link_Object_Throws_JsonException(string json)
		{
			Deserializing(json).Should().Throw<JsonException>();
		}

		[Fact]
		public void NonObject_Link_Object_Throws_JsonException_When_Deserializing_A_LinkObject_Directly()
		{
			Action act = () => JsonSerializer.Deserialize<LinkObject>("[]");

			act.Should().Throw<JsonException>();
		}

		// #94.3 — LinkCollectionConverter: a numeric/boolean rel value previously threw InvalidOperationException.

		[Theory]
		[InlineData("{\"_links\":{\"self\":123}}")]
		[InlineData("{\"_links\":{\"self\":3.14}}")]
		[InlineData("{\"_links\":{\"self\":true}}")]
		public void NonString_Rel_Value_Throws_JsonException(string json)
		{
			Deserializing(json).Should().Throw<JsonException>();
		}

		[Fact]
		public void NonString_Rel_Value_Throws_JsonException_When_Deserializing_A_LinkCollection_Directly()
		{
			Action act = () => JsonSerializer.Deserialize<LinkCollection>("{\"self\":123}");

			act.Should().Throw<JsonException>();
		}

		[Fact]
		public void String_Rel_Shorthand_Is_Still_Accepted()
		{
			var resource = Resource.Parse("{\"_links\":{\"self\":\"/orders/123\"}}");

			resource!.Links.Single().LinkObjects.Single().Href.Should().Be("/orders/123");
		}

		// #94.4 — LinkCollectionConverter / EmbeddedResourceCollectionConverter: an array containing
		// non-objects previously threw InvalidOperationException from AsObject().

		[Theory]
		[InlineData("{\"_links\":[1]}")]
		[InlineData("{\"_links\":[\"self\"]}")]
		[InlineData("{\"_links\":[[]]}")]
		[InlineData("{\"_embedded\":[1]}")]
		[InlineData("{\"_embedded\":[\"ea:order\"]}")]
		[InlineData("{\"_embedded\":[[]]}")]
		public void NonObject_Element_In_A_Reserved_Array_Throws_JsonException(string json)
		{
			Deserializing(json).Should().Throw<JsonException>();
		}

		[Theory]
		[InlineData("{\"_links\":\"not-an-object\"}")]
		[InlineData("{\"_links\":42}")]
		[InlineData("{\"_embedded\":\"not-an-object\"}")]
		[InlineData("{\"_embedded\":42}")]
		public void NonObject_NonArray_Reserved_Value_Throws_JsonException(string json)
		{
			Deserializing(json).Should().Throw<JsonException>();
		}

		[Fact]
		public void Array_Of_Link_Objects_Is_Still_Accepted()
		{
			var resource = Resource.Parse("{\"_links\":[{\"self\":{\"href\":\"/a\"}},{\"next\":{\"href\":\"/b\"}}]}");

			resource!.Links.Should().HaveCount(2);
			resource.Links.Select(l => l.Rel).Should().BeEquivalentTo(new[] { "self", "next" });
		}

		// #94.5 — duplicate property names previously surfaced as an ArgumentException ("An item with
		// the same key has already been added"), deferred to first property access or re-serialization.
		// They are now normalized last-wins, matching what most JSON parsers do with a construct
		// RFC 8259 leaves undefined.

		[Fact]
		public void Duplicate_Rel_Is_Normalized_Last_Wins()
		{
			var json = "{\"_links\":{\"self\":{\"href\":\"/a\"},\"self\":{\"href\":\"/b\"}}}";

			var resource = Resource.Parse(json);

			resource.Should().NotBeNull();
			resource!.Links.Should().ContainSingle();
			resource.Links.Single().Rel.Should().Be("self");
			resource.Links.Single().LinkObjects.Single().Href.Should().Be("/b");
		}

		[Fact]
		public void Duplicate_Rel_Does_Not_Throw_On_Re_Serialization()
		{
			var json = "{\"_links\":{\"self\":{\"href\":\"/a\"},\"self\":{\"href\":\"/b\"}}}";

			var resource = Resource.Parse(json);
			Action act = () => JsonSerializer.Serialize(resource);

			act.Should().NotThrow();
			JsonSerializer.Serialize(resource).Should().Contain("/b").And.NotContain("/a");
		}

		[Fact]
		public void Duplicate_Rel_Keeps_First_Seen_Position()
		{
			var json = "{\"_links\":{\"self\":{\"href\":\"/a\"},\"next\":{\"href\":\"/n\"},\"self\":{\"href\":\"/b\"}}}";

			var resource = Resource.Parse(json);

			resource!.Links.Select(l => l.Rel).Should().ContainInOrder("self", "next");
			resource.Links.Single(l => l.Rel == "self").LinkObjects.Single().Href.Should().Be("/b");
		}

		[Fact]
		public void Duplicate_Embedded_Name_Is_Normalized_Last_Wins()
		{
			var json = "{\"_embedded\":{\"ea:order\":{\"total\":10},\"ea:order\":{\"total\":20}}}";

			var resource = Resource.Parse(json);

			resource.Should().NotBeNull();
			resource!.Embedded.Should().ContainSingle();

			var embedded = resource.Embedded.Single();
			embedded.Name.Should().Be("ea:order");
			JsonSerializer.Serialize(embedded.Resources.Single()).Should().Contain("20").And.NotContain("10");
		}

		[Fact]
		public void Duplicate_State_Property_Is_Normalized_Last_Wins()
		{
			var json = "{\"total\":10,\"total\":20}";

			var resource = Resource.Parse(json);

			resource.Should().NotBeNull();

			var roundTripped = JsonSerializer.Serialize(resource);
			using var document = JsonDocument.Parse(roundTripped);

			document.RootElement.EnumerateObject().Should().ContainSingle();
			document.RootElement.GetProperty("total").GetInt32().Should().Be(20);
		}

		[Fact]
		public void Duplicate_Link_Object_Attribute_Is_Normalized_Last_Wins()
		{
			var json = "{\"_links\":{\"self\":{\"href\":\"/a\",\"href\":\"/b\"}}}";

			var resource = Resource.Parse(json);

			resource!.Links.Single().LinkObjects.Single().Href.Should().Be("/b");
		}

		// Syntactically broken JSON keeps surfacing as JsonException from the reader.

		[Theory]
		[InlineData("{")]
		[InlineData("{\"_links\":}")]
		[InlineData("{\"_links\":{\"self\":{\"href\":\"/a\"}}")]
		public void Syntactically_Invalid_Json_Throws_JsonException(string json)
		{
			Action act = () => Resource.Parse(json);

			act.Should().Throw<JsonException>();
		}
	}
}
