using Chatter.Rest.Hal.Converters;
using System.Collections;
using System.Collections.ObjectModel;

namespace Chatter.Rest.Hal;

[JsonConverter(typeof(EmbeddedResourceCollectionConverter))]
public sealed record EmbeddedResourceCollection : ICollection<EmbeddedResource>
{
	private readonly Collection<EmbeddedResource> _embedded = new();

	public int Count => _embedded.Count;
	public bool IsReadOnly => false;
	public void Add(EmbeddedResource item) => _embedded.Add(item);
	public void Clear() => _embedded.Clear();
	public bool Contains(EmbeddedResource item) => _embedded.Contains(item);
	public void CopyTo(EmbeddedResource[] array, int arrayIndex) => _embedded.CopyTo(array, arrayIndex);
	public IEnumerator<EmbeddedResource> GetEnumerator() => _embedded.GetEnumerator();
	public bool Remove(EmbeddedResource item) => _embedded.Remove(item);
	IEnumerator IEnumerable.GetEnumerator() => _embedded.GetEnumerator();
}
