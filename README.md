# 🔗 Chatter.Rest.Hal

> A comprehensive .NET/C# implementation of the HAL (Hypertext Application Language) specification for building and consuming RESTful APIs

[![CI - HAL](https://github.com/brenpike/Chatter.Rest.Hal/actions/workflows/hal-cicd.yml/badge.svg)](https://github.com/brenpike/Chatter.Rest.Hal/actions/workflows/hal-cicd.yml)
[![CI - Code Generation](https://github.com/brenpike/Chatter.Rest.Hal/actions/workflows/codegen-cicd.yml/badge.svg)](https://github.com/brenpike/Chatter.Rest.Hal/actions/workflows/codegen-cicd.yml)
[![NuGet - Core](https://img.shields.io/nuget/v/Chatter.Rest.Hal?label=Chatter.Rest.Hal)](https://www.nuget.org/packages/Chatter.Rest.Hal)
[![NuGet - Code Generators](https://img.shields.io/nuget/v/Chatter.Rest.Hal.CodeGenerators?label=Chatter.Rest.Hal.CodeGenerators)](https://www.nuget.org/packages/Chatter.Rest.Hal.CodeGenerators)

---

## Features

- **Fluent Builder API** - Intuitive, chainable methods for constructing HAL resources
- **Multiple Deserialization Options** - Support for strongly-typed objects, Resource objects, and source-generated types
- **Embedded Resources** - Easily work with nested HAL resources
- **Link Management** - Comprehensive link handling with curies, templates, and metadata
- **Source Generators** - Code generation support via `Chatter.Rest.Hal.CodeGenerators` package
- **Flexible Data Access** - Extension methods for querying links and embedded resources
- **HAL Specification Compliant** - Full compliance with the [official HAL specification](https://datatracker.ietf.org/doc/html/draft-kelly-json-hal)

---

## Table of Contents

- [🔗 Chatter.Rest.Hal](#-chatterresthal)
  - [Features](#features)
  - [Table of Contents](#table-of-contents)
  - [Quick Start](#quick-start)
  - [Installation](#installation)
    - [Core Library](#core-library)
    - [Code Generators (Optional)](#code-generators-optional)
  - [What is HAL?](#what-is-hal)
    - [Learn More](#learn-more)
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
  - [Additional Resources](#additional-resources)
  - [License](#license)
  - [Contributing](#contributing)

---

## Quick Start

Install the NuGet package:

```bash
dotnet add package Chatter.Rest.Hal
```

Minimal, copy-paste example (build, serialize):

```csharp
using System;
using System.Text.Json;
using Chatter.Rest.Hal;

// Build a simple resource with state and a self link
var resource = ResourceBuilder
    .WithState(new { message = "Hello, HAL!" })
    .AddSelf().AddLinkObject("/api/greeting")
    .Build();

// Serialize with System.Text.Json (pretty-print for readability)
var json = JsonSerializer.Serialize(resource, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);

/* Output:
{
  "message": "Hello, HAL!",
  "_links": {
    "self": {
      "href": "/api/greeting"
    }
  }
}
*/
```

If you want compile-time helpers generated for your response types, also install the code generators package:

```bash
dotnet add package Chatter.Rest.Hal.CodeGenerators
```

---

## Installation

### Core Library

The core library provides builders and runtime support for HAL resources:

```bash
dotnet add package Chatter.Rest.Hal
```

**NuGet Link:** [Chatter.Rest.Hal](https://www.nuget.org/packages/Chatter.Rest.Hal)

### Code Generators (Optional)

For compile-time code generation support, add the code generators package:

```bash
dotnet add package Chatter.Rest.Hal.CodeGenerators
```

**NuGet Link:** [Chatter.Rest.Hal.CodeGenerators](https://www.nuget.org/packages/Chatter.Rest.Hal.CodeGenerators)

---

## What is HAL?


**HAL** stands for **Hypertext Application Language** — a simple, standardized format for designing REST APIs that are easy to explore, understand, and consume. It enables self-documenting APIs where hypermedia controls and resource relationships are discoverable through the API itself.

According to [Mike Kelly, the HAL specification creator](https://stateless.group/hal_specification.html):

> "HAL is a simple format that gives a consistent and easy way to hyperlink between resources in your API. Adopting HAL will make your API explorable, and its documentation easily discoverable from within the API itself. In short, it will make your API easier to work with and therefore more attractive to client developers."

### Learn More

- [Official HAL Specification](https://datatracker.ietf.org/doc/html/draft-kelly-json-hal)
- [HAL Specification Overview](https://stateless.group/hal_specification.html)
- [GitHub Repository](https://github.com/mikekelly/hal_specification)

---

## Building a HAL Resource


### Example JSON

A typical HAL response includes `_links` for navigation and `_embedded` for related resources. Here's a comprehensive example:

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

The Fluent Builder API makes it easy to construct complex, HAL-compliant resources. It ensures your resource structure matches the HAL specification while maintaining readable, chainable code.

**Example:** Building the resource structure shown in the JSON above:

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

In real-world scenarios, you typically construct resources dynamically from database objects. This example demonstrates building a HAL resource from a collection of `Order` objects:

**Define your model:**

```csharp
public class Order
{
    public string Id { get; set; }
    public float Total { get; set; }
    public string Currency { get; set; }
    public string Status { get; set; }
}
```

**Build the resource dynamically:**

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

**Resulting JSON output:**

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

---

## Deserializing a HAL Resource


### Strongly Typed Object

Deserialize HAL+JSON responses into strongly typed .NET objects for type safety and IntelliSense support:

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

// Deserialize from HAL+JSON
var stronglyTypedOrder = JsonSerializer.Deserialize<OrderCollection>(halJson);
```

### Resource Object

For more flexible deserialization when you don't have a strongly-typed target class, use the `Resource` object:

```csharp
public class OrderState
{
	[JsonPropertyName("currentlyProcessing")]
	public int CurrentlyProcessing { get; set; }
	[JsonPropertyName("shippedToday")]
	public int ShippedToday { get; set; }
}

// Deserialize to a Resource object
var resource = JsonSerializer.Deserialize<Resource>(halJson);

// Access the state as a strongly-typed object
var orderState = resource.State<OrderState>();
```

**Convert a Resource object to a strongly-typed object:**

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

// Cast to strongly-typed object using the .As<T>() method
var orderCollection = resource!.As<OrderCollection>();
```

For more details, see the [Resource class documentation](https://github.com/brenpike/Chatter.Rest.Hal/blob/main/src/Chatter.Rest.Hal/Resource.cs).

### Strongly Typed Object + Source Generator

For automatic property generation, use the `Chatter.Rest.Hal.CodeGenerators` package to generate HAL-specific properties at compile time.

**Step 1:** Install the code generators package:

```bash
dotnet add package Chatter.Rest.Hal.CodeGenerators
```

**Step 2:** Decorate your class with the `[HalResponse]` attribute:

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

The `[HalResponse]` attribute automatically generates a partial class that adds the required HAL properties:

```csharp
[JsonPropertyName("_links")]
public LinkCollection? Links { get; set; }
[JsonPropertyName("_embedded")]
public EmbeddedResourceCollection? Embedded { get; set; }
```

See the [HalResponseAttribute documentation](https://github.com/brenpike/Chatter.Rest.Hal/blob/main/src/Chatter.Rest.Hal.Core/HalResponseAttribute.cs) for more information.

---

## Accessing data in a HAL Resource


Once you've received a `application/hal+json` response from your API, the library provides convenient extension methods to navigate links and access embedded resources. The following examples show how to use the [Resource object](https://github.com/brenpike/Chatter.Rest.Hal/blob/main/src/Chatter.Rest.Hal/Resource.cs) extension methods.

### Get strongly typed embedded resources

Extract embedded resources as strongly-typed objects:

```csharp
var embeddedOrders = resource!.GetEmbeddedResources<Order>("ea:order");
```

### Get a Resource Collection

Retrieve a collection of Resource objects by relation name:

```csharp
var resources = resource!.GetResourceCollection("ea:order");
```

### Get a Link by relation

Retrieve a single link by its relation. Returns `null` if not found, or throws an exception if multiple links with the same relation exist:

```csharp
var link = resource!.GetLinkOrDefault("self");
```

### Get all Link Objects of a Link by relation

Retrieve all link objects associated with a specific relation:

```csharp
var linkObjCol = resource!.GetLinkObjects("self");
```

### Get a Link Object of a Link by relation and Link Object name

When using custom namespaces (curies), retrieve a link object by both relation and name. Returns `null` if not found, or throws an exception if multiple matches exist:

```csharp
var linkObj = resource!.GetLinkObjectOrDefault("curies", "ea");
```

### Get a Link Object of a Link by relation only

For relations with a single link object, retrieve it directly by relation:

```csharp
var linkObj = resource!.GetLinkObjectOrDefault("self");
```

If used on the resource from the example JSON above, this would return a Link Object with `{ "href": "/orders" }` since "self" is the only link object for that relation.

---

## Additional Resources

- **Source Code:** [GitHub Repository](https://github.com/brenpike/Chatter.Rest.Hal)
- **Core Library:** [Resource.cs](https://github.com/brenpike/Chatter.Rest.Hal/blob/main/src/Chatter.Rest.Hal/Resource.cs)
- **Code Generators:** [Source Generator Project](https://github.com/brenpike/Chatter.Rest.Hal/tree/main/src/Chatter.Rest.Hal.CodeGenerators)
- **HAL Specification:** [Official Spec](https://datatracker.ietf.org/doc/html/draft-kelly-json-hal)

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit issues, fork the repository, and create pull requests for any improvements.

For more information, visit the [GitHub Repository](https://github.com/brenpike/Chatter.Rest.Hal).

---
