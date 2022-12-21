using Xunit;

namespace Chatter.Rest.Hal.Tests.Extensions;

public class EmbeddedResourceCollectionExtensionsTests
{
	[Fact]
	public void GetEmbeddedResource_ByName_Should_Return_Exactly_One_EMbeddedResource_When_Name_Matches()
	{
		var er = new EmbeddedResource("sameName");
		var erc = new EmbeddedResourceCollection
		{
			er,
			new EmbeddedResource("differentName")
		};

		var result = erc.GetEmbeddedResource("sameName");

		Assert.NotNull(result);
		Assert.True(result is EmbeddedResource);
		Assert.Same(er, result);
	}

	[Fact]
	public void GetEmbeddedResource_ByName_Should_Return_Null_When_No_Matching_Name()
	{
		var er = new EmbeddedResource("sameName");
		var erc = new EmbeddedResourceCollection
		{
			er
		};

		var result = erc.GetEmbeddedResource("not a valid name");

		Assert.Null(result);
	}

	[Fact]
	public void GetEmbeddedResource_ByName_Should_Throw_If_More_Than_One_Matching_Name()
	{
		var erc = new EmbeddedResourceCollection
		{
			new EmbeddedResource("sameName"),
			new EmbeddedResource("sameName")
		};
		Assert.Throws<InvalidOperationException>(() => erc.GetEmbeddedResource("sameName"));
	}
}
