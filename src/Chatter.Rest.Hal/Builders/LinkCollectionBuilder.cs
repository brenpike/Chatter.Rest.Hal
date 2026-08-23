using System;
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

	private readonly IList<LinkBuilder> _linkBuilders = new List<LinkBuilder>();
	private readonly IDictionary<string, LinkBuilder> _linkBuildersByRel = new Dictionary<string, LinkBuilder>(StringComparer.Ordinal);

	/// <summary>
	/// Adds a link with the specified relation to the collection.
	/// </summary>
	/// <param name="rel">The link relation.</param>
	/// <returns>A link creation stage.</returns>
	/// <remarks>
	/// Repeating a relation returns the builder already registered for it, so its link objects
	/// merge into the single link for that relation. HAL's "_links" is a JSON object keyed by
	/// relation, so two links sharing a relation could never serialize into spec-valid output.
	/// </remarks>
	public ILinkCreationStage AddLink(string rel) => GetOrAddLink(rel, r => LinkBuilder.WithRel(this, r));

	/// <summary>
	/// Adds a "self" link to the collection.
	/// </summary>
	/// <returns>A link creation stage.</returns>
	/// <remarks>Repeated calls merge into the single "self" link, as described on <see cref="AddLink"/>.</remarks>
	public ILinkCreationStage AddSelf() => GetOrAddLink(LinkBuilder.SelfLink, _ => LinkBuilder.Self(this));

	/// <summary>
	/// Adds a "curies" link to the collection for defining compact URI relations.
	/// </summary>
	/// <returns>A curies link creation stage.</returns>
	/// <remarks>
	/// Repeated calls merge into the single "curies" link, so each additional CURIE definition
	/// extends that link's array rather than emitting a second "curies" entry.
	/// </remarks>
	public ICuriesLinkCreationStage AddCuries() => GetOrAddLink(LinkBuilder.CuriesLink, _ => LinkBuilder.Curies(this));

	private LinkBuilder GetOrAddLink(string rel, Func<string, LinkBuilder> create)
	{
		if (_linkBuildersByRel.TryGetValue(rel, out var existing))
		{
			return existing;
		}

		var link = create(rel);
		_linkBuilders.Add(link);
		_linkBuildersByRel.Add(rel, link);
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
