using System.Collections.Generic;
using System.Text.Json.Serialization;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

/// <summary>
/// Covers structural equality of the HAL domain types and the stability of
/// <see cref="Resource.GetHashCode"/> across cache-populating property reads (issue #100).
/// </summary>
public class HalDomainEqualityTests
{
	private sealed class WidgetState
	{
		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("size")]
		public int Size { get; set; }
	}

	[Fact]
	public void Resource_HashCode_Is_Stable_Across_Getter_Reads()
	{
		var resource = TestHelpers.CreateResourceWithLink("self", "/items/1");
		var before = resource.GetHashCode();

		_ = resource.Links;
		_ = resource.Embedded;
		_ = resource.State<WidgetState>();
		_ = resource.As<Dictionary<string, object>>();

		resource.GetHashCode().Should().Be(before);
	}

	[Fact]
	public void Resource_Stays_Findable_In_HashSet_After_Reading_Links()
	{
		var resource = new Resource();
		var set = new HashSet<Resource> { resource };

		_ = resource.Links;
		_ = resource.Embedded;

		set.Contains(resource).Should().BeTrue();
	}

	[Fact]
	public void Parsed_Resource_HashCode_Is_Stable_Across_State_Reads()
	{
		const string json = @"{ ""_links"": { ""self"": { ""href"": ""/items/1"" } }, ""name"": ""widget"", ""size"": 3 }";
		var resource = Resource.Parse(json)!;
		var set = new HashSet<Resource> { resource };
		var before = resource.GetHashCode();

		resource.State<WidgetState>()!.Name.Should().Be("widget");
		_ = resource.Links;
		_ = resource.Embedded;

		resource.GetHashCode().Should().Be(before);
		set.Contains(resource).Should().BeTrue();
	}

	[Fact]
	public void Parsed_Resources_With_Same_Content_Are_Equal()
	{
		const string json = @"{ ""_links"": { ""self"": { ""href"": ""/items/1"" } }, ""name"": ""widget"" }";

		var first = Resource.Parse(json)!;
		var second = Resource.Parse(json)!;

		first.Should().Be(second);
		first.GetHashCode().Should().Be(second.GetHashCode());
	}

	[Fact]
	public void Resources_With_Different_State_Are_Not_Equal()
	{
		var first = Resource.Parse(@"{ ""name"": ""widget"" }")!;
		var second = Resource.Parse(@"{ ""name"": ""gadget"" }")!;

		first.Should().NotBe(second);
	}

	[Fact]
	public void Resources_With_Different_Links_Are_Not_Equal()
	{
		var first = TestHelpers.CreateResourceWithLink("self", "/items/1");
		var second = TestHelpers.CreateResourceWithLink("self", "/items/2");

		first.Should().NotBe(second);
	}

	[Fact]
	public void Resource_Mutation_Changes_Equality()
	{
		var first = new Resource();
		var second = new Resource();
		first.Should().Be(second);

		first.Links.Add(TestHelpers.CreateLink("self", "/items/1"));

		first.Should().NotBe(second);
	}

	[Fact]
	public void Resource_With_Expression_Is_Still_Supported()
	{
		var resource = TestHelpers.CreateResourceWithLink("self", "/items/1");

		var copy = resource with { };

		copy.Should().Be(resource);
		copy.Should().NotBeSameAs(resource);
	}

	[Fact]
	public void Link_Equality_Is_Structural()
	{
		var first = TestHelpers.CreateLink("self", "/items/1");
		var second = TestHelpers.CreateLink("self", "/items/1");

		first.Should().Be(second);
		first.GetHashCode().Should().Be(second.GetHashCode());
	}

	[Fact]
	public void Link_Equality_Distinguishes_Rel_And_LinkObjects()
	{
		TestHelpers.CreateLink("self", "/items/1").Should().NotBe(TestHelpers.CreateLink("next", "/items/1"));
		TestHelpers.CreateLink("self", "/items/1").Should().NotBe(TestHelpers.CreateLink("self", "/items/2"));

		var withArray = TestHelpers.CreateLink("self", "/items/1");
		withArray.IsArray = true;
		withArray.Should().NotBe(TestHelpers.CreateLink("self", "/items/1"));
	}

	[Fact]
	public void LinkCollection_Remove_Finds_A_Structurally_Equal_Link()
	{
		var links = new LinkCollection();
		links.Add(new Link("self"));

		links.Remove(new Link("self")).Should().BeTrue();

		links.Count.Should().Be(0);
		links.TryGetByRel("self", out _).Should().BeFalse();
	}

	[Fact]
	public void LinkCollection_Contains_Finds_A_Structurally_Equal_Link()
	{
		var links = new LinkCollection();
		links.Add(TestHelpers.CreateLink("self", "/items/1"));

		links.Contains(TestHelpers.CreateLink("self", "/items/1")).Should().BeTrue();
		links.Contains(TestHelpers.CreateLink("self", "/items/2")).Should().BeFalse();
	}

	[Fact]
	public void LinkCollection_Equality_Ignores_Relation_Order()
	{
		var first = new LinkCollection
		{
			TestHelpers.CreateLink("self", "/items/1"),
			TestHelpers.CreateLink("next", "/items/2")
		};
		var second = new LinkCollection
		{
			TestHelpers.CreateLink("next", "/items/2"),
			TestHelpers.CreateLink("self", "/items/1")
		};

		first.Equals(second).Should().BeTrue();
		first.GetHashCode().Should().Be(second.GetHashCode());
	}

	[Fact]
	public void LinkCollection_Equality_Distinguishes_Content()
	{
		var first = new LinkCollection { TestHelpers.CreateLink("self", "/items/1") };
		var second = new LinkCollection { TestHelpers.CreateLink("self", "/items/2") };
		var third = new LinkCollection();

		first.Equals(second).Should().BeFalse();
		first.Equals(third).Should().BeFalse();
		first.Equals(null).Should().BeFalse();
	}

	[Fact]
	public void LinkObjectCollection_Equality_Is_Order_Sensitive()
	{
		var first = new LinkObjectCollection
		{
			TestHelpers.CreateLinkObject("/items/1"),
			TestHelpers.CreateLinkObject("/items/2")
		};
		var same = new LinkObjectCollection
		{
			TestHelpers.CreateLinkObject("/items/1"),
			TestHelpers.CreateLinkObject("/items/2")
		};
		var reordered = new LinkObjectCollection
		{
			TestHelpers.CreateLinkObject("/items/2"),
			TestHelpers.CreateLinkObject("/items/1")
		};

		first.Equals(same).Should().BeTrue();
		first.GetHashCode().Should().Be(same.GetHashCode());
		first.Equals(reordered).Should().BeFalse();
	}

	[Fact]
	public void ResourceCollection_Equality_Is_Structural_And_Order_Sensitive()
	{
		var first = new ResourceCollection
		{
			TestHelpers.CreateResourceWithLink("self", "/items/1"),
			TestHelpers.CreateResourceWithLink("self", "/items/2")
		};
		var same = new ResourceCollection
		{
			TestHelpers.CreateResourceWithLink("self", "/items/1"),
			TestHelpers.CreateResourceWithLink("self", "/items/2")
		};
		var reordered = new ResourceCollection
		{
			TestHelpers.CreateResourceWithLink("self", "/items/2"),
			TestHelpers.CreateResourceWithLink("self", "/items/1")
		};

		first.Equals(same).Should().BeTrue();
		first.GetHashCode().Should().Be(same.GetHashCode());
		first.Equals(reordered).Should().BeFalse();
	}

	[Fact]
	public void EmbeddedResourceCollection_Equality_Ignores_Name_Order()
	{
		var first = new EmbeddedResourceCollection
		{
			CreateEmbedded("ea:order", "/orders/1"),
			CreateEmbedded("ea:basket", "/baskets/1")
		};
		var second = new EmbeddedResourceCollection
		{
			CreateEmbedded("ea:basket", "/baskets/1"),
			CreateEmbedded("ea:order", "/orders/1")
		};

		first.Equals(second).Should().BeTrue();
		first.GetHashCode().Should().Be(second.GetHashCode());
	}

	[Fact]
	public void EmbeddedResource_Equality_Is_Structural()
	{
		var first = CreateEmbedded("ea:order", "/orders/1");
		var second = CreateEmbedded("ea:order", "/orders/1");
		var different = CreateEmbedded("ea:order", "/orders/2");

		first.Should().Be(second);
		first.GetHashCode().Should().Be(second.GetHashCode());
		first.Should().NotBe(different);
	}

	[Fact]
	public void Embedded_Resources_Participate_In_Resource_Equality()
	{
		var first = new Resource();
		first.Embedded.Add(CreateEmbedded("ea:order", "/orders/1"));
		var second = new Resource();
		second.Embedded.Add(CreateEmbedded("ea:order", "/orders/1"));
		var different = new Resource();
		different.Embedded.Add(CreateEmbedded("ea:order", "/orders/2"));

		first.Should().Be(second);
		first.GetHashCode().Should().Be(second.GetHashCode());
		first.Should().NotBe(different);
	}

	private static EmbeddedResource CreateEmbedded(string name, string href)
	{
		var embedded = new EmbeddedResource(name);
		embedded.Resources.Add(TestHelpers.CreateResourceWithLink("self", href));
		return embedded;
	}
}
