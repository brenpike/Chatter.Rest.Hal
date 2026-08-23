using System;
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

		// HAL section 5.1 defines href by reference to RFC 3986, and RFC 3986 section 4.4 makes the empty
		// string a valid same-document reference. The read path therefore accepts an empty href while every
		// other blank or non-string form stays rejected. The tests below pin each rung of that ladder.
		// https://datatracker.ietf.org/doc/html/rfc3986#section-4.4

		[Fact]
		public void Href_Empty_String_Deserializes_As_Same_Document_Reference()
		{
			var json = "{ \"_links\": { \"self\": { \"href\": \"\" } } }";
			var node = JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
			var resource = node.Deserialize<Chatter.Rest.Hal.Resource>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			resource.Should().NotBeNull();
			var link = resource!.Links.Single(l => l.Rel == "self");

			link.LinkObjects.Should().HaveCount(1);
			link.LinkObjects.Single().Href.Should().BeEmpty();
		}

		[Fact]
		public void Href_Empty_String_Preserves_Sibling_Attributes()
		{
			// Dropping the whole Link Object also discarded its other attributes, which is the defect
			// reported in issue #120. Cover a boolean-valued and a string-valued optional so both
			// branches of the shared optional-attribute population are exercised on this path.
			var json = "{ \"_links\": { \"self\": { \"href\": \"\", \"title\": \"t\", \"name\": \"n\", \"templated\": true } } }";
			var node = JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
			var resource = node.Deserialize<Chatter.Rest.Hal.Resource>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			resource.Should().NotBeNull();
			var link = resource!.Links.Single(l => l.Rel == "self");
			link.LinkObjects.Should().HaveCount(1);
			var lo = link.LinkObjects.Single();

			lo.Href.Should().BeEmpty();
			lo.Title.Should().Be("t");
			lo.Name.Should().Be("n");
			lo.Templated.Should().BeTrue();
		}

		[Fact]
		public void Href_Whitespace_Only_Is_Invalid_On_Deserialization()
		{
			// Only the empty string is a same-document reference; a whitespace-only href stays invalid,
			// so the converter returns null and the relation survives with no link objects.
			var json = "{ \"_links\": { \"self\": { \"href\": \"   \" } } }";
			var node = JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
			var resource = node.Deserialize<Chatter.Rest.Hal.Resource>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			resource.Should().NotBeNull();
			var link = resource!.Links.Single(l => l.Rel == "self");

			link.LinkObjects.Should().BeEmpty();
		}

		[Fact]
		public void Href_Null_Is_Invalid_On_Deserialization()
		{
			var json = "{ \"_links\": { \"self\": { \"href\": null } } }";
			var node = JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
			var resource = node.Deserialize<Chatter.Rest.Hal.Resource>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			resource.Should().NotBeNull();
			var link = resource!.Links.Single(l => l.Rel == "self");

			link.LinkObjects.Should().BeEmpty();
		}

		[Fact]
		public void Href_NonString_Throws_JsonException_On_Deserialization()
		{
			// A non-string href is malformed rather than blank, so it keeps failing loudly instead of
			// being tolerated. The link collection materializes lazily, so touching it forces the read.
			var json = "{ \"_links\": { \"self\": { \"href\": 123 } } }";
			var node = JsonNode.Parse(json, new JsonNodeOptions { PropertyNameCaseInsensitive = true });

			Action deserializing = () =>
			{
				var resource = node.Deserialize<Chatter.Rest.Hal.Resource>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				_ = resource!.Links;
			};

			deserializing.Should().Throw<JsonException>();
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
