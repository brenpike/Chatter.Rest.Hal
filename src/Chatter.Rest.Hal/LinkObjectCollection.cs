using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal.Converters;

namespace Chatter.Rest.Hal;

/// <summary>
/// Represents a collection of link objects within a single link relation.
/// </summary>
/// <remarks>
/// No O(1) string-keyed lookup index is provided. Link objects may share names or have none,
/// making a string-keyed index semantically ambiguous. Use the integer indexer or LINQ to
/// query by position or predicate.
/// </remarks>
[JsonConverter(typeof(LinkObjectCollectionConverter))]
public sealed record LinkObjectCollection : ICollection<LinkObject>, IHalPart
{
	private readonly Collection<LinkObject> _linkObjects = new();

	/// <summary>
	/// Gets the link object at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the link object to get.</param>
	/// <returns>The link object at the specified index.</returns>
	public LinkObject this[int index] => _linkObjects[index];

	/// <summary>
	/// Gets the number of link objects in the collection.
	/// </summary>
	public int Count => _linkObjects.Count;

	/// <summary>
	/// Gets a value indicating whether the collection is read-only.
	/// </summary>
	public bool IsReadOnly => false;

	/// <summary>
	/// Adds a link object to the collection.
	/// </summary>
	/// <param name="item">The link object to add.</param>
	public void Add(LinkObject item) => _linkObjects.Add(item);

	/// <summary>
	/// Removes all link objects from the collection.
	/// </summary>
	public void Clear() => _linkObjects.Clear();

	/// <summary>
	/// Determines whether the collection contains a specific link object.
	/// </summary>
	/// <param name="item">The link object to locate.</param>
	/// <returns>true if the link object is found; otherwise, false.</returns>
	public bool Contains(LinkObject item) => _linkObjects.Contains(item);

	/// <summary>
	/// Copies the elements of the collection to an array, starting at a particular index.
	/// </summary>
	/// <param name="array">The destination array.</param>
	/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
	public void CopyTo(LinkObject[] array, int arrayIndex) => _linkObjects.CopyTo(array, arrayIndex);

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	public IEnumerator<LinkObject> GetEnumerator() => _linkObjects.GetEnumerator();

	/// <summary>
	/// Removes a specific link object from the collection.
	/// </summary>
	/// <param name="item">The link object to remove.</param>
	/// <returns>true if the item was successfully removed; otherwise, false.</returns>
	public bool Remove(LinkObject item) => _linkObjects.Remove(item);

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	IEnumerator IEnumerable.GetEnumerator() => _linkObjects.GetEnumerator();
}
