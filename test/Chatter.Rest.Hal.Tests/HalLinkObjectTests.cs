using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests
{
	public class HalLinkObjectTests
	{
		[Fact]
		public void LinkObject_Serializes_Required_Href()
		{
			var lo = TestHelpers.CreateLinkObject("/orders/1");
			var json = JsonSerializer.Serialize(lo);

			// Should contain the required href property
			TestHelpers.AssertJsonContainsNormalized(json, "{ \"href\": \"/orders/1\" }");
		}

		[Fact]
		public void LinkObject_Reads_Templated_True_For_Template()
		{
			var resource = TestHelpers.LoadResourceFromFixture("templated_link.json");
			var link = resource.Links.FirstOrDefault(l => l.Rel == "self");
			link.Should().NotBeNull();
			link!.LinkObjects.Should().HaveCount(1);
			var lo = link.LinkObjects.First();
			lo.Templated.Should().BeTrue();
		}

		[Fact]
		public void LinkObject_Missing_Href_Produces_Null_On_Deserialization()
		{
			var resource = TestHelpers.LoadResourceFromFixture("invalid_link_missing_href.json");
			var link = resource.Links.FirstOrDefault(l => l.Rel == "self");
			link.Should().NotBeNull();

			// The LinkObjectConverter returns null for malformed link objects (missing href)
			// The LinkCollectionConverter will add a Link with no LinkObjects in this case.
			link!.LinkObjects.Should().BeEmpty();
		}

		[Fact]
		public void LinkObject_Preserves_Optional_Attributes_On_Roundtrip()
		{
			var original = new Chatter.Rest.Hal.LinkObject("/orders/{id}")
			{
				Templated = true,
				Type = "application/json",
				Deprecation = "http://example.com/deprecated",
				Name = "order",
				Profile = "http://example.com/profile",
				Title = "Order",
				Hreflang = "en-US"
			};

			var json = JsonSerializer.Serialize(original);
			var round = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkObject>(json);

			round.Should().NotBeNull();
			round!.Href.Should().Be(original.Href);
			round.Templated.Should().Be(original.Templated);
			round.Type.Should().Be(original.Type);
			round.Deprecation.Should().Be(original.Deprecation);
			round.Name.Should().Be(original.Name);
			round.Profile.Should().Be(original.Profile);
			round.Title.Should().Be(original.Title);
			round.Hreflang.Should().Be(original.Hreflang);
		}

		[Fact]
		public void LinkObject_With_Unknown_Properties_Are_Ignored()
		{
			// Spec Section 3.11: Unknown Link Object properties MUST be ignored
			// Testing tolerant reader behavior with various JSON types
			var json = @"{
                ""href"": ""/orders/123"",
                ""templated"": true,
                ""type"": ""application/json"",
                ""name"": ""order-detail"",
                ""title"": ""Order Details"",
                ""profile"": ""http://example.com/profile"",
                ""unknownString"": ""some value"",
                ""unknownNumber"": 42,
                ""unknownBoolean"": true,
                ""unknownArray"": [1, 2, 3],
                ""unknownObject"": { ""nested"": ""value"" },
                ""unknownNull"": null,
                ""anotherUnknown"": ""mixed with known properties""
            }";

			// Deserialization should succeed without errors
			var linkObject = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkObject>(json);

			// Verify deserialization succeeded
			linkObject.Should().NotBeNull();

			// Verify all known properties are preserved correctly
			linkObject!.Href.Should().Be("/orders/123");
			linkObject.Templated.Should().BeTrue();
			linkObject.Type.Should().Be("application/json");
			linkObject.Name.Should().Be("order-detail");
			linkObject.Title.Should().Be("Order Details");
			linkObject.Profile.Should().Be("http://example.com/profile");

			// Verify optional properties that weren't set remain null/default
			linkObject.Deprecation.Should().BeNull();
			linkObject.Hreflang.Should().BeNull();

			// Unknown properties are silently ignored (no exception thrown, no properties added)
			// The LinkObject type only exposes the standard HAL properties
		}

		[Fact]
		public void LinkObject_Type_Property_Serializes_And_Deserializes()
		{
			// Spec Section 3.4: type property is a hint to indicate the media type expected
			var original = new Chatter.Rest.Hal.LinkObject("/orders/123")
			{
				Type = "application/json"
			};

			var json = JsonSerializer.Serialize(original);
			var deserialized = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkObject>(json);

			// Verify the JSON contains only href and type
			TestHelpers.AssertJsonContainsNormalized(json, "{ \"href\": \"/orders/123\", \"type\": \"application/json\" }");

			// Verify roundtrip preserves the value
			deserialized.Should().NotBeNull();
			deserialized!.Href.Should().Be("/orders/123");
			deserialized.Type.Should().Be("application/json");
			deserialized.Templated.Should().BeNull();
			deserialized.Deprecation.Should().BeNull();
			deserialized.Name.Should().BeNull();
			deserialized.Profile.Should().BeNull();
			deserialized.Title.Should().BeNull();
			deserialized.Hreflang.Should().BeNull();
		}

		[Fact]
		public void LinkObject_Deprecation_Property_Serializes_And_Deserializes()
		{
			// Spec Section 3.5: deprecation property provides a URL with deprecation information
			var original = new Chatter.Rest.Hal.LinkObject("/orders/123")
			{
				Deprecation = "http://example.com/deprecated"
			};

			var json = JsonSerializer.Serialize(original);
			var deserialized = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkObject>(json);

			// Verify the JSON contains only href and deprecation
			TestHelpers.AssertJsonContainsNormalized(json, "{ \"href\": \"/orders/123\", \"deprecation\": \"http://example.com/deprecated\" }");

			// Verify roundtrip preserves the value
			deserialized.Should().NotBeNull();
			deserialized!.Href.Should().Be("/orders/123");
			deserialized.Deprecation.Should().Be("http://example.com/deprecated");
			deserialized.Templated.Should().BeNull();
			deserialized.Type.Should().BeNull();
			deserialized.Name.Should().BeNull();
			deserialized.Profile.Should().BeNull();
			deserialized.Title.Should().BeNull();
			deserialized.Hreflang.Should().BeNull();
		}

		[Fact]
		public void LinkObject_Name_Property_Serializes_And_Deserializes()
		{
			// Spec Section 3.6: name property is a secondary key for selecting among multiple links with same relation
			var original = new Chatter.Rest.Hal.LinkObject("/orders/123")
			{
				Name = "order-detail"
			};

			var json = JsonSerializer.Serialize(original);
			var deserialized = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkObject>(json);

			// Verify the JSON contains only href and name
			TestHelpers.AssertJsonContainsNormalized(json, "{ \"href\": \"/orders/123\", \"name\": \"order-detail\" }");

			// Verify roundtrip preserves the value
			deserialized.Should().NotBeNull();
			deserialized!.Href.Should().Be("/orders/123");
			deserialized.Name.Should().Be("order-detail");
			deserialized.Templated.Should().BeNull();
			deserialized.Type.Should().BeNull();
			deserialized.Deprecation.Should().BeNull();
			deserialized.Profile.Should().BeNull();
			deserialized.Title.Should().BeNull();
			deserialized.Hreflang.Should().BeNull();
		}

		[Fact]
		public void LinkObject_Profile_Property_Serializes_And_Deserializes()
		{
			// Spec Section 3.7: profile property provides a URI hinting at the profile of the target resource
			var original = new Chatter.Rest.Hal.LinkObject("/orders/123")
			{
				Profile = "http://example.com/profiles/order"
			};

			var json = JsonSerializer.Serialize(original);
			var deserialized = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkObject>(json);

			// Verify the JSON contains only href and profile
			TestHelpers.AssertJsonContainsNormalized(json, "{ \"href\": \"/orders/123\", \"profile\": \"http://example.com/profiles/order\" }");

			// Verify roundtrip preserves the value
			deserialized.Should().NotBeNull();
			deserialized!.Href.Should().Be("/orders/123");
			deserialized.Profile.Should().Be("http://example.com/profiles/order");
			deserialized.Templated.Should().BeNull();
			deserialized.Type.Should().BeNull();
			deserialized.Deprecation.Should().BeNull();
			deserialized.Name.Should().BeNull();
			deserialized.Title.Should().BeNull();
			deserialized.Hreflang.Should().BeNull();
		}

		[Fact]
		public void LinkObject_Title_Property_Serializes_And_Deserializes()
		{
			// Spec Section 3.8: title property is intended for human consumption
			var original = new Chatter.Rest.Hal.LinkObject("/orders/123")
			{
				Title = "Order Details & Summary"
			};

			var json = JsonSerializer.Serialize(original);
			var deserialized = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkObject>(json);

			// Verify the JSON contains only href and title
			TestHelpers.AssertJsonContainsNormalized(json, "{ \"href\": \"/orders/123\", \"title\": \"Order Details & Summary\" }");

			// Verify roundtrip preserves the value, including special characters
			deserialized.Should().NotBeNull();
			deserialized!.Href.Should().Be("/orders/123");
			deserialized.Title.Should().Be("Order Details & Summary");
			deserialized.Templated.Should().BeNull();
			deserialized.Type.Should().BeNull();
			deserialized.Deprecation.Should().BeNull();
			deserialized.Name.Should().BeNull();
			deserialized.Profile.Should().BeNull();
			deserialized.Hreflang.Should().BeNull();
		}

		[Fact]
		public void LinkObject_Hreflang_Property_Serializes_And_Deserializes()
		{
			// Spec Section 3.9: hreflang property indicates the language of the target resource
			var original = new Chatter.Rest.Hal.LinkObject("/orders/123")
			{
				Hreflang = "en-US"
			};

			var json = JsonSerializer.Serialize(original);
			var deserialized = JsonSerializer.Deserialize<Chatter.Rest.Hal.LinkObject>(json);

			// Verify the JSON contains only href and hreflang
			TestHelpers.AssertJsonContainsNormalized(json, "{ \"href\": \"/orders/123\", \"hreflang\": \"en-US\" }");

			// Verify roundtrip preserves the value
			deserialized.Should().NotBeNull();
			deserialized!.Href.Should().Be("/orders/123");
			deserialized.Hreflang.Should().Be("en-US");
			deserialized.Templated.Should().BeNull();
			deserialized.Type.Should().BeNull();
			deserialized.Deprecation.Should().BeNull();
			deserialized.Name.Should().BeNull();
			deserialized.Profile.Should().BeNull();
			deserialized.Title.Should().BeNull();
		}
	}
}
