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
	/// <param name="item">The link to add.</param>
	public void Add(Link item) => _links.Add(item);

	/// <summary>
	/// Removes all links from the collection.
	/// </summary>
	public void Clear() => _links.Clear();

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
	public bool Remove(Link item) => _links.Remove(item);

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	IEnumerator IEnumerable.GetEnumerator() => _links.GetEnumerator();
}
