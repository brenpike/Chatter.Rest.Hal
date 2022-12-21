using Xunit;

namespace Chatter.Rest.Hal.Tests.Extensions;

public class ResourceCollectionExtensionsTests
{
	[Fact]
	public void Should_Cast_All_Resources_In_ResourceCollection_To_Type_Parameter()
	{
		var resourceCollection = new ResourceCollection();
		resourceCollection.Add(new Resource(new Order() { Currency = "USD", Status = "foo", Total = 1 }));
		resourceCollection.Add(new Resource(new Order() { Currency = "CAD", Status = "bar", Total = 2 }));

		var embeddedOrders = ResourceCollectionExtensions.As<Order>(resourceCollection);

		Assert.All(embeddedOrders, o => Assert.IsType<Order>(o));
	}

	[Fact]
	public void Should_Throw_If_ResourceCollection_Is_Null()
	{
		Assert.Throws<ArgumentNullException>(() => ResourceCollectionExtensions.As<Order>(null));
	}
}
