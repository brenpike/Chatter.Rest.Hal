![ci](https://github.com/brenpike/Chatter.Rest.Hal/actions/workflows/hal-cicd.yml/badge.svg) ![ci](https://github.com/brenpike/Chatter.Rest.Hal/actions/workflows/codegen-cicd.yml/badge.svg)

## Hypertext Application Language

A dotnet/c# implementation of [HAL - The Hypertext Application Language specification](https://datatracker.ietf.org/doc/html/draft-kelly-json-hal) for RESTful Web Api

> ```text
> "HAL is a simple format that gives a consistent and easy way to
> hyperlink between resources in your API.
>
> Adopting HAL will make your API explorable, and its documentation easily
> discoverable from within the API itself. In short, it will make your API
> easier to work with and therefore more attractive to client developers.
>
> APIs that adopt HAL can be easily served and consumed using open source
> libraries available for most major programming languages. It's also
> simple enough that you can just deal with it as you would any other
> JSON."
> 
>  - Mike Kelly, HAL Specification
> ```

More information regarding HAL can be found [here](https://stateless.group/hal_specification.html) or on [Mike Kelly's github](https://github.com/mikekelly/hal_specification/blob/master/hal_specification.md).

## Building a HAL Resource

### Example JSON

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

### Creating a HAL Resource leveraging the Fluent Builder

The Fluent Builder will build a HAL Resource that is compliant with the HAL Specification. The example below shows how to build a complex HAL Resource object for the JSON [above](###example-json).

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

## Deserializing a HAL Resource

### Strongly Typed Object

Deserializing `application/hal+json` content type as defined [above](###example-json) to a strongly typed `Order` object:

```csharp
public class Order
{
	[JsonPropertyName("currentlyProcessing")]
	public int CurrentlyProcessing { get; set; }
	[JsonPropertyName("shippedToday")]
	public int ShippedToday { get; set; }
	[JsonPropertyName("_links")]
	public LinkCollection? Links { get; set; }
	[JsonPropertyName("_embedded")]
	public EmbeddedResourceCollection? Embedded { get; set; }
}

...

var stronglyTypedOrder = JsonSerializer.Deserialize<Order>(halJson);
```

### Resource Object

Deserializing `application/hal+json` content type as defined [above](###example-json) to a [Resource object](https://github.com/brenpike/Chatter.Rest.Hal/blob/main/src/Resource.cs):

```csharp
public class Order
{
	[JsonPropertyName("currentlyProcessing")]
	public int CurrentlyProcessing { get; set; }
	[JsonPropertyName("shippedToday")]
	public int ShippedToday { get; set; }
}

...

var resource = JsonSerializer.Deserialize<Resource>(halJson);
```

To get a strongly typed object from the [Resource's state](https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-4):

```csharp
var stronglyTypeOrder = resource.State<Order>();
```
