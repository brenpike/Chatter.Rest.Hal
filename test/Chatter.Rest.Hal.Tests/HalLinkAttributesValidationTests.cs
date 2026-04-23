using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests
{
	public class HalLinkAttributesValidationTests
	{
		[Fact]
		public void NonBoolean_Templated_Value_Treated_As_False()
		{
			var json = "{ \"_links\": { \"self\": { \"href\": \"/x\", \"templated\": \"true\" } } }";
			var node = JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
			var resource = node.Deserialize<Chatter.Rest.Hal.Resource>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			resource.Should().NotBeNull();
			var link = resource!.Links.Single(l => l.Rel == "self");
			link.LinkObjects.Should().HaveCount(1);
			var lo = link.LinkObjects.Single();

			// Non-boolean templated values (e.g. the string "true") should NOT be treated as boolean true
			lo.Templated.Should().NotBeTrue();
		}

		[Fact]
		public void Href_Empty_String_Is_Invalid_On_Deserialization()
		{
			var json = "{ \"_links\": { \"self\": { \"href\": \"\" } } }";
			var node = JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
			var resource = node.Deserialize<Chatter.Rest.Hal.Resource>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			resource.Should().NotBeNull();
			var link = resource!.Links.Single(l => l.Rel == "self");

			// LinkObjectConverter treats an empty href as invalid and returns null, so the Link will have no LinkObjects
			link.LinkObjects.Should().BeEmpty();
		}

		[Fact]
		public void NonString_Optional_Attributes_Are_Treated_As_Null()
		{
			var json = "{ \"_links\": { \"self\": { \"href\": \"/x\", \"type\": 123, \"deprecation\": true } } }";
			var node = JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
			var resource = node.Deserialize<Chatter.Rest.Hal.Resource>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			resource.Should().NotBeNull();
			var link = resource!.Links.Single(l => l.Rel == "self");
			link.LinkObjects.Should().HaveCount(1);
			var lo = link.LinkObjects.Single();

			// Non-string optional attributes should be treated as null rather than throwing
			lo.Type.Should().BeNull();
			lo.Deprecation.Should().BeNull();
		}

		[Fact]
		public void Link_Relation_Types_Are_Strings()
		{
			// HAL Spec Section 5: Link relation types MUST be strings
			// Validates IANA names, full URIs, and CURIE formats
			var json = @"{
                ""_links"": {
                    ""self"": { ""href"": ""/orders/123"" },
                    ""next"": { ""href"": ""/orders/124"" },
                    ""prev"": { ""href"": ""/orders/122"" },
                    ""first"": { ""href"": ""/orders/1"" },
                    ""last"": { ""href"": ""/orders/999"" },
                    ""item"": { ""href"": ""/items/1"" },
                    ""collection"": { ""href"": ""/orders"" },
                    ""https://example.com/rels/custom"": { ""href"": ""/custom"" },
                    ""http://docs.api.com/relations/order"": { ""href"": ""/order"" },
                    ""ex:widgets"": { ""href"": ""/widgets/{id}"", ""templated"": true },
                    ""acme:orders"": { ""href"": ""/acme/orders"" },
                    ""rel-with-hyphens"": { ""href"": ""/hyphens"" },
                    ""rel_with_underscores"": { ""href"": ""/underscores"" },
                    ""rel.with.dots"": { ""href"": ""/dots"" }
                }
            }";

			var node = JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
			var resource = node.Deserialize<Chatter.Rest.Hal.Resource>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			resource.Should().NotBeNull();
			resource!.Links.Should().HaveCount(14);

			// Validate IANA registered names
			resource.Links.Should().Contain(l => l.Rel == "self");
			resource.Links.Should().Contain(l => l.Rel == "next");
			resource.Links.Should().Contain(l => l.Rel == "prev");
			resource.Links.Should().Contain(l => l.Rel == "first");
			resource.Links.Should().Contain(l => l.Rel == "last");
			resource.Links.Should().Contain(l => l.Rel == "item");
			resource.Links.Should().Contain(l => l.Rel == "collection");

			// Validate full URI format
			resource.Links.Should().Contain(l => l.Rel == "https://example.com/rels/custom");
			resource.Links.Should().Contain(l => l.Rel == "http://docs.api.com/relations/order");

			// Validate CURIE format
			resource.Links.Should().Contain(l => l.Rel == "ex:widgets");
			resource.Links.Should().Contain(l => l.Rel == "acme:orders");

			// Validate special characters (hyphens, underscores, dots)
			resource.Links.Should().Contain(l => l.Rel == "rel-with-hyphens");
			resource.Links.Should().Contain(l => l.Rel == "rel_with_underscores");
			resource.Links.Should().Contain(l => l.Rel == "rel.with.dots");

			// All relation types should be parseable as strings and have their LinkObjects
			foreach (var link in resource.Links)
			{
				link.Rel.Should().NotBeNullOrEmpty();
				link.Rel.Should().BeOfType<string>();
				link.LinkObjects.Should().HaveCount(1);
				link.LinkObjects.Single().Href.Should().NotBeNullOrEmpty();
			}
		}
	}
}
