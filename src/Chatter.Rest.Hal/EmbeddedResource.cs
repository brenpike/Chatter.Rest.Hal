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
	public bool ForceWriteAsCollection { get; set; }
	public ResourceCollection Resources { get; set; } = new();
}
