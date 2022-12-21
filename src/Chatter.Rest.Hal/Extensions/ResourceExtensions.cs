using System.Collections.Generic;

namespace Chatter.Rest.Hal;

public static class ResourceExtensions
{
	public static IEnumerable<T?>? GetEmbeddedResources<T>(this Resource resource, string name) where T : class
		=> resource?.Embedded?.GetResources<T>(name);

	public static ResourceCollection? GetResourceCollection(this Resource resource, string name)
		=> resource?.Embedded?.GetResourceCollection(name);

	public static Link? GetLinkOrDefault(this Resource resource, string relation)
		=> resource?.Links?.GetLinkOrDefault(relation);

	public static LinkObjectCollection? GetLinkObjects(this Resource resource, string relation)
		=> resource?.Links?.GetLinkObjects(relation);

	public static LinkObject? GetLinkObjectOrDefault(this Resource resource, string linkRelation, string linkObjectName)
		=> resource?.Links?.GetLinkObjectOrDefault(linkRelation, linkObjectName);

	public static LinkObject? GetLinkObjectOrDefault(this Resource resource, string linkRelation)
		=> resource?.Links?.GetLinkObjectOrDefault(linkRelation);
}
