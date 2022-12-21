using Chatter.Rest.Hal.Builders;
using System.Text.Json;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

public class ResourceTests
{
	[Fact]
	public void Resource_State_Should_Allow_Tolerant_Reader_After_Deserialization()
	{
		var resource = JsonSerializer.Deserialize<Resource>(File.ReadAllText(@"Json/ordercollection.json"));

		var oc = resource?.State<OrderCollection>();

		Assert.NotNull(oc);
		Assert.IsType<OrderCollection>(oc);
	}

	[Fact]
	public void Getting_State_Should_Only_Return_Resource_State()
	{
		var resource = JsonSerializer.Deserialize<Resource>(File.ReadAllText(@"Json/ordercollection.json"));

		var oc = resource?.State<OrderCollection>();

		Assert.NotNull(oc);
		Assert.IsType<OrderCollection>(oc);
		Assert.Equal(14, oc!.CurrentlyProcessing);
		Assert.Equal(20, oc!.ShippedToday);
		Assert.Null(oc!.EmbeddedResources);
		Assert.Null(oc!.Links);
	}

	[Fact]
	public void Should_Return_State_As_Strongly_Typed_Object_If_State_Is_Valid_JsonElement()
	{
		var json = JsonSerializer.Serialize(new OrderCollectionState { CurrentlyProcessing = 14, ShippedToday = 20 });
		var je = JsonSerializer.Deserialize<object>(json);
		var res = ResourceBuilder.WithState(je!).Build();

		var state = res!.State<OrderCollectionState>();

		Assert.NotNull(state);
		Assert.IsType<OrderCollectionState>(state);
	}

	[Fact]
	public void State_Should_Return_Null_If_State_As_JsonElement_Does_Not_Match_Type_Parameter()
	{
		var json = JsonSerializer.Serialize(new OrderCollectionState { CurrentlyProcessing = 14, ShippedToday = 20 });
		var je = JsonSerializer.Deserialize<object>(json);
		var res = ResourceBuilder.WithState(je!).Build();

		var state = res!.State<Link>();

		Assert.Null(state);
	}

	[Fact]
	public void State_Should_Return_Strongly_Typed_State_If_State_Type_Matches_Type_Parameter()
	{
		var ocs = new OrderCollectionState { CurrentlyProcessing = 14, ShippedToday = 20 };
		var res = ResourceBuilder.WithState(ocs).Build();

		var state = res!.State<OrderCollectionState>();

		Assert.Same(ocs, state);
	}

	[Fact]
	public void State_Should_Return_Null_If_State_Type_Does_Not_Match_Type_Parameter()
	{
		var res = ResourceBuilder.WithState(new { foo = 1, bar = "baz" }).Build();
		var state = res!.State<OrderCollectionState>();

		Assert.Null(state);
	}

	[Fact]
	public void State_Should_Return_Null_If_State_Creator_Throws()
	{
		var node = JsonSerializer.SerializeToNode(new { foo = 1, bar = "baz" });
		var res = new Resource(node, () => throw new Exception("error getting state"), () => new LinkCollection(), () => new EmbeddedResourceCollection());
		var state = res!.State<OrderCollectionState>();

		Assert.Null(state);
	}

	[Fact]
	public void State_Should_Striongly_Typed_State_If_StateObject_Matches_Type_Parameter()
	{
		var ocs = new OrderCollectionState { CurrentlyProcessing = 14, ShippedToday = 20 };
		var res = new Resource
		{
			StateObject = ocs
		};

		var state = res!.State<OrderCollectionState>();

		Assert.Same(ocs, state);
	}

	[Fact]
	public void State_Should_Return_Null_If_StateObject_Does_Not_Match_Type_Parameter()
	{
		var res = new Resource
		{
			StateObject = new { foo = 1, bar = "baz" }
		};

		var state = res!.State<OrderCollectionState>();

		Assert.Null(state);
	}

	[Fact]
	public void Links_Should_Return_Empty_LinksCollection_If_Resource_Has_No_Links()
	{
		var ocs = new OrderCollectionState { CurrentlyProcessing = 14, ShippedToday = 20 };
		var res = ResourceBuilder.WithState(ocs).Build()!;

		var links = res.Links;

		Assert.Empty(links);
	}

