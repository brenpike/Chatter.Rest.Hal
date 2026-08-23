using System;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal.Converters;

namespace Chatter.Rest.Hal;

/// <summary>
/// Represents an embedded resource entry within a HAL document. Contains a named collection of resources.
/// </summary>
[JsonConverter(typeof(EmbeddedResourceConverter))]
public sealed record EmbeddedResource : IHalPart
{
	/// <summary>
	/// Initializes a new instance of the <see cref="EmbeddedResource"/> record with the specified name.
	/// </summary>
	/// <param name="name">The name identifying this embedded resource. Must not be null or whitespace.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
	public EmbeddedResource(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
		}
		Name = name;
	}

	/// <summary>
	/// Gets the name that identifies this embedded resource within the parent resource.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Flag indicating (if true) that this embedded resource represents a collection, to override the default
	/// behavior of writing to JSON as an object or a collection based on count of <see cref="Resources"/>,
	/// as specified by <see href="https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#section-4.1.2"/>
	/// </summary>
	public bool ForceWriteAsCollection { get; set; } = false;

	/// <summary>
	/// Gets or sets the collection of resources contained within this embedded resource entry.
	/// </summary>
	public ResourceCollection Resources { get; set; } = new();

	/// <summary>
	/// Determines whether this entry represents the same HAL content as another entry.
	/// </summary>
	/// <remarks>
	/// <see cref="ForceWriteAsCollection"/> only affects the serialized document when the entry
	/// holds exactly one resource (zero or many always serialize as an array), so the flag
	/// participates in equality only in that case.
	/// </remarks>
	public bool Equals(EmbeddedResource? other)
	{
		if (other is null)
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		return Name == other.Name
			&& EffectiveForceWriteAsCollection == other.EffectiveForceWriteAsCollection
			&& Resources.Equals(other.Resources);
	}

	/// <inheritdoc cref="Equals(EmbeddedResource?)"/>
	public override int GetHashCode()
	{
		unchecked
		{
			var hash = 17;
			hash = (hash * 31) + Name.GetHashCode();
			hash = (hash * 31) + EffectiveForceWriteAsCollection.GetHashCode();
			hash = (hash * 31) + Resources.GetHashCode();
			return hash;
		}
	}

	/// <summary>The collection flag as it affects serialization: fixed when the count already forces an array.</summary>
	private bool EffectiveForceWriteAsCollection => Resources.Count == 1 ? ForceWriteAsCollection : true;
}
