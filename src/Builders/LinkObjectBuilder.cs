using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;
using Chatter.Rest.Hal.Builders.Stages.Resource;

namespace Chatter.Rest.Hal.Builders;

public class LinkObjectBuilder : HalBuilder<LinkObject>, ILinkObjectPropertiesSelectionStage
{
	private readonly string _href;
	private bool? _templated;
	private string? _type;
	private string? _deprecation;
	private string? _name;
	private string? _profile;
	private string? _title;
	private string? _hreflang;

	private LinkObjectBuilder(IBuildHalPart<LinkObjectCollection> parent, string href) : base(parent) => _href = href;

	/// <inheritdoc/>
	internal static LinkObjectBuilder WithHref(IBuildHalPart<LinkObjectCollection> parent, string href) => new(parent, href);

	/// <inheritdoc/>
	public ILinkObjectPropertiesSelectionStage Templated()
	{
		_templated = true;
		return this;
	}

	/// <inheritdoc/>
	public ILinkObjectPropertiesSelectionStage WithType(string type)
	{
		_type = type;
		return this;
	}

	/// <inheritdoc/>
	public ILinkObjectPropertiesSelectionStage WithDeprecationUrl(string deprecation)
	{
		_deprecation = deprecation;
		return this;
	}

	/// <inheritdoc/>
	public ILinkObjectPropertiesSelectionStage WithName(string name)
	{
		_name = name;
		return this;
	}

	/// <inheritdoc/>
	public ILinkObjectPropertiesSelectionStage WithProfileUri(string profile)
	{
		_profile = profile;
		return this;
	}

	/// <inheritdoc/>
	public ILinkObjectPropertiesSelectionStage WithTitle(string title)
	{
		_title = title;
		return this;
	}

	/// <inheritdoc/>
	public ILinkObjectPropertiesSelectionStage WithHreflang(string hreflang)
	{
		_hreflang = hreflang;
		return this;
	}

	/// <inheritdoc/>
	public IAddResourceStage AddEmbedded(string name)
	{
		var embedded = FindParent<Resource>() as IAddEmbeddedResourceToResourceStage;
		return embedded!.AddEmbedded(name);
	}

	/// <inheritdoc/>
	public ILinkCreationStage AddLink(string rel)
	{
		var linkCollectionBuilder = FindParent<LinkCollection>() as IAddLinkStage;
		return linkCollectionBuilder!.AddLink(rel);
	}

	/// <inheritdoc/>
	public ILinkCreationStage AddSelf()
	{
		var linkCollectionBuilder = FindParent<LinkCollection>() as IAddSelfLinkStage;
		return linkCollectionBuilder!.AddSelf();
	}

	/// <inheritdoc/>
	public ICuriesLinkCreationStage AddCuries()
	{
		var linkCollectionBuilder = FindParent<LinkCollection>() as IAddCuriesLinkStage;
		return linkCollectionBuilder!.AddCuries();
	}

	public ILinkObjectPropertiesSelectionStage AddLinkObject(string href)
		=> WithHref((IBuildHalPart<LinkObjectCollection>)Parent!, href);

	public ILinkObjectPropertiesSelectionStage AddLinkObject(string href, string name)
		=> WithHref((IBuildHalPart<LinkObjectCollection>)Parent!, href).WithName(name);

	public override LinkObject BuildPart()
	{
		return new LinkObject(_href)
		{
			Templated = _templated,
			Deprecation = _deprecation,
			Type = _type,
			Name = _name,
			Profile = _profile,
			Title = _title,
			Hreflang = _hreflang
		};
	}

	IResourceLinkObjectPropertiesSelectionStage IResourceLinkCreationStage.AddLinkObject(string href) => AddLinkObject(href);
	IResourceLinkObjectPropertiesSelectionStage IResourceCuriesLinkCreationStage.AddLinkObject(string href, string name) => AddLinkObject(href, name);
	IResourceLinkCreationStage IAddLinkToResourceStage.AddLink(string rel) => AddLink(rel);
	IResourceLinkCreationStage IAddSelfLinkToResourceStage.AddSelf() => AddSelf();
	IResourceCuriesLinkCreationStage IAddCuriesLinkToResourceStage.AddCuries() => AddCuries();
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkCreationStage.AddLinkObject(string href) => AddLinkObject(href);
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedCuriesLinkCreationStage.AddLinkObject(string href, string name) => AddLinkObject(href, name);
	IEmbeddedLinkCreationStage IAddLinkToEmbeddedStage.AddLink(string rel) => AddLink(rel);
	IEmbeddedLinkCreationStage IAddSelfLinkToEmbeddedStage.AddSelf() => AddSelf();
	IEmbeddedCuriesLinkCreationStage IAddCuriesLinkToEmbeddedStage.AddCuries() => AddCuries();

	IEmbeddedResource IAddResourceStage.AddResource()
	{
		var resource = FindParent<ResourceCollection>() as IAddResourceStage;
		return resource!.AddResource();
	}

	IEmbeddedResource IAddResourceStage.AddResource(object? state)
	{
		var resource = FindParent<ResourceCollection>() as IAddResourceStage;
		return resource!.AddResource(state);
	}

	IResourceLinkObjectPropertiesSelectionStage IResourceLinkObjectPropertiesSelectionStage.Templated() => Templated();
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkObjectPropertiesSelectionStage.WithType(string type) => WithType(type);
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkObjectPropertiesSelectionStage.WithDeprecationUrl(string deprecation) => WithDeprecationUrl(deprecation);
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkObjectPropertiesSelectionStage.WithName(string name) => WithName(name);
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkObjectPropertiesSelectionStage.WithProfileUri(string profile) => WithProfileUri(profile);
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkObjectPropertiesSelectionStage.WithTitle(string title) => WithTitle(title);
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkObjectPropertiesSelectionStage.WithHreflang(string hreflang) => WithHreflang(hreflang);
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkObjectPropertiesSelectionStage.Templated() => Templated();
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkObjectPropertiesSelectionStage.WithType(string type) => WithType(type);
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkObjectPropertiesSelectionStage.WithDeprecationUrl(string deprecation) => WithDeprecationUrl(deprecation);
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkObjectPropertiesSelectionStage.WithName(string name) => WithName(name);
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkObjectPropertiesSelectionStage.WithProfileUri(string profile) => WithProfileUri(profile);
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkObjectPropertiesSelectionStage.WithTitle(string title) => WithTitle(title);
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkObjectPropertiesSelectionStage.WithHreflang(string hreflang) => WithHreflang(hreflang);
}
