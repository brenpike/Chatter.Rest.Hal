using Chatter.Rest.Hal.Builders;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Builders;

public class BuilderTests
{
	[Fact]
	public void test()
	{
		var orders = new List<Order>()
		{
			new Order() { Id = Guid.NewGuid().ToString(), Currency = "USD", Total = 10, Status = "shipped" },
			new Order() { Id = Guid.NewGuid().ToString(), Currency = "CAD", Total = 20, Status = "processing" },
			new Order() { Id = Guid.NewGuid().ToString(), Currency = "EUR", Total = 30, Status = "customs" },
			new Order() { Id = Guid.NewGuid().ToString(), Currency = "USD", Total = 40, Status = "shipped" },
			new Order() { Id = Guid.NewGuid().ToString(), Currency = "USD", Total = 50, Status = "complete" },
			new Order() { Id = Guid.NewGuid().ToString(), Currency = "CAD", Total = 69, Status = "nice" }
		};

		var resource = ResourceBuilder.WithState(new { currentlyProcessing = 14, shippedToday = 20 })
			.AddSelf().AddLinkObject("/orders")
			.AddCuries().AddLinkObject("http://example.com/docs/rels/{rel}", "ea")
			.AddLink("next").AddLinkObject("/orders?page=2")
			.AddLink("ea:find").AddLinkObject("/orders{?id}").Templated()
			.AddLink("ea:admin").AddLinkObject("/admins/2").WithTitle("Fred")
								.AddLinkObject("/admins/5").WithTitle("Kate")
			.AddEmbedded("ea:order")
				.AddResources(orders, (o, builder) =>
				{
					builder.AddSelf().AddLinkObject($"/orders/{o.Id}")
						   .AddLink("ea:basket").AddLinkObject("/baskets/{basketId}").Templated()
						   .AddLink("ea:customer").AddLinkObject("/customers/{custId}").Templated();
				})
			.Build();

		var json = JsonSerializer.Serialize(resource);
	}

