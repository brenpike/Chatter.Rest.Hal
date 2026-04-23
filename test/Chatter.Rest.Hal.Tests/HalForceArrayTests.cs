using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests
{
	/// <summary>
	/// Integration tests for the force-array link serialization feature.
	/// Covers both the global AlwaysUseArrayForLinks option (via AddHalConverters) and
	/// the per-relation IsArray flag on individual Link instances.
	/// </summary>
	public class HalForceArrayTests
	{
		[Fact]
		public void Serialize_WithAlwaysUseArrayForLinksTrue_ViaAddHalConverters_SingleLink_ProducesJsonArray()
		{
			// When AddHalConverters is called with AlwaysUseArrayForLinks=true, every link relation
			// must serialize as a JSON array regardless of the per-relation IsArray flag.
			var options = new JsonSerializerOptions();
			options.AddHalConverters(new HalJsonOptions { AlwaysUseArrayForLinks = true });

			var resource = TestHelpers.CreateResourceWithLink("self", "/orders/1");
			var json = JsonSerializer.Serialize(resource, options);
			var doc = JsonDocument.Parse(json);

			var selfNode = doc.RootElement.GetProperty("_links").GetProperty("self");
			selfNode.ValueKind.Should().Be(JsonValueKind.Array);
			selfNode.GetArrayLength().Should().Be(1);
			selfNode[0].GetProperty("href").GetString().Should().Be("/orders/1");
		}

		[Fact]
		public void Serialize_WithAlwaysUseArrayForLinksFalse_ViaAddHalConverters_SingleLink_ProducesSingleObject()
		{
			// When AddHalConverters is called with AlwaysUseArrayForLinks=false, a single link object
			// must serialize as a plain JSON object (existing behavior preserved).
			var options = new JsonSerializerOptions();
			options.AddHalConverters(new HalJsonOptions { AlwaysUseArrayForLinks = false });

			var resource = TestHelpers.CreateResourceWithLink("self", "/orders/1");
			var json = JsonSerializer.Serialize(resource, options);
			var doc = JsonDocument.Parse(json);

			var selfNode = doc.RootElement.GetProperty("_links").GetProperty("self");
			selfNode.ValueKind.Should().Be(JsonValueKind.Object);
			selfNode.GetProperty("href").GetString().Should().Be("/orders/1");
		}

		[Fact]
		public void Serialize_WithNoAddHalConverters_SingleLink_ProducesSingleObject()
		{
			// Without calling AddHalConverters(), the attribute-wired converters are used and a
			// single link object serializes as a plain JSON object (unchanged baseline behavior).
			var options = new JsonSerializerOptions();

			var resource = TestHelpers.CreateResourceWithLink("self", "/orders/1");
			var json = JsonSerializer.Serialize(resource, options);
			var doc = JsonDocument.Parse(json);

			var selfNode = doc.RootElement.GetProperty("_links").GetProperty("self");
			selfNode.ValueKind.Should().Be(JsonValueKind.Object);
			selfNode.GetProperty("href").GetString().Should().Be("/orders/1");
		}

		[Fact]
		public void Serialize_GlobalTrue_PerRelationIsArrayDefault_ProducesArray()
		{
			// Global AlwaysUseArrayForLinks=true overrides the per-relation IsArray default (false),
			// so the output must still be an array.
			var options = new JsonSerializerOptions();
			options.AddHalConverters(new HalJsonOptions { AlwaysUseArrayForLinks = true });

			// link.IsArray is not set — defaults to false
			var link = new Link("orders");
			link.LinkObjects.Add(new LinkObject("/orders/1"));
			var resource = new Resource();
			resource.Links.Add(link);

			var json = JsonSerializer.Serialize(resource, options);
			var doc = JsonDocument.Parse(json);

			var ordersNode = doc.RootElement.GetProperty("_links").GetProperty("orders");
			ordersNode.ValueKind.Should().Be(JsonValueKind.Array);
		}

		[Fact]
		public void Serialize_GlobalFalse_PerRelationIsArrayTrue_ProducesArray()
		{
			// Even when global AlwaysUseArrayForLinks=false, a link with IsArray=true must
			// serialize as an array (per-relation flag takes effect).
			var options = new JsonSerializerOptions();
			options.AddHalConverters(new HalJsonOptions { AlwaysUseArrayForLinks = false });

			var link = new Link("orders") { IsArray = true };
			link.LinkObjects.Add(new LinkObject("/orders/1"));
			var resource = new Resource();
			resource.Links.Add(link);

			var json = JsonSerializer.Serialize(resource, options);
			var doc = JsonDocument.Parse(json);

			var ordersNode = doc.RootElement.GetProperty("_links").GetProperty("orders");
			ordersNode.ValueKind.Should().Be(JsonValueKind.Array);
		}

		[Fact]
		public void RoundTrip_LinkWithIsArrayTrue_PreservesArrayRepresentation()
		{
			// Starting from JSON with an array-form link, deserialize -> re-serialize must
			// preserve the array representation for that relation.
			var inputJson = "{\"_links\":{\"orders\":[{\"href\":\"/orders/1\"}]}}";
			var resource = JsonSerializer.Deserialize<Resource>(inputJson);

			resource.Should().NotBeNull();
			var ordersLink = resource!.Links.FirstOrDefault(l => l.Rel == "orders");
			ordersLink.Should().NotBeNull();
			ordersLink!.IsArray.Should().BeTrue("deserialization of an array relation must set IsArray=true");

			// Re-serialize using default (attribute-wired) converters — IsArray=true drives the output
			var outputJson = JsonSerializer.Serialize(resource);
			var doc = JsonDocument.Parse(outputJson);

			var ordersNode = doc.RootElement.GetProperty("_links").GetProperty("orders");
			ordersNode.ValueKind.Should().Be(JsonValueKind.Array,
				"re-serializing a link with IsArray=true must produce a JSON array");
		}

		[Fact]
		public void AddHalConverters_CalledTwice_DoesNotDuplicateConverters()
		{
			// AddHalConverters has a duplicate-call guard: calling it a second time on the same
			// options instance must not register additional converters.
			var options = new JsonSerializerOptions();
			options.AddHalConverters(new HalJsonOptions { AlwaysUseArrayForLinks = false });
			var countAfterFirst = options.Converters.Count;

			options.AddHalConverters(new HalJsonOptions { AlwaysUseArrayForLinks = true });
			var countAfterSecond = options.Converters.Count;

			countAfterSecond.Should().Be(countAfterFirst,
				"calling AddHalConverters twice must not register converters a second time");
		}
	}
}
