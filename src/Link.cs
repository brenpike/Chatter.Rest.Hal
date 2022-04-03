﻿using Chatter.Rest.Hal.Converters;

namespace Chatter.Rest.Hal;

[JsonConverter(typeof(LinkConverter))]
public sealed record Link
{
	public Link(string rel)
	{
		if (string.IsNullOrWhiteSpace(rel))
		{
			throw new ArgumentNullException(nameof(rel));
		}
		Rel = rel;
	}

	public string Rel { get; init; }
	public LinkObjectCollection LinkObjects { get; init; } = new();
}
