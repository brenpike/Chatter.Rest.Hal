using System.Collections.Generic;
using Chatter.Rest.Hal.Builders.Stages;

namespace Chatter.Rest.Hal.Builders;

/// <summary>
/// Builder for constructing link collections within a HAL resource.
/// </summary>
public sealed class LinkCollectionBuilder : HalBuilder<LinkCollection>, IAddLinkStage, IAddSelfLinkStage, IAddCuriesLinkStage
{
	private LinkCollectionBuilder(IBuildHalPart<Resource> parent) : base(parent) { }
	internal static LinkCollectionBuilder New(IBuildHalPart<Resource> parent) => new(parent);

	private readonly IList<IBuildHalPart<Link>> _linkBuilders = new List<IBuildHalPart<Link>>();

	/// <summary>
	/// Adds a link with the specified relation to the collection.
	/// </summary>
	/// <param name="rel">The link relation.</param>
	/// <returns>A link creation stage.</returns>
	public ILinkCreationStage AddLink(string rel)
	{
		var link = LinkBuilder.WithRel(this, rel);
		_linkBuilders.Add(link);
		return link;
	}

	/// <summary>
	/// Adds a "self" link to the collection.
	/// </summary>
	/// <returns>A link creation stage.</returns>
	public ILinkCreationStage AddSelf()
	{
		var link = LinkBuilder.Self(this);
		_linkBuilders.Add(link);
		return link;
	}

	/// <summary>
	/// Adds a "curies" link to the collection for defining compact URI relations.
	/// </summary>
	/// <returns>A curies link creation stage.</returns>
	public ICuriesLinkCreationStage AddCuries()
	{
		var link = LinkBuilder.Curies(this);
		_linkBuilders.Add(link);
		return link;
	}

	/// <summary>
	/// Builds the link collection from all added links.
	/// </summary>
	/// <returns>The constructed link collection.</returns>
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
