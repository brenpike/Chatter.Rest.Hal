using System.Collections.Generic;
using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;
using Chatter.Rest.Hal.Builders.Stages.Resource;

namespace Chatter.Rest.Hal.Builders;

public sealed class LinkObjectCollectionBuilder : HalBuilder<LinkObjectCollection>, ILinkCreationStage, ICuriesLinkCreationStage
{
	private readonly IList<LinkObjectBuilder> _linkObjectBuilders = new List<LinkObjectBuilder>();

	private LinkObjectCollectionBuilder(IBuildHalPart<Link>? parent) : base(parent) { }

	internal static LinkObjectCollectionBuilder New(IBuildHalPart<Link> parent) => new(parent);

	internal ILinkObjectPropertiesSelectionStage AddLinkObject(string href)
	{
		var lob = LinkObjectBuilder.WithHref(this, href);
		_linkObjectBuilders.Add(lob);
		return lob;
	}

	internal ILinkObjectPropertiesSelectionStage AddLinkObject(string href, string name)
	{
		var lob = LinkObjectBuilder.WithCuriesProperties(this, href, name);
		_linkObjectBuilders.Add(lob);
		return lob;
	}

	///<inheritdoc/>
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkCreationStage.AddLinkObject(string href) => AddLinkObject(href);
	///<inheritdoc/>
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkCreationStage.AddLinkObject(string href) => AddLinkObject(href);
	///<inheritdoc/>
	IResourceLinkObjectPropertiesSelectionStage IResourceCuriesLinkCreationStage.AddLinkObject(string href, string name) => AddLinkObject(href, name);
	///<inheritdoc/>
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedCuriesLinkCreationStage.AddLinkObject(string href, string name) => AddLinkObject(href, name);

	public override LinkObjectCollection BuildPart()
	{
		var linkObjectCollection = new LinkObjectCollection();
		foreach (var lob in _linkObjectBuilders)
		{
			linkObjectCollection.Add(lob.BuildPart());
		}
		return linkObjectCollection;
	}
}
