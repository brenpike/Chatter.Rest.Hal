using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

/// <summary>
/// Covers the State&lt;T&gt; and As&lt;T&gt; caches on <see cref="Resource"/> (issue #101).
/// </summary>
public class ResourceStateAndAsCacheTests
{
	private const string OrderJson = @"{
		""_links"": { ""self"": { ""href"": ""/orders/1"" } },
		""orderNumber"": ""A-100"",
		""total"": 30.5,
		""currency"": ""USD""
	}";

	private sealed class OrderSummary
	{
		[JsonPropertyName("orderNumber")]
		public string? OrderNumber { get; set; }
	}

	private sealed class OrderTotals
	{
		[JsonPropertyName("total")]
		public decimal Total { get; set; }

		[JsonPropertyName("currency")]
		public string? Currency { get; set; }
	}

	private sealed class FullOrder
	{
		[JsonPropertyName("orderNumber")]
		public string? OrderNumber { get; set; }

		[JsonPropertyName("total")]
		public decimal Total { get; set; }

		[JsonPropertyName("currency")]
		public string? Currency { get; set; }
	}

	[Fact]
	public void State_Of_Second_Type_Deserializes_Instead_Of_Failing_The_Cast()
	{
		var resource = Resource.Parse(OrderJson)!;

		var summary = resource.State<OrderSummary>();
		var totals = resource.State<OrderTotals>();

		summary.Should().NotBeNull();
		summary!.OrderNumber.Should().Be("A-100");
		totals.Should().NotBeNull();
		totals!.Total.Should().Be(30.5m);
		totals.Currency.Should().Be("USD");
	}

	[Fact]
	public void State_Of_First_Type_Still_Works_After_Projecting_Onto_A_Second_Type()
	{
		var resource = Resource.Parse(OrderJson)!;

		resource.State<OrderSummary>().Should().NotBeNull();
		resource.State<OrderTotals>().Should().NotBeNull();

		resource.State<OrderSummary>()!.OrderNumber.Should().Be("A-100");
	}

	[Fact]
	public void State_Returns_Detached_Projections_For_A_Repeated_Type()
	{
		var resource = Resource.Parse(OrderJson)!;

		var first = resource.State<OrderSummary>();
		var second = resource.State<OrderSummary>();

		// Each call materializes a detached snapshot from the original JSON: equal content, never
		// the same instance, so a mutated projection cannot desynchronize equality from Write.
		first.Should().NotBeSameAs(second);
		first.Should().BeEquivalentTo(second);
	}

	[Fact]
	public void State_Projection_Onto_A_Second_Type_Does_Not_Replace_The_Serialized_State()
	{
		var resource = Resource.Parse(OrderJson)!;

		resource.State<FullOrder>().Should().NotBeNull();
		resource.State<OrderSummary>().Should().NotBeNull();

		var json = JsonSerializer.Serialize(resource);

		json.Should().Contain("\"orderNumber\":\"A-100\"");
		json.Should().Contain("\"currency\":\"USD\"");
	}

	[Fact]
	public void State_Returns_Null_When_The_State_Cannot_Be_Deserialized_As_The_Requested_Type()
	{
		var resource = Resource.Parse(@"{ ""total"": ""not-a-number"" }")!;

		resource.State<OrderTotals>().Should().BeNull();
	}

	[Fact]
	public void State_Of_A_Resource_Constructed_With_A_State_Object_Returns_That_Object()
	{
		var state = new OrderSummary { OrderNumber = "A-100" };
		var resource = new Resource(state);

		resource.State<OrderSummary>().Should().BeSameAs(state);
		resource.State<OrderTotals>().Should().BeNull();
		resource.State<OrderSummary>().Should().BeSameAs(state);
	}

	[Fact]
	public void As_Reflects_Links_Added_After_An_Earlier_As_Call()
	{
		var resource = new Resource();

		var before = resource.As<Dictionary<string, JsonElement>>();
		before.Should().NotBeNull();
		before!.ContainsKey("_links").Should().BeFalse();

		resource.Links.Add(TestHelpers.CreateLink("self", "/items/1"));

		var after = resource.As<Dictionary<string, JsonElement>>();
		after.Should().NotBeNull();
		after!.ContainsKey("_links").Should().BeTrue();
	}

	[Fact]
	public void As_Reflects_Embedded_Resources_Added_After_An_Earlier_As_Call()
	{
		var resource = new Resource();
		_ = resource.As<JsonNode>();

		var embedded = new EmbeddedResource("ea:order");
		embedded.Resources.Add(TestHelpers.CreateResourceWithLink("self", "/orders/1"));
		resource.Embedded.Add(embedded);

		var node = resource.As<JsonNode>();

		node.Should().NotBeNull();
		node!["_embedded"]!["ea:order"]!["_links"]!["self"]!["href"]!.ToString().Should().Be("/orders/1");
	}

	[Fact]
	public void As_Reflects_State_Mutations_Made_After_An_Earlier_As_Call()
	{
		var state = new OrderSummary { OrderNumber = "A-100" };
		var resource = new Resource(state);

		resource.As<OrderSummary>()!.OrderNumber.Should().Be("A-100");

		state.OrderNumber = "A-200";

		resource.As<OrderSummary>()!.OrderNumber.Should().Be("A-200");
	}

	[Fact]
	public void As_Of_A_Parsed_Resource_Still_Converts_The_Parsed_Document()
	{
		var resource = Resource.Parse(OrderJson)!;

		var order = resource.As<FullOrder>();
		var node = resource.As<JsonNode>();

		order.Should().NotBeNull();
		order!.OrderNumber.Should().Be("A-100");
		order.Currency.Should().Be("USD");
		node!["_links"]!["self"]!["href"]!.ToString().Should().Be("/orders/1");
	}
}
