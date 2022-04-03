using Chatter.Rest.Hal.Builders;
using System.Text.Json;
using Xunit;

namespace Chatter.Rest.Hal.Tests;

public class Tests
{
	//	{
	//    "_links": {
	//        "self": { "href": "/orders" },
	//        "curies": [{ "name": "ea", "href": "http://example.com/docs/rels/{rel}", "templated": true }],
	//        "next": { "href": "/orders?page=2" },
	//        "ea:find": {
	//	"href": "/orders{?id}",
	//            "templated": true
	//		},
	//        "ea:admin": [{
	//            "href": "/admins/2",
	//            "title": "Fred"
	//        }, {
	//			  "href": "/admins/5",
	//            "title": "Kate"
	//		}]
	//    },
	//    "currentlyProcessing": 14,
	//    "shippedToday": 20,
	//    "_embedded": {
	//	"ea:order": [{
	//		"_links": {
	//			"self": { "href": "/orders/123" },
	//                "ea:basket": { "href": "/baskets/98712" },
	//                "ea:customer": { "href": "/customers/7809" }
	//		},
	//            "total": 30.00,
	//            "currency": "USD",
	//            "status": "shipped"

	//		}, {
	//		"_links": {
	//			"self": { "href": "/orders/124" },
	//                "ea:basket": { "href": "/baskets/97213" },
	//                "ea:customer": { "href": "/customers/12369" }
	//		},
	//            "total": 20.00,
	//            "currency": "USD",
	//            "status": "processing"

	//		}]
	//    }
	//}

	[Fact]
	public void test1()
	{
		var resource = ResourceBuilder.New(new { currentlyProcessing = 14, shippedToday = 20 })
			.AddLink(LinkBuilder.Self()
				.AddLinkObject(LinkObjectBuilder.WithHref("/orders")))
			.AddLink(LinkBuilder.Curies()
				.AddLinkObject(LinkObjectBuilder.WithHref("http://example.com/docs/rels/{rel}")
					.WithName("ea")
					.Templated()))
			.AddLink(LinkBuilder.WithRel("next")
				.AddLinkObject(LinkObjectBuilder.WithHref("/orders?page=2")))
			.AddLink(LinkBuilder.WithRel("ea:find")
				.AddLinkObject(LinkObjectBuilder.WithHref("/orders{?id}")
					.Templated()))
			.AddLink(LinkBuilder.WithRel("ea:admin")
				.AddLinkObject(LinkObjectBuilder.WithHref("/admins/2")
					.WithTitle("Fred"))
				.AddLinkObject(LinkObjectBuilder.WithHref("/admins/5")
					.WithTitle("Kate")))
			.AddEmbedded(EmbeddedResourceBuilder.WithName("ea:order")
				.AddResource(ResourceBuilder.New(new { total = 30.00F, currency = "USD", status = "shipped" })
					.AddLink(LinkBuilder.Self()
						.AddLinkObject(LinkObjectBuilder.WithHref("/orders/123")))
					.AddLink(LinkBuilder.WithRel("ea:basket")
						.AddLinkObject(LinkObjectBuilder.WithHref("/baskets/98712")))
					.AddLink(LinkBuilder.WithRel("ea:customer")
						.AddLinkObject(LinkObjectBuilder.WithHref("/customers/7809"))))
				.AddResource(ResourceBuilder.New(new { total = 20.00F, currency = "USD", status = "processing" })
					.AddLink(LinkBuilder.Self()
						.AddLinkObject(LinkObjectBuilder.WithHref("/orders/124")))
					.AddLink(LinkBuilder.WithRel("ea:basket")
						.AddLinkObject(LinkObjectBuilder.WithHref("/baskets/97213")))
					.AddLink(LinkBuilder.WithRel("ea:customer")
						.AddLinkObject(LinkObjectBuilder.WithHref("/customers/12369")))))
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
	}

	[Fact]
	public void Must_Deserialize_LinkObjectCollection_With_More_Than_One_LinkObject()
	{
		var loc = new LinkObjectCollection();
		loc.Add(LinkObjectBuilder.WithHref("/foo/2").Build());
		loc.Add(LinkObjectBuilder.WithHref("/bar/4").Build());

		var serial = JsonSerializer.Serialize(loc);
		var deserial = JsonSerializer.Deserialize<LinkObjectCollection>(serial);
		var serialAgain = JsonSerializer.Serialize(deserial);
		Assert.Equal(serial, serialAgain);
	}

	[Fact]
	public void Must_Deserialize_LinkObjectCollection_With_Exactly_One_LinkObject()
	{
		var loc = new LinkObjectCollection();
		loc.Add(LinkObjectBuilder.WithHref("/foo/2").Build());

		var serial = JsonSerializer.Serialize(loc);
		var deserial = JsonSerializer.Deserialize<LinkObjectCollection>(serial);
		var serialAgain = JsonSerializer.Serialize(deserial);
		Assert.Equal(serial, serialAgain);
	}

	[Fact]
	public void Must_Deserialize_EmbeddedResourceCollection_With_More_Than_One_EmbeddedResource()
	{
		var erc = new EmbeddedResourceCollection();
		erc.Add(EmbeddedResourceBuilder.WithName("num1")
					.AddResource(ResourceBuilder.New(new { Id = 123 }))
				.Build());
		erc.Add(EmbeddedResourceBuilder.WithName("num2")
					.AddResource(ResourceBuilder.New(new { Id = 234, Name = "Steve" }))
				.Build());

		var serial = JsonSerializer.Serialize(erc);
		var deserial = JsonSerializer.Deserialize<EmbeddedResourceCollection>(serial);
		var serialAgain = JsonSerializer.Serialize(deserial);
		Assert.Equal(serial, serialAgain);
	}

	[Fact]
	public void Must_Deserialize_EmbeddedResourceCollection_With_Exactly_One_EmbeddedResource()
	{
		var erc = new EmbeddedResourceCollection();
		erc.Add(EmbeddedResourceBuilder.WithName("num1")
					.AddResource(ResourceBuilder.New(new { Id = 234, Name = "Steve" }))
				.Build());

		var serial = JsonSerializer.Serialize(erc);
		var deserial = JsonSerializer.Deserialize<EmbeddedResourceCollection>(serial);
		var serialAgain = JsonSerializer.Serialize(deserial);
		Assert.Equal(serial, serialAgain);
	}

	[Fact]
	public void Must_Serialize_Resource_State_If_Exists()
	{
		var obj = new { Id = 123, Name = "Bob" };
		var res = ResourceBuilder.New(obj).Build();

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
	public void LinkCollection()
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
	public void Must_Deserialize_LinkCollection_From_Collection_Of_Links()
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
}
