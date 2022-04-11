using System.Collections;
using System.Collections.ObjectModel;

namespace Chatter.Rest.Hal;

[JsonConverter(typeof(LinkCollectionConverter))]
public sealed record LinkCollection : ICollection<Link>, IHalPart
{
	private readonly Collection<Link> _links = new();

	public int Count => _links.Count;
	public bool IsReadOnly => false;
	public void Add(Link item) => _links.Add(item);
	public void Clear() => _links.Clear();
	public bool Contains(Link item) => _links.Contains(item);
	public void CopyTo(Link[] array, int arrayIndex) => _links.CopyTo(array, arrayIndex);
	public IEnumerator<Link> GetEnumerator() => _links.GetEnumerator();
	public bool Remove(Link item) => _links.Remove(item);
	IEnumerator IEnumerable.GetEnumerator() => _links.GetEnumerator();
}
