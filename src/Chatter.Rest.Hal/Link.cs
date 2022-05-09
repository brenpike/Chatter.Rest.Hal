using System;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal.Converters;

namespace Chatter.Rest.Hal;

[JsonConverter(typeof(LinkConverter))]
public sealed record Link : IHalPart
{
	public Link(string rel)
	{
		if (string.IsNullOrWhiteSpace(rel))
		{
			throw new ArgumentNullException(nameof(rel));
		}
		Rel = rel;
	}

	public string Rel { get; }
	public LinkObjectCollection LinkObjects { get; set; } = new();
}
