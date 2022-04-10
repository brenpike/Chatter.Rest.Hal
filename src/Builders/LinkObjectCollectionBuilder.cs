using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;
using Chatter.Rest.Hal.Builders.Stages.Resource;

namespace Chatter.Rest.Hal.Builders;

public class LinkObjectCollectionBuilder : HalBuilder<LinkObjectCollection>, ILinkCreationStage, ICuriesLinkCreationStage
{
	private readonly IList<LinkObjectBuilder> _linkObjectBuilders = new List<LinkObjectBuilder>();

	private LinkObjectCollectionBuilder(IBuildHalPart<Link>? parent) : base(parent) { }

	public static LinkObjectCollectionBuilder New(IBuildHalPart<Link> parent) => new(parent);

	public ILinkObjectPropertiesSelectionStage AddLinkObject(string href)
	{
		var lob = LinkObjectBuilder.WithHref(this, href);
		_linkObjectBuilders.Add(lob);
		return lob;
	}

	public ILinkObjectPropertiesSelectionStage AddLinkObject(string href, string name)
	{
		var lob = (LinkObjectBuilder)LinkObjectBuilder.WithHref(this, href).WithName(name).Templated();
		_linkObjectBuilders.Add(lob);
		return lob;
	}

	public override LinkObjectCollection BuildPart()
	{
		var linkObjectCollection = new LinkObjectCollection();
		foreach (var lob in _linkObjectBuilders)
		{
			linkObjectCollection.Add(lob.BuildPart());
		}
		return linkObjectCollection;
	}

	IResourceLinkObjectPropertiesSelectionStage IResourceLinkCreationStage.AddLinkObject(string href) => AddLinkObject(href);
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkCreationStage.AddLinkObject(string href) => AddLinkObject(href);
	IResourceLinkObjectPropertiesSelectionStage IResourceCuriesLinkCreationStage.AddLinkObject(string href, string name) => AddLinkObject(href, name);
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedCuriesLinkCreationStage.AddLinkObject(string href, string name) => AddLinkObject(href, name);
}
