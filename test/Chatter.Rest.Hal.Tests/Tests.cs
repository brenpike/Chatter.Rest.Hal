using Chatter.Rest.Hal.Builders;
using System.Text.Json;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

public class Tests
{
	[Fact]
	public void Json_Must_Be_Same_After_Deserialization_To_Strongly_Typed_Object()
	{
		var resource = ResourceBuilder.WithState(new { currentlyProcessing = 14, shippedToday = 20 })
			.AddSelf().AddLinkObject("/orders")
			.AddCuries().AddLinkObject("http://example.com/docs/rels/{rel}", "ea")
			.AddLink("next").AddLinkObject("/orders?page=2")
			.AddLink("ea:find").AddLinkObject("/orders{?id}").Templated()
			.AddLink("ea:admin").AddLinkObject("/admins/2").WithTitle("Fred")
								.AddLinkObject("/admins/5").WithTitle("Kate")
			.AddEmbedded("ea:order")
				.AddResource(new { total = 30.00F, currency = "USD", status = "shipped" })
					.AddSelf().AddLinkObject("/orders/123")
					.AddLink("ea:basket").AddLinkObject("/baskets/98712")
					.AddLink("ea:customer").AddLinkObject("/customers/7809")
				.AddResource(new { total = 20.00F, currency = "USD", status = "processing" })
					.AddSelf().AddLinkObject("/orders/124")
					.AddLink("ea:basket").AddLinkObject("/baskets/97213")
					.AddLink("ea:customer").AddLinkObject("/customers/12369")
			.Build();

		var serial = JsonSerializer.Serialize(resource);
		var deserial = JsonSerializer.Deserialize<OrderCollection>(serial);
		var serialAgain = JsonSerializer.Serialize(deserial);

		Assert.Equal(serial, serialAgain);
	}

	private record Object1(string P1, int P2, List<Object2> P3);
	private record Object2(string P1, int P2, bool P3, List<Object3> P4);
	private record Object3(string P1, int P2, bool P3);

	[Fact]
	public void Must_Add_Complex_Object_As_Resource_State()
	{
		var ob3one = new Object3("str1", 6, false);
		var ob2one = new Object2("str2", 2, true, new List<Object3>() { ob3one });
		var ob1one = new Object1("str3", 3, new List<Object2> { ob2one });

		var resource = ResourceBuilder.WithState(ob1one).Build();

		var serial = JsonSerializer.Serialize(resource);
		var deserial = JsonSerializer.Deserialize<Resource>(serial);

		var s1 = resource?.State<Object1>()?.ToString();
		var s2 = deserial?.State<Object1>()?.ToString();
		Assert.Equal(s1, s2);
	}

	[Fact]
	public void Must_Add_Multiple_Resources_To_EmbeddedResourceCollection()
	{
		var resource = ResourceBuilder.WithState(new { currentlyProcessing = 14, shippedToday = 20 })
			.AddEmbedded("ea:order")
				.AddResource(new { total = 30.00F, currency = "USD", status = "shipped" })
					.AddSelf().AddLinkObject("/orders/123")
				.AddResource(new { total = 20.00F, currency = "USD", status = "processing" })
					.AddSelf().AddLinkObject("/orders/124")
			.AddEmbedded("gf:adsad")
				.AddResource(new { total = 11.00F, currency = "CAD" })
					.AddSelf().AddLinkObject("/adsad/134")
			.Build();

		var serial = JsonSerializer.Serialize(resource);
		var deserial = JsonSerializer.Deserialize<Resource>(serial);
		var serialAgain = JsonSerializer.Serialize(deserial);

		Assert.Equal(serial, serialAgain);
	}

	[Fact]
	public void Resource_Must_Be_Same_After_Serialization_And_Deserialization()
	{
		var resource = ResourceBuilder.WithState(new { currentlyProcessing = 14, shippedToday = 20 })
			.AddSelf().AddLinkObject("/orders")
			.AddCuries().AddLinkObject("http://example.com/docs/rels/{rel}", "ea")
			.AddLink("next").AddLinkObject("/orders?page=2")
			.AddLink("ea:find").AddLinkObject("/orders{?id}").Templated()
			.AddLink("ea:admin").AddLinkObject("/admins/2").WithTitle("Fred")
								.AddLinkObject("/admins/5").WithTitle("Kate")
			.AddEmbedded("ea:order")
				.AddResource(new { total = 30.00F, currency = "USD", status = "shipped" })
					.AddSelf().AddLinkObject("/orders/123")
					.AddLink("ea:basket").AddLinkObject("/baskets/98712")
					.AddLink("ea:customer").AddLinkObject("/customers/7809")
				.AddResource(new { total = 20.00F, currency = "USD", status = "processing" })
					.AddSelf().AddLinkObject("/orders/124")
					.AddLink("ea:basket").AddLinkObject("/baskets/97213")
					.AddLink("ea:customer").AddLinkObject("/customers/12369")
			.Build();

		var serial = JsonSerializer.Serialize(resource);
		var deserial = JsonSerializer.Deserialize<Resource>(serial);
		var serialAgain = JsonSerializer.Serialize(deserial);

		Assert.Equal(serial, serialAgain);
	}

	[Fact]
	public void Link()
	{
		var l = new Link("next")
		{
			LinkObjects = new LinkObjectCollection()
		};
		l.LinkObjects.Add(new LinkObject("1"));
		l.LinkObjects.Add(new LinkObject("2"));

		var serial = JsonSerializer.Serialize(l);
		var deserial = JsonSerializer.Deserialize<Link>(serial);
		var serialAgain = JsonSerializer.Serialize(deserial);
		Assert.Equal(serial, serialAgain);
	}

