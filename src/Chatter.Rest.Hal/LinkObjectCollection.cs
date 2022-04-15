using System.Collections;
using System.Collections.ObjectModel;

namespace Chatter.Rest.Hal;

[JsonConverter(typeof(LinkObjectCollectionConverter))]
public sealed record LinkObjectCollection : ICollection<LinkObject>,  IHalPart
{
	private readonly Collection<LinkObject> _linkObjects = new();

	public int Count => _linkObjects.Count;
	public bool IsReadOnly => false;
	public void Add(LinkObject item) => _linkObjects.Add(item);
	public void Clear() => _linkObjects.Clear();
	public bool Contains(LinkObject item) => _linkObjects.Contains(item);
	public void CopyTo(LinkObject[] array, int arrayIndex) => _linkObjects.CopyTo(array, arrayIndex);
	public IEnumerator<LinkObject> GetEnumerator() => _linkObjects.GetEnumerator();
	public bool Remove(LinkObject item) => _linkObjects.Remove(item);
	IEnumerator IEnumerable.GetEnumerator() => _linkObjects.GetEnumerator();
}
