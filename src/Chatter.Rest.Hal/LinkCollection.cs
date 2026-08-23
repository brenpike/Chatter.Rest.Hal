using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal.Converters;

namespace Chatter.Rest.Hal;

/// <summary>
/// Represents a collection of links within a HAL document. Serialized as the "_links" property.
/// </summary>
[JsonConverter(typeof(LinkCollectionConverter))]
public sealed record LinkCollection : ICollection<Link>, IHalPart
{
	private readonly Collection<Link> _links = new();
	private readonly Dictionary<string, Link> _index = new(StringComparer.Ordinal);

	/// <summary>
	/// Gets the link at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the link to get.</param>
	/// <returns>The link at the specified index.</returns>
	public Link this[int index] => _links[index];

	/// <summary>
	/// Gets the number of links in the collection.
	/// </summary>
	public int Count => _links.Count;

	/// <summary>
	/// Gets a value indicating whether the collection is read-only.
	/// </summary>
	public bool IsReadOnly => false;

	/// <summary>
	/// Adds a link to the collection.
	/// </summary>
	/// <remarks>
	/// Link relations are unique within a collection. The HAL specification models "_links" as a
	/// JSON object keyed by relation, so two links sharing a relation can never serialize into
	/// spec-valid output. Adding a duplicate relation therefore throws rather than silently
	/// overwriting the relation index.
	/// </remarks>
	/// <param name="item">The link to add.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when a link with the same relation has already been added.</exception>
	public void Add(Link item)
	{
		if (item is null)
		{
			throw new ArgumentNullException(nameof(item));
		}

		if (_index.ContainsKey(item.Rel))
		{
			throw new ArgumentException($"A link with relation '{item.Rel}' has already been added. Link relations must be unique within a link collection.", nameof(item));
		}

		_links.Add(item);
		_index.Add(item.Rel, item);
	}

	/// <summary>
	/// Removes all links from the collection.
	/// </summary>
	public void Clear()
	{
		_links.Clear();
		_index.Clear();
	}

	/// <summary>
	/// Determines whether the collection contains a specific link.
	/// </summary>
	/// <param name="item">The link to locate.</param>
	/// <returns>true if the link is found; otherwise, false.</returns>
	public bool Contains(Link item) => _links.Contains(item);

	/// <summary>
	/// Copies the elements of the collection to an array, starting at a particular index.
	/// </summary>
	/// <param name="array">The destination array.</param>
	/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
	public void CopyTo(Link[] array, int arrayIndex) => _links.CopyTo(array, arrayIndex);

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	public IEnumerator<Link> GetEnumerator() => _links.GetEnumerator();

	/// <summary>
	/// Removes a specific link from the collection.
	/// </summary>
	/// <param name="item">The link to remove.</param>
	/// <returns>true if the item was successfully removed; otherwise, false.</returns>
	public bool Remove(Link item)
	{
		if (item is null)
		{
			return false;
		}

		if (!_links.Remove(item))
		{
			return false;
		}

		// Relations are unique, so the removed link was the only holder of its relation and the
		// index entry keyed by that relation can no longer refer to a link still in the list.
		_index.Remove(item.Rel);
		return true;
	}

	/// <summary>
	/// Attempts to retrieve a link by its relation name using O(1) dictionary lookup.
	/// </summary>
	/// <param name="rel">The link relation to find.</param>
	/// <param name="link">When this method returns, contains the link if found; otherwise, null.</param>
	/// <returns>true if a link with the specified relation was found; otherwise, false.</returns>
	public bool TryGetByRel(string rel, out Link? link)
		=> _index.TryGetValue(rel, out link);

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	IEnumerator IEnumerable.GetEnumerator() => _links.GetEnumerator();
}
