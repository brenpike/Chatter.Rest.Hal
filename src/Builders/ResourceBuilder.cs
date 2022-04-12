using Chatter.Rest.Hal.Builders.Stages;
using Chatter.Rest.Hal.Builders.Stages.Embedded;
using Chatter.Rest.Hal.Builders.Stages.Resource;

namespace Chatter.Rest.Hal.Builders;

public sealed class ResourceBuilder : HalBuilder<Resource>, IEmbeddedResourceCreationStage, IResourceCreationStage
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

	public static IResourceCreationStage New() => new ResourceBuilder(null, null);
	public static IResourceCreationStage WithState(object state) => new ResourceBuilder(null, state);
	internal static IEmbeddedResourceCreationStage Embedded() => new ResourceBuilder(null, null);
	internal static IEmbeddedResourceCreationStage Embedded(object state) => new ResourceBuilder(null, state);
	internal static IResourceCreationStage Resource(IBuildHalPart<ResourceCollection> parent, object? state) => new ResourceBuilder(parent, state);
	internal static IEmbeddedResourceCreationStage Embedded(IBuildHalPart<ResourceCollection> parent, object? state) => new ResourceBuilder(parent, state);

	private ILinkCreationStage AddLink(string rel) => _linkCollectionBuilder.AddLink(rel);
	private ILinkCreationStage AddSelf() => _linkCollectionBuilder.AddSelf();
	private ICuriesLinkCreationStage AddCuries() => _linkCollectionBuilder.AddCuries();

	public IAddResourceStage AddEmbedded(string name) => _embeddedCollectionBuilder.AddEmbedded(name);

	IEmbeddedLinkCreationStage IAddLinkToEmbeddedStage.AddLink(string rel) => AddLink(rel);
	IEmbeddedLinkCreationStage IAddSelfLinkToEmbeddedStage.AddSelf() => AddSelf();
	IEmbeddedCuriesLinkCreationStage IAddCuriesLinkToEmbeddedStage.AddCuries() => AddCuries();
	IResourceCuriesLinkCreationStage IAddCuriesLinkToResourceStage.AddCuries() => AddCuries();
	IResourceLinkCreationStage IAddLinkToResourceStage.AddLink(string rel) => AddLink(rel);
	IResourceLinkCreationStage IAddSelfLinkToResourceStage.AddSelf() => AddSelf();

	public override Resource BuildPart()
	{
		return new Resource(_state)
		{
			Links = _linkCollectionBuilder.BuildPart(),
			EmbeddedResources = _embeddedCollectionBuilder.BuildPart()
		};
	}
}
