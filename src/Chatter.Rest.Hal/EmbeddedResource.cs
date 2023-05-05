using System;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal.Converters;

namespace Chatter.Rest.Hal;

[JsonConverter(typeof(EmbeddedResourceConverter))]
public sealed record EmbeddedResource : IHalPart
{
	public EmbeddedResource(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
		}
		Name = name;
	}

	public string Name { get; }

	/// <summary>
	/// Flag indicating (if true) that this embedded resource represents a collection, to override the default
	/// behavior of writing to JSON as an object or a collection based on count of <see cref="Resources"/>,
	/// as specified by <see href="https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-4.1.2"/>
	/// </summary>
	public bool ForceWriteAsCollection { get; set; } = false;

	public ResourceCollection Resources { get; set; } = new();
}
