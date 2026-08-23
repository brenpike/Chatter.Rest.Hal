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

		[Theory]
		[InlineData("{ \"_LINKS\": { \"other\": { \"href\": \"/x\" } }, \"_links\": { \"self\": { \"href\": \"/orders/1\" } } }")]
		[InlineData("{ \"_links\": { \"self\": { \"href\": \"/orders/1\" } }, \"_LINKS\": { \"other\": { \"href\": \"/x\" } } }")]
		public void Reserved_Name_And_Case_Variant_Both_Survive_Under_Case_Insensitive_Options(string json)
		{
			// The node tree is always ordinal. A case-insensitive DOM would merge these two distinct
			// members before the reserved lookup ran, either losing the literal _links or promoting
			// _LINKS to reserved status depending on their order.
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			var resource = JsonSerializer.Deserialize<Resource>(json, options);

			resource.Should().NotBeNull();
			resource!.Links.Should().ContainSingle(l => l.Rel == "self");
			resource.Links.Single().LinkObjects.Single().Href.Should().Be("/orders/1");

			var roundTripped = JsonSerializer.Serialize(resource, options);
			using var document = JsonDocument.Parse(roundTripped);

			document.RootElement.TryGetProperty("_LINKS", out var variant).Should().BeTrue();
			variant.GetProperty("other").GetProperty("href").GetString().Should().Be("/x");
			document.RootElement.TryGetProperty("_links", out _).Should().BeTrue();
		}

		[Fact]
		public void State_Properties_Differing_Only_By_Case_Survive_Under_Case_Insensitive_Options()
		{
			// PropertyNameCaseInsensitive governs how JSON names are matched to members, not whether
			// two distinct JSON members collapse into one.
			var json = "{ \"Name\": \"a\", \"name\": \"b\" }";
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			var resource = JsonSerializer.Deserialize<Resource>(json, options);

			var roundTripped = JsonSerializer.Serialize(resource, options);
			using var document = JsonDocument.Parse(roundTripped);

			document.RootElement.EnumerateObject().Should().HaveCount(2);
			document.RootElement.GetProperty("Name").GetString().Should().Be("a");
			document.RootElement.GetProperty("name").GetString().Should().Be("b");
		}

		[Fact]
		public void Relation_Names_Differing_Only_By_Case_Survive_Under_Case_Insensitive_Options()
		{
			// Link relation types are case-sensitive; two rels differing only by case are two rels.
			var json = "{ \"_links\": { \"Self\": { \"href\": \"/a\" }, \"self\": { \"href\": \"/b\" } } }";
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			var resource = JsonSerializer.Deserialize<Resource>(json, options);

			resource!.Links.Should().HaveCount(2);
			resource.Links.Single(l => l.Rel == "Self").LinkObjects.Single().Href.Should().Be("/a");
			resource.Links.Single(l => l.Rel == "self").LinkObjects.Single().Href.Should().Be("/b");
		}

		[Fact]
		public void Embedded_Names_Differing_Only_By_Case_Survive_Under_Case_Insensitive_Options()
		{
			var json = "{ \"_embedded\": { \"Order\": { \"total\": 1 }, \"order\": { \"total\": 2 } } }";
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			var resource = JsonSerializer.Deserialize<Resource>(json, options);

			resource!.Embedded.Should().HaveCount(2);
			resource.Embedded.Select(e => e.Name).Should().BeEquivalentTo(new[] { "Order", "order" });
		}

		[Fact]
		public void Exact_Link_Object_Attribute_Wins_Over_A_Case_Variant()
		{
			var json = "{ \"_links\": { \"self\": { \"HREF\": \"/a\", \"href\": \"/b\" } } }";
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			var resource = JsonSerializer.Deserialize<Resource>(json, options);

			resource!.Links.Single().LinkObjects.Single().Href.Should().Be("/b");
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
