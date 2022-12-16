using System.Collections.Generic;
using System.Linq;

namespace Chatter.Rest.Hal.Extensions;

public static class EmbeddedResourceCollectionExtensions
{
	public static IEnumerable<Resource> GetResourcesByName(this EmbeddedResourceCollection erc, string name) 
		=> erc.Where(er => er.Name.Equals(name)).SelectMany(r => r.Resources);
}
