using Chatter.Rest.Hal.Converters;
using System.Collections;
using System.Collections.ObjectModel;

namespace Chatter.Rest.Hal;

[JsonConverter(typeof(ResourceCollectionConverter))]
public sealed record ResourceCollection : ICollection<Resource>
{
	private readonly Collection<Resource> _resources = new();

	public int Count => _resources.Count;
	public bool IsReadOnly => false;
	public void Add(Resource item) => _resources.Add(item);
	public void Clear() => _resources.Clear();
	public bool Contains(Resource item) => _resources.Contains(item);
	public void CopyTo(Resource[] array, int arrayIndex) => _resources.CopyTo(array, arrayIndex);
	public IEnumerator<Resource> GetEnumerator() => _resources.GetEnumerator();
	public bool Remove(Resource item) => _resources.Remove(item);
	IEnumerator IEnumerable.GetEnumerator() => _resources.GetEnumerator();
}
