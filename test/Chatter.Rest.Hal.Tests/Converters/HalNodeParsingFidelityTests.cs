using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Converters
{
	/// <summary>
	/// Guards the converter parse path (<c>ConverterHelpers.ParseNode</c>). Well-formed payloads take
	/// the lazy <c>JsonNode.Parse</c> path; only a payload that actually contains duplicate property
	/// names is rebuilt through <see cref="JsonDocument"/>. Both paths must produce the same tree, and
	/// neither may reformat state values.
	/// </summary>
	public class HalNodeParsingFidelityTests
	{
		[Theory]
		[InlineData("30.00")]
		[InlineData("1e5")]
		[InlineData("1E+5")]
		[InlineData("-0.0")]
		[InlineData("123456789012345678901234567890")]
		[InlineData("0.1234567890123456789012345")]
		public void Number_Formatting_Is_Preserved_On_The_Fast_Path(string literal)
		{
			var resource = Resource.Parse($"{{\"total\":{literal}}}");

			JsonSerializer.Serialize(resource).Should().Be($"{{\"total\":{literal}}}");
		}

		[Theory]
		[InlineData("30.00")]
		[InlineData("1e5")]
		[InlineData("123456789012345678901234567890")]
		public void Number_Formatting_Is_Preserved_On_The_Duplicate_Normalizing_Path(string literal)
		{
			// The leading duplicate forces the rebuild path for the whole document.
			var resource = Resource.Parse($"{{\"dup\":1,\"dup\":2,\"total\":{literal}}}");

			var json = JsonSerializer.Serialize(resource);

			json.Should().Contain($"\"total\":{literal}");
			json.Should().Contain("\"dup\":2");
		}

		[Fact]
		public void String_Escapes_Survive_Both_Paths()
		{
			const string json = "{\"text\":\"a\\u0041\\\"b\\\\c\\n\"}";

			var fastPath = Resource.Parse(json);
			var rebuildPath = Resource.Parse("{\"dup\":1,\"dup\":2,\"text\":\"a\\u0041\\\"b\\\\c\\n\"}");

			fastPath!.As<System.Text.Json.Nodes.JsonNode>()!["text"]!.GetValue<string>()
				.Should().Be("aA\"b\\c\n");
			rebuildPath!.As<System.Text.Json.Nodes.JsonNode>()!["text"]!.GetValue<string>()
				.Should().Be("aA\"b\\c\n");
		}

		[Theory]
		[InlineData("{\"a\":1,\"a\":2}")]
		[InlineData("{\"x\":{\"a\":1,\"a\":2}}")]
		[InlineData("{\"x\":[{\"a\":1,\"a\":2}]}")]
		[InlineData("{\"x\":[[{\"a\":1,\"a\":2}]]}")]
		[InlineData("{\"x\":{\"y\":{\"z\":{\"a\":1,\"a\":2}}}}")]
		public void Duplicates_Are_Detected_At_Every_Nesting_Level(string json)
		{
			// Detection must not depend on where the duplicate sits, or JsonNode would defer an
			// ArgumentException to whenever that nested object is first materialized.
			var resource = Resource.Parse(json);

			System.Action act = () => JsonSerializer.Serialize(resource);

			act.Should().NotThrow();
			JsonSerializer.Serialize(resource).Should().Contain("2").And.NotContain("1");
		}

		[Fact]
		public void Duplicate_Inside_An_Embedded_Array_Element_Is_Normalized()
		{
			var json = "{\"_embedded\":{\"ea:order\":[{\"total\":1,\"total\":2}]}}";

			var resource = Resource.Parse(json);

			var embedded = resource!.Embedded.Single().Resources.Single();
			JsonSerializer.Serialize(embedded).Should().Be("{\"total\":2}");
		}

		[Fact]
		public void Escaped_Property_Name_Duplicating_A_Literal_One_Is_Normalized()
		{
			// "\u0061" is "a". Comparing raw name bytes would miss this, so the pre-scan treats any
			// escaped name as a possible duplicate and takes the normalizing path.
			var resource = Resource.Parse("{\"a\":1,\"\\u0061\":2}");

			JsonSerializer.Serialize(resource).Should().Be("{\"a\":2}");
		}

		[Fact]
		public void Escaped_Property_Name_Without_A_Duplicate_Is_Unchanged()
		{
			var resource = Resource.Parse("{\"\\u0061\":1,\"b\":2}");

			JsonSerializer.Serialize(resource).Should().Be("{\"a\":1,\"b\":2}");
		}

		[Fact]
		public void Object_With_More_Properties_Than_The_Inline_Scan_Capacity_Detects_A_Duplicate()
		{
			// The pre-scan keeps the first 16 property-name hashes on the stack and spills the rest,
			// so a duplicate beyond that boundary must still be found.
			var properties = Enumerable.Range(0, 40).Select(i => $"\"p{i}\":{i}");
			var json = "{" + string.Join(",", properties) + ",\"p3\":999}";

			var resource = Resource.Parse(json);

			var roundTripped = JsonSerializer.Serialize(resource);
			using var document = JsonDocument.Parse(roundTripped);

			document.RootElement.EnumerateObject().Should().HaveCount(40);
			document.RootElement.GetProperty("p3").GetInt32().Should().Be(999);
		}

		[Fact]
		public void Object_With_Many_Distinct_Properties_Is_Left_Alone()
		{
			var properties = Enumerable.Range(0, 40).Select(i => $"\"p{i}\":{i}");
			var json = "{" + string.Join(",", properties) + "}";

			var resource = Resource.Parse(json);

			JsonSerializer.Serialize(resource).Should().Be(json);
		}

		[Fact]
		public void Duplicate_In_A_Root_Array_Element_Is_Normalized()
		{
			var collection = JsonSerializer.Deserialize<ResourceCollection>("[{\"a\":1,\"a\":2}]");

			JsonSerializer.Serialize(collection!.Single()).Should().Be("{\"a\":2}");
		}

		[Fact]
		public void Deeply_Nested_Document_Still_Round_Trips()
		{
			var json = string.Concat(Enumerable.Repeat("{\"n\":", 30)) + "1" + new string('}', 30);

			var resource = Resource.Parse(json);

			JsonSerializer.Serialize(resource).Should().Be(json);
		}

		[Fact]
		public void Exceeding_Max_Depth_Throws_JsonException()
		{
			var json = string.Concat(Enumerable.Repeat("{\"n\":", 200)) + "1" + new string('}', 200);

			System.Action act = () => Resource.Parse(json);

			act.Should().Throw<JsonException>();
		}
	}
}
