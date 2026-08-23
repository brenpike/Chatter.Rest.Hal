using System;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Converters
{
	/// <summary>
	/// Covers issue #95: a non-object resource used to deserialize "successfully" and then throw
	/// <see cref="InvalidOperationException"/> from the lazy creators at property-access time, far
	/// from the deserialization call. Per the exception contract in #86 it is now rejected at parse
	/// time with <see cref="JsonException"/>.
	/// </summary>
	public class HalNonObjectResourceRejectionTests
	{
		[Theory]
		[InlineData("123")]
		[InlineData("3.14")]
		[InlineData("-1")]
		[InlineData("\"string\"")]
		[InlineData("\"\"")]
		[InlineData("true")]
		[InlineData("false")]
		[InlineData("[]")]
		[InlineData("[{\"foo\":\"bar\"}]")]
		public void NonObject_Resource_Throws_JsonException_At_Parse_Time(string json)
		{
			Action act = () => Resource.Parse(json);

			act.Should().Throw<JsonException>();
		}

		[Fact]
		public void NonObject_Embedded_Resource_Throws_JsonException_At_Parse_Time()
		{
			// Verified input from #95: previously reachable only as
			// Resource.Parse(json)!.Embedded[0].Resources[0].Links.
			Action act = () => Resource.Parse("{\"_embedded\":{\"orders\":[123]}}");

			act.Should().Throw<JsonException>();
		}

		[Theory]
		[InlineData("{\"_embedded\":{\"orders\":[\"a\"]}}")]
		[InlineData("{\"_embedded\":{\"orders\":[true]}}")]
		[InlineData("{\"_embedded\":{\"orders\":[[]]}}")]
		public void NonObject_Element_Of_An_Embedded_Array_Throws_JsonException(string json)
		{
			Action act = () => Resource.Parse(json);

			act.Should().Throw<JsonException>();
		}

		[Fact]
		public void Null_Root_Still_Deserializes_To_Null()
		{
			Resource.Parse("null").Should().BeNull();
		}

		[Fact]
		public void Null_Element_Of_An_Embedded_Array_Is_Skipped()
		{
			var resource = Resource.Parse("{\"_embedded\":{\"orders\":[null]}}");

			resource!.Embedded.Should().ContainSingle();
			resource.Embedded[0].Resources.Should().BeEmpty();
		}

		[Fact]
		public void Malformed_Links_Fail_At_Deserialization_Rather_Than_Property_Access()
		{
			// The failure must not be deferred: parsing alone throws, without touching .Links.
			Action act = () => Resource.Parse("{\"_links\":{\"self\":{\"href\":123}}}");

			act.Should().Throw<JsonException>();
		}

		[Fact]
		public void Malformed_Embedded_Fail_At_Deserialization_Rather_Than_Property_Access()
		{
			Action act = () => Resource.Parse("{\"_embedded\":[1]}");

			act.Should().Throw<JsonException>();
		}

		[Fact]
		public void Malformed_Nested_Embedded_Resource_Fails_At_Deserialization()
		{
			var json = "{\"_embedded\":{\"orders\":[{\"_links\":{\"self\":{\"href\":123}}}]}}";

			Action act = () => Resource.Parse(json);

			act.Should().Throw<JsonException>();
		}

		[Fact]
		public void Valid_Resource_Still_Exposes_Links_And_Embedded()
		{
			var json = "{\"total\":30,\"_links\":{\"self\":{\"href\":\"/orders/1\"}},\"_embedded\":{\"ea:order\":{\"_links\":{\"self\":{\"href\":\"/orders/1/items\"}}}}}";

			var resource = Resource.Parse(json);

			resource.Should().NotBeNull();
			resource!.Links.Single().LinkObjects.Single().Href.Should().Be("/orders/1");
			resource.Embedded.Single().Resources.Single().Links.Single().LinkObjects.Single().Href
				.Should().Be("/orders/1/items");
		}

		[Fact]
		public void Object_Root_Without_Reserved_Members_Still_Yields_Empty_Collections()
		{
			var resource = Resource.Parse("{\"foo\":\"bar\"}");

			resource.Should().NotBeNull();
			resource!.Links.Should().BeEmpty();
			resource.Embedded.Should().BeEmpty();
		}
	}
}
