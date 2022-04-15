using Chatter.Rest.Hal.Builders.Stages;

namespace Chatter.Rest.Hal.Builders;

public sealed class LinkCollectionBuilder : HalBuilder<LinkCollection>, IAddLinkStage, IAddSelfLinkStage, IAddCuriesLinkStage
{
	private LinkCollectionBuilder(IBuildHalPart<Resource> parent) : base(parent) { }
	internal static LinkCollectionBuilder New(IBuildHalPart<Resource> parent) => new(parent);

	private readonly IList<IBuildHalPart<Link>> _linkBuilders = new List<IBuildHalPart<Link>>();

	public ILinkCreationStage AddLink(string rel)
	{
		var link = LinkBuilder.WithRel(this, rel);
		_linkBuilders.Add(link);
		return link;
	}

	public ILinkCreationStage AddSelf()
	{
		var link = LinkBuilder.Self(this);
		_linkBuilders.Add(link);
		return link;
	}

	public ICuriesLinkCreationStage AddCuries()
	{
		var link = LinkBuilder.Curies(this);
		_linkBuilders.Add(link);
		return link;
	}

	public override LinkCollection BuildPart()
	{
		var lc = new LinkCollection();
		foreach (var link in _linkBuilders)
		{
			lc.Add(link.BuildPart());
		}
		return lc;
	}
}
