using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;
using Chatter.Rest.Hal.Builders.Stages.Resource;

namespace Chatter.Rest.Hal.Builders;

public class LinkBuilder : HalBuilder<Link>, ILinkCreationStage, ICuriesLinkCreationStage
{
	public const string SelfLink = "self";
	public const string CuriesLink = "curies";

	private readonly string _rel;
	private readonly LinkObjectCollectionBuilder _linkObjects;

	private LinkBuilder(IBuildHalPart<LinkCollection> parent, string rel) : base(parent)
	{
		_rel = rel;
		_linkObjects = LinkObjectCollectionBuilder.New(this);
	}

	public static LinkBuilder WithRel(IBuildHalPart<LinkCollection> parent, string rel) => new(parent, rel);
	public static LinkBuilder Self(IBuildHalPart<LinkCollection> parent) => new(parent, SelfLink);
	public static LinkBuilder Curies(IBuildHalPart<LinkCollection> parent) => new(parent, CuriesLink);

	public ILinkObjectPropertiesSelectionStage AddLinkObject(string href) => _linkObjects.AddLinkObject(href);
	public ILinkObjectPropertiesSelectionStage AddLinkObject(string href, string name) => _linkObjects.AddLinkObject(href, name);

	public override Link BuildPart()
	{
		return new Link(_rel)
		{
			LinkObjects = _linkObjects.BuildPart()
		};
	}

	IResourceLinkObjectPropertiesSelectionStage IResourceCuriesLinkCreationStage.AddLinkObject(string href, string name) => AddLinkObject(href, name);
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedCuriesLinkCreationStage.AddLinkObject(string href, string name) => AddLinkObject(href, name);
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkCreationStage.AddLinkObject(string href) => AddLinkObject(href);
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkCreationStage.AddLinkObject(string href) => AddLinkObject(href);
}
