![ci](https://github.com/brenpike/Chatter.Rest.Hal/actions/workflows/hal-cicd.yml/badge.svg) ![ci](https://github.com/brenpike/Chatter.Rest.Hal/actions/workflows/codegen-cicd.yml/badge.svg)

- [Hypertext Application Language](#hypertext-application-language)
- [Using the Library](#using-the-library)
- [Building a HAL Resource](#building-a-hal-resource)
  - [Example JSON](#example-json)
  - [Creating a HAL Resource leveraging the Fluent Builder](#creating-a-hal-resource-leveraging-the-fluent-builder)
  - [Creating a HAL Resource dynamically](#creating-a-hal-resource-dynamically)
- [Deserializing a HAL Resource](#deserializing-a-hal-resource)
  - [Strongly Typed Object](#strongly-typed-object)
  - [Resource Object](#resource-object)
  - [Strongly Typed Object + Source Generator](#strongly-typed-object--source-generator)
- [Accessing data in a HAL Resource](#accessing-data-in-a-hal-resource)
  - [Get strongly typed embedded resources](#get-strongly-typed-embedded-resources)
  - [Get a Resource Collection](#get-a-resource-collection)
  - [Get a Link by relation](#get-a-link-by-relation)
  - [Get all Link Objects of a Link by relation](#get-all-link-objects-of-a-link-by-relation)
  - [Get a Link Object of a Link by relation and Link Object name](#get-a-link-object-of-a-link-by-relation-and-link-object-name)
  - [Get a Link Object of a Link by relation only](#get-a-link-object-of-a-link-by-relation-only)

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

## Using the Library

The libraries can be found on nuget:

- Core library can be found at [https://www.nuget.org/packages/Chatter.Rest.Hal](https://www.nuget.org/packages/Chatter.Rest.Hal.CodeGenerators)
- Code generation library can be found at [https://www.nuget.org/packages/Chatter.Rest.Hal.CodeGenerators](https://www.nuget.org/packages/Chatter.Rest.Hal.CodeGenerators)

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

### Creating a HAL Resource dynamically

In most cases, your resource state and embedded resources will be strongly typed objects retrieved from a datastore and links will be calculated values based on these objects. The example below shows how the fluent builder can be used to construct a valid HAL object dynamically.

```csharp
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
```

Below is the resulting JSON:

```json
{
    "currentlyProcessing": 14,
    "shippedToday": 20,
    "_links": {
        "self": {
            "href": "/orders"
        },
        "curies": {
            "href": "http://example.com/docs/rels/{rel}",
            "templated": true,
            "name": "ea"
        },
        "next": {
            "href": "/orders?page=2"
        },
        "ea:find": {
            "href": "/orders{?id}",
            "templated": true
        },
        "ea:admin": {
            "href": "/admins/2",
            "title": "Fred"
        }
    },
    "_embedded": {
        "ea:order": [
            {
                "total": 10,
                "currency": "USD",
                "status": "shipped",
                "id": "6d5edc98-8b81-435f-ad7a-a66a60d91bd2",
                "_links": {
                    "self": {
                        "href": "/orders/6d5edc98-8b81-435f-ad7a-a66a60d91bd2"
                    },
                    "ea:basket": {
                        "href": "/baskets/{basketId}",
                        "templated": true
                    },
                    "ea:customer": {
                        "href": "/customers/{custId}",
                        "templated": true
                    }
                }
            },
            {
                "total": 20,
                "currency": "CAD",
                "status": "processing",
                "id": "418845a7-ec41-4288-83eb-22a8bb22e472",
                "_links": {
                    "self": {
                        "href": "/orders/418845a7-ec41-4288-83eb-22a8bb22e472"
                    },
                    "ea:basket": {
                        "href": "/baskets/{basketId}",
                        "templated": true
                    },
                    "ea:customer": {
                        "href": "/customers/{custId}",
                        "templated": true
                    }
                }
            },
            {
                "total": 30,
                "currency": "EUR",
                "status": "customs",
                "id": "4441db6d-54c1-4298-a040-c6f31c7eafc8",
                "_links": {
                    "self": {
                        "href": "/orders/4441db6d-54c1-4298-a040-c6f31c7eafc8"
                    },
                    "ea:basket": {
                        "href": "/baskets/{basketId}",
                        "templated": true
                    },
                    "ea:customer": {
                        "href": "/customers/{custId}",
                        "templated": true
                    }
                }
            },
            {
                "total": 40,
                "currency": "USD",
                "status": "shipped",
                "id": "533d4c68-01b9-4260-9331-af35bcaf1bda",
                "_links": {
                    "self": {
                        "href": "/orders/533d4c68-01b9-4260-9331-af35bcaf1bda"
                    },
                    "ea:basket": {
                        "href": "/baskets/{basketId}",
                        "templated": true
                    },
                    "ea:customer": {
                        "href": "/customers/{custId}",
                        "templated": true
                    }
                }
            },
            {
                "total": 50,
                "currency": "USD",
                "status": "complete",
                "id": "bfd43505-ce9b-4eff-8f91-e75912f9510f",
                "_links": {
                    "self": {
                        "href": "/orders/bfd43505-ce9b-4eff-8f91-e75912f9510f"
                    },
                    "ea:basket": {
                        "href": "/baskets/{basketId}",
                        "templated": true
                    },
                    "ea:customer": {
                        "href": "/customers/{custId}",
                        "templated": true
                    }
                }
            },
            {
                "total": 69,
                "currency": "CAD",
                "status": "nice",
                "id": "40711d60-0f85-4435-b7a8-cf4f5df4c551",
                "_links": {
                    "self": {
                        "href": "/orders/40711d60-0f85-4435-b7a8-cf4f5df4c551"
                    },
                    "ea:basket": {
                        "href": "/baskets/{basketId}",
                        "templated": true
                    },
                    "ea:customer": {
                        "href": "/customers/{custId}",
                        "templated": true
                    }
                }
            }
        ]
    }
}
```

## Deserializing a HAL Resource

### Strongly Typed Object

Deserializing `application/hal+json` content type as defined [above](###example-json) to a strongly typed `OrderCollection` object:

```csharp
public class OrderCollection
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

var stronglyTypedOrder = JsonSerializer.Deserialize<OrderCollection>(halJson);
```

### Resource Object

Deserializing `application/hal+json` content type as defined [above](###example-json) to a [Resource object](https://github.com/brenpike/Chatter.Rest.Hal/blob/main/src/Chatter.Rest.Hal/Resource.cs):

```csharp
public class OrderState
{
	[JsonPropertyName("currentlyProcessing")]
	public int CurrentlyProcessing { get; set; }
	[JsonPropertyName("shippedToday")]
	public int ShippedToday { get; set; }
}

...

var resource = JsonSerializer.Deserialize<Resource>(halJson);
```

To get a [Resource's state](https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-4), use a [Resource object's](https://github.com/brenpike/Chatter.Rest.Hal/blob/main/src/Chatter.Rest.Hal/Resource.cs) `State<T>` method:

```csharp
var stronglyTypeOrder = resource.State<OrderState>();
```

To cast the [Resource object](https://github.com/brenpike/Chatter.Rest.Hal/blob/main/src/Chatter.Rest.Hal/Resource.cs) as a strongly typed object use the `.As<T>` method:

```csharp
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

var orderCollection = resource!.As<OrderCollection>();
```

### Strongly Typed Object + Source Generator

Deserializing `application/hal+json` content as defined [above](###example-json) to a strongly typed `OrderCollection` object using [source generators](https://github.com/brenpike/Chatter.Rest.Hal/tree/main/src/Chatter.Rest.Hal.CodeGenerators):

Add the `Chatter.Rest.Hal.CodeGenerators` nuget package to your project and decorate your strongly typed object with the [HalResponseAttribute](https://github.com/brenpike/Chatter.Rest.Hal/blob/main/src/Chatter.Rest.Hal.Core/HalResponseAttribute.cs)

```csharp
[HalResponse]
public partial class OrderCollection
{
	[JsonPropertyName("currentlyProcessing")]
	public int CurrentlyProcessing { get; set; }
	[JsonPropertyName("shippedToday")]
	public int ShippedToday { get; set; }
}
```

Decorating with the [HalResponseAttribute](https://github.com/brenpike/Chatter.Rest.Hal/blob/main/src/Chatter.Rest.Hal.Core/HalResponseAttribute.cs) will create another partial class with the same name as your strongly typed object, eg., `OrderCollection`, and add the `_links` and `_embedded` properties:

```csharp
[JsonPropertyName("_links")]
public LinkCollection? Links {{ get; set; }}
[JsonPropertyName("_embedded")]
public EmbeddedResourceCollection? Embedded {{ get; set; }}
```

## Accessing data in a HAL Resource

Once you've received a response with a `application/hal+json` content type from a server, you'll want to easily access various parts of the [Resource object](https://github.com/brenpike/Chatter.Rest.Hal/blob/main/src/Chatter.Rest.Hal/Resource.cs).  This can be done using the various extension methods.

### Get strongly typed embedded resources

```csharp
var embeddedOrders = resource!.GetEmbeddedResources<Order>("ea:order");
```

### Get a Resource Collection

```csharp
var resources = resource!.GetResourceCollection("ea:order");
```

### Get a Link by relation

This will return the Link with the matching relation, null if no matching relations are found or throw an exception if more than one relation is found.

```csharp
var link = resource!.GetLinkOrDefault("self");
```

### Get all Link Objects of a Link by relation

```csharp
var linkObjCol = resource!.GetLinkObjects("self");
```

### Get a Link Object of a Link by relation and Link Object name

In this case, the Link Object name is used as a secondary key. Common when curies are used. Returns null if no matching relations+name is found or throws an exception if more than one relation+name is found.

```csharp
var linkObj = resource!.GetLinkObjectOrDefault("curies", "ea");
```

### Get a Link Object of a Link by relation only

Most Links have only a single Link Object. Use this extension method to get the exact Link Object you're expecting using the Link's relation. Returns null if no matching relations are found or throw an exception if more than one relation is found.

```csharp
var linkObj = resource!.GetLinkObjectOrDefault("self");
```

If the extension above were used on a Resource which was deserialized from [the example JSON](###example-json), a Link Object with `{ "href": "/orders" }` would be returned as it is the only Link Object with the relation "self".
