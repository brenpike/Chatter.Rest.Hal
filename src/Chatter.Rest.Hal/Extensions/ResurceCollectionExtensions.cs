using System.Collections.Generic;
using System.Linq;

namespace Chatter.Rest.Hal;

public static class ResurceCollectionExtensions
{
	public static IEnumerable<TResource?> GetResource<TResource>(this ResourceCollection rc) where TResource : class
		=> rc.Select(r => r.State<TResource>());

	public static IEnumerable<TResource?> GetResource<TResource>(this IEnumerable<Resource> rc) where TResource : class
		=> rc.Select(r => r.State<TResource>());
}
