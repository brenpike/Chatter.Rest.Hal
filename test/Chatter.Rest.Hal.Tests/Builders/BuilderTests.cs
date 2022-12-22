using Chatter.Rest.Hal.Builders;
using System.Text.Json;
using Xunit;

namespace Chatter.Rest.Hal.Tests.Builders;

public class BuilderTests
{
	[Fact]
	public void test()
	{
		var orders = new List<Order>()
		{
			new Order() { Id = Guid.NewGuid().ToString(), Currency = "USD", Total = 10, Status = "shipped" },
			new Order() { Id = Guid.NewGuid().ToString(), Currency = "CAD", Total = 20, Status = "processing" },
			new Order() { Id = Guid.NewGuid().ToString(), Currency = "EUR", Total = 30, Status = "customs" },
			new Order() { Id = Guid.NewGuid().ToString(), Currency = "USD", Total = 40, Status = "shipped" },
			new Order() { Id = Guid.NewGuid().ToString(), Currency = "USD", Total = 50, Status = "complete" },
			new Order() { Id = Guid.NewGuid().ToString(), Currency = "CAD", Total = 69, Status = "nice" }
		};

		var resource = ResourceBuilder.WithState(new { currentlyProcessing = 14, shippedToday = 20 })
			.AddSelf().AddLinkObject("/orders")
			.AddCuries().AddLinkObject("http://example.com/docs/rels/{rel}", "ea")
			.AddLink("next").AddLinkObject("/orders?page=2")
			.AddLink("ea:find").AddLinkObject("/orders{?id}").Templated()
			.AddLink("ea:admin").AddLinkObject("/admins/2").WithTitle("Fred")
								.AddLinkObject("/admins/5").WithTitle("Kate")
			.AddEmbedded("ea:order")
				.AddResources(orders, (o, builder) =>
				{
					builder.AddSelf().AddLinkObject($"/orders/{o.Id}")
						   .AddLink("ea:basket").AddLinkObject("/baskets/{basketId}").Templated()
						   .AddLink("ea:customer").AddLinkObject("/customers/{custId}").Templated();
				})
			.Build();

		var json = JsonSerializer.Serialize(resource);
	}
}
