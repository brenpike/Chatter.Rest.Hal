using System.Collections.Generic;
using System.Linq;

namespace Chatter.Rest.Hal;

public static class EmbeddedResourceCollectionExtensions
{
	public static EmbeddedResource? GetEmbeddedResource(this EmbeddedResourceCollection erc, string name)
		=> erc?.SingleOrDefault(er => er.Name.Equals(name));

	public static ResourceCollection? GetResourceCollection(this EmbeddedResourceCollection erc, string name)
		=> erc?.GetEmbeddedResource(name)?.Resources;

	public static IEnumerable<T?>? GetResources<T>(this EmbeddedResourceCollection erc, string name) where T : class
		=> erc?.GetResourceCollection(name)?.As<T>();
}
