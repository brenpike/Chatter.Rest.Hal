using Chatter.Rest.Hal.Builders;
using System.Text.Json;
using System.Text.Json.Nodes;
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
	public void State_Should_Strongly_Typed_State_If_StateObject_Matches_Type_Parameter()
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
	public void Getting_Resource_Embedded_Should_Return_New_EmbeddedResourceCollection_If_EmbeddedCreator_Returns_Null()
	{
		var node = JsonSerializer.SerializeToNode(new { foo = 1, bar = "baz" });
		Func<EmbeddedResourceCollection> embeddedCollection = () => null!;
		var res = new Resource(node, () => node!.AsObject(), () => new LinkCollection(), embeddedCollection);

		var embedded = res.Embedded;

		Assert.NotNull(embedded);
		Assert.Empty(embedded);
	}

	// --- State<T>(JsonSerializerOptions?) tests ---

	private record StateDto(string FirstName, string LastName);

	[Fact]
	public void State_Should_Respect_Custom_Options_When_Deserializing_From_JsonObject()
	{
		// Arrange: JSON uses camelCase keys; default options would fail to bind PascalCase DTO
		var json = """{"firstName":"Alice","lastName":"Smith"}""";
		var node = JsonNode.Parse(json);
		JsonObject? stateCreator() => node?.AsObject();
		var resource = new Resource(node, stateCreator, () => new LinkCollection(), () => new EmbeddedResourceCollection());
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

		// Act
		var result = resource.State<StateDto>(options);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("Alice", result!.FirstName);
		Assert.Equal("Smith", result!.LastName);
	}

	[Fact]
	public void State_Should_Apply_NamingPolicy_When_Options_Supplied()
	{
		// Arrange: camelCase JSON + camelCase naming policy
		var json = """{"firstName":"Bob","lastName":"Jones"}""";
		var node = JsonNode.Parse(json);
		JsonObject? stateCreator() => node?.AsObject();
		var resource = new Resource(node, stateCreator, () => new LinkCollection(), () => new EmbeddedResourceCollection());
		var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

		// Act
		var result = resource.State<StateDto>(options);

		// Assert: CamelCase policy means "firstName" in JSON matches "FirstName" on DTO
		Assert.NotNull(result);
		Assert.Equal("Bob", result!.FirstName);
	}

	[Fact]
	public void State_With_Null_Options_Should_Behave_Same_As_Parameterless_Overload()
	{
		// Arrange: PascalCase JSON matches PascalCase DTO with default options
		var json = """{"FirstName":"Carol","LastName":"White"}""";
		var node = JsonNode.Parse(json);
		JsonObject? stateCreator() => node?.AsObject();
		var r1 = new Resource(node, stateCreator, () => new LinkCollection(), () => new EmbeddedResourceCollection());
		var r2 = new Resource(node, stateCreator, () => new LinkCollection(), () => new EmbeddedResourceCollection());

		// Act
		var withNull = r1.State<StateDto>(null);
		var parameterless = r2.State<StateDto>();

		// Assert
		Assert.NotNull(withNull);
		Assert.NotNull(parameterless);
		Assert.Equal(withNull!.FirstName, parameterless!.FirstName);
		Assert.Equal(withNull.LastName, parameterless.LastName);
	}

	[Fact]
	public void State_Should_Return_Cached_Object_Regardless_Of_Options_After_First_Deserialization()
	{
		// Arrange
		var json = """{"FirstName":"Dave","LastName":"Brown"}""";
		var node = JsonNode.Parse(json);
		JsonObject? stateCreator() => node?.AsObject();
		var resource = new Resource(node, stateCreator, () => new LinkCollection(), () => new EmbeddedResourceCollection());

		// Act: first call caches the result
		var first = resource.State<StateDto>();
		// Second call with different options should return the same cached object
		var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
		var second = resource.State<StateDto>(options);

		// Assert: same reference (caching)
		Assert.Same(first, second);
	}

	[Fact]
	public void State_Should_Pass_Options_To_Deserialize_When_State_Is_JsonElement()
	{
		// Arrange: simulate a Resource whose _stateObject starts as a JsonElement
		// Build a resource via JSON deserialization with custom options so _stateObject is JsonElement
		var halJson = """{"firstName":"Eve","lastName":"Green"}""";
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		// Deserialize through the converter so _stateObject starts as the raw JsonObject (not yet typed)
		var resource = JsonSerializer.Deserialize<Resource>(halJson, options);
		Assert.NotNull(resource);

		// Act
		var result = resource!.State<StateDto>(options);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("Eve", result!.FirstName);
	}

	// --- Parse() tests ---

	private const string SimpleHalJson = """
		{
			"firstName": "Test",
			"lastName": "User",
			"_links": {
				"self": { "href": "/users/1" }
			}
		}
		""";

	[Fact]
	public void Resource_Parse_Should_Return_Resource_With_Links_Embedded_And_State()
	{
		// Arrange
		var json = """
			{
				"firstName": "Alice",
				"_links": { "self": { "href": "/users/1" } },
				"_embedded": { "orders": { "_links": { "self": { "href": "/orders/1" } } } }
			}
			""";

		// Act
		var resource = Resource.Parse(json);

		// Assert
		Assert.NotNull(resource);
		Assert.True(resource!.Links.Count > 0);
		Assert.True(resource.Embedded.Count > 0);
	}

	[Fact]
	public void Resource_Parse_Should_Thread_Options_Into_State_Deserialization()
	{
		// Arrange: camelCase JSON
		var json = """{"firstName":"Alice","lastName":"Smith"}""";
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

		// Act
		var resource = Resource.Parse(json, options);
		Assert.NotNull(resource);
		var state = resource!.State<StateDto>();

		// Assert: State<T>() (parameterless) should use the threaded options
		Assert.NotNull(state);
		Assert.Equal("Alice", state!.FirstName);
	}

	[Fact]
	public void Resource_Parse_Should_Return_Empty_Resource_For_Empty_Object()
	{
		// Act
		var resource = Resource.Parse("{}");

		// Assert
		Assert.NotNull(resource);
		Assert.Empty(resource!.Links);
		Assert.Empty(resource.Embedded);
		// Empty JSON object deserializes to a StateDto with null property values (no matching keys)
		var state = resource.State<StateDto>();
		Assert.NotNull(state);
		Assert.Null(state!.FirstName);
		Assert.Null(state.LastName);
	}

	[Fact]
	public void Resource_Parse_Should_Return_Null_For_Null_Json()
	{
		// Act
		var resource = Resource.Parse("null");

		// Assert
		Assert.Null(resource);
	}

	[Fact]
	public void Resource_Parse_Should_Throw_For_Invalid_Json()
	{
		// Act & Assert
		Assert.ThrowsAny<Exception>(() => Resource.Parse("not valid json {{"));
	}

}
