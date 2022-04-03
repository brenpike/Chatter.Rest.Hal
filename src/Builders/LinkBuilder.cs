using System.Collections.ObjectModel;

namespace Chatter.Rest.Hal.Builders;

public class LinkBuilder : IBuildLink
{
	public const string SelfLink = "self";
	public const string CuriesLink = "curies";

	private readonly string _rel;
	private readonly LinkObjectCollection _linkObjects = new();

	private LinkBuilder(string rel) => _rel = rel;

	public static IBuildLink WithRel(string rel) => new LinkBuilder(rel);
	public static IBuildLink Self() => new LinkBuilder(SelfLink);
	public static IBuildLink Curies() => new LinkBuilder(CuriesLink);

	public IBuildLink AddLinkObject(IBuildLinkObject linkObjectBuilder)
	{
		_linkObjects.Add(linkObjectBuilder.Build());
		return this;
	}

	Link IBuildLink.Build()
	{
		return new Link(_rel)
		{
			LinkObjects = _linkObjects
		};
	}
}