	[Fact]
	public void Links_Should_Return_Resource_Links()
	{
		var ocs = new OrderCollectionState { CurrentlyProcessing = 14, ShippedToday = 20 };
		var res = ResourceBuilder.WithState(ocs)
			.AddLink("self").AddLinkObject("blah")
			.Build()!;

		var links = res.Links;

		Assert.Single(links);
		Assert.Equal("self", links!.Single()!.Rel!);
		Assert.Equal("blah", links!.Single()!.LinkObjects!.Single()!.Href!);
	}

	[Fact]
	public void Getting_Resource_Links_Should_Throw_If_LinkCreator_Throws()
	{
		var node = JsonSerializer.SerializeToNode(new { foo = 1, bar = "baz" });
		Func<LinkCollection> linkCreator = () => throw new Exception("error getting links");
		var res = new Resource(node, () => node!.AsObject(), linkCreator, () => new EmbeddedResourceCollection());

		Assert.ThrowsAny<Exception>(() => res.Links);
	}


	[Fact]
	public void Getting_Resource_Links_Should_Not_Use_LinksCreator_If_Concrete_Links_Set()
	{
		var node = JsonSerializer.SerializeToNode(new { foo = 1, bar = "baz" });
		Func<LinkCollection> linkCreator = () => throw new Exception("error getting links");
		var res = new Resource(node, () => node!.AsObject(), linkCreator, () => new EmbeddedResourceCollection());
		var links = new LinkCollection();
		res.Links = links;

		Assert.Equal(links, res.Links);
	}


	[Fact]
	public void Getting_Resource_Links_Should_Return_New_LinkCollection_If_LinksCreator_Returns_Null()
	{
		var node = JsonSerializer.SerializeToNode(new { foo = 1, bar = "baz" });
		Func<LinkCollection?> linkCreator = () => null;
		var res = new Resource(node, () => node!.AsObject(), linkCreator, () => new EmbeddedResourceCollection());

		var links = res.Links;

		Assert.NotNull(links);
		Assert.Empty(links);
	}

	[Fact]
	public void Embedded_Should_Return_Empty_EmbeddedCollection_If_Resource_Has_No_Embedded()
	{
		var ocs = new OrderCollectionState { CurrentlyProcessing = 14, ShippedToday = 20 };
		var res = ResourceBuilder.WithState(ocs).Build()!;

		var embedded = res.Embedded;

		Assert.Empty(embedded);
	}

	[Fact]
	public void Embedded_Should_Return_Resource_EmbeddedResources()
	{
		var ocs = new OrderCollectionState { CurrentlyProcessing = 14, ShippedToday = 20 };
		var res = ResourceBuilder.WithState(ocs)
			.AddEmbedded("blah").AddResource()
			.Build()!;

		var embedded = res.Embedded;

		Assert.Single(embedded);
		Assert.Equal("blah", embedded!.Single()!.Name!);
		Assert.Single(embedded!.Single()!.Resources);
	}

	[Fact]
	public void Getting_Resource_Embedded_Should_Throw_If_EmbeddedCreator_Throws()
	{
		var node = JsonSerializer.SerializeToNode(new { foo = 1, bar = "baz" });
		Func<EmbeddedResourceCollection> embeddedCollection = () => throw new Exception("error getting Embedded");
		var res = new Resource(node, () => node!.AsObject(), () => new LinkCollection(), embeddedCollection);

		Assert.ThrowsAny<Exception>(() => res.Embedded);
	}


	[Fact]
	public void Getting_Resource_Embedded_Should_Not_Use_EmbeddedCreator_If_Concrete_Embedded_Set()
	{
		var node = JsonSerializer.SerializeToNode(new { foo = 1, bar = "baz" });
		Func<EmbeddedResourceCollection> embeddedCollection = () => throw new Exception("error getting Embedded");
		var res = new Resource(node, () => node!.AsObject(), () => new LinkCollection(), embeddedCollection);
		var embedded = new EmbeddedResourceCollection();
		res.Embedded = embedded;

		Assert.Equal(embedded, res.Embedded);
	}


	[Fact]
	public void Getting_Resource_Embedded_Should_Return_New_EmbeddedResourceollection_If_EmbeddedCreator_Returns_Null()
	{
		var node = JsonSerializer.SerializeToNode(new { foo = 1, bar = "baz" });
		Func<EmbeddedResourceCollection> embeddedCollection = () => null;
		var res = new Resource(node, () => node!.AsObject(), () => new LinkCollection(), embeddedCollection);

		var embedded = res.Embedded;

		Assert.NotNull(embedded);
		Assert.Empty(embedded);
	}

	//TODO: As<T> tests
}
