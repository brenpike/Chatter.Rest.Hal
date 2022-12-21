using System.Linq;

namespace Chatter.Rest.Hal;

public static class LinkCollectionExtensions
{
	public static Link? GetLinkOrDefault(this LinkCollection links, string relation)
		=> links?.SingleOrDefault(l => l.Rel.Equals(relation));

	public static LinkObjectCollection? GetLinkObjects(this LinkCollection links, string relation)
		=> links?.GetLinkOrDefault(relation)?.LinkObjects;

	public static LinkObject? GetLinkObjectOrDefault(this LinkCollection links, string linkRelation, string linkObjectName)
		=> links?.GetLinkObjects(linkRelation)?.GetLinkObjectOrDefault(linkObjectName);

	public static LinkObject? GetLinkObjectOrDefault(this LinkCollection links, string linkRelation)
		=> links?.GetLinkObjects(linkRelation)?.SingleOrDefault();
}
