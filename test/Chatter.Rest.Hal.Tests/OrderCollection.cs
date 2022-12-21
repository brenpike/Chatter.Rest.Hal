using System.Text.Json.Serialization;

namespace Chatter.Rest.Hal.Tests;

public partial class OrderCollection
{
	[JsonPropertyName("currentlyProcessing")]
	public int CurrentlyProcessing { get; set; }
	[JsonPropertyName("shippedToday")]
	public int ShippedToday { get; set; }
	[JsonPropertyName("_links")]
	public LinkCollection? Links { get; set; }
	[JsonPropertyName("_embedded")]
	public EmbeddedResourceCollection? EmbeddedResources { get; set; }
}

public partial class Order
{
	[JsonPropertyName("_links")]
	public LinkCollection? Links { get; set; }

	[JsonPropertyName("total")]
	public decimal? Total { get; set; }

	[JsonPropertyName("currency")]
	public string? Currency { get; set; }

	[JsonPropertyName("status")]
	public string? Status { get; set; }
}

public partial class OrderCollectionState
{
	[JsonPropertyName("currentlyProcessing")]
	public int CurrentlyProcessing { get; set; }
	[JsonPropertyName("shippedToday")]
	public int ShippedToday { get; set; }
}


