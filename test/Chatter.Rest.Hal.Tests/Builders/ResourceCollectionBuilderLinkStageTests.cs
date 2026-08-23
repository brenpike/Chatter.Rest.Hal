using System;
using System.Linq;
using Chatter.Rest.Hal.Builders;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Builders;

/// <summary>
/// Regression coverage for issue #102: AddLink/AddSelf/AddCuries after AddResources threw
/// NullReferenceException because the lookup they used could never resolve.
/// </summary>
public class ResourceCollectionBuilderLinkStageTests
{
	[Fact]
	public void AddLink_After_AddResources_Adds_The_Link_To_The_Owning_Resource()
	{
		var resource = ResourceBuilder.New()
			.AddEmbedded("items")
				.AddResources(new[] { 1, 2 })
			.AddLink("next").AddLinkObject("/items?page=2")
			.Build();

		resource.Should().NotBeNull();

		var next = resource!.Links.SingleOrDefault(l => l.Rel == "next");
		next.Should().NotBeNull();
		next!.LinkObjects.Single().Href.Should().Be("/items?page=2");

		// The embedded collection itself is untouched.
		resource.Embedded.Single(e => e.Name == "items").Resources.Should().HaveCount(2);
	}

	[Fact]
	public void AddSelf_After_AddResources_Adds_The_Self_Link_To_The_Owning_Resource()
	{
		var resource = ResourceBuilder.New()
			.AddEmbedded("items")
				.AddResources(new[] { 1, 2 })
			.AddSelf().AddLinkObject("/items")
			.Build();

		resource.Should().NotBeNull();

		var self = resource!.Links.SingleOrDefault(l => l.Rel == "self");
		self.Should().NotBeNull();
		self!.LinkObjects.Single().Href.Should().Be("/items");
	}

	[Fact]
	public void AddCuries_After_AddResources_Adds_The_Curies_Link_To_The_Owning_Resource()
	{
		var resource = ResourceBuilder.New()
			.AddEmbedded("items")
				.AddResources(new[] { 1, 2 })
			.AddCuries().AddLinkObject("http://example.com/docs/{rel}", "ex")
			.Build();

		resource.Should().NotBeNull();

		var curies = resource!.Links.SingleOrDefault(l => l.Rel == "curies");
		curies.Should().NotBeNull();

		var definition = curies!.LinkObjects.Single();
		definition.Name.Should().Be("ex");
		definition.Href.Should().Be("http://example.com/docs/{rel}");
		definition.Templated.Should().BeTrue();
	}

	[Fact]
	public void Link_Stages_After_AddResources_Do_Not_Throw_NullReferenceException()
	{
		// The three verified failing call sequences from #102, each of which used to throw.
		Action addLink = () => ResourceBuilder.New().AddEmbedded("items").AddResources(new[] { 1, 2 }).AddLink("next");
		Action addSelf = () => ResourceBuilder.New().AddEmbedded("items").AddResources(new[] { 1, 2 }).AddSelf();
		Action addCuries = () => ResourceBuilder.New().AddEmbedded("items").AddResources(new[] { 1, 2 }).AddCuries();

		addLink.Should().NotThrow();
		addSelf.Should().NotThrow();
		addCuries.Should().NotThrow();
	}

	[Fact]
	public void Link_Stages_After_AddResources_On_A_Nested_Collection_Target_The_Nested_Resource()
	{
		// The link must attach to the resource that owns the "_embedded" entry being filled, not
		// to the document root.
		var resource = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/orders")
			.AddEmbedded("orders")
				.AddResource(new { id = 1 })
					.AddEmbedded("items")
						.AddResources(new[] { 1, 2 })
					.AddLink("next").AddLinkObject("/orders/1/items?page=2")
			.Build();

		resource.Should().NotBeNull();

		// The root keeps only its own self link.
		resource!.Links.Select(l => l.Rel).Should().BeEquivalentTo(new[] { "self" });

		var order = resource.Embedded.Single(e => e.Name == "orders").Resources.Single();
		order.Links.Select(l => l.Rel).Should().BeEquivalentTo(new[] { "next" });
		order.Links.Single().LinkObjects.Single().Href.Should().Be("/orders/1/items?page=2");
	}

	[Fact]
	public void Per_Resource_Links_Are_Still_Configured_Through_The_AddResources_Callback()
	{
		var resource = ResourceBuilder.New()
			.AddEmbedded("items")
				.AddResources(new[] { 1, 2 }, (i, b) => b.AddSelf().AddLinkObject($"/items/{i}"))
			.Build();

		resource.Should().NotBeNull();

		var items = resource!.Embedded.Single(e => e.Name == "items").Resources;
		items.Should().HaveCount(2);
		items.Select(r => r.Links.Single(l => l.Rel == "self").LinkObjects.Single().Href)
			 .Should().BeEquivalentTo(new[] { "/items/1", "/items/2" });
	}
}
