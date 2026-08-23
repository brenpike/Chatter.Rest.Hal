using System.Linq;
using Chatter.Rest.Hal.Builders;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Builders;

/// <summary>
/// Regression coverage for issue #103: AddEmbedded attached to the wrong resource once a link
/// object was in the chain, and FindParent self-matches hid a latent recursion.
/// </summary>
public class BuilderParentResolutionTests
{
	[Fact]
	public void AddEmbedded_From_LinkObject_On_Embedded_Resource_Nests_Under_That_Resource()
	{
		// Verified failing sequence from #103: adding .AddSelf().AddLinkObject(...) before
		// .AddEmbedded("items") relocated "items" to the root resource and left the order
		// resource's _embedded empty.
		var resource = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/orders")
			.AddEmbedded("orders")
				.AddResource(new { id = 1 })
					.AddSelf().AddLinkObject("/orders/1")
					.AddEmbedded("items")
						.AddResource(new { sku = "abc" })
			.Build();

		resource.Should().NotBeNull();

		// The root must carry only the "orders" embed.
		resource!.Embedded.Select(e => e.Name).Should().BeEquivalentTo(new[] { "orders" });

		// "items" must be nested under the order resource, not the root.
		var order = resource.Embedded.Single(e => e.Name == "orders").Resources.Single();
		order.Embedded.Select(e => e.Name).Should().BeEquivalentTo(new[] { "items" });
		order.Embedded.Single(e => e.Name == "items").Resources.Should().HaveCount(1);
	}

	[Fact]
	public void AddEmbedded_From_Embedded_Resource_Without_Link_Still_Nests_Under_That_Resource()
	{
		// Control for the case above: the identical chain minus the link object already nested
		// correctly and must keep doing so.
		var resource = ResourceBuilder.New()
			.AddEmbedded("orders")
				.AddResource(new { id = 1 })
					.AddEmbedded("items")
						.AddResource(new { sku = "abc" })
			.Build();

		resource.Should().NotBeNull();
		resource!.Embedded.Select(e => e.Name).Should().BeEquivalentTo(new[] { "orders" });

		var order = resource.Embedded.Single(e => e.Name == "orders").Resources.Single();
		order.Embedded.Select(e => e.Name).Should().BeEquivalentTo(new[] { "items" });
	}

	[Fact]
	public void AddEmbedded_From_LinkObject_On_Root_Resource_Nests_Under_The_Root()
	{
		var resource = ResourceBuilder.New()
			.AddSelf().AddLinkObject("/orders")
			.AddEmbedded("orders")
				.AddResource(new { id = 1 })
			.Build();

		resource.Should().NotBeNull();
		resource!.Embedded.Select(e => e.Name).Should().BeEquivalentTo(new[] { "orders" });
	}

	[Fact]
	public void FindParent_Does_Not_Return_The_Builder_Itself()
	{
		// A root resource builder has no ancestors at all, so FindParent<Resource>() must be null
		// rather than the builder itself.
		var root = (ResourceBuilder)ResourceBuilder.New();

		root.FindParent<Resource>().Should().BeNull();
	}

	[Fact]
	public void FindParent_Resource_From_ResourceCollectionBuilder_Skips_The_Collection_Builder()
	{
		// AddResources returns the ResourceCollectionBuilder, which covariantly satisfies
		// IBuildHalPart<Resource> but throws from that BuildPart(). FindParent<Resource> must walk
		// past it to the owning resource builder.
		var collectionStage = ResourceBuilder.New()
			.AddEmbedded("items")
			.AddResources(new[] { 1, 2 });

		var owner = collectionStage.FindParent<Resource>();

		owner.Should().NotBeNull();
		owner.Should().NotBeSameAs(collectionStage);
		owner!.BuildPart().Should().NotBeNull();
	}

	[Fact]
	public void AddEmbedded_From_ResourceCollectionBuilder_Attaches_To_The_Owning_Resource()
	{
		// The fallback branch in ResourceCollectionBuilder.AddEmbedded used to be unreachable dead
		// code that would have recursed forever; it must now resolve the owning resource.
		var resource = ResourceBuilder.New()
			.AddEmbedded("items")
				.AddResources(new[] { 1, 2 })
			.AddEmbedded("more")
				.AddResource(new { id = 3 })
			.Build();

		resource.Should().NotBeNull();
		resource!.Embedded.Select(e => e.Name).Should().BeEquivalentTo(new[] { "items", "more" });
		resource.Embedded.Single(e => e.Name == "items").Resources.Should().HaveCount(2);
		resource.Embedded.Single(e => e.Name == "more").Resources.Should().HaveCount(1);
	}
}
