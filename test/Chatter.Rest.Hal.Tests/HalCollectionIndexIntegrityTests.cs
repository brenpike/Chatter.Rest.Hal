using System;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

/// <summary>
/// Covers the duplicate-relation policy and the relation/name index integrity of
/// <see cref="LinkCollection"/> and <see cref="EmbeddedResourceCollection"/> (issue #99).
/// </summary>
public class HalCollectionIndexIntegrityTests
{
	[Fact]
	public void LinkCollection_Add_Throws_On_Duplicate_Rel()
	{
		var links = new LinkCollection();
		links.Add(new Link("self"));

		var add = () => links.Add(new Link("self"));

		add.Should().Throw<ArgumentException>().WithMessage("*self*");
		links.Count.Should().Be(1);
	}

	[Fact]
	public void LinkCollection_Add_Duplicate_Rel_Leaves_Collection_Unchanged()
	{
		var links = new LinkCollection();
		var first = TestHelpers.CreateLink("self", "/items/1");
		links.Add(first);

		try
		{
			links.Add(TestHelpers.CreateLink("self", "/items/2"));
		}
		catch (ArgumentException)
		{
			// expected
		}

		links.Count.Should().Be(1);
		links.TryGetByRel("self", out var indexed).Should().BeTrue();
		indexed.Should().BeSameAs(first);
	}

	[Fact]
	public void LinkCollection_Add_Rel_Comparison_Is_Ordinal()
	{
		var links = new LinkCollection();
		links.Add(new Link("self"));

		links.Invoking(l => l.Add(new Link("SELF"))).Should().NotThrow();
		links.Count.Should().Be(2);
	}

