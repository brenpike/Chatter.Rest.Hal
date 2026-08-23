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
	/// <exception cref="System.ArgumentException">Thrown when <paramref name="rel"/> is null or whitespace.</exception>
	public Link(string rel)
	{
		if (string.IsNullOrWhiteSpace(rel))
		{
			throw new ArgumentException("Value cannot be null or whitespace.", nameof(rel));
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

	/// <summary>
	/// When true, this link relation serializes as a JSON array regardless of count.
	/// Use to guarantee a stable response shape when a relation may sometimes
	/// contain one and sometimes multiple links.
	/// </summary>
	public bool IsArray { get; set; } = false;

	/// <summary>
	/// Determines whether this link represents the same HAL content as another link.
	/// </summary>
	/// <remarks>
	/// <see cref="IsArray"/> only affects the serialized document when the link holds exactly one
	/// link object (zero or many always serialize as an array), so the flag participates in
	/// equality only in that case. Two links differing solely in a flag that cannot change the
	/// serialized shape are the same HAL content.
	/// </remarks>
	public bool Equals(Link? other)
	{
		if (other is null)
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		return Rel == other.Rel
			&& EffectiveIsArray == other.EffectiveIsArray
			&& LinkObjects.Equals(other.LinkObjects);
	}

	/// <inheritdoc cref="Equals(Link?)"/>
	public override int GetHashCode()
	{
		unchecked
		{
			var hash = 17;
			hash = (hash * 31) + Rel.GetHashCode();
			hash = (hash * 31) + EffectiveIsArray.GetHashCode();
			hash = (hash * 31) + LinkObjects.GetHashCode();
			return hash;
		}
	}

	/// <summary>The array flag as it affects serialization: fixed when the count already forces an array.</summary>
	private bool EffectiveIsArray => LinkObjects.Count == 1 ? IsArray : true;
}
