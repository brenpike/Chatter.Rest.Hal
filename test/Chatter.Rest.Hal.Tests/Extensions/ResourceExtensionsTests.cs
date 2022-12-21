using Chatter.Rest.Hal.Builders;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Extensions;

public class ResourceExtensionsTests
{
	[Fact]
	public void GetEmbeddedResources_Should_Cast_All_Resources_In_ResourceCollection_To_Type_Parameter()
	{
		var o1 = new Order() { Currency = "USD", Status = "foo", Total = 1 };
		var o2 = new Order() { Currency = "CAN", Status = "bar", Total = 2 };
		var resource = ResourceBuilder.WithState(new { currentlyProcessing = 14, shippedToday = 20 })
			.AddEmbedded("ea:order")
				.AddResource(o1)
				.AddResource(o2)
			.Build();

		var embeddedOrders = resource!.GetEmbeddedResources<Order>("ea:order");

		Assert.All(embeddedOrders, o => Assert.IsType<Order>(o));
	}

	[Fact]
	public void GetEmbeddedResources_Should_Return_Null_If_No_Embedded_Resources_Exist_With_Name()
	{
		var resource = ResourceBuilder.WithState(new { currentlyProcessing = 14, shippedToday = 20 })
			.AddEmbedded("ea:order")
				.AddResource(new Order() { Currency = "USD", Status = "foo", Total = 1 })
				.AddResource(new Order() { Currency = "CAN", Status = "bar", Total = 2 })
			.Build();

		var embeddedOrders = resource!.GetEmbeddedResources<Order>("nope!");

		Assert.Null(embeddedOrders);
	}


	[Fact]
	public void GetEmbeddedResources_Should_Return_Null_If_Embedded_Is_Null()
	{
		var resource = ResourceBuilder.WithState(new { currentlyProcessing = 14 }).Build();
		resource!.Embedded = null;

		Assert.Null(resource!.GetEmbeddedResources<Order>("foo"));
	}

	[Fact]
	public void GetEmbeddedResources_Should_Return_Null_If_Resource_Is_Null()
	{
		Assert.Null(ResourceExtensions.GetEmbeddedResources<Order>(null, "foo"));
	}

	[Fact]
	public void GetResourceCollection_Should_Return_ResourceCollection_If_EmbeddedResource_With_Name_Exists()
	{
		var resource = ResourceBuilder.WithState(new { currentlyProcessing = 14, shippedToday = 20 })
			.AddEmbedded("ea:order")
				.AddResource(new Order() { Currency = "USD", Status = "foo", Total = 1 })
				.AddResource(new Order() { Currency = "CAN", Status = "bar", Total = 2 })
			.Build();

		var resources = resource!.GetResourceCollection("ea:order");

		Assert.IsType<ResourceCollection>(resources);
		Assert.Equal(2, resources!.Count());
	}

	[Fact]
	public void GetResourceCollection_Should_Return_Null_If_No_Embedded_Resources_Exist_With_Name()
	{
		var resource = ResourceBuilder.WithState(new { currentlyProcessing = 14, shippedToday = 20 })
			.AddEmbedded("ea:order")
				.AddResource(new Order() { Currency = "USD", Status = "foo", Total = 1 })
				.AddResource(new Order() { Currency = "CAN", Status = "bar", Total = 2 })
			.Build();

		var resources = resource!.GetResourceCollection("nope!");

		Assert.Null(resources);
	}


	[Fact]
	public void GetResourceCollection_Should_Return_Null_If_Embedded_Is_Null()
	{
		var resource = ResourceBuilder.WithState(new { currentlyProcessing = 14 }).Build();
		resource!.Embedded = null;

		Assert.Null(resource!.GetResourceCollection("foo"));
	}

	[Fact]
	public void GetResourceCollection_Should_Return_Null_If_Resource_Is_Null()
	{
		Assert.Null(ResourceExtensions.GetResourceCollection(null, "foo"));
	}

	[Fact]
	public void GetLink_Should_Return_Link_If_Link_With_Relation_Exists()
	{
		var l = new Link("self") { LinkObjects = new LinkObjectCollection() { new LinkObject("/orders/1") } };
		var resource = new Resource() { Links = new LinkCollection() { l } };

		var link = resource!.GetLinkOrDefault("self");

		Assert.Same(l, link);
	}

	[Fact]
	public void GetLink_Should_Return_Null_If_No_Link_Exist_With_Relation()
	{
		var resource = ResourceBuilder.New().AddSelf().AddLinkObject("/orders/1").Build();

		var link = resource!.GetLinkOrDefault("nope!");

		Assert.Null(link);
	}

	[Fact]
	public void GetLink_Should_Return_Null_If_Links_Is_Null()
	{
		var resource = ResourceBuilder.New().Build();
		resource!.Links = null;

		Assert.Null(resource!.GetLinkOrDefault("foo"));
	}

	[Fact]
	public void GetLink_Should_Return_Null_If_Resource_Is_Null()
	{
		Assert.Null(ResourceExtensions.GetLinkOrDefault(null, "foo"));
	}

	[Fact]
	public void GetLink_Should_Throw_If_More_Than_One_Link_Matches_Relation()
	{
		var lo = new LinkObject("/orders/1") { Name = "name" };
		var l = new Link("self") { LinkObjects = new LinkObjectCollection() { lo } };
		var resource = new Resource() { Links = new LinkCollection() { l, l } };

		Assert.ThrowsAny<Exception>(() => resource.GetLinkOrDefault("self"));
	}

