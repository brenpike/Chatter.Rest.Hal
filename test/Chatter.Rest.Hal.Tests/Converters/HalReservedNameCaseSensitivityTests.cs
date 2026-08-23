using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Converters
{
	/// <summary>
	/// Covers issue #93: converters must not force <c>PropertyNameCaseInsensitive</c>.
	/// The HAL reserved names <c>_links</c>/<c>_embedded</c> are literal and case-sensitive
	/// (draft-kelly-json-hal section 4.1), and legal JSON whose property names differ only by
	/// case must survive deserialization intact.
	/// </summary>
	public class HalReservedNameCaseSensitivityTests
	{
		[Theory]
		[InlineData("_LiNkS")]
		[InlineData("_LINKS")]
		[InlineData("_Links")]
		public void Case_Variant_Of__links_Is_Not_Treated_As_Reserved(string variant)
		{
			var json = $"{{ \"{variant}\": {{ \"self\": {{ \"href\": \"/orders/1\" }} }} }}";

			var resource = JsonSerializer.Deserialize<Resource>(json);

			resource.Should().NotBeNull();
			resource!.Links.Should().BeEmpty();
		}

		[Theory]
		[InlineData("_EMBEDDED")]
		[InlineData("_Embedded")]
		public void Case_Variant_Of__embedded_Is_Not_Treated_As_Reserved(string variant)
		{
			var json = $"{{ \"{variant}\": {{ \"ea:order\": {{ \"total\": 30 }} }} }}";

			var resource = JsonSerializer.Deserialize<Resource>(json);

			resource.Should().NotBeNull();
			resource!.Embedded.Should().BeEmpty();
		}

		[Fact]
		public void Case_Variant_Of__links_Stays_In_State_And_Is_Not_Serialized_Twice()
		{
			// Before the fix the reserved lookup was case-insensitive while the state stripping was
			// case-sensitive, so "_LINKS" both populated Links and stayed in state, emitting the
			// link data twice on re-serialization.
			var json = "{ \"_LINKS\": { \"self\": { \"href\": \"/orders/1\" } } }";

			var resource = JsonSerializer.Deserialize<Resource>(json);
			resource.Should().NotBeNull();

			var roundTripped = JsonSerializer.Serialize(resource);
			using var document = JsonDocument.Parse(roundTripped);

			document.RootElement.TryGetProperty("_LINKS", out _).Should().BeTrue();
			document.RootElement.TryGetProperty("_links", out _).Should().BeFalse();
			document.RootElement.EnumerateObject().Should().HaveCount(1);
		}

		[Fact]
		public void Reserved_Names_Stay_Case_Sensitive_When_Caller_Opts_Into_Case_Insensitivity()
		{
			// HAL-spec conformance is a hard rule: the reserved names are literal even when the
			// caller asks for case-insensitive matching of ordinary property names.
			var json = "{ \"_LINKS\": { \"self\": { \"href\": \"/orders/1\" } } }";
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			var resource = JsonSerializer.Deserialize<Resource>(json, options);

			resource.Should().NotBeNull();
			resource!.Links.Should().BeEmpty();
		}

		[Fact]
		public void Literal_Reserved_Names_Are_Still_Read()
		{
			var json = "{ \"_links\": { \"self\": { \"href\": \"/orders/1\" } }, \"_embedded\": { \"ea:order\": { \"total\": 30 } } }";

			var resource = JsonSerializer.Deserialize<Resource>(json);

			resource.Should().NotBeNull();
			resource!.Links.Should().ContainSingle(l => l.Rel == "self");
			resource.Embedded.Should().ContainSingle(e => e.Name == "ea:order");
		}

		[Fact]
		public void State_Properties_Differing_Only_By_Case_Are_Preserved()
		{
			// {"Name":"a","name":"b"} is legal JSON. Forcing a case-insensitive node tree collided
			// the two keys and broke the whole resource.
			var json = "{ \"Name\": \"a\", \"name\": \"b\", \"_links\": { \"self\": { \"href\": \"/orders/1\" } } }";

			var resource = JsonSerializer.Deserialize<Resource>(json);

			resource.Should().NotBeNull();
			resource!.Links.Should().ContainSingle(l => l.Rel == "self");

			var roundTripped = JsonSerializer.Serialize(resource);
			using var document = JsonDocument.Parse(roundTripped);

			document.RootElement.GetProperty("Name").GetString().Should().Be("a");
			document.RootElement.GetProperty("name").GetString().Should().Be("b");
		}

		[Fact]
		public void State_Properties_Differing_Only_By_Case_Do_Not_Break_State_Access()
		{
			var json = "{ \"Name\": \"a\", \"name\": \"b\" }";

			var resource = JsonSerializer.Deserialize<Resource>(json);

			resource.Should().NotBeNull();
			var node = resource!.As<JsonNode>();

			node.Should().NotBeNull();
			node!["Name"]!.GetValue<string>().Should().Be("a");
			node["name"]!.GetValue<string>().Should().Be("b");
		}

		[Fact]
		public void Link_Object_Attribute_Names_Honor_Caller_Case_Insensitivity()
		{
			// Non-reserved names follow the caller's options rather than a hardcoded policy.
			var json = "{ \"_links\": { \"self\": { \"HREF\": \"/orders/1\" } } }";

			var caseSensitive = JsonSerializer.Deserialize<Resource>(json);
			caseSensitive!.Links.Single().LinkObjects.Should().BeEmpty();

			var caseInsensitive = JsonSerializer.Deserialize<Resource>(
				json,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
			caseInsensitive!.Links.Single().LinkObjects.Single().Href.Should().Be("/orders/1");
		}
	}
}