	[Fact]
	public void LinkCollection_Add_Throws_On_Null()
	{
		var links = new LinkCollection();

		links.Invoking(l => l.Add(null!)).Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void LinkCollection_Remove_Keeps_Index_Consistent()
	{
		var links = new LinkCollection();
		var self = TestHelpers.CreateLink("self", "/items/1");
		var next = TestHelpers.CreateLink("next", "/items/2");
		links.Add(self);
		links.Add(next);

		links.Remove(self).Should().BeTrue();

		links.Count.Should().Be(1);
		links.TryGetByRel("self", out _).Should().BeFalse();
		links.TryGetByRel("next", out var remaining).Should().BeTrue();
		remaining.Should().BeSameAs(next);
	}

	[Fact]
	public void LinkCollection_Remove_Then_ReAdd_Same_Rel_Succeeds()
	{
		var links = new LinkCollection();
		var first = TestHelpers.CreateLink("self", "/items/1");
		links.Add(first);
		links.Remove(first);

		var second = TestHelpers.CreateLink("self", "/items/2");
		links.Invoking(l => l.Add(second)).Should().NotThrow();

		links.Count.Should().Be(1);
		links.TryGetByRel("self", out var indexed).Should().BeTrue();
		indexed!.LinkObjects[0].Href.Should().Be("/items/2");
	}

	[Fact]
	public void LinkCollection_Remove_Of_Absent_Rel_Leaves_Index_Intact()
	{
		var links = new LinkCollection();
		var self = TestHelpers.CreateLink("self", "/items/1");
		links.Add(self);

		links.Remove(TestHelpers.CreateLink("next", "/items/2")).Should().BeFalse();

		links.TryGetByRel("self", out var indexed).Should().BeTrue();
		indexed.Should().BeSameAs(self);
	}

	[Fact]
	public void LinkCollection_Clear_Empties_Index()
	{
		var links = new LinkCollection();
		links.Add(TestHelpers.CreateLink("self", "/items/1"));

		links.Clear();

		links.Count.Should().Be(0);
		links.TryGetByRel("self", out _).Should().BeFalse();
		links.Invoking(l => l.Add(TestHelpers.CreateLink("self", "/items/9"))).Should().NotThrow();
	}

	[Fact]
	public void EmbeddedResourceCollection_Add_Throws_On_Duplicate_Name()
	{
		var embedded = new EmbeddedResourceCollection();
		embedded.Add(new EmbeddedResource("ea:order"));

		var add = () => embedded.Add(new EmbeddedResource("ea:order"));

		add.Should().Throw<ArgumentException>().WithMessage("*ea:order*");
		embedded.Count.Should().Be(1);
	}

	[Fact]
	public void EmbeddedResourceCollection_Add_Throws_On_Null()
	{
		var embedded = new EmbeddedResourceCollection();

		embedded.Invoking(e => e.Add(null!)).Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void EmbeddedResourceCollection_Remove_Keeps_Index_Consistent()
	{
		var embedded = new EmbeddedResourceCollection();
		var orders = new EmbeddedResource("ea:order");
		var baskets = new EmbeddedResource("ea:basket");
		embedded.Add(orders);
		embedded.Add(baskets);

		embedded.Remove(orders).Should().BeTrue();

		embedded.Count.Should().Be(1);
		embedded.TryGetByName("ea:order", out _).Should().BeFalse();
		embedded.TryGetByName("ea:basket", out var remaining).Should().BeTrue();
		remaining.Should().BeSameAs(baskets);
	}

	[Fact]
	public void EmbeddedResourceCollection_Clear_Empties_Index()
	{
		var embedded = new EmbeddedResourceCollection();
		embedded.Add(new EmbeddedResource("ea:order"));

		embedded.Clear();

		embedded.TryGetByName("ea:order", out _).Should().BeFalse();
		embedded.Invoking(e => e.Add(new EmbeddedResource("ea:order"))).Should().NotThrow();
	}

	[Fact]
	public void Deserializing_Valid_Hal_With_Many_Rels_Still_Works()
	{
		const string json = @"{
			""_links"": {
				""self"": { ""href"": ""/orders"" },
				""next"": { ""href"": ""/orders?page=2"" },
				""curies"": [ { ""name"": ""ea"", ""href"": ""http://example.com/docs/rels/{rel}"", ""templated"": true } ],
				""ea:admin"": [ { ""href"": ""/admins/2"" }, { ""href"": ""/admins/5"" } ]
			},
			""_embedded"": {
				""ea:order"": [
					{ ""_links"": { ""self"": { ""href"": ""/orders/123"" } }, ""total"": 30.0 },
					{ ""_links"": { ""self"": { ""href"": ""/orders/124"" } }, ""total"": 20.0 }
				],
				""ea:basket"": { ""_links"": { ""self"": { ""href"": ""/baskets/98712"" } } }
			},
			""currentlyProcessing"": 14
		}";

		var resource = Resource.Parse(json);

		resource.Should().NotBeNull();
		resource!.Links.Count.Should().Be(4);
		resource.Links.Select(l => l.Rel).Should().BeEquivalentTo(new[] { "self", "next", "curies", "ea:admin" });
		resource.Links.TryGetByRel("ea:admin", out var admin).Should().BeTrue();
		admin!.LinkObjects.Count.Should().Be(2);
		resource.Embedded.Count.Should().Be(2);
		resource.Embedded.TryGetByName("ea:order", out var orders).Should().BeTrue();
		orders!.Resources.Count.Should().Be(2);
	}

	[Fact]
	public void Deserializing_Nested_Embedded_Resources_Still_Works()
	{
		const string json = @"{
			""_embedded"": {
				""parent"": {
					""id"": ""p1"",
					""_embedded"": { ""child"": { ""id"": ""c1"" } },
					""_links"": { ""self"": { ""href"": ""/parents/p1"" } }
				}
			}
		}";

		var resource = Resource.Parse(json);

		resource.Should().NotBeNull();
		resource!.Embedded.TryGetByName("parent", out var parent).Should().BeTrue();
		var child = parent!.Resources[0];
		child.Embedded.TryGetByName("child", out var grandchild).Should().BeTrue();
		grandchild!.Resources.Count.Should().Be(1);
		child.Links.TryGetByRel("self", out _).Should().BeTrue();
	}

	[Fact]
	public void RoundTrip_Of_Deserialized_Resource_Still_Works()
	{
		const string json = @"{
			""_links"": { ""self"": { ""href"": ""/items/1"" }, ""next"": { ""href"": ""/items/2"" } },
			""name"": ""widget""
		}";

		var resource = Resource.Parse(json);
		var serialized = JsonSerializer.Serialize(resource);
		var again = Resource.Parse(serialized);

		again.Should().NotBeNull();
		again!.Links.Count.Should().Be(2);
		again.Links.TryGetByRel("self", out var self).Should().BeTrue();
		self!.LinkObjects[0].Href.Should().Be("/items/1");
	}
}
