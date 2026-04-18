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
}