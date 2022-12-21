using Xunit;

namespace Chatter.Rest.Hal.Tests.Extensions;

public class LinkObjectCollectionExtensionsTests
{
	[Fact]
	public void GetLinkObject_ByName_Should_Return_Exactly_One_Link_When_Relation_Matches()
	{
		var lo = new LinkObject("href") { Name = "name1" };
		var los = new LinkObjectCollection
		{
			lo,
			new LinkObject("href2") { Name = "name2" }
		};

		var result = los.GetLinkObjectOrDefault("name1");

		Assert.NotNull(result);
		Assert.True(result is LinkObject);
		Assert.Same(lo, result);
	}

	[Fact]
	public void GetLinkObject_ByName_Should_Return_Null_When_No_Matching_Name()
	{
		var los = new LinkObjectCollection
		{
			new LinkObject("href2") { Name = "name2" }
		};

		var result = los.GetLinkObjectOrDefault("name1");

		Assert.Null(result);
	}

	[Fact]
	public void GetLinkObject_ByName_Should_Throw_If_More_Than_One_Matching_Name()
	{
		var los = new LinkObjectCollection
		{
			new LinkObject("href2") { Name = "name2" },
			new LinkObject("href2") { Name = "name2" }
		};

		Assert.Throws<InvalidOperationException>(() => los.GetLinkObjectOrDefault("name2"));
	}

	[Fact]
	public void GetLinkObject_ByName_Should_Return_Null_When_Name_Is_Null()
	{
		var los = new LinkObjectCollection
		{
			new LinkObject("href2") { Name = null }
		};

		var result = los.GetLinkObjectOrDefault("name1");

		Assert.Null(result);
	}
}
