using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal.Converters;

namespace Chatter.Rest.Hal;

/// <summary>
/// Represents a collection of embedded resources within a HAL document. Serialized as the "_embedded" property.
/// </summary>
[JsonConverter(typeof(EmbeddedResourceCollectionConverter))]
public sealed record EmbeddedResourceCollection : ICollection<EmbeddedResource>, IHalPart
{
	private readonly Collection<EmbeddedResource> _embedded = new();
	private readonly Dictionary<string, EmbeddedResource> _index = new(StringComparer.Ordinal);

	/// <summary>
	/// Gets the embedded resource at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the embedded resource to get.</param>
	/// <returns>The embedded resource at the specified index.</returns>
	public EmbeddedResource this[int index] => _embedded[index];

	/// <summary>
	/// Gets the number of embedded resources in the collection.
	/// </summary>
	public int Count => _embedded.Count;

	/// <summary>
	/// Gets a value indicating whether the collection is read-only.
	/// </summary>
	public bool IsReadOnly => false;

	/// <summary>
	/// Adds an embedded resource to the collection.
	/// </summary>
	/// <remarks>
	/// Embedded resource names are unique within a collection. The HAL specification models
	/// "_embedded" as a JSON object keyed by link relation name, so two entries sharing a name can
	/// never serialize into spec-valid output. Adding a duplicate name therefore throws rather than
	/// silently overwriting the name index.
	/// </remarks>
	/// <param name="item">The embedded resource to add.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when an embedded resource with the same name has already been added.</exception>
	public void Add(EmbeddedResource item)
	{
		if (item is null)
		{
			throw new ArgumentNullException(nameof(item));
		}

		if (_index.ContainsKey(item.Name))
		{
			throw new ArgumentException($"An embedded resource with name '{item.Name}' has already been added. Embedded resource names must be unique within an embedded resource collection.", nameof(item));
		}

		_embedded.Add(item);
		_index.Add(item.Name, item);
	}

	/// <summary>
	/// Removes all embedded resources from the collection.
	/// </summary>
	public void Clear()
	{
		_embedded.Clear();
		_index.Clear();
	}

	/// <summary>
	/// Determines whether the collection contains a specific embedded resource.
	/// </summary>
	/// <param name="item">The embedded resource to locate.</param>
	/// <returns>true if the embedded resource is found; otherwise, false.</returns>
	public bool Contains(EmbeddedResource item) => _embedded.Contains(item);

	/// <summary>
	/// Copies the elements of the collection to an array, starting at a particular index.
	/// </summary>
	/// <param name="array">The destination array.</param>
	/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
	public void CopyTo(EmbeddedResource[] array, int arrayIndex) => _embedded.CopyTo(array, arrayIndex);

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	public IEnumerator<EmbeddedResource> GetEnumerator() => _embedded.GetEnumerator();

	/// <summary>
	/// Removes a specific embedded resource from the collection.
	/// </summary>
	/// <param name="item">The embedded resource to remove.</param>
	/// <returns>true if the item was successfully removed; otherwise, false.</returns>
	public bool Remove(EmbeddedResource item)
	{
		if (item is null)
		{
			return false;
		}

		if (!_embedded.Remove(item))
		{
			return false;
		}

		// Names are unique, so the removed entry was the only holder of its name and the index entry
		// keyed by that name can no longer refer to an embedded resource still in the list.
		_index.Remove(item.Name);
		return true;
	}

	/// <summary>
	/// Attempts to retrieve an embedded resource by its name using O(1) dictionary lookup.
	/// </summary>
	/// <param name="name">The embedded resource name to find.</param>
	/// <param name="embedded">When this method returns, contains the embedded resource if found; otherwise, null.</param>
	/// <returns>true if an embedded resource with the specified name was found; otherwise, false.</returns>
	public bool TryGetByName(string name, out EmbeddedResource? embedded)
		=> _index.TryGetValue(name, out embedded);

	/// <summary>
	/// Determines whether this collection holds the same embedded resources as another collection.
	/// </summary>
	/// <remarks>
	/// Entries are compared by name rather than by position: a HAL "_embedded" value is a JSON
	/// object and JSON object members are unordered, so two collections holding the same entries in
	/// a different order represent the same HAL document. The name index is derived from the
	/// entries and therefore takes no part in the comparison.
	/// </remarks>
	/// <param name="other">The collection to compare with.</param>
	/// <returns>true if both collections hold equal embedded resources for the same names; otherwise, false.</returns>
	public bool Equals(EmbeddedResourceCollection? other)
	{
		if (other is null)
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		if (_embedded.Count != other._embedded.Count)
		{
			return false;
		}

		foreach (var embedded in _embedded)
		{
			if (!other._index.TryGetValue(embedded.Name, out var candidate) || !Equals(embedded, candidate))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Returns a hash code derived from the embedded resources in the collection, independent of their order.
	/// </summary>
	/// <returns>A hash code for the collection.</returns>
	public override int GetHashCode()
	{
		unchecked
		{
			var hash = _embedded.Count;
			foreach (var embedded in _embedded)
			{
				hash += embedded?.GetHashCode() ?? 0;
			}
			return hash;
		}
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	IEnumerator IEnumerable.GetEnumerator() => _embedded.GetEnumerator();
}
