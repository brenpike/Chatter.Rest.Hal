using System;
using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;
using Chatter.Rest.Hal.Builders.Stages.Resource;

namespace Chatter.Rest.Hal.Builders;

public sealed class LinkObjectBuilder : HalBuilder<LinkObject>, ILinkObjectPropertiesSelectionStage
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
	private LinkObjectBuilder(IBuildHalPart<LinkObjectCollection> parent, string href, string name) : base(parent)
	{
		_href = href;
		_name = name;
		_templated = true;
	}

	internal static LinkObjectBuilder WithHref(IBuildHalPart<LinkObjectCollection> parent, string href) => new(parent, href);
	internal static LinkObjectBuilder WithCuriesProperties(IBuildHalPart<LinkObjectCollection> parent, string href, string name) => new(parent, href, name);

	private ILinkObjectPropertiesSelectionStage Templated()
	{
		_templated = true;
		return this;
	}

	private ILinkObjectPropertiesSelectionStage WithType(string type)
	{
		_type = type;
		return this;
	}

	private ILinkObjectPropertiesSelectionStage WithDeprecationUrl(string deprecation)
	{
		_deprecation = deprecation;
		return this;
	}

	private ILinkObjectPropertiesSelectionStage WithName(string name)
	{
		_name = name;
		return this;
	}

	private ILinkObjectPropertiesSelectionStage WithProfileUri(string profile)
	{
		_profile = profile;
		return this;
	}

	private ILinkObjectPropertiesSelectionStage WithTitle(string title)
	{
		_title = title;
		return this;
	}

	private ILinkObjectPropertiesSelectionStage WithHreflang(string hreflang)
	{
		_hreflang = hreflang;
		return this;
	}

	private ILinkCreationStage AddLink(string rel)
	{
		var linkCollectionBuilder = FindParent<LinkCollection>() as IAddLinkStage;
		return linkCollectionBuilder!.AddLink(rel);
	}

	private ILinkCreationStage AddSelf()
	{
		var linkCollectionBuilder = FindParent<LinkCollection>() as IAddSelfLinkStage;
		return linkCollectionBuilder!.AddSelf();
	}

	private ICuriesLinkCreationStage AddCuries()
	{
		var linkCollectionBuilder = FindParent<LinkCollection>() as IAddCuriesLinkStage;
		return linkCollectionBuilder!.AddCuries();
	}

	private ILinkObjectPropertiesSelectionStage AddLinkObject(string href)
		=> WithHref((IBuildHalPart<LinkObjectCollection>)Parent!, href);

	private ILinkObjectPropertiesSelectionStage AddLinkObject(string href, string name)
		=> WithHref((IBuildHalPart<LinkObjectCollection>)Parent!, href).WithName(name);

	///<inheritdoc/>
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkCreationStage.AddLinkObject(string href) => AddLinkObject(href);
	///<inheritdoc/>
	IResourceLinkObjectPropertiesSelectionStage IResourceCuriesLinkCreationStage.AddLinkObject(string href, string name) => AddLinkObject(href, name);
	///<inheritdoc/>
	IResourceLinkCreationStage IAddLinkToResourceStage.AddLink(string rel) => AddLink(rel);
	///<inheritdoc/>
	IResourceLinkCreationStage IAddSelfLinkToResourceStage.AddSelf() => AddSelf();
	///<inheritdoc/>
	IResourceCuriesLinkCreationStage IAddCuriesLinkToResourceStage.AddCuries() => AddCuries();
	///<inheritdoc/>
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkCreationStage.AddLinkObject(string href) => AddLinkObject(href);
	///<inheritdoc/>
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedCuriesLinkCreationStage.AddLinkObject(string href, string name) => AddLinkObject(href, name);
	///<inheritdoc/>
	IEmbeddedLinkCreationStage IAddLinkToEmbeddedStage.AddLink(string rel) => AddLink(rel);
	///<inheritdoc/>
	IEmbeddedLinkCreationStage IAddSelfLinkToEmbeddedStage.AddSelf() => AddSelf();
	///<inheritdoc/>
	IEmbeddedCuriesLinkCreationStage IAddCuriesLinkToEmbeddedStage.AddCuries() => AddCuries();

	///<inheritdoc/>
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkObjectPropertiesSelectionStage.Templated() => Templated();
	///<inheritdoc/>
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkObjectPropertiesSelectionStage.WithType(string type) => WithType(type);
	///<inheritdoc/>
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkObjectPropertiesSelectionStage.WithDeprecationUrl(string deprecation) => WithDeprecationUrl(deprecation);
	///<inheritdoc/>
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkObjectPropertiesSelectionStage.WithName(string name) => WithName(name);
	///<inheritdoc/>
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkObjectPropertiesSelectionStage.WithProfileUri(string profile) => WithProfileUri(profile);
	///<inheritdoc/>
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkObjectPropertiesSelectionStage.WithTitle(string title) => WithTitle(title);
	///<inheritdoc/>
	IResourceLinkObjectPropertiesSelectionStage IResourceLinkObjectPropertiesSelectionStage.WithHreflang(string hreflang) => WithHreflang(hreflang);
	///<inheritdoc/>
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkObjectPropertiesSelectionStage.Templated() => Templated();
	///<inheritdoc/>
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkObjectPropertiesSelectionStage.WithType(string type) => WithType(type);
	///<inheritdoc/>
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkObjectPropertiesSelectionStage.WithDeprecationUrl(string deprecation) => WithDeprecationUrl(deprecation);
	///<inheritdoc/>
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkObjectPropertiesSelectionStage.WithName(string name) => WithName(name);
	///<inheritdoc/>
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkObjectPropertiesSelectionStage.WithProfileUri(string profile) => WithProfileUri(profile);
	///<inheritdoc/>
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkObjectPropertiesSelectionStage.WithTitle(string title) => WithTitle(title);
	///<inheritdoc/>
	IEmbeddedLinkObjectPropertiesSelectionStage IEmbeddedLinkObjectPropertiesSelectionStage.WithHreflang(string hreflang) => WithHreflang(hreflang);

	///<inheritdoc/>
	IEmbeddedResourceCreationStage IAddResourceStage.AddResource()
	{
		var resource = FindParent<ResourceCollection>() as IAddResourceStage;
		return resource!.AddResource();
	}

	///<inheritdoc/>
	IEmbeddedResourceCreationStage IAddResourceStage.AddResource(object? state)
	{
		var resource = FindParent<ResourceCollection>() as IAddResourceStage;
		return resource!.AddResource(state);
	}

	///<inheritdoc/>
	IAddResourceStage IAddEmbeddedResourceToResourceStage.AddEmbedded(string name)
	{
		if (FindParent<EmbeddedResourceCollection>() is IAddEmbeddedResourceToResourceStage embedded)
		{
			return embedded.AddEmbedded(name);
		}

		if (FindParent<Resource>() is IAddEmbeddedResourceToResourceStage resource)
		{
			return resource.AddEmbedded(name);
		}

		throw new InvalidOperationException();
	}

	///<inheritdoc/>
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
}
