![ci](https://github.com/brenpike/Chatter.Rest.Hal/actions/workflows/cicd.yml/badge.svg)

```json
{
    "_links": {
        "self": { "href": "/orders" },
        "curies": [{ "name": "ea", "href": "http://example.com/docs/rels/{rel}", "templated": true }],
        "next": { "href": "/orders?page=2" },
        "ea:find": {
            "href": "/orders{?id}",
            "templated": true
        },
        "ea:admin": [{
            "href": "/admins/2",
            "title": "Fred"
        }, {
            "href": "/admins/5",
            "title": "Kate"
        }]
    },
    "currentlyProcessing": 14,
    "shippedToday": 20,
    "_embedded": {
        "ea:order": [{
            "_links": {
                "self": { "href": "/orders/123" },
                "ea:basket": { "href": "/baskets/98712" },
                "ea:customer": { "href": "/customers/7809" }
            },
            "total": 30.00,
            "currency": "USD",
            "status": "shipped"
        }, {
            "_links": {
                "self": { "href": "/orders/124" },
                "ea:basket": { "href": "/baskets/97213" },
                "ea:customer": { "href": "/customers/12369" }
            },
            "total": 20.00,
            "currency": "USD",
            "status": "processing"
        }]
    }
}
```

```csharp
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
```
