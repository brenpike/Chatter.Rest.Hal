using FluentAssertions;
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

	[Fact]
	public void ExpandCurieRelation_Should_Return_Full_Uri_When_Curie_Exists()
	{
		// Arrange: Create a LinkCollection with a CURIE definition
		// The CURIE "acme:widgets" should expand to "https://docs.acme.com/relations/widgets"
		var curieLink = new Link("curies");
		var curieDefinition = new LinkObject("https://docs.acme.com/relations/{rel}")
		{
			Templated = true,
			Name = "acme"
		};
		curieLink.LinkObjects.Add(curieDefinition);

		var links = new LinkCollection { curieLink };

		// Act
		var result = links.ExpandCurieRelation("acme:widgets");

		// Assert
		result.Should().Be("https://docs.acme.com/relations/widgets");
	}

	[Fact]
	public void ExpandCurieRelation_Should_Return_Original_When_No_Colon()
	{
		// Arrange: Create a LinkCollection with a CURIE definition
		var curieLink = new Link("curies");
		var curieDefinition = new LinkObject("https://docs.acme.com/relations/{rel}")
		{
			Templated = true,
			Name = "acme"
		};
		curieLink.LinkObjects.Add(curieDefinition);

		var links = new LinkCollection { curieLink };

		// Act: Pass a relation without a colon separator
		// This is not a CURIE format and should return unchanged
		var result = links.ExpandCurieRelation("widgets");

		// Assert
		result.Should().Be("widgets");
	}

	[Fact]
	public void ExpandCurieRelation_Should_Return_Original_When_Curie_Undefined()
	{
		// Arrange: Create a LinkCollection with a CURIE definition for "acme"
		var curieLink = new Link("curies");
		var curieDefinition = new LinkObject("https://docs.acme.com/relations/{rel}")
		{
			Templated = true,
			Name = "acme"
		};
		curieLink.LinkObjects.Add(curieDefinition);

		var links = new LinkCollection { curieLink };

		// Act: Try to expand a CURIE with an undefined prefix "other"
		// Since "other" is not defined, the original relation should be returned
		var result = links.ExpandCurieRelation("other:widgets");

		// Assert
		result.Should().Be("other:widgets");
	}

	[Fact]
	public void ExpandCurieRelation_Should_Handle_Empty_Suffix()
	{
		// Arrange: Create a LinkCollection with a CURIE definition
		var curieLink = new Link("curies");
		var curieDefinition = new LinkObject("https://docs.acme.com/relations/{rel}")
		{
			Templated = true,
			Name = "acme"
		};
		curieLink.LinkObjects.Add(curieDefinition);

		var links = new LinkCollection { curieLink };

		// Act: Expand a CURIE with an empty suffix (trailing colon only)
		// The template should be expanded with an empty string
		var result = links.ExpandCurieRelation("acme:");

		// Assert
		result.Should().Be("https://docs.acme.com/relations/");
	}
}