	[Fact]
	public void GetLinkObject_Should_Return_Link_If_LinkObject_With_Relation_And_Name_Exists()
	{
		var lo = new LinkObject("/orders/1") { Name = "name" };
		var l = new Link("self") { LinkObjects = new LinkObjectCollection() { lo } };
		var resource = new Resource() { Links = new LinkCollection() { l } };

		var linkObj = resource!.GetLinkObjectOrDefault("self", "name");

		Assert.Same(lo, linkObj);
	}

	[Fact]
	public void GetLinkObject_Should_Return_Null_If_No_LinkObject_Exist_With_Relation()
	{
		var lo = new LinkObject("/orders/1") { Name = "name" };
		var l = new Link("self") { LinkObjects = new LinkObjectCollection() { lo } };
		var resource = new Resource() { Links = new LinkCollection() { l } };

		var linkObj = resource!.GetLinkObjectOrDefault("self", "nope!");

		Assert.Null(linkObj);
	}

	[Fact]
	public void GetLinkObject_Should_Return_Null_If_No_Link_Exist_With_Relation()
	{
		var lo = new LinkObject("/orders/1") { Name = "name" };
		var l = new Link("self") { LinkObjects = new LinkObjectCollection() { lo } };
		var resource = new Resource() { Links = new LinkCollection() { l } };

		var linkObj = resource!.GetLinkObjectOrDefault("nope!", "name");

		Assert.Null(linkObj);
	}

	[Fact]
	public void GetLinkObject_Should_Return_Null_If_Links_Is_Null()
	{
		var resource = ResourceBuilder.New().Build();
		resource!.Links = null;

		Assert.Null(resource!.GetLinkObjectOrDefault("foo", "bar"));
	}

	[Fact]
	public void GetLinkObject_Should_Return_Null_If_Resource_Is_Null()
	{
		Assert.Null(ResourceExtensions.GetLinkObjectOrDefault(null, "foo", "bar"));
	}

	[Fact]
	public void GetLinkObject_Should_Throw_If_More_Than_One_LinkObject_Matches_Name()
	{
		var lo = new LinkObject("/orders/1") { Name = "name" };
		var l = new Link("self") { LinkObjects = new LinkObjectCollection() { lo, lo } };
		var resource = new Resource() { Links = new LinkCollection() { l } };

		Assert.ThrowsAny<Exception>(() => resource.GetLinkObjectOrDefault("self", "name"));
	}

	[Fact]
	public void GetLinkObject_Should_Return_Link_If_LinkObject_With_Relation_Exists()
	{
		var lo = new LinkObject("/orders/1") { Name = "name" };
		var l = new Link("self") { LinkObjects = new LinkObjectCollection() { lo } };
		var resource = new Resource() { Links = new LinkCollection() { l } };

		var linkObj = resource!.GetLinkObjectOrDefault("self");

		Assert.Same(lo, linkObj);
	}

	[Fact]
	public void GetLinkObject_Should_Return_Null_If_No_LinkObject_Exist_For_Link_With_Relation()
	{
		var lo = new LinkObject("/orders/1");
		var l = new Link("self") { LinkObjects = new LinkObjectCollection() { lo } };
		var resource = new Resource() { Links = new LinkCollection() { l } };

		var linkObj = resource!.GetLinkObjectOrDefault("nope!");

		Assert.Null(linkObj);
	}

	[Fact]
	public void GetLinkObject_Should_Return_Null_If_Links_Is_Null_2()
	{
		var resource = ResourceBuilder.New().Build();
		resource!.Links = null;

		Assert.Null(resource!.GetLinkObjectOrDefault("foo"));
	}

	[Fact]
	public void GetLinkObject_Should_Return_Null_If_Resource_Is_Null_2()
	{
		Assert.Null(ResourceExtensions.GetLinkObjectOrDefault(null, "foo"));
	}

	[Fact]
	public void GetLinkObjectCollection_Should_Return_LinkObjectCollection_If_LinkCollection_With_Relation_Exists()
	{
		var lo = new LinkObject("/orders/1") { Name = "name" };
		var loc = new LinkObjectCollection() { lo };
		var l = new Link("self") { LinkObjects =  loc };
		var resource = new Resource() { Links = new LinkCollection() { l } };
		var linkObjCol = resource!.GetLinkObjects("self");

		Assert.IsType<LinkObjectCollection>(linkObjCol);
		Assert.Single(linkObjCol);
		Assert.Same(loc, linkObjCol);
	}

	[Fact]
	public void GetLinkObjectCollection_Should_Return_Null_If_No_LinkCollection_Exist_With_Relation()
	{
		var lo = new LinkObject("/orders/1") { Name = "name" };
		var loc = new LinkObjectCollection() { lo };
		var l = new Link("self") { LinkObjects = loc };
		var resource = new Resource() { Links = new LinkCollection() { l } };

		var linkObjCol = resource!.GetLinkObjects("nope!");

		Assert.Null(linkObjCol);
	}

	[Fact]
	public void GetLinkObjectCollection_Should_Return_Null_If_Links_Is_Null()
	{
		var resource = ResourceBuilder.WithState(new { currentlyProcessing = 14 }).Build();
		resource!.Links = null;

		Assert.Null(resource!.GetLinkObjects("foo"));
	}

	[Fact]
	public void GetLinkObjectCollection_Should_Return_Null_If_Resource_Is_Null()
	{
		Assert.Null(ResourceExtensions.GetLinkObjects(null, "foo"));
	}
}
