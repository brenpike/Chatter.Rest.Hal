using System.Text.Json;
using System.Linq;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

public class LinkBehaviorTests
{
	[Fact]
	public void Single_LinkObject_Serializes_As_Object_Not_Array()
	{
		var links = new LinkCollection();
		var link = new Link("self");
		link.LinkObjects.Add(new LinkObject("/items/1"));
		links.Add(link);

		var json = JsonSerializer.Serialize(links);

		// Expect property name 'self' with an object for the link object
		Assert.Contains("\"self\":{", json);
		Assert.Contains("\"href\":\"/items/1\"", json);
	}

	[Fact]
	public void Multiple_LinkObjects_Serializes_As_Array()
	{
		var links = new LinkCollection();
		var link = new Link("self");
		link.LinkObjects.Add(new LinkObject("/items/1"));
		link.LinkObjects.Add(new LinkObject("/items/2"));
		links.Add(link);

		var json = JsonSerializer.Serialize(links);

		// Expect the link value to be an array
		Assert.Contains("\"self\":[", json);
		Assert.Contains("/items/1", json);
		Assert.Contains("/items/2", json);
	}

	[Fact]
	public void Reading_Single_LinkObject_From_Json_Object_Works()
	{
		var json = "{ \"self\": { \"href\": \"/x\" } }";
		var links = JsonSerializer.Deserialize<LinkCollection>(json);

		Assert.NotNull(links);
		Assert.Single(links);
		var l = links!.Single();
		Assert.Equal("self", l.Rel);
		Assert.Single(l.LinkObjects);
		Assert.Equal("/x", l.LinkObjects.Single().Href);
	}

	[Fact]
	public void Reading_Link_With_Null_Value_Produces_Link_With_No_LinkObjects()
	{
		var json = "{ \"self\": null }";
		var links = JsonSerializer.Deserialize<LinkCollection>(json);

		Assert.NotNull(links);
		Assert.Single(links);
		var l = links!.Single();
		Assert.Equal("self", l.Rel);
		Assert.Empty(l.LinkObjects);
	}

	[Fact]
	public void Reading_Array_Of_Link_Objects_Works()
	{
		var json = "[ { \"self\": { \"href\": \"/a\" } }, { \"next\": { \"href\": \"/b\" } } ]";
		var links = JsonSerializer.Deserialize<LinkCollection>(json);

		Assert.NotNull(links);
		// Expect two entries: self and next
		Assert.Equal(2, links!.Count);
		Assert.Contains(links, l => l.Rel == "self");
		Assert.Contains(links, l => l.Rel == "next");
	}

	[Fact]
	public void Link_Array_Form_Is_Preserved_Through_Roundtrip()
	{
		// HAL spec Section 2.4: Servers SHOULD NOT change a relation between single-object
		// and array form across responses. This test verifies that when a link is provided
		// in array form with multiple LinkObjects, it maintains that array form through
		// serialize → deserialize → serialize cycles.

		// Step 1: Create a LinkCollection with a link containing multiple LinkObjects (array form)
		var links = new LinkCollection();
		var link = new Link("alternate");
		link.LinkObjects.Add(new LinkObject("/items/1"));
		link.LinkObjects.Add(new LinkObject("/items/2"));
		links.Add(link);

		// Step 2: Serialize to JSON
		var firstSerializedJson = JsonSerializer.Serialize(links);

		// Step 3: Deserialize back to object model
		var linksSecondPass = JsonSerializer.Deserialize<LinkCollection>(firstSerializedJson);
		Assert.NotNull(linksSecondPass);
		Assert.Single(linksSecondPass!);
		var alternateLink = linksSecondPass!.Single();
		Assert.Equal("alternate", alternateLink.Rel);
		Assert.Equal(2, alternateLink.LinkObjects.Count);

		// Step 4: Re-serialize to JSON
		var secondSerializedJson = JsonSerializer.Serialize(linksSecondPass);

		// Step 5: Verify array form is preserved throughout the round-trip.
		// The JSON should contain "alternate":[ with brackets indicating array form,
		// not "alternate":{ which would indicate object form.
		Assert.Contains("\"alternate\":[", firstSerializedJson);
		Assert.Contains("\"alternate\":[", secondSerializedJson);

		// Bonus: Verify that if we deserialize the original array-form JSON,
		// it also maintains array form on re-serialization
		var originalArrayJson = "{ \"alternate\": [ { \"href\": \"/items/1\" }, { \"href\": \"/items/2\" } ] }";
		var linksFromArray = JsonSerializer.Deserialize<LinkCollection>(originalArrayJson);
		var reserializedFromArray = JsonSerializer.Serialize(linksFromArray);
		Assert.Contains("\"alternate\":[", reserializedFromArray);
	}
}