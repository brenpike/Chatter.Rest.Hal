using Chatter.Rest.Hal.Builders.Stages;

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

	public IBuildResource AddResource()
	{
		var resource = FindParent<ResourceCollection>() as IAddResourceStage;
		return resource!.AddResource();
	}

	public IBuildResource AddResource(object? state)
	{
		var resource = FindParent<ResourceCollection>() as IAddResourceStage;
		return resource!.AddResource(state);
	}

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
