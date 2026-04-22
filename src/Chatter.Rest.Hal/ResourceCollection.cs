using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Chatter.Rest.Hal.Converters;

namespace Chatter.Rest.Hal;

/// <summary>
/// Represents a collection of resources within an embedded resource entry.
/// </summary>
[JsonConverter(typeof(ResourceCollectionConverter))]
public sealed record ResourceCollection : ICollection<Resource>, IHalPart
{
	private readonly Collection<Resource> _resources = new();

	/// <summary>
	/// Gets the resource at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the resource to get.</param>
	/// <returns>The resource at the specified index.</returns>
	public Resource this[int index] => _resources[index];

	/// <summary>
	/// Gets the number of resources in the collection.
	/// </summary>
	public int Count => _resources.Count;

	/// <summary>
	/// Gets a value indicating whether the collection is read-only.
	/// </summary>
	public bool IsReadOnly => false;

	/// <summary>
	/// Adds a resource to the collection.
	/// </summary>
	/// <param name="item">The resource to add.</param>
	public void Add(Resource item) => _resources.Add(item);

	/// <summary>
	/// Removes all resources from the collection.
	/// </summary>
	public void Clear() => _resources.Clear();

	/// <summary>
	/// Determines whether the collection contains a specific resource.
	/// </summary>
	/// <param name="item">The resource to locate.</param>
	/// <returns>true if the resource is found; otherwise, false.</returns>
	public bool Contains(Resource item) => _resources.Contains(item);

	/// <summary>
	/// Copies the elements of the collection to an array, starting at a particular index.
	/// </summary>
	/// <param name="array">The destination array.</param>
	/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
	public void CopyTo(Resource[] array, int arrayIndex) => _resources.CopyTo(array, arrayIndex);

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	public IEnumerator<Resource> GetEnumerator() => _resources.GetEnumerator();

	/// <summary>
	/// Removes a specific resource from the collection.
	/// </summary>
	/// <param name="item">The resource to remove.</param>
	/// <returns>true if the item was successfully removed; otherwise, false.</returns>
	public bool Remove(Resource item) => _resources.Remove(item);

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	IEnumerator IEnumerable.GetEnumerator() => _resources.GetEnumerator();
}
