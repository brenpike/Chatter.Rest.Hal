using System;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal.Converters;

namespace Chatter.Rest.Hal;

/// <summary>
/// Represents a HAL link entry. A Link is identified by its relation (rel) and
/// may contain one or more link objects with href, templated, type and other
/// link-level properties.
/// </summary>
[JsonConverter(typeof(LinkConverter))]
public sealed record Link : IHalPart
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Link"/> record with the specified relation.
	/// </summary>
	/// <param name="rel">The link relation (rel). Must be a non-empty string.</param>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="rel"/> is null or whitespace.</exception>
	public Link(string rel)
	{
		if (string.IsNullOrWhiteSpace(rel))
		{
			throw new ArgumentNullException(nameof(rel));
		}
		Rel = rel;
	}

	/// <summary>
	/// Gets the link relation (rel) which identifies the semantics of the link.
	/// </summary>
	public string Rel { get; }

	/// <summary>
	/// Gets or sets the collection of link objects associated with this relation.
	/// Use this to add one or more actual link entries (href, templated, etc.).
	/// </summary>
	public LinkObjectCollection LinkObjects { get; set; } = new();
}
