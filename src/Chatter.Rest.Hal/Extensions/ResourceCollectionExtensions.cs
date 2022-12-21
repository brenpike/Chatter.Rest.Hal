using System.Collections.Generic;
using System.Linq;

namespace Chatter.Rest.Hal;

public static class ResourceCollectionExtensions
{
	public static IEnumerable<TResource?> As<TResource>(this ResourceCollection rc) where TResource : class
		=> rc.Select(r => r.As<TResource>());
}
