using Chatter.Rest.Hal.Builders.Stages;

namespace Chatter.Rest.Hal.Builders;

public class ResourceBuilder : HalBuilder<Resource>, IAddLinkStage, IAddSelfLinkStage, IAddCuriesLinkStage, IAddEmbeddedResourceToResourceStage, IBuildResource
{
	private readonly object? _state;
	private readonly LinkCollectionBuilder _linkCollectionBuilder;
	private readonly EmbeddedResourceCollectionBuilder _embeddedCollectionBuilder;

	private ResourceBuilder(IBuildHalPart<ResourceCollection>? parent, object? state) : base(parent)
	{
		_state = state;
		_linkCollectionBuilder = LinkCollectionBuilder.New(this);
		_embeddedCollectionBuilder = EmbeddedResourceCollectionBuilder.New(this);
	}

	public static ResourceBuilder New() => new(null, null);
	public static ResourceBuilder New(object state) => new(null, state);
	internal static ResourceBuilder New(IBuildHalPart<ResourceCollection> parent, object? state) => new(parent, state);

	public ILinkCreationStage AddLink(string rel) => _linkCollectionBuilder.AddLink(rel);
	public ILinkCreationStage AddSelf() => _linkCollectionBuilder.AddSelf();
	public ICuriesLinkCreationStage AddCuries() => _linkCollectionBuilder.AddCuries();
	public IAddResourceToEmbeddedResourceStage AddEmbedded(string name) => _embeddedCollectionBuilder.AddEmbedded(name);

	public override Resource BuildPart()
	{
		return new Resource(_state)
		{
			Links = _linkCollectionBuilder.BuildPart(),
			EmbeddedResources = _embeddedCollectionBuilder.BuildPart()
		};
	}
}
