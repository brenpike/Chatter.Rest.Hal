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
	/// <param name="item">The embedded resource to add.</param>
	public void Add(EmbeddedResource item) => _embedded.Add(item);

	/// <summary>
	/// Removes all embedded resources from the collection.
	/// </summary>
	public void Clear() => _embedded.Clear();

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
	public bool Remove(EmbeddedResource item) => _embedded.Remove(item);

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	IEnumerator IEnumerable.GetEnumerator() => _embedded.GetEnumerator();
}
