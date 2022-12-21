using Xunit;

namespace Chatter.Rest.Hal.Tests.Extensions;

public class LinkCollectionExtensionsTests
{
	[Fact]
	public void GetLink_ByRelation_Should_Return_Exactly_One_Link_When_Relation_Matches()
	{
		var link = new Link("rel1");
		var links = new LinkCollection
		{
			link,
			new Link("rel2")
		};

		var result = links.GetLinkOrDefault("rel1");

		Assert.NotNull(result);
		Assert.True(result is Link);
		Assert.Same(link, result);
	}

	[Fact]
	public void GetLink_ByRelation_Should_Return_Null_When_No_Matching_Relation()
	{
		var link = new Link("rel1");
		var links = new LinkCollection
		{
			link
		};

		var result = links.GetLinkOrDefault("rel2");

		Assert.Null(result);
	}

	[Fact]
	public void GetLink_ByRelation_Should_Throw_If_More_Than_One_Matching_Relation()
	{
		var links = new LinkCollection
		{
			new Link("rel2"),
			new Link("rel2")
		};
		Assert.Throws<InvalidOperationException>(() => links.GetLinkOrDefault("rel2"));
	}
}
