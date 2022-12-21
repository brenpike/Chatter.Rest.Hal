using System.Linq;

namespace Chatter.Rest.Hal;

public static class LinkObjectCollectionExtensions
{
	public static LinkObject? GetLinkObjectOrDefault(this LinkObjectCollection linkObjects, string name)
		=> linkObjects?.SingleOrDefault(lo => lo.Name?.Equals(name) ?? false);
}
