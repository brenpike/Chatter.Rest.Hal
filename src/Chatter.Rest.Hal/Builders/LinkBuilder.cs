using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;
using Chatter.Rest.Hal.Builders.Stages.Resource;

namespace Chatter.Rest.Hal.Builders;

/// <summary>
/// Builder for constructing individual links within a link collection.
/// </summary>
public sealed class LinkBuilder : HalBuilder<Link>, ILinkCreationStage, ICuriesLinkCreationStage
{
	/// <summary>
	/// The reserved link relation for "self" links.
	/// </summary>
	public const string SelfLink = "self";

	/// <summary>
	/// The reserved link relation for "curies" links.
	/// </summary>
	public const string CuriesLink = "curies";

	private readonly string _rel;
	private readonly LinkObjectCollectionBuilder _linkObjects;
	private bool _isArray = false;

	private LinkBuilder(IBuildHalPart<LinkCollection> parent, string rel) : base(parent)
	{
		_rel = rel;
		_linkObjects = LinkObjectCollectionBuilder.New(this);
	}

	/// <summary>
	/// Creates a new link builder with the specified relation.
	/// </summary>
	/// <param name="parent">The parent link collection builder.</param>
	/// <param name="rel">The link relation.</param>
	/// <returns>A new link builder.</returns>
	public static LinkBuilder WithRel(IBuildHalPart<LinkCollection> parent, string rel) => new(parent, rel);

	/// <summary>
	/// Creates a new link builder for a "self" link.
	/// </summary>
	/// <param name="parent">The parent link collection builder.</param>
	/// <returns>A new link builder.</returns>
	public static LinkBuilder Self(IBuildHalPart<LinkCollection> parent) => new(parent, SelfLink);

	/// <summary>
	/// Creates a new link builder for a "curies" link.
	/// </summary>
	/// <param name="parent">The parent link collection builder.</param>
	/// <returns>A new link builder.</returns>
	public static LinkBuilder Curies(IBuildHalPart<LinkCollection> parent) => new(parent, CuriesLink);

	internal void SetIsArray() => _isArray = true;

	///<inheritdoc/>
	private ILinkObjectPropertiesSelectionStage AddLinkObject(string href) => _linkObjects.AddLinkObject(href);
	///<inheritdoc/>
	private ILinkObjectPropertiesSelectionStage AddLinkObject(string href, string name) => _linkObjects.AddLinkObject(href, name);

	private ILinkCreationStage AsArray()
	{
		_isArray = true;
		return this;
	}

	///<inheritdoc/>
	IResourceLinkObjectPropertiesSelectionStage IResourceCuriesLinkCreationStage.AddLinkObject(string href, string name) => AddLinkObject(href, name);
	///<inheritdoc/>
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedCuriesLinkCreationStage.AddLinkObject(string href, string name) => AddLinkObject(href, name);
	///<inheritdoc/>
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkCreationStage.AddLinkObject(string href) => AddLinkObject(href);
	///<inheritdoc/>
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkCreationStage.AddLinkObject(string href) => AddLinkObject(href);
	///<inheritdoc/>
	IResourceLinkCreationStage IResourceLinkCreationStage.AsArray() => (IResourceLinkCreationStage)AsArray();
	///<inheritdoc/>
	IEmbeddedLinkCreationStage IEmbeddedLinkCreationStage.AsArray() => (IEmbeddedLinkCreationStage)AsArray();
	///<inheritdoc/>
	IResourceCuriesLinkCreationStage IResourceCuriesLinkCreationStage.AsArray() => (IResourceCuriesLinkCreationStage)AsArray();
	///<inheritdoc/>
	IEmbeddedCuriesLinkCreationStage IEmbeddedCuriesLinkCreationStage.AsArray() => (IEmbeddedCuriesLinkCreationStage)AsArray();

	/// <summary>
	/// Builds the Link with its relation and link objects.
	/// </summary>
	/// <returns>The constructed Link.</returns>
	public override Link BuildPart()
	{
		return new Link(_rel)
		{
			LinkObjects = _linkObjects.BuildPart(),
			IsArray = _isArray
		};
	}
}