	[Fact]
	public void Builder_Sets_Templated_True_For_URI_Template()
	{
		// HAL spec section 7.2: The "templated" property is OPTIONAL.
		// Its value is boolean and SHOULD be true when the Link Object's "href"
		// property is a URI Template.
		//
		// This test validates that the ResourceBuilder's .Templated() method
		// correctly sets the templated flag to true and that it serializes
		// properly in the JSON output for various URI template patterns.

		// Test case 1: URI template with single variable
		var resourceSingleVar = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/test")
			.AddLink("item").AddLinkObject("/items/{id}").Templated()
			.Build();

		var jsonSingleVar = JsonSerializer.Serialize(resourceSingleVar);
		var docSingleVar = JsonDocument.Parse(jsonSingleVar);
		var itemLink = docSingleVar.RootElement.GetProperty("_links").GetProperty("item");

		itemLink.GetProperty("href").GetString().Should().Be("/items/{id}");
		itemLink.GetProperty("templated").GetBoolean().Should().BeTrue();

		// Test case 2: URI template with multiple variables
		var resourceMultiVar = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/test")
			.AddLink("review").AddLinkObject("/items/{id}/reviews/{reviewId}").Templated()
			.Build();

		var jsonMultiVar = JsonSerializer.Serialize(resourceMultiVar);
		var docMultiVar = JsonDocument.Parse(jsonMultiVar);
		var reviewLink = docMultiVar.RootElement.GetProperty("_links").GetProperty("review");

		reviewLink.GetProperty("href").GetString().Should().Be("/items/{id}/reviews/{reviewId}");
		reviewLink.GetProperty("templated").GetBoolean().Should().BeTrue();

		// Test case 3: URI template with query parameters
		var resourceQueryParams = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/test")
			.AddLink("search").AddLinkObject("/search{?q,page}").Templated()
			.Build();

		var jsonQueryParams = JsonSerializer.Serialize(resourceQueryParams);
		var docQueryParams = JsonDocument.Parse(jsonQueryParams);
		var searchLink = docQueryParams.RootElement.GetProperty("_links").GetProperty("search");

		searchLink.GetProperty("href").GetString().Should().Be("/search{?q,page}");
		searchLink.GetProperty("templated").GetBoolean().Should().BeTrue();

		// Test case 4: Verify all edge cases together in a single resource
		var resourceAllCases = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/test")
			.AddLink("single").AddLinkObject("/items/{id}").Templated()
			.AddLink("multi").AddLinkObject("/items/{id}/reviews/{reviewId}").Templated()
			.AddLink("query").AddLinkObject("/search{?q,page}").Templated()
			.Build();

		var jsonAllCases = JsonSerializer.Serialize(resourceAllCases);
		var docAllCases = JsonDocument.Parse(jsonAllCases);
		var linksAllCases = docAllCases.RootElement.GetProperty("_links");

		// Verify single variable template
		var singleLink = linksAllCases.GetProperty("single");
		singleLink.GetProperty("href").GetString().Should().Be("/items/{id}");
		singleLink.GetProperty("templated").GetBoolean().Should().BeTrue();

		// Verify multiple variable template
		var multiLink = linksAllCases.GetProperty("multi");
		multiLink.GetProperty("href").GetString().Should().Be("/items/{id}/reviews/{reviewId}");
		multiLink.GetProperty("templated").GetBoolean().Should().BeTrue();

		// Verify query parameter template
		var queryLink = linksAllCases.GetProperty("query");
		queryLink.GetProperty("href").GetString().Should().Be("/search{?q,page}");
		queryLink.GetProperty("templated").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Builder_Constructs_Valid_CURIE_Structure()
	{
		// HAL spec section 7.3: CURIEs (Compact URIs) are defined using the reserved
		// "curies" link relation. The value is an array of Link Objects, each with:
		// - "name": the CURIE prefix
		// - "href": a URI template containing the {rel} token
		// - "templated": true (required for URI templates)
		//
		// This test validates that the ResourceBuilder's .AddCuries() method
		// correctly constructs the CURIE structure and serializes to valid HAL JSON.
		// Note: Current serialization behavior follows link object conventions:
		// single LinkObject = object, multiple LinkObjects = array.

		// Test case 1: Single CURIE definition via builder
		// Note: Single link object serializes as an object (not array) per current implementation
		var resourceSingleCurie = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/orders")
			.AddCuries().AddLinkObject("https://docs.acme.com/relations/{rel}", "acme")
			.Build();

		var jsonSingleCurie = JsonSerializer.Serialize(resourceSingleCurie);
		var docSingleCurie = JsonDocument.Parse(jsonSingleCurie);
		var curiesProperty = docSingleCurie.RootElement.GetProperty("_links").GetProperty("curies");

		// Single CURIE serializes as an object (consistent with other single link objects)
		curiesProperty.ValueKind.Should().Be(JsonValueKind.Object);

		curiesProperty.GetProperty("name").GetString().Should().Be("acme");
		curiesProperty.GetProperty("href").GetString().Should().Be("https://docs.acme.com/relations/{rel}");
		curiesProperty.GetProperty("templated").GetBoolean().Should().BeTrue();

		// Verify href contains the {rel} token
		curiesProperty.GetProperty("href").GetString().Should().Contain("{rel}");

		// Test case 2: CURIE used in subsequent link relation
		// This validates the complete workflow: define CURIE, then use it in a link
		var resourceWithCuriedLink = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/orders")
			.AddCuries().AddLinkObject("https://docs.acme.com/relations/{rel}", "acme")
			.AddLink("acme:widgets").AddLinkObject("/widgets")
			.Build();

		var jsonWithCuriedLink = JsonSerializer.Serialize(resourceWithCuriedLink);
		var docWithCuriedLink = JsonDocument.Parse(jsonWithCuriedLink);
		var linksWithCurie = docWithCuriedLink.RootElement.GetProperty("_links");

		// Verify CURIE definition exists as an object (single CURIE)
		var curiesWithLink = linksWithCurie.GetProperty("curies");
		curiesWithLink.ValueKind.Should().Be(JsonValueKind.Object);
		curiesWithLink.GetProperty("name").GetString().Should().Be("acme");
		curiesWithLink.GetProperty("href").GetString().Should().Be("https://docs.acme.com/relations/{rel}");
		curiesWithLink.GetProperty("templated").GetBoolean().Should().BeTrue();

		// Verify the curied link exists (validates the full CURIE pattern)
		var curiedLink = linksWithCurie.GetProperty("acme:widgets");
		curiedLink.GetProperty("href").GetString().Should().Be("/widgets");

		// Test case 3: CURIE href with {rel} token in different positions
		var resourcePrefixToken = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/test")
			.AddCuries().AddLinkObject("https://example.com/{rel}", "prefix")
			.Build();

		var jsonPrefixToken = JsonSerializer.Serialize(resourcePrefixToken);
		var docPrefixToken = JsonDocument.Parse(jsonPrefixToken);
		var curiePrefix = docPrefixToken.RootElement.GetProperty("_links").GetProperty("curies");

		curiePrefix.GetProperty("href").GetString().Should().Be("https://example.com/{rel}");
		curiePrefix.GetProperty("href").GetString().Should().Contain("{rel}");
		curiePrefix.GetProperty("templated").GetBoolean().Should().BeTrue();

		var resourceMiddleToken = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/test")
			.AddCuries().AddLinkObject("https://example.org/docs/{rel}/info", "middle")
			.Build();

		var jsonMiddleToken = JsonSerializer.Serialize(resourceMiddleToken);
		var docMiddleToken = JsonDocument.Parse(jsonMiddleToken);
		var curieMiddle = docMiddleToken.RootElement.GetProperty("_links").GetProperty("curies");

		curieMiddle.GetProperty("href").GetString().Should().Be("https://example.org/docs/{rel}/info");
		curieMiddle.GetProperty("href").GetString().Should().Contain("{rel}");
		curieMiddle.GetProperty("templated").GetBoolean().Should().BeTrue();

		var resourceQueryToken = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/test")
			.AddCuries().AddLinkObject("https://api.test.com/relations/{rel}?format=json", "query")
			.Build();

		var jsonQueryToken = JsonSerializer.Serialize(resourceQueryToken);
		var docQueryToken = JsonDocument.Parse(jsonQueryToken);
		var curieQuery = docQueryToken.RootElement.GetProperty("_links").GetProperty("curies");

		curieQuery.GetProperty("href").GetString().Should().Be("https://api.test.com/relations/{rel}?format=json");
		curieQuery.GetProperty("href").GetString().Should().Contain("{rel}");
		curieQuery.GetProperty("templated").GetBoolean().Should().BeTrue();
	}
}