	[Fact]
	public void LinkObject()
	{
		var lo = new LinkObject("/baskets/97213")
		{
			Deprecation = "dep",
			Name = "name"
		};

		var serial = JsonSerializer.Serialize(lo);
		var deserial = JsonSerializer.Deserialize<LinkObject>(serial);
		var serialAgain = JsonSerializer.Serialize(deserial);
		Assert.Equal(serial, serialAgain);
		Assert.Equal(lo, deserial);
	}

	[Fact]
	public void Must_Deserialize_LinkObjectCollection_With_More_Than_One_LinkObject()
	{
		var loc = new LinkObjectCollection
		{
			LinkObjectBuilder.WithHref(null, "/foo/2").BuildPart(),
			LinkObjectBuilder.WithHref(null, "/bar/4").BuildPart()
		};

		var serial = JsonSerializer.Serialize(loc);
		var deserial = JsonSerializer.Deserialize<LinkObjectCollection>(serial);
		Assert.Equal(loc!.ToString(), deserial!.ToString());
	}

	[Fact]
	public void Must_Deserialize_LinkObjectCollection_With_Exactly_One_LinkObject()
	{
		var loc = new LinkObjectCollection
		{
			LinkObjectBuilder.WithHref(null, "/bar/4").BuildPart()
		};

		var serial = JsonSerializer.Serialize(loc);
		var deserial = JsonSerializer.Deserialize<LinkObjectCollection>(serial);
		Assert.Equal(loc!.ToString(), deserial!.ToString());
	}

	[Fact]
	public void Must_Deserialize_EmbeddedResourceCollection_With_More_Than_One_EmbeddedResource()
	{
		var erc = new EmbeddedResourceCollection
		{
			EmbeddedResourceBuilder.WithName(null, "num1").BuildPart(),
			EmbeddedResourceBuilder.WithName(null, "num2").BuildPart(),
		};

		var serial = JsonSerializer.Serialize(erc);
		var deserial = JsonSerializer.Deserialize<EmbeddedResourceCollection>(serial);
		Assert.Equal(erc!.ToString(), deserial!.ToString());
	}

	[Fact]
	public void Must_Deserialize_EmbeddedResourceCollection_With_Exactly_One_EmbeddedResource()
	{
		var erc = new EmbeddedResourceCollection
		{
			EmbeddedResourceBuilder.WithName(null, "num1").BuildPart()
		};

		var serial = JsonSerializer.Serialize(erc);
		var deserial = JsonSerializer.Deserialize<EmbeddedResourceCollection>(serial);
		Assert.Equal(erc!.ToString(), deserial!.ToString());
	}

	[Fact]
	public void Must_Serialize_Resource_State_If_Exists()
	{
		var obj = new { Id = 123, Name = "Bob" };
		var res = ResourceBuilder.WithState(obj).Build();

		var serial = JsonSerializer.Serialize(res);
		var deserial = JsonSerializer.Deserialize<Resource>(serial);
		var serialAgain = JsonSerializer.Serialize(deserial);

		Assert.Equal(serial, serialAgain);
	}

	[Fact]
	public void Must_Serialize_Resource_If_No_State()
	{
		var res = ResourceBuilder.New().Build();

		var serial = JsonSerializer.Serialize(res);
		var deserial = JsonSerializer.Deserialize<Resource>(serial);
		var serialAgain = JsonSerializer.Serialize(deserial);
		Assert.Equal(serial, serialAgain);
	}

	[Fact]
	public void Must_Deserialize_LinkCollection_From_JsonObject_Of_Links()
	{
		var linkCollection = new LinkCollection()
		{
			new Link("self")
			{
				LinkObjects = new LinkObjectCollection() { new LinkObject("/foo/2")}
			},
			new Link("next"),
			new Link("last")
		};
		var serial = JsonSerializer.Serialize(linkCollection);
		var deserial = JsonSerializer.Deserialize<LinkCollection>(serial);
		var serialAgain = JsonSerializer.Serialize(deserial);
		Assert.Equal(serial, serialAgain);
	}

	[Fact]
	public void Must_Deserialize_LinkCollection_From_JsonArray_Of_Links()
	{
		var linkCollection = new LinkCollection()
		{
			new Link("self"),
			new Link("next"),
			new Link("last")
		};
		var serial = JsonSerializer.Serialize(linkCollection);
		var str = "[" + serial + "]";
		var deserial = JsonSerializer.Deserialize<LinkCollection>(str);
		var serialAgain = JsonSerializer.Serialize(deserial);
		Assert.Equal(serial, serialAgain);
	}

	[Fact]
	public void Empty_Object_Json_Should_Be_Valid_Resource()
	{
		string json = "{}";
		var deserial = JsonSerializer.Deserialize<Resource>(json);

		Assert.Equal(json, deserial!.StateObject!.ToString());
		Assert.Empty(deserial.Embedded);
		Assert.Empty(deserial.Links);
	}

	[Fact]
	public void Default_Resource_Should_Deserialize_To_Empty_Json_Object()
	{
		string json = "{}";
		var resource = ResourceBuilder.New();
		var serial = JsonSerializer.Serialize(resource);
		Assert.Equal(json, serial);
	}
}
